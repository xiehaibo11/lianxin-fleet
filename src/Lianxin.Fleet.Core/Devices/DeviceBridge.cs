using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;
using Lianxin.Fleet.Core.Serialization;

namespace Lianxin.Fleet.Core.Devices;

public sealed partial class DeviceBridge : IDeviceBridge, IDisposable
{
    public const int WdaDevicePort = 8100;
    public const int MjpegDevicePort = 9100;
    private readonly HttpClient client;
    private readonly bool ownsClient;
    private readonly HostDeviceTools host;
    private readonly Func<FleetDevice, CancellationToken, Task<Stream>> streamConnector;
    private readonly ConcurrentDictionary<int, string> sessions = new();
    private readonly ConcurrentDictionary<int, SemaphoreSlim> locks = new();
    // 每个 WDA 端口最近一次读到的点尺寸和名称，状态探测时手机忙得来不及回答就沿用。
    private readonly ConcurrentDictionary<int, (int Width, int Height, string Name)> lastStatus = new();
    private bool disposed;

    public DeviceBridge(string? pythonPath = null, HttpClient? httpClient = null, string? platform = null,
        IProcessRunner? processRunner = null, Func<FleetDevice, CancellationToken, Task<Stream>>? streamConnector = null)
    {
        this.streamConnector = streamConnector ?? ((device, token) => Usbmux.ConnectAsync(device.Udid, MjpegDevicePort, token));
        ownsClient = httpClient is null;
        client = httpClient ?? new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false })
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        host = new HostDeviceTools(pythonPath, platform ?? (OperatingSystem.IsWindows() ? "win32"
            : OperatingSystem.IsMacOS() ? "darwin" : "linux"), processRunner ?? new ProcessRunner());
    }

    public static bool ValidUdid(string udid) => Regex.IsMatch(udid, "^[A-Za-z0-9][A-Za-z0-9-]{7,79}$");

    private static void Validate(FleetDevice device, bool needsWda = true)
    {
        if (!ValidUdid(device.Udid))
            throw new ArgumentException("设备 UDID 无效。");
        if (needsWda && device.WdaPort is < 1024 or > 65535)
            throw new ArgumentException("WDA 本机端口须为 1024 至 65535。");
    }

    public Task<IReadOnlyList<DiscoveredDevice>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return host.DiscoverAsync(cancellationToken);
    }

    public async Task<DeviceStatus> GetStatusAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        Validate(device);
        var ready = RequestAsync(device, "/status", timeoutSeconds: 2, cancellationToken: cancellationToken);
        var size = DimensionsAsync(device, 2, cancellationToken);
        var info = RequestAsync(device, "/wda/device/info", timeoutSeconds: 2, cancellationToken: cancellationToken);
        try
        {
            await Task.WhenAll(ready, size, info).ConfigureAwait(false);
        }
        catch (DeviceOperationException) when (ready.IsCompletedSuccessfully && lastStatus.ContainsKey(device.WdaPort))
        {
            // 真机实测：应用刚启动、键盘弹出时 window/size 和 device/info 要等 XCTest 主线程，常超过 2 秒；
            // /status 不经过主线程，仍立即返回。这时沿用上次的尺寸和名称，不把能控制的手机报成离线
            // （否则集控投屏的同步操作会悄悄跳过这台）。
        }
        if (!ready.Result.TryGetProperty("ready", out var value) || value.ValueKind != System.Text.Json.JsonValueKind.True)
            throw new DeviceOperationException("WDA 尚未就绪。");
        var known = lastStatus.GetValueOrDefault(device.WdaPort);
        var (width, height) = size.IsCompletedSuccessfully ? size.Result : (known.Width, known.Height);
        var name = !info.IsCompletedSuccessfully ? known.Name
            : info.Result.TryGetProperty("name", out var label) && label.ValueKind == System.Text.Json.JsonValueKind.String
                ? label.GetString() ?? "" : "";
        name = name.Length > 120 ? name[..120] : name;
        lastStatus[device.WdaPort] = (width, height, name);
        // shortcut: WDA returns a vendor UUID, not UDID; verify the USB mapping with the actual device name.
        return new DeviceStatus(true, width, height, name);
    }

    // 只问 window/size：手机正在动画或弹出键盘时它可能要一两秒，按动作的 8 秒超时等。
    public Task<(int Width, int Height)> GetWindowSizeAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return DimensionsAsync(device, 8, cancellationToken);
    }

    public async Task<byte[]> GetScreenshotAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        return await SerializedAsync(device, async () =>
        {
            await SessionAsync(device, cancellationToken).ConfigureAwait(false);
            var value = await RequestAsync(device, "/screenshot", limit: 24 * 1024 * 1024,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            byte[] image;
            try
            {
                image = Convert.FromBase64String(value.GetString() ?? "");
            }
            catch (Exception error) when (error is FormatException or InvalidOperationException)
            {
                throw new DeviceOperationException("WDA 截图编码无效。", false, error);
            }
            byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
            if (!image.AsSpan().StartsWith(signature))
                throw new DeviceOperationException("WDA 未返回 PNG 截图。");
            return image;
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> SerializedAsync<T>(FleetDevice device, Func<Task<T>> action, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Validate(device);
        var gate = locks.GetOrAdd(device.WdaPort, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action().ConfigureAwait(false);
        }
        finally { gate.Release(); }
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        if (ownsClient)
            client.Dispose();
        // Active requests release their gates when cancelled; disposing a held gate would break that cleanup.
    }
    private async Task<JsonElement> RequestAsync(FleetDevice device, string path, object? body = null,
        bool action = false, int timeoutSeconds = 8, int limit = 256 * 1024,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Validate(device);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        using var request = new HttpRequestMessage(body is null ? HttpMethod.Get : HttpMethod.Post,
            new Uri($"http://127.0.0.1:{device.WdaPort}{path}"));
        if (body is not null)
            // WDA rejects chunked request bodies, so buffer the JSON to send Content-Length.
            request.Content = new StringContent(JsonSerializer.Serialize(body, FleetJson.Options), Encoding.UTF8, "application/json");
        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                deadline.Token).ConfigureAwait(false);
            if (response.Content.Headers.ContentLength > limit)
                throw new InvalidDataException("WDA 响应过大。");
            using var stream = await response.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
            using var output = new MemoryStream();
            var buffer = new byte[8192];
            int count;
            while ((count = await stream.ReadAsync(buffer, deadline.Token).ConfigureAwait(false)) > 0)
            {
                if (output.Length + count > limit)
                    throw new InvalidDataException("WDA 响应过大。");
                output.Write(buffer, 0, count);
            }
            using var json = JsonDocument.Parse(output.ToArray());
            var root = json.RootElement;
            if (!root.TryGetProperty("value", out var value))
                throw new InvalidDataException("WDA 响应格式无效。");
            JsonElement error = default;
            var rejected = value.ValueKind == JsonValueKind.Object && value.TryGetProperty("error", out error);
            if (!response.IsSuccessStatusCode || rejected || (root.TryGetProperty("status", out var status)
                && status.ValueKind == JsonValueKind.Number && status.GetInt32() != 0))
            {
                if (rejected && error.ValueKind == JsonValueKind.String && error.GetString() == "invalid session id")
                    sessions.TryRemove(device.WdaPort, out _);
                var description = value.ValueKind == JsonValueKind.Object && value.TryGetProperty("message", out var message)
                    && message.ValueKind == JsonValueKind.String ? message.GetString() : $"HTTP {(int)response.StatusCode}";
                throw new DeviceOperationException("WDA 拒绝执行：" + (description?.Length > 300 ? description[..300] : description));
            }
            return value.Clone();
        }
        catch (DeviceOperationException) { throw; }
        catch (Exception error) when (error is HttpRequestException or OperationCanceledException or JsonException
            or InvalidDataException or IOException or InvalidOperationException)
        {
            throw new DeviceOperationException("无法确认 WDA 响应，请检查设备及本机 USB 端口映射。", action, error);
        }
    }

    // 截图保持 PNG；画面流降到约 5 帧/秒、30% 尺寸，让一台电脑能同时预览数十台手机。
    private static readonly object Settings = new
    {
        settings = new
        {
            screenshotQuality = 0,
            mjpegServerFramerate = 5,
            mjpegScalingFactor = 30,
            mjpegServerScreenshotQuality = 30
        }
    };

    private async Task<string> SessionAsync(FleetDevice device, CancellationToken cancellationToken)
    {
        if (sessions.TryGetValue(device.WdaPort, out var cached))
            return cached;
        var value = await RequestAsync(device, "/session", new
        {
            capabilities = new
            {
                alwaysMatch = new
                {
                }
            }
        },
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!value.TryGetProperty("sessionId", out var item) || item.ValueKind != JsonValueKind.String
            || !Regex.IsMatch(item.GetString() ?? "", "^[A-Za-z0-9_-]{1,128}$"))
            throw new DeviceOperationException("WDA 未返回有效会话编号。");
        var id = item.GetString()!;
        await RequestAsync(device, $"/session/{id}/appium/settings", Settings,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        sessions[device.WdaPort] = id;
        return id;
    }

    private async Task<(int Width, int Height)> DimensionsAsync(FleetDevice device, int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var size = await RequestAsync(device, "/window/size", timeoutSeconds: timeoutSeconds,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!size.TryGetProperty("width", out var width) || !size.TryGetProperty("height", out var height)
            || !width.TryGetDouble(out var x) || !height.TryGetDouble(out var y)
            || !double.IsFinite(x) || !double.IsFinite(y) || x is < 1 or > 20000 || y is < 1 or > 20000)
            throw new DeviceOperationException("WDA 屏幕尺寸无效。");
        return ((int)Math.Round(x), (int)Math.Round(y));
    }

    public async IAsyncEnumerable<byte[]> StreamAsync(FleetDevice device,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await SerializedAsync(device, async () =>
        {
            var id = await SessionAsync(device, cancellationToken).ConfigureAwait(false);
            // 每次开流都重设：WDA 重启后恢复默认的 10 帧原尺寸，几十台同时预览会压垮 USB 带宽。
            await RequestAsync(device, $"/session/{id}/appium/settings", Settings,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
        Stream stream;
        try
        {
            stream = await streamConnector(device, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or System.Net.Sockets.SocketException or InvalidDataException
            or System.Xml.XmlException)
        {
            throw new DeviceOperationException(error.Message, false, error);
        }
        await using (stream)
            await foreach (var frame in MjpegReader.ReadFramesAsync(stream, cancellationToken).ConfigureAwait(false))
                yield return frame;
    }
}
