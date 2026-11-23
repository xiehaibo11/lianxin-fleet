using System.Text.Json;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Infrastructure;

// 设备管理：信息、电量、应用、开发者镜像和电源。全部走 USB（或 usbmuxd 的 Wi-Fi）锁屏服务，不需要自动化。
internal sealed partial class HostDeviceTools
{
    public async Task<DeviceInfo> GetDeviceInfoAsync(string udid, CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        return ParseDeviceInfo(udid, await ManageAsync("info", [udid], false, cancellationToken).ConfigureAwait(false));
    }

    // 一个子进程读取多台设备的电量；读不到的设备不在结果里。键为规范化 UDID。
    public async Task<IReadOnlyDictionary<string, BatteryInfo>> GetBatteriesAsync(IReadOnlyCollection<string> udids,
        CancellationToken cancellationToken)
    {
        if (udids.Count == 0)
            return new Dictionary<string, BatteryInfo>();
        foreach (var udid in udids)
            CheckUdid(udid);
        return ParseBatteries(await ManageAsync("batteries", udids.ToArray(), false, cancellationToken)
            .ConfigureAwait(false));
    }

    public async Task<IReadOnlyList<DeviceApp>> GetAppsAsync(string udid, CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        return ParseApps(await ManageAsync("apps", [udid], false, cancellationToken).ConfigureAwait(false));
    }

