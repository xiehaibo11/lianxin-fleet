using System.Text.Json;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Infrastructure;

internal sealed class HostDeviceTools(string? pythonPath, string platform, IProcessRunner runner)
{
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
        return json.RootElement.EnumerateArray().Where(item => String(item, "DeviceClass") is "iPhone" or "iPad")
            .Select(item => new DiscoveredDevice(String(item, "UniqueDeviceID") ?? String(item, "Identifier") ?? "",
                String(item, "DeviceName") ?? "iPhone"))
            .Where(device => DeviceBridge.ValidUdid(device.Udid)).DistinctBy(device => device.Udid).ToArray();
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

    private bool AbsolutePath(string path) => platform == "win32"
        ? System.Text.RegularExpressions.Regex.IsMatch(path, @"^(?:[A-Za-z]:[\\/]|\\\\[^\\]+\\[^\\]+)")
        : Path.IsPathFullyQualified(path);

    private async Task<ProcessResult> PythonAsync(string[] arguments, bool installing, CancellationToken cancellationToken)
    {
        try
        {
            var result = await runner.RunAsync(python, ["-m", "pymobiledevice3", "--no-color", .. arguments],
                TimeSpan.FromSeconds(installing ? 125 : 15), cancellationToken).ConfigureAwait(false);
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
