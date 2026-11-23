namespace Lianxin.Fleet.Core.Domain;

public enum ScriptRunState
{
    Running, Succeeded, Failed, Stopped, Unknown
}

public enum ScheduleKind
{
    Daily, Interval
}

// Command 是原有的单行命令：先执行 Action（可为空），再等待 WaitSeconds。其余种类见 ScriptStep 的各属性。
public enum ScriptStepKind
{
    Command, Repeat, If, While, Break, Stop, Fail, RandomWait, Log, Capture,
    TapProbe, WaitProbe, WaitProbeGone, InputToElement
}

// 界面元素选择器：文字=、包含=、id=、类型=、xpath=、谓词=。
public enum ElementSelectorKind
{
    Text, Contains, Id, Type, XPath, Predicate
}

public sealed record ElementSelector(ElementSelectorKind Kind, string Value);

public enum ScriptProbeKind
{
    Element, ColorAt, ColorSearch, Image
}

// 区域与坐标都是 0–1 的相对位置。
public sealed record ScreenRegion(double Left, double Top, double Right, double Bottom)
{
    public static ScreenRegion Full { get; } = new(0, 0, 1, 1);
}

// 屏幕上要找的目标：元素、某点颜色、区域找色或模板图片。Rgb 为 0xRRGGBB，Tolerance 为每个通道允许的差值（0–255）。
public sealed record ScriptProbe(ScriptProbeKind Kind, ElementSelector? Selector = null, double X = 0, double Y = 0,
    int Rgb = 0, int Tolerance = 0, ScreenRegion? Region = null, string? Template = null, double Similarity = 0);

public sealed record ScriptCondition(ScriptProbe Probe, bool Negate = false);

// 脚本的一条语句。旧脚本只产生 Command：设备动作（Action 不为空）或纯等待。
// 块语句（重复、如果、当）的子语句在 Body/ElseBody 中；TimeoutSeconds 用于等待元素/图片，
// RandomWait 在 WaitSeconds 与 MaxWaitSeconds 之间随机等待。
public sealed record ScriptStep(int Line, string Label, FleetAction? Action, ActionParameters Parameters,
    double WaitSeconds = 0)
{
    public ScriptStepKind Kind { get; init; } = ScriptStepKind.Command;
    public ScriptCondition? Condition
    {
        get; init;
    }
    public ScriptProbe? Probe
    {
        get; init;
    }
    public IReadOnlyList<ScriptStep> Body { get; init; } = [];
    public IReadOnlyList<ScriptStep> ElseBody { get; init; } = [];
    public int Count
    {
        get; init;
    }
    public double MaxWaitSeconds
    {
        get; init;
    }
    public double TimeoutSeconds
    {
        get; init;
    }
    public string? Text
    {
        get; init;
    }
}

// 界面树里的一个可见元素，坐标单位为点（与 WDA 窗口尺寸一致）。Selector 是可直接写进脚本的选择器；
// Unique 为 false 表示整棵界面树里还有别的元素也符合它，「点击元素」会点到第一个。
public sealed record ScreenElement(string Type, string? Name, string? Label, string? Value,
    double X, double Y, double Width, double Height, string Selector, bool Unique = true);

// 元素位置，单位为点。
public sealed record ElementBounds(double X, double Y, double Width, double Height);

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
