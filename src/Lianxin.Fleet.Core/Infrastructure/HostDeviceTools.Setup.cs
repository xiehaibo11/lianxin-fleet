using System.Text.Json;

namespace Lianxin.Fleet.Core.Infrastructure;

internal sealed partial class HostDeviceTools
{
    public async Task<bool> GetDeveloperModeAsync(string udid, CancellationToken cancellationToken)
    {
        var result = await PythonAsync(["amfi", "developer-mode-status", "--udid", udid], false, cancellationToken)
            .ConfigureAwait(false);
        return result.StandardOutput.Trim() switch
        {
            "true" => true,
            "false" => false,
            _ => throw new InvalidDataException("设备工具返回的开发者模式状态无法识别。")
        };
    }

    // 手机会重启；有锁屏密码时 iOS 不允许自动开启，需在手机设置里手动打开。
    public Task EnableDeveloperModeAsync(string udid, CancellationToken cancellationToken) =>
        PythonAsync(["amfi", "enable-developer-mode", "--udid", udid], true, cancellationToken, TimeSpan.FromMinutes(5));

    // 按 iOS 版本选择开发者镜像，本机没有时自动下载；已挂载时直接成功。
    public Task MountDeveloperImageAsync(string udid, CancellationToken cancellationToken) =>
        PythonAsync(["mounter", "auto-mount", "--udid", udid], true, cancellationToken, TimeSpan.FromMinutes(4));

    public async Task<bool> GetWifiConnectionsAsync(string udid, CancellationToken cancellationToken)
    {
        var result = await PythonAsync(["lockdown", "wifi-connections", "--udid", udid], false, cancellationToken)
            .ConfigureAwait(false);
        using var json = JsonDocument.Parse(result.StandardOutput);
        return json.RootElement.ValueKind == JsonValueKind.Object
            && json.RootElement.TryGetProperty("EnableWifiConnections", out var value)
            && value.ValueKind == JsonValueKind.True;
    }

    public Task SetWifiConnectionsAsync(string udid, bool enabled, CancellationToken cancellationToken) =>
        PythonAsync(["lockdown", "wifi-connections", "--udid", udid, enabled ? "on" : "off"], false, cancellationToken);
}