    public Task UninstallAppAsync(string udid, string bundleId, CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        bundleId = FleetValidation.BundleId(bundleId);
        if (bundleId.StartsWith("com.apple.", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("系统应用不能卸载。");
        return CliAsync(["apps", "uninstall", "--udid", udid, "--", bundleId], true, cancellationToken,
            TimeSpan.FromSeconds(125), failureUncertain: true);
    }

    public async Task<DeveloperImageStatus> GetDeveloperImageAsync(string udid, CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        var result = await CliAsync(["mounter", "list", "--udid", udid], false, cancellationToken).ConfigureAwait(false);
        return ParseImages(result.StandardOutput);
    }

    // iOS 16 及以下为 Developer 镜像，iOS 17 及以上为 Personalized 镜像，卸载命令不同。
    public Task UnmountDeveloperImageAsync(string udid, bool personalized, CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        return CliAsync(["mounter", personalized ? "umount-personalized" : "umount-developer", "--udid", udid], true,
            cancellationToken, TimeSpan.FromSeconds(60), failureUncertain: true);
    }

    public Task RestartAsync(string udid, CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        return CliAsync(["diagnostics", "restart", "--udid", udid], true, cancellationToken, failureUncertain: true);
    }

    public Task ShutdownAsync(string udid, CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        return CliAsync(["diagnostics", "shutdown", "--udid", udid], true, cancellationToken, failureUncertain: true);
    }

    internal static DeviceInfo ParseDeviceInfo(string udid, string output)
    {
        using var json = Parse(output, JsonValueKind.Object, "设备信息");
        var root = json.RootElement;
        var battery = Property(root, "battery");
        var disk = Property(root, "disk");
        var screen = Property(root, "screen");
        var health = Property(root, "health");
        var design = Long(health, "DesignCapacity");
        var nominal = Long(health, "NominalChargeCapacity");
        return new DeviceInfo(udid, String(root, "DeviceName"), String(root, "ProductType"), String(root, "ProductVersion"),
            String(root, "BuildVersion"), String(root, "SerialNumber"), String(root, "ModelNumber"),
            String(root, "HardwareModel"), String(root, "RegionInfo"), String(root, "ActivationState"),
            (int?)Long(screen, "ScreenWidth"), (int?)Long(screen, "ScreenHeight"), Double(screen, "ScreenScaleFactor"),
            Long(disk, "TotalDiskCapacity"), Long(disk, "TotalDataCapacity"), Long(disk, "TotalDataAvailable"),
            Battery(battery), Double(health, "Temperature") / 100, (int?)Long(health, "CycleCount"),
            design > 0 && nominal > 0 ? (int)Math.Round(100.0 * nominal.Value / design.Value) : null);
    }

    internal static Dictionary<string, BatteryInfo> ParseBatteries(string output)
    {
        using var json = Parse(output, JsonValueKind.Array, "电量");
        var result = new Dictionary<string, BatteryInfo>(StringComparer.Ordinal);
        foreach (var item in json.RootElement.EnumerateArray())
            if (String(item, "Identifier") is { } udid && Battery(item) is { } battery && String(item, "Error") is null)
                result[Usbmux.Normalize(udid)] = battery;
        return result;
    }

    internal static IReadOnlyList<DeviceApp> ParseApps(string output)
    {
        using var json = Parse(output, JsonValueKind.Array, "应用列表");
        return json.RootElement.EnumerateArray()
            .Select(item => (Bundle: String(item, "CFBundleIdentifier"), Item: item))
            .Where(entry => entry.Bundle is { Length: > 0 and <= 160 })
            .Select(entry => new DeviceApp(entry.Bundle!,
                Clip(String(entry.Item, "CFBundleDisplayName") ?? String(entry.Item, "CFBundleName") ?? entry.Bundle!),
                String(entry.Item, "CFBundleShortVersionString"), String(entry.Item, "CFBundleVersion")))
            .DistinctBy(app => app.BundleId)
            .OrderBy(app => app.Name, StringComparer.OrdinalIgnoreCase).ThenBy(app => app.BundleId, StringComparer.Ordinal)
            .ToArray();
    }

    internal static DeveloperImageStatus ParseImages(string output)
    {
        using var json = Parse(output, JsonValueKind.Array, "镜像列表");
        foreach (var item in json.RootElement.EnumerateArray())
        {
            var type = String(item, "DiskImageType");
            var path = String(item, "MountPath");
            var developer = type is "Developer" or "Personalized" || path is "/Developer" or "/System/Developer";
            if (developer && !(item.TryGetProperty("IsMounted", out var mounted) && mounted.ValueKind == JsonValueKind.False))
                return new DeveloperImageStatus(true, type ?? (path == "/System/Developer" ? "Personalized" : "Developer"), path);
        }
        return new DeveloperImageStatus(false);
    }

    private static BatteryInfo? Battery(JsonElement item) =>
        Long(item, "BatteryCurrentCapacity") is { } level && level is >= 0 and <= 100
            ? new BatteryInfo((int)level, Bool(item, "BatteryIsCharging"), Bool(item, "ExternalConnected"),
                Bool(item, "FullyCharged"))
            : null;

    private static JsonDocument Parse(string output, JsonValueKind kind, string label)
    {
        JsonDocument json;
        try
        {
            json = JsonDocument.Parse(output);
        }
        catch (JsonException error)
        {
            throw new InvalidDataException($"设备工具返回的{label}无法解析。", error);
        }
        if (json.RootElement.ValueKind == kind)
            return json;
        json.Dispose();
        throw new InvalidDataException($"设备工具返回的{label}格式无效。");
    }

    private static JsonElement Property(JsonElement item, string key) =>
        item.ValueKind == JsonValueKind.Object && item.TryGetProperty(key, out var value) ? value : default;

    private static long? Long(JsonElement item, string key) =>
        Property(item, key) is { ValueKind: JsonValueKind.Number } value && value.TryGetDouble(out var number)
            && double.IsFinite(number) && Math.Abs(number) < 1e15 ? (long)Math.Round(number) : null;

    private static double? Double(JsonElement item, string key) =>
        Property(item, key) is { ValueKind: JsonValueKind.Number } value && value.TryGetDouble(out var number)
            && double.IsFinite(number) ? number : null;

    private static bool? Bool(JsonElement item, string key) => Property(item, key).ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };

    private static string Clip(string value) => value.Length > 120 ? value[..120] : value;
}
