using System.Text.Json;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Infrastructure;

internal sealed partial class HostDeviceTools(string? pythonPath, string platform, IProcessRunner runner)
{
    private const int MaxScreenshotBytes = 24 * 1024 * 1024;
    private const string CaptureUsbScreenshot = """
        import asyncio
        import sys
        import tempfile
        from pathlib import Path

        from pymobiledevice3.exceptions import PyMobileDevice3Exception
        from pymobiledevice3.lockdown import create_using_usbmux
        from pymobiledevice3.services.screenshot import ScreenshotService

        async def capture(serial, output):
            async with await create_using_usbmux(serial=serial, connection_type="USB", autopair=False) as lockdown:
                if not lockdown.paired:
                    raise SystemExit("请先在 iPhone 上信任此电脑。")
                async with ScreenshotService(lockdown) as service:
                    data = await service.take_screenshot()
                output.write_bytes(data)

        output = Path(sys.argv[2])
        tempfile.tempdir = str(output.parent)
        try:
            asyncio.run(capture(sys.argv[1], output))
        except PyMobileDevice3Exception as error:
            raise SystemExit(f"{type(error).__name__}: {error}") from None
        """;

    private readonly string python = pythonPath ?? Environment.GetEnvironmentVariable("PYTHON_PATH")
        ?? (platform == "win32" ? "python" : "python3");

    public async Task<IReadOnlyList<DiscoveredDevice>> DiscoverAsync(CancellationToken cancellationToken)
    {
        if (platform is not ("win32" or "darwin"))
            throw new PlatformNotSupportedException("USB 发现支持 Windows 11 与 macOS。");
        try
        {
            return await DiscoverUsbAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DeviceOperationException error) when (platform == "darwin" && !cancellationToken.IsCancellationRequested
            && (error.InnerException is ProcessRunException { Interrupted: false }
                || error.Message.Contains("No module named", StringComparison.Ordinal)))
        {
            var devices = await DiscoverDevicectlAsync(cancellationToken).ConfigureAwait(false);
            if (devices.Count == 0)
                throw;
            return devices;
        }
    }

