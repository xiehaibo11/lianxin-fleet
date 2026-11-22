namespace Lianxin.Fleet.Core.Domain;

public enum ScriptRunState
{
    Running, Succeeded, Failed, Stopped, Unknown
}

public enum ScheduleKind
{
    Daily, Interval
}

// 脚本的一行：设备动作（Action 不为空）或纯等待。
public sealed record ScriptStep(int Line, string Label, FleetAction? Action, ActionParameters Parameters,
    double WaitSeconds = 0);

// Repeat 为 0 表示一直循环，直到手动停止。
public sealed record FleetScript(long Id, string Name, string Source, int Repeat, DateTimeOffset UpdatedAt);

public sealed record ScriptRun(long Id, long ScriptId, string ScriptName, long DeviceId, ScriptRunState State,
    string? Message, string Trigger, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt);

// 目标：TargetIds 非空时只跑这些设备；否则 TargetGroup 非空时跑该分组；都为空时跑全部设备。
// Weekdays 按 DayOfWeek 位掩码（周日为第 0 位），127 表示每天。
public sealed record FleetSchedule(long Id, string Name, long ScriptId, ScheduleKind Kind, TimeOnly At,
    int Weekdays, int IntervalMinutes, string? TargetGroup, IReadOnlyList<long> TargetIds, bool Enabled,
    DateTimeOffset CreatedAt, DateTimeOffset? LastRunAt = null);

public sealed record DeviceConnection(string Udid, string ConnectionType);
