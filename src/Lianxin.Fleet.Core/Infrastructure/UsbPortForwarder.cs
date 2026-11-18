using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Lianxin.Fleet.Core.Infrastructure;

// 进程内本机端口转发，替代每台手机一个 Python usbmux forward 进程；只监听回环地址。
public sealed class UsbPortForwarder : IAsyncDisposable
{
    private readonly TcpListener listener;
    private readonly Func<CancellationToken, Task<Stream>> connect;
    private readonly CancellationTokenSource lifetime = new();
    private readonly ConcurrentDictionary<Task, bool> pipes = new();
    private readonly Task accepting;

    public UsbPortForwarder(int localPort, Func<CancellationToken, Task<Stream>> connect)
    {
        this.connect = connect;
        listener = new TcpListener(IPAddress.Loopback, localPort);
        listener.Start();
        LocalPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        accepting = AcceptAsync();
    }

    public int LocalPort
    {
        get;
    }

    private async Task AcceptAsync()
    {
        try
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync(lifetime.Token).ConfigureAwait(false);
                var pipe = PipeAsync(client);
                pipes[pipe] = true;
                _ = pipe.ContinueWith(done => pipes.TryRemove(done, out _), CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }
        catch (Exception error) when (error is OperationCanceledException or ObjectDisposedException or SocketException) { }
    }

    private async Task PipeAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                await using var device = await connect(lifetime.Token).ConfigureAwait(false);
                var local = client.GetStream();
                using var done = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                var upstream = local.CopyToAsync(device, done.Token);
                var downstream = device.CopyToAsync(local, done.Token);
                await Task.WhenAny(upstream, downstream).ConfigureAwait(false);
                done.Cancel();
                try
                {
                    await Task.WhenAll(upstream, downstream).ConfigureAwait(false);
                }
                catch (Exception error) when (error is OperationCanceledException or IOException or ObjectDisposedException) { }
            }
            // 连接失败时直接断开本机连接，WDA 请求方会按"无法确认响应"处理。
            catch (Exception error) when (error is IOException or SocketException or OperationCanceledException
                or InvalidDataException or System.Xml.XmlException or ObjectDisposedException)
            {
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (lifetime.IsCancellationRequested)
            return;
        lifetime.Cancel();
        listener.Stop();
        await accepting.ConfigureAwait(false);
        await Task.WhenAll(pipes.Keys).ConfigureAwait(false);
        lifetime.Dispose();
    }
}
