using System.Text;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Services;

public enum RecordOutcome
{
    Recorded, NotRecording, Unsupported, Full
}

// 集控投屏的脚本录制：记下主控窗口提交成功的操作和操作之间的间隔，停止后生成与 ScriptParser 相同语法的脚本。
// 只在界面线程使用，不加锁。
public sealed class ScriptRecorder(TimeProvider? time = null)
{
    public const double MinWait = 0.05;
    public const double MaxWait = 3600;
    // 预留给录制说明注释的字符数，保证生成的脚本不超过 ScriptParser.MaxSourceLength。
    private const int HeaderAllowance = 400;
    private const int WaitLineAllowance = 12;
    private readonly TimeProvider clock = time ?? TimeProvider.System;
    private readonly List<Entry> entries = [];
    private long startedAt, stoppedAt;
    private int length;

    // At：相对开始录制的秒数；Busy：这一步在手机上本身要执行的时长（滑动、长按），回放时不再重复等待。
    private sealed record Entry(string Line, string? Note, double At, double Busy);

    public bool IsRecording
    {
        get; private set;
    }
    public int Count => entries.Count;
    public string? LastLine => entries.Count == 0 ? null : entries[^1].Line;
    public TimeSpan Elapsed => clock.GetElapsedTime(startedAt, IsRecording ? clock.GetTimestamp() : stoppedAt);

    public void Start()
    {
        entries.Clear();
        length = 0;
        startedAt = stoppedAt = clock.GetTimestamp();
        IsRecording = true;
    }

    public void Stop()
    {
        if (!IsRecording)
            return;
        stoppedAt = clock.GetTimestamp();
        IsRecording = false;
    }

    // 用户操作那一刻的时间戳，传给 Record 的 at：等主控执行成功后再写入，等待时长仍按操作的时刻计算。
    public long Stamp() => clock.GetTimestamp();

    // 这个操作能否写进脚本（安装应用等不能）。
    public bool Accepts(FleetAction action, ActionParameters parameters) => Line(action, parameters, out _) is not null;

    // 在主控执行成功后调用；参数按任务同一套规则校验，安装应用等无法写进脚本的操作返回 Unsupported。
    // at 为 Stamp() 的时间戳：录制期间做的操作即使在停止录制后才执行完也照样写入，按操作时刻排在正确的位置。
    public RecordOutcome Record(FleetAction action, ActionParameters parameters, long? at = null)
    {
        var moment = at ?? clock.GetTimestamp();
        if (moment < startedAt || (!IsRecording && moment > stoppedAt) || (!IsRecording && at is null))
            return RecordOutcome.NotRecording;
        if (Line(action, parameters, out var note) is not { } line)
            return RecordOutcome.Unsupported;
        var added = line.Length + 1 + (note?.Length + 1 ?? 0) + WaitLineAllowance;
        // 每个操作之间有一条等待，两者都算脚本步数。
        if (entries.Count * 2 + 1 > ScriptParser.MaxSteps || length + added + HeaderAllowance > ScriptParser.MaxSourceLength)
            return RecordOutcome.Full;
        var validated = FleetValidation.Action(action, parameters, "");
        var busy = action is FleetAction.Swipe or FleetAction.LongPress ? validated.Duration : 0;
        var entry = new Entry(line, note, clock.GetElapsedTime(startedAt, moment).TotalSeconds, busy);
        var position = entries.FindLastIndex(item => item.At <= entry.At) + 1;
        entries.Insert(position, entry);
        length += added;
        return RecordOutcome.Recorded;
    }

