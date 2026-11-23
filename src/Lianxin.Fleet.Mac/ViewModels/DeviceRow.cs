using System.ComponentModel;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Services;

namespace Lianxin.Fleet.Mac.ViewModels;

// 中控设备表格的一行。每次刷新整体更新，界面按需重新绑定全部列。
public sealed class DeviceRow(DeviceSnapshot snapshot) : INotifyPropertyChanged
{
    private bool selected;
    private string? note;
    private DeviceSnapshot current = snapshot;
    private bool channel;
    private ScriptActivity? activity;
    private ScriptRun? lastRun;
    private BatteryInfo? battery;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DeviceSnapshot Snapshot => current;
    public long Id => current.Device.Id;
    public FleetDevice Device => current.Device;
    public DeviceStatus Status => current.Status;
    public string Udid => Device.Udid;
    public string Alias => Device.Alias ?? "";
    public string Name => Device.Name;
    public string Model => ProductNames.Friendly(Device.ProductType);
    public string ModelCode => Device.ProductType ?? "";
    public string OsVersion => Device.OsVersion ?? "";
    public string Ecid => Device.Ecid ?? "";
    public string Group => Device.GroupName;
    public bool Online => current.UsbConnected == true || current.NetworkConnected == true;
    public bool OverWifi => current.UsbConnected != true && current.NetworkConnected == true;
    public string Connection => current.UsbConnected == true ? "USB" : current.NetworkConnected == true ? "Wi-Fi"
        : current.UsbConnected == false ? "离线" : "未知";
    // USB 状态未知（Apple 设备服务读不到）时不阻止已经在线的 WDA。
    public bool AutomationReady => Status.Online && (Online || current.UsbConnected is null);
    public string Automation => AutomationReady ? "是" : channel && Online ? "启动中" : "否";
    public string AutomationTip => AutomationReady ? $"WDA 已就绪 · 本机端口 {Device.WdaPort}" : Status.Error ?? "自动化未启动";
    public bool ScriptRunning => activity is not null;
    public string Script => activity?.ScriptName ?? lastRun?.ScriptName ?? "";
    public string Message => note ?? (activity is not null ? (activity.Paused ? "已暂停 · " : "运行中 · ") + activity.Message
        : lastRun is null ? "—" : $"{State(lastRun.State)} · {lastRun.Message}");
    // 电量列：数值用于排序，读不到时为 -1。
    public int BatteryLevel => battery?.Level ?? -1;
    public string Battery => battery?.Level is { } level ? $"{level}%" + (battery.Charging == true ? " ⚡" : "") : "—";
    public string BatteryTip => battery is null ? "电量未读取：需要 USB 连接并信任电脑，每分钟刷新一次。"
        : DeviceText.Battery(battery) + (battery.Source == "WDA" ? " · 来自自动化" : " · 来自 USB");

    public void SetBattery(BatteryInfo? value)
    {
        if (Equals(battery, value))
            return;
        battery = value;
        foreach (var name in new[] { nameof(Battery), nameof(BatteryLevel), nameof(BatteryTip) })
            PropertyChanged?.Invoke(this, new(name));
    }

    public bool IsSelected
    {
        get => selected;
        set
        {
            if (selected == value)
                return;
            selected = value;
            PropertyChanged?.Invoke(this, new(nameof(IsSelected)));
        }
    }

    public void Update(DeviceSnapshot snapshot, bool hasChannel, ScriptActivity? running, ScriptRun? latest)
    {
        current = snapshot;
        channel = hasChannel;
        activity = running;
        lastRun = latest;
        PropertyChanged?.Invoke(this, new(string.Empty));
    }

    // 一键配置等临时操作的进度，优先于脚本结果显示；传 null 清除。
    public void SetNote(string? value)
    {
        note = value;
        PropertyChanged?.Invoke(this, new(nameof(Message)));
    }

    public bool Matches(string query) => query.Length == 0
        || new[] { Udid, Alias, Name, Ecid, Group, Model }.Any(value => value.Contains(query, StringComparison.OrdinalIgnoreCase));

    private static string State(ScriptRunState state) => state switch
    {
        ScriptRunState.Succeeded => "完成",
        ScriptRunState.Failed => "失败",
        ScriptRunState.Stopped => "已停止",
        ScriptRunState.Unknown => "待确认",
        _ => "运行中"
    };
}

public static class ProductNames
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.Ordinal)
    {
        ["iPhone8,1"] = "iPhone 6s",
        ["iPhone8,2"] = "iPhone 6s Plus",
        ["iPhone8,4"] = "iPhone SE",
        ["iPhone9,1"] = "iPhone 7",
        ["iPhone9,3"] = "iPhone 7",
        ["iPhone9,2"] = "iPhone 7 Plus",
        ["iPhone9,4"] = "iPhone 7 Plus",
        ["iPhone10,1"] = "iPhone 8",
        ["iPhone10,4"] = "iPhone 8",
        ["iPhone10,2"] = "iPhone 8 Plus",
        ["iPhone10,5"] = "iPhone 8 Plus",
        ["iPhone10,3"] = "iPhone X",
        ["iPhone10,6"] = "iPhone X",
        ["iPhone11,2"] = "iPhone XS",
        ["iPhone11,4"] = "iPhone XS Max",
        ["iPhone11,6"] = "iPhone XS Max",
        ["iPhone11,8"] = "iPhone XR",
        ["iPhone12,1"] = "iPhone 11",
        ["iPhone12,3"] = "iPhone 11 Pro",
        ["iPhone12,5"] = "iPhone 11 Pro Max",
        ["iPhone12,8"] = "iPhone SE (第二代)",
        ["iPhone13,1"] = "iPhone 12 mini",
        ["iPhone13,2"] = "iPhone 12",
        ["iPhone13,3"] = "iPhone 12 Pro",
        ["iPhone13,4"] = "iPhone 12 Pro Max",
        ["iPhone14,4"] = "iPhone 13 mini",
        ["iPhone14,5"] = "iPhone 13",
        ["iPhone14,2"] = "iPhone 13 Pro",
        ["iPhone14,3"] = "iPhone 13 Pro Max",
        ["iPhone14,6"] = "iPhone SE (第三代)",
        ["iPhone14,7"] = "iPhone 14",
        ["iPhone14,8"] = "iPhone 14 Plus",
        ["iPhone15,2"] = "iPhone 14 Pro",
        ["iPhone15,3"] = "iPhone 14 Pro Max"
    };

    public static string Friendly(string? code) =>
        code is null ? "" : Names.TryGetValue(code, out var name) ? name : code;
}
