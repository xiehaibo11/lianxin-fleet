using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Mac.ViewModels;

public sealed record DeviceGroupItem(string Name, int Count, bool IsAll = false)
{
    public string Title => IsAll ? "所有设备" : Name;
    public string Detail => IsAll ? $"共 {Count} 台" : $"{Count} 台";
    // 列表项的可访问名称（读屏软件与 UI 自动化使用）。
    public override string ToString() => Title;
}

public sealed record ScriptItem(FleetScript Script)
{
    public long Id => Script.Id;
    public string Name => Script.Name;
    public string Detail => (Script.Repeat == 0 ? "循环运行" : Script.Repeat == 1 ? "运行 1 次" : $"运行 {Script.Repeat} 次")
        + " · " + Script.UpdatedAt.LocalDateTime.ToString("MM-dd HH:mm");
    public override string ToString() => Name;
}

public sealed record ScheduleItem(FleetSchedule Schedule, string ScriptName)
{
    private static readonly string[] Days = ["日", "一", "二", "三", "四", "五", "六"];
    public long Id => Schedule.Id;
    public string Name => Schedule.Name;
    public string Target => Schedule.TargetIds.Count > 0 ? $"指定 {Schedule.TargetIds.Count} 台设备"
        : Schedule.TargetGroup is { } group ? "分组：" + group : "全部设备";
    public string Rule => Schedule.Kind == ScheduleKind.Interval ? $"每隔 {Schedule.IntervalMinutes} 分钟"
        : (Schedule.Weekdays == 127 ? "每天" : "每周" + string.Concat(Enumerable.Range(0, 7)
            .Where(day => (Schedule.Weekdays & (1 << day)) != 0).Select(day => Days[day]))) + $" {Schedule.At:HH\\:mm}";
    public string Enabled => Schedule.Enabled ? "启用" : "停用";
    public string LastRun => Schedule.LastRunAt?.LocalDateTime.ToString("MM-dd HH:mm") ?? "—";
}

public sealed record AgentLogItem(DateTime Time, string Kind, string Text)
{
    public string Stamp => Time.ToString("HH:mm:ss");
}