    // 生成脚本文本。maxIdleSeconds 不为空时，超过它的空闲间隔按它计（去掉录制时的发呆时间）。
    public string Render(double? maxIdleSeconds = null, IEnumerable<string>? comments = null)
    {
        if (maxIdleSeconds is { } cap && (!double.IsFinite(cap) || cap < MinWait || cap > MaxWait))
            throw new ArgumentOutOfRangeException(nameof(maxIdleSeconds), $"空闲等待上限须为 {MinWait} 至 {MaxWait} 秒。");
        var text = new StringBuilder();
        foreach (var comment in comments ?? [])
            foreach (var part in comment.ReplaceLineEndings("\n").Split('\n'))
                text.Append("# ").Append(part.Trim()).Append('\n');
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            if (index > 0)
            {
                var previous = entries[index - 1];
                var gap = entry.At - previous.At - previous.Busy;
                if (maxIdleSeconds is { } limit)
                    gap = Math.Min(gap, limit);
                text.Append("等待 ").Append(Number(WaitSeconds(gap))).Append('\n');
            }
            if (entry.Note is not null)
                text.Append("# ").Append(entry.Note).Append('\n');
            text.Append(entry.Line).Append('\n');
        }
        return text.ToString();
    }

    // 间隔取整：10 秒以内保留一位小数，之后取整秒；最少 0.05 秒，最多 3600 秒。
    public static double WaitSeconds(double gap)
    {
        if (double.IsNaN(gap) || gap < MinWait)
            return MinWait;
        if (gap >= MaxWait)
            return MaxWait;
        var rounded = gap < 10 ? Math.Round(gap, 1, MidpointRounding.AwayFromZero) : Math.Round(gap, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, MinWait, MaxWait);
    }

    // 脚本里的数字一律用不带千分位的点号小数，最多三位，与系统区域设置无关；加 0.0 把 -0 变成 0。
    public static string Number(double value) =>
        (Math.Round(value, 3, MidpointRounding.AwayFromZero) + 0.0).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

    public static string? Line(FleetAction action, ActionParameters parameters) => Line(action, parameters, out _);

    private static string? Line(FleetAction action, ActionParameters parameters, out string? note)
    {
        note = null;
        if (!Enum.IsDefined(action) || action == FleetAction.Install)
            return null;
        var p = FleetValidation.Action(action, parameters, "");
        string Point(string command) => $"{command} {Number(p.X!.Value)} {Number(p.Y!.Value)}";
        return action switch
        {
            FleetAction.Tap => Point("点击"),
            FleetAction.DoubleTap => Point("双击"),
            FleetAction.LongPress => $"{Point("长按")} {Seconds(p.Duration, 0.2)}",
            FleetAction.Swipe => SwipeLine(p),
            FleetAction.Home => "主屏",
            FleetAction.Lock => "锁屏",
            FleetAction.Unlock => "解锁",
            FleetAction.VolumeUp => "音量加",
            FleetAction.VolumeDown => "音量减",
            FleetAction.AppSwitcher => "多任务",
            FleetAction.Text => "输入 " + SingleLine(p.Text!, out note),
            FleetAction.Clipboard => "剪贴板 " + SingleLine(p.Text!, out note),
            FleetAction.OpenUrl => "打开 " + p.Text,
            FleetAction.Launch => "启动 " + p.BundleId,
            FleetAction.Terminate => "关闭 " + p.BundleId,
            _ => null
        };
    }

    private static readonly (string Command, double FromX, double FromY, double ToX, double ToY, double Duration)[] Presets =
    [
        ("上滑", .5, .78, .5, .25, .4),
        ("下滑", .5, .25, .5, .78, .4),
        ("左滑", .8, .5, .2, .5, .4),
        ("右滑", .2, .5, .8, .5, .4),
        ("返回", 0, .5, .7, .5, .05)
    ];

    private static string SwipeLine(ActionParameters p)
    {
        static bool Same(double expected, double? actual) => Math.Abs(expected - Math.Round(actual!.Value, 3)) < 1e-9;
        foreach (var preset in Presets)
            if (Same(preset.FromX, p.FromX) && Same(preset.FromY, p.FromY) && Same(preset.ToX, p.ToX) && Same(preset.ToY, p.ToY)
                && Same(preset.Duration, p.Duration))
                return preset.Command;
        return $"滑动 {Number(p.FromX!.Value)} {Number(p.FromY!.Value)} {Number(p.ToX!.Value)} {Number(p.ToY!.Value)} {Seconds(p.Duration, 0.05)}";
    }

    private static string Seconds(double value, double minimum) =>
        Number(Math.Clamp(Math.Round(value, 2, MidpointRounding.AwayFromZero), minimum, 5));

    // 脚本一行一个命令，文字里的换行无法写入，改成空格并加注释说明。
    private static string SingleLine(string text, out string? note)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        note = lines.Length > 1 ? "原文含换行，脚本每行一个命令，换行已改为空格" : null;
        return string.Join(' ', lines);
    }
}
