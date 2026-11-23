using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Lianxin.Fleet.Core.OpenApi;

// 兼容 EasyClick「企业版开放接口」的 HTTP/1.1 服务。只用 TcpListener，不依赖 ASP.NET：
// 每个连接只处理一个请求（Connection: close），正文必须带 Content-Length，所有上限见 OpenApiOptions。
public sealed partial class OpenApiServer : IAsyncDisposable
{
    private readonly OpenApiBackend backend;
    private readonly OpenApiOptions options;
    private readonly OpenApiRouter router;
    private readonly IReadOnlyCollection<string> machineNames;
    private readonly CancellationTokenSource lifetime = new();
    private readonly SemaphoreSlim slots;
    private readonly ConcurrentDictionary<Task, bool> active = new();
    private readonly Dictionary<IPAddress, int> perAddress = [];
    private readonly object sync = new();
    private int rejecting;
    private long bodyBytes;
    private TcpListener? listener;
    private Task? acceptLoop;
    private Task? stopping;
    private long requests;

    private OpenApiServer(OpenApiBackend backend, OpenApiOptions options, OpenApiRouter router)
    {
        this.backend = backend;
        this.options = options;
        this.router = router;
        machineNames = options.AllowLan ? OpenApiSecurity.MachineNames() : [];
        slots = new SemaphoreSlim(options.MaxConnections, options.MaxConnections);
    }

    public int Port
    {
        get; private set;
    }
    public bool AllowLan => options.AllowLan;
    public bool IsRunning
    {
        get
        {
            lock (sync)
                return listener is not null && stopping is null;
        }
    }
    public long RequestCount => Interlocked.Read(ref requests);
    public string Address => (options.AllowLan ? "0.0.0.0" : "127.0.0.1") + ":" + Port;
    // 处理函数的意外错误（返回 500 的情况），可能来自任意线程。
    public event Action<string>? Log;

    // 端口已被其他程序（例如 EasyClick 中控本身也默认用 8019）占用时抛出 OpenApiPortBusyException。
    public static async Task<OpenApiServer> StartAsync(OpenApiBackend backend, OpenApiOptions options,
        OpenApiRouter? router = null, CancellationToken cancellationToken = default)
    {
        options.Validate();
        if (options.Port != 0 && await PortAnswersAsync(options.Port, cancellationToken).ConfigureAwait(false))
            throw new OpenApiPortBusyException(options.Port);
        var server = new OpenApiServer(backend, options, router ?? OpenApiRouter.CreateDefault());
        server.Listen();
        return server;
    }

    private void Listen()
    {
        var created = new TcpListener(options.ListenAddress ?? (options.AllowLan ? IPAddress.Any : IPAddress.Loopback), options.Port);
        // Windows 默认允许别的程序以更具体的地址抢占同一端口，独占绑定可避免请求被截走。
        if (OperatingSystem.IsWindows())
            created.ExclusiveAddressUse = true;
        try
        {
            created.Start(64);
        }
        catch (SocketException error) when (error.SocketErrorCode is SocketError.AddressAlreadyInUse or SocketError.AccessDenied)
        {
            created.Dispose();
            throw new OpenApiPortBusyException(options.Port, error);
        }
        Port = ((IPEndPoint)created.LocalEndpoint).Port;
        lock (sync)
            listener = created;
        acceptLoop = AcceptAsync(created);
    }

    // 本机已有程序在该端口应答，说明端口被占用。
    private static async Task<bool> PortAnswersAsync(int port, CancellationToken cancellationToken)
    {
        using var probe = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(400));
        try
        {
            await probe.ConnectAsync(IPAddress.Loopback, port, timeout.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return false; }
        catch (SocketException) { return false; }
    }

