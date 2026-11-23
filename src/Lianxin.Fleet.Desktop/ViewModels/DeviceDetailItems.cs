using System.Globalization;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Desktop.ViewModels;

// 设备详情对话框的显示行：信息项、应用和媒体区文件。
public sealed record InfoPair(string Label, string Value);

public sealed class DeviceAppItem(DeviceApp app)
{
    public DeviceApp App { get; } = app;
    public string Name => App.Name;
    public string BundleId => App.BundleId;
    public string Version => App.Version is null ? App.Build ?? ""
        : App.Build is null || App.Build == App.Version ? App.Version : $"{App.Version}（{App.Build}）";
}

public sealed class DeviceFileItem(DeviceFileEntry entry)
{
    public DeviceFileEntry Entry { get; } = entry;
    public string Name => Entry.Name;
    public string Kind => Entry.IsDirectory ? "文件夹" : "文件";
    public string Size => Entry.IsDirectory ? "" : DeviceText.Bytes(Entry.Size);
    public string Modified => Entry.Modified?.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "";
}

public static class DeviceText
{
    // 与 iOS「设置 → 通用 → 关于本机」一致，按 1000 进制显示。
    public static string Bytes(long? bytes) => bytes switch
    {
        null => "—",
        < 1000 => $"{bytes} B",
        < 1_000_000 => (bytes.Value / 1e3).ToString("0.#", CultureInfo.InvariantCulture) + " KB",
        < 1_000_000_000 => (bytes.Value / 1e6).ToString("0.#", CultureInfo.InvariantCulture) + " MB",
        _ => (bytes.Value / 1e9).ToString("0.0", CultureInfo.InvariantCulture) + " GB"
    };

    public static string Battery(BatteryInfo? battery)
    {
        if (battery?.Level is not { } level)
            return "—";
        var state = battery.Charging == true ? "充电中" : battery.FullyCharged == true ? "已充满"
            : battery.ExternalPower == true ? "已接电源" : battery.Charging == false ? "未充电" : "充电状态未知";
        return $"{level}% · {state}";
    }

    public static IReadOnlyList<InfoPair> Describe(DeviceInfo info, DeviceRow row)
    {
        var model = info.ProductType ?? row.ModelCode;
        var health = new[]
        {
            info.BatteryTemperature is { } celsius ? $"温度 {celsius.ToString("0.0", CultureInfo.InvariantCulture)} ℃" : null,
            info.BatteryCycleCount is { } cycles ? $"循环 {cycles} 次" : null,
            info.BatteryHealth is { } percent ? $"最大容量约 {percent}%" : null
        }.OfType<string>().ToArray();
        return
        [
            new("设备名称", info.DeviceName ?? row.Name),
            new("型号", model.Length == 0 ? "—" : $"{ProductNames.Friendly(model)}（{model}）"),
            new("系统版本", info.OsVersion is null ? "—" : $"iOS {info.OsVersion}" + (info.BuildVersion is null ? "" : $"（{info.BuildVersion}）")),
            new("序列号", info.SerialNumber ?? "—"),
            new("销售型号", string.Join(" · ", new[] { info.ModelNumber, info.RegionInfo }.OfType<string>()) is { Length: > 0 } sale ? sale : "—"),
            new("硬件型号", info.HardwareModel ?? "—"),
            new("激活状态", info.ActivationState switch { null => "—", "Activated" => "已激活", var other => other }),
            new("屏幕", info.ScreenWidth is { } width && info.ScreenHeight is { } height
                ? $"{width} × {height} 像素" + (info.ScreenScale is { } scale ? $" · @{scale.ToString("0.#", CultureInfo.InvariantCulture)}x" : "") : "—"),
            new("总容量", Bytes(info.TotalCapacity)),
            new("可用空间", info.DataAvailable is null ? "—" : $"{Bytes(info.DataAvailable)}（数据分区 {Bytes(info.DataCapacity)}）"),
            new("电量", Battery(info.Battery)),
            new("电池", health.Length == 0 ? "—" : string.Join(" · ", health)),
            new("信息来源", info.Source switch { "USB+WDA" => "USB 锁屏服务 + 自动化（电量为实时值）", "WDA" => "自动化（USB 读取失败）", _ => "USB 锁屏服务" }),
            new("设备 ID", row.Udid),
            new("ECID", row.Ecid.Length == 0 ? "—" : row.Ecid)
        ];
    }
}
