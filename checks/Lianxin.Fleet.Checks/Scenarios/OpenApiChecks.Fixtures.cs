using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;
using Lianxin.Fleet.Core.OpenApi;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;

namespace Lianxin.Fleet.Checks.Scenarios;

// 开放接口检查用的假设备、运行时组合和原始 HTTP 客户端。全部走回环地址上的临时端口，不接触真实手机。
internal static partial class OpenApiChecks
{
    internal const string Udid = "00008030-001234567890A001";

    // 请求体：键值对写成一行，避免匿名对象被格式化成多行。
    internal static Dictionary<string, object?> J(params (string Key, object? Value)[] fields) =>
        fields.ToDictionary(field => field.Key, field => field.Value);

    // 只写文件头和 IHDR，足够让服务端读出截图尺寸。
    internal static byte[] Png(int width, int height)
    {
        var png = new byte[33];
        new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 13 }.CopyTo(png, 0);
        "IHDR"u8.CopyTo(png.AsSpan(12));
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(16), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(20), height);
        return png;
    }

    internal sealed class ApiBridge : IDeviceBridge
    {
        public ConcurrentQueue<(FleetAction Action, ActionParameters Parameters)> Calls { get; } = new();
        public DeviceStatus Status { get; set; } = new(true, 390, 844, "Phone");
        public byte[] Screenshot { get; set; } = Png(1170, 2532);
        public byte[] UsbScreenshot { get; set; } = Png(750, 1334);
        public int ScreenshotCount;
        public Func<FleetAction, CancellationToken, Task>? Execute
        {
            get; set;
        }

        public Task<IReadOnlyList<DiscoveredDevice>> DiscoverAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DiscoveredDevice>>([]);
        public Task<IReadOnlyList<string>> GetUsbConnectedUdidsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([Udid]);
        public Task<DeviceStatus> GetStatusAsync(FleetDevice device, CancellationToken cancellationToken = default) =>
            Task.FromResult(Status);
        public Task<byte[]> GetUsbScreenshotAsync(FleetDevice device, CancellationToken cancellationToken = default) =>
            Task.FromResult(UsbScreenshot);
        public Task<byte[]> GetScreenshotAsync(FleetDevice device, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref ScreenshotCount);
            return Task.FromResult(Screenshot);
        }
        public IAsyncEnumerable<byte[]> StreamAsync(FleetDevice device, CancellationToken cancellationToken = default) =>
            AsyncEnumerable.Empty<byte[]>();
        public async Task ExecuteAsync(FleetDevice device, FleetAction action, ActionParameters parameters,
            CancellationToken cancellationToken = default)
        {
            Calls.Enqueue((action, parameters));
            if (Execute is not null)
                await Execute(action, cancellationToken);
        }

        public (FleetAction Action, ActionParameters Parameters) Last() => Calls.Last();

        // 锁屏、旋转与界面树。RotationLocked 模拟界面不跟随旋转；Unsupported 模拟没有这些能力的旧通道。
        public bool Locked
        {
            get; set;
        }
        public bool RotationLocked
        {
            get; set;
        }
        public bool Unsupported
        {
            get; set;
        }
        public string PageSource { get; set; } = "<XCUIElementTypeApplication type=\"XCUIElementTypeApplication\"/>";
        public ConcurrentQueue<ScreenOrientation> Rotations { get; } = new();

        public Task<bool> IsLockedAsync(FleetDevice device, CancellationToken cancellationToken = default) =>
            Unsupported ? throw new NotSupportedException("测试通道不支持查询锁屏状态。") : Task.FromResult(Locked);
        public Task SetOrientationAsync(FleetDevice device, ScreenOrientation orientation, CancellationToken cancellationToken = default)
        {
            Rotations.Enqueue(orientation);
            int longSide = Math.Max(Status.Width, Status.Height), shortSide = Math.Min(Status.Width, Status.Height);
            var landscape = orientation == ScreenOrientation.Landscape;
            if (!RotationLocked)
                Status = new(Status.Online, landscape ? longSide : shortSide, landscape ? shortSide : longSide, Status.DeviceName);
            return Task.CompletedTask;
        }
        public Task<string> GetPageSourceAsync(FleetDevice device, CancellationToken cancellationToken = default) =>
            Task.FromResult(PageSource);
    }

    // 一套完整的开放接口：内存数据库、假桥接、真实脚本执行器和 HTTP 服务。
    internal sealed class ApiHarness : IAsyncDisposable
    {
        private readonly TemporaryFolder folder = new();

        private ApiHarness()
        {
            Apps = Directory.CreateDirectory(Path.Combine(folder.Path, "apps")).FullName;
            Store = new FleetStore(Path.Combine(folder.Path, "fleet.sqlite"));
            Service = new FleetService(Store, Bridge, Apps);
            Scripts = new ScriptRunner(Store, Bridge);
            Host = new WdaHost(Path.Combine(folder.Path, "must-not-launch-python"));
            Device = Store.RegisterDevice("Phone A", Udid, FreePort());
            ManagerRunner = new RecordingRunner(arguments => ManagerReply(arguments));
            Transfer = Path.Combine(folder.Path, "data", "transfer");
        }

        public string Root => folder.Path;

        private static ProcessResult DefaultManagerReply(IReadOnlyList<string> arguments) =>
            RecordingRunner.Ok(arguments.Contains("batteries") ? "[]" : "{}");

        public string Apps
        {
            get;
        }
        public ApiBridge Bridge { get; } = new();
        public FleetStore Store
        {
            get;
        }
        public FleetService Service
        {
            get;
        }
        public ScriptRunner Scripts
        {
            get;
        }
        public WdaHost Host
        {
            get;
        }
        public FleetDevice Device
        {
            get;
        }
        public RecordingRunner Runner
        {
            get;
        } = new(arguments => arguments.Contains("developer-mode-status")
            ? RecordingRunner.Ok("false\n") : RecordingRunner.Ok());
        // 设备管理（电量、应用、文件、镜像、电源）的假子进程；默认读不到电量，其余命令成功。
        public Func<IReadOnlyList<string>, ProcessResult> ManagerReply { get; set; } = DefaultManagerReply;
        public RecordingRunner ManagerRunner
        {
            get;
        }
        // 开放接口的电脑端中转目录。
        public string Transfer
        {
            get;
        }
        public OpenApiServer Server { get; private set; } = null!;
        public int Port => Server.Port;

        public static async Task<ApiHarness> StartAsync(Func<OpenApiOptions, OpenApiOptions>? configure = null,
            Func<OpenApiBackend, OpenApiBackend>? backend = null, OpenApiRouter? router = null)
        {
            var harness = new ApiHarness();
            var created = new OpenApiBackend(harness.Store, harness.Bridge, harness.Service, harness.Scripts, harness.Host,
                new DeviceSetup(harness.Apps, @"C:\Tools\Python\python.exe", harness.Runner, "win32"), harness.Apps)
            {
                AutomationReadyTimeout = TimeSpan.FromSeconds(2),
                JpegEncoder = (png, quality) => [0xff, 0xd8, 0xff, (byte)quality],
                Manager = new DeviceManager(harness.Bridge, @"C:\Tools\Python\python.exe", harness.ManagerRunner, "win32"),
                TransferDirectory = harness.Transfer
            };
            var options = configure?.Invoke(new OpenApiOptions { Port = 0 }) ?? new OpenApiOptions { Port = 0 };
            try
            {
                harness.Server = await OpenApiServer.StartAsync(backend?.Invoke(created) ?? created, options, router);
            }
            catch
            {
                await harness.DisposeAsync();
                throw;
            }
            return harness;
        }

        public Task<RawResponse> PostAsync(string path, object? body = null, string extraHeaders = "") =>
            RawAsync(Port, Request("POST", path, Port, body is string text ? text : JsonSerializer.Serialize(body ?? new
            {
            }), extraHeaders));

        public async Task<JsonElement> OkAsync(string path, object? body = null)
        {
            var response = await PostAsync(path, body);
            Assert.True(response.Status == 200 && response.Code == 0, $"{path}: {response.Status} {response.Text}");
            return response.Json.GetProperty("data");
        }

        public async ValueTask DisposeAsync()
        {
            if (Server is not null)
                await Server.StopAsync();
            await Scripts.DisposeAsync();
            await Host.DisposeAsync();
            await Service.DisposeAsync();
            Store.Dispose();
            folder.Dispose();
        }
    }

    internal static int FreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        return ((IPEndPoint)probe.LocalEndpoint).Port;
    }

    internal static byte[] Request(string method, string path, int port, string? body, string extraHeaders = "",
        string contentType = "application/json")
    {
        var bytes = body is null ? [] : Encoding.UTF8.GetBytes(body);
        var head = $"{method} {path} HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\n"
            + (body is null ? "" : $"Content-Type: {contentType}\r\nContent-Length: {bytes.Length}\r\n")
            + extraHeaders + "\r\n";
        return [.. Encoding.ASCII.GetBytes(head), .. bytes];
    }

    internal sealed record RawResponse(int Status, Dictionary<string, string> Headers, byte[] Body)
    {
        public string Text => Encoding.UTF8.GetString(Body);
        public JsonElement Json => JsonDocument.Parse(Body).RootElement;
        public int Code => Json.GetProperty("code").GetInt32();
        public string Msg => Json.GetProperty("msg").GetString() ?? "";
    }

    // 发送原始字节并读到服务器关闭连接，再按 HTTP/1.1 解析（跳过 100 Continue）。
    internal static async Task<RawResponse> RawAsync(int port, byte[] request, TimeSpan? timeout = null)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();
        await stream.WriteAsync(request);
        return await ReadResponseAsync(stream, timeout ?? TimeSpan.FromSeconds(15));
    }

    internal static async Task<RawResponse> ReadResponseAsync(Stream stream, TimeSpan timeout)
    {
        using var deadline = new CancellationTokenSource(timeout);
        using var output = new MemoryStream();
        await stream.CopyToAsync(output, deadline.Token);
        return Parse(output.ToArray());
    }

    internal static RawResponse Parse(byte[] raw)
    {
        var text = Encoding.Latin1.GetString(raw);
        while (text.StartsWith("HTTP/1.1 100", StringComparison.Ordinal))
        {
            var skip = text.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4;
            text = text[skip..];
            raw = raw[skip..];
        }
        var end = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        Assert.True(end > 0, "Response has no header terminator: " + text);
        var lines = text[..end].Split("\r\n");
        var headers = lines.Skip(1).Select(line => line.Split(':', 2))
            .ToDictionary(parts => parts[0], parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);
        var body = raw[(end + 4)..];
        Assert.Equal(int.Parse(headers["Content-Length"]), body.Length);
        Assert.Equal("close", headers["Connection"]);
        return new(int.Parse(lines[0].Split(' ')[1]), headers, body);
    }
}