    private async Task<IReadOnlyList<DiscoveredDevice>> DiscoverUsbAsync(CancellationToken cancellationToken)
    {
        var output = await PythonAsync(["usbmux", "list", "--usb"], false, cancellationToken).ConfigureAwait(false);
        using var json = JsonDocument.Parse(output.StandardOutput);
        if (json.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("设备工具未返回 JSON 数组。");
        var devices = json.RootElement.EnumerateArray().Where(item => String(item, "DeviceClass") is "iPhone" or "iPad")
            .Select(item => new DiscoveredDevice(String(item, "UniqueDeviceID") ?? String(item, "Identifier") ?? "",
                String(item, "DeviceName") ?? "iPhone", null, String(item, "ProductType"), String(item, "ProductVersion")))
            .Where(device => DeviceBridge.ValidUdid(device.Udid)).DistinctBy(device => device.Udid).ToArray();
        return await EnrichAsync(devices, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<DiscoveredDevice>> DiscoverDevicectlAsync(CancellationToken cancellationToken)
    {
        var result = await DevicectlAsync(cancellationToken).ConfigureAwait(false);
        return result.GetProperty("devices").EnumerateArray().Where(item =>
        {
            var hardware = Section(item, "hardwareProperties", "hardware");
            var connection = Section(item, "connectionProperties", "connection");
            return String(hardware, "platform") == "iOS" && String(hardware, "reality") != "simulated"
                && (String(connection, "state") ?? String(connection, "tunnelState")) == "connected";
        }).Select(item => new DiscoveredDevice(String(Section(item, "hardwareProperties", "hardware"), "udid") ?? "",
            String(Section(item, "deviceProperties", "state"), "name") ?? "iPhone"))
            .Where(device => DeviceBridge.ValidUdid(device.Udid)).ToArray();
    }

    public async Task InstallAsync(FleetDevice device, string? appPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(appPath) || appPath.Contains('\0') || !AbsolutePath(appPath)
            || !(appPath.EndsWith(".ipa", StringComparison.OrdinalIgnoreCase) || appPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("安装包必须是本机绝对路径的 .ipa 或 .app。");
        await PythonAsync(["apps", "install", "--udid", device.Udid, appPath], true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<byte[]> GetUsbScreenshotAsync(string udid, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = Directory.CreateTempSubdirectory("lianxin-usb-frame-");
        var output = Path.Combine(directory.FullName, "frame");
        try
        {
            // Use the same screenshot service as the CLI, with explicit USB transport and existing trust only.
            var result = await runner.RunAsync(python, ["-c", CaptureUsbScreenshot, udid, output],
                TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (result.ExitCode != 0)
                throw new DeviceOperationException("USB 画面获取失败，请检查 Python、USB 信任及开发者磁盘映像。"
                    + Short(result.StandardError.Trim()));
            using var input = File.OpenRead(output);
            var length = input.Length;
            if (length is < 8 or > MaxScreenshotBytes)
                throw new InvalidDataException("USB 截图大小无效。");
            var image = new byte[(int)length];
            await input.ReadExactlyAsync(image, cancellationToken).ConfigureAwait(false);
            if (!IsScreenshot(image))
                throw new InvalidDataException("USB 截图格式无效。");
            return image;
        }
        catch (ProcessRunException error)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new DeviceOperationException(error.Message, false, error);
        }
        finally
        {
            try
            {
                directory.Delete(recursive: true);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
    }

    private static bool IsScreenshot(ReadOnlySpan<byte> image) =>
        image.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a })
        || image.StartsWith(new byte[] { 0xff, 0xd8, 0xff })
        || image.StartsWith("II*\0"u8) || image.StartsWith("MM\0*"u8);

    private bool AbsolutePath(string path) => platform == "win32"
        ? System.Text.RegularExpressions.Regex.IsMatch(path, @"^(?:[A-Za-z]:[\\/]|\\\\[^\\]+\\[^\\]+)")
        : path.StartsWith('/');

    // installing：中断时操作可能已在手机上生效，结果标为待确认。
    private async Task<ProcessResult> PythonAsync(string[] arguments, bool installing, CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        try
        {
            var result = await runner.RunAsync(python, ["-m", "pymobiledevice3", "--no-color", .. arguments],
                timeout ?? TimeSpan.FromSeconds(installing ? 125 : 15), cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0)
                throw new DeviceOperationException("设备工具拒绝操作，请检查驱动、USB 信任、签名及设备状态。"
                + Short(result.StandardError));
            return result;
        }
        catch (ProcessRunException error) { throw new DeviceOperationException(error.Message, installing && error.Interrupted, error); }
    }

    private async Task<JsonElement> DevicectlAsync(CancellationToken cancellationToken)
    {
        if (platform != "darwin")
            throw new PlatformNotSupportedException("devicectl 只支持 macOS。");
        var directory = Path.Combine(Path.GetTempPath(), "lianxin-devicectl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var output = Path.Combine(directory, "result.json");
        try
        {
            var result = await runner.RunAsync("/usr/bin/xcrun", ["devicectl", "--timeout", "10",
                "--json-output", output, "list", "devices"], TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0)
                throw new DeviceOperationException("Apple 设备发现失败，请检查 Xcode 和设备信任。");
            if (new FileInfo(output).Length > 1024 * 1024)
                throw new InvalidDataException("设备工具响应过大。");
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(output, cancellationToken).ConfigureAwait(false));
            if (String(json.RootElement.GetProperty("info"), "outcome") != "success")
                throw new DeviceOperationException("Apple 设备工具未确认操作成功。");
            return json.RootElement.GetProperty("result").Clone();
        }
        catch (ProcessRunException error) { throw new DeviceOperationException(error.Message, false, error); }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
    }

    private static JsonElement Section(JsonElement item, string flat, string nested) =>
        item.TryGetProperty(flat, out var direct) ? direct : item.TryGetProperty("properties", out var props)
            && props.TryGetProperty(nested, out var section) ? section : default;
    private static string? String(JsonElement item, string key) => item.ValueKind == JsonValueKind.Object
        && item.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static string Short(string value) => value.Length > 400 ? value[..400] : value;
}