    private async Task AcceptAsync(TcpListener source)
    {
        while (!lifetime.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await source.AcceptTcpClientAsync(lifetime.Token).ConfigureAwait(false);
            }
            catch (Exception error) when (error is OperationCanceledException or ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                if (lifetime.IsCancellationRequested)
                    break;
                // 单个连接在握手阶段出错（例如对方立即重置）；稍等再接受下一个，避免异常时空转。
                await Task.Delay(20).ConfigureAwait(false);
                continue;
            }
            var address = RemoteAddress(client);
            Task work;
            if (!TryEnter(address))
                work = RejectAsync(client, "同一地址的开放接口连接数已满，请稍后重试。");
            else if (!slots.Wait(0))
            {
                Leave(address);
                work = RejectAsync(client, "开放接口连接数已满，请稍后重试。");
            }
            else
                work = ServeAsync(client, address);
            active.TryAdd(work, true);
            _ = work.ContinueWith(done => active.TryRemove(done, out _), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }

    // 连接数已满：同时最多 MaxPendingRejects 个 503 回复（写完即关，不读对方的数据），再多的连接直接关闭。
    private async Task RejectAsync(TcpClient client, string message)
    {
        if (Interlocked.Increment(ref rejecting) > options.MaxPendingRejects)
        {
            Interlocked.Decrement(ref rejecting);
            client.Dispose();
            return;
        }
        try
        {
            using (client)
                await ReplyAndCloseAsync(client, OpenApiSecurity.Reject(503, message)).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or SocketException or ObjectDisposedException or OperationCanceledException) { }
        finally { Interlocked.Decrement(ref rejecting); }
    }

    private async Task ServeAsync(TcpClient client, IPAddress? address)
    {
        try
        {
            using (client)
            {
                client.NoDelay = true;
                var reply = await ProcessAsync(client.GetStream()).ConfigureAwait(false);
                if (reply is not null)
                    await ReplyAndCloseAsync(client, reply).ConfigureAwait(false);
            }
        }
        catch (Exception error) when (error is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            // 对方提前断开或写超时：无法再回复，连接直接关闭。
        }
        catch (Exception error)
        {
            Report("开放接口连接处理异常：" + error.Message);
        }
        finally
        {
            slots.Release();
            Leave(address);
        }
    }

    private async Task ReplyAndCloseAsync(TcpClient client, OpenApiReply reply)
    {
        var stream = client.GetStream();
        using (var timeout = new CancellationTokenSource(options.WriteTimeout))
            await reply.WriteAsync(stream, timeout.Token).ConfigureAwait(false);
        // 先关闭发送方向，再读掉对方未发完的数据，避免直接关闭触发 RST 让对方读不到错误响应。
        // 503、408 只读已经到达的（最多 100 毫秒，对方占着连接或发得太慢）；413 时对方可能还在发大正文，最多读 2 秒 / 8 MB；
        // 其余最多 250 毫秒 / 64 KB。读掉已到达的数据才能正常关闭（否则系统会发 RST，对方可能读不到回复）。
        var (limit, wait) = reply.Status switch
        {
            408 or 503 => (64L * 1024, TimeSpan.FromMilliseconds(100)),
            413 => (8L * 1024 * 1024, TimeSpan.FromSeconds(2)),
            _ => (64L * 1024, TimeSpan.FromMilliseconds(250))
        };
        try
        {
            client.Client.Shutdown(SocketShutdown.Send);
            if (limit == 0)
                return;
            using var drain = new CancellationTokenSource(wait);
            var sink = new byte[16 * 1024];
            long drained = 0;
            int count;
            while (drained < limit && (count = await stream.ReadAsync(sink, drain.Token).ConfigureAwait(false)) > 0)
                drained += count;
        }
        catch (Exception error) when (error is IOException or SocketException or OperationCanceledException or ObjectDisposedException) { }
    }

    // 停止流程放到线程池执行：取消回调可能同步运行请求的后续代码，不能在持有锁时触发。
    public Task StopAsync()
    {
        lock (sync)
            return stopping ??= Task.Run(StopCoreAsync);
    }

    private async Task StopCoreAsync()
    {
        lifetime.Cancel();
        lock (sync)
            listener?.Stop();
        if (acceptLoop is not null)
            await acceptLoop.ConfigureAwait(false);
        try
        {
            // 正在执行的设备操作收到取消后返回 503；超过 10 秒仍未结束的连接不再等待。
            await Task.WhenAll(active.Keys).WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        }
        catch (Exception error) { Report("开放接口停止时有连接未正常结束：" + error.Message); }
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private void Report(string message)
    {
        try
        {
            Log?.Invoke(message);
        }
        catch { /* 界面日志回调不能影响请求处理。 */ }
    }
}

public sealed class OpenApiPortBusyException(int port, Exception? inner = null)
    : Exception($"端口 {port} 已被其他程序占用（EasyClick 中控默认也使用 8019），请在设置里换一个端口或先关闭占用它的程序。", inner)
{
    public int Port { get; } = port;
}
