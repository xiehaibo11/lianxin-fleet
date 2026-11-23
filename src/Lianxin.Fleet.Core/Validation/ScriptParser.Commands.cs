using System.Globalization;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Validation;

// 单行命令：原有的设备动作与等待，以及元素、图色、日志、截图等扩展命令。
public static partial class ScriptParser
{
    public const double DefaultTimeout = 10;
    public const double DefaultSimilarity = 0.9;
    public const int DefaultTolerance = 16;

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["点击"] = "tap",
        ["双击"] = "doubletap",
        ["长按"] = "longpress",
        ["滑动"] = "swipe",
        ["上滑"] = "swipeup",
        ["下滑"] = "swipedown",
        ["左滑"] = "swipeleft",
        ["右滑"] = "swiperight",
        ["返回"] = "back",
        ["输入"] = "text",
        ["剪贴板"] = "clipboard",
        ["打开"] = "openurl",
        ["启动"] = "launch",
        ["关闭"] = "terminate",
        ["等待"] = "wait",
        ["主屏"] = "home",
        ["锁屏"] = "lock",
        ["解锁"] = "unlock",
        ["音量加"] = "volumeup",
        ["音量减"] = "volumedown",
        ["多任务"] = "appswitcher",
        ["重复"] = "repeat",
        ["如果"] = "if",
        ["否则"] = "else",
        ["当"] = "while",
        ["结束"] = "end",
        ["跳出"] = "break",
        ["停止"] = "stop",
        ["失败"] = "fail",
        ["点击元素"] = "tapelement",
        ["等待元素"] = "waitelement",
        ["等待消失"] = "waitgone",
        ["输入到"] = "inputto",
        ["点击颜色"] = "tapcolor",
        ["点击图片"] = "tapimage",
        ["等待图片"] = "waitimage",
        ["随机等待"] = "randomwait",
        ["日志"] = "log",
        ["截图"] = "capture"
    };

    private static ScriptStep Command(int number, string label, string command, string word, string rest)
    {
        var args = rest.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        ScriptStep Step(FleetAction action, ActionParameters parameters) =>
            new(number, label, action, FleetValidation.Action(action, parameters, ""));
        ScriptStep Swipe(double fromX, double fromY, double toX, double toY, double seconds = 0.4) =>
            Step(FleetAction.Swipe, new(FromX: fromX, FromY: fromY, ToX: toX, ToY: toY, Duration: seconds));
        ScriptStep Special(ScriptStepKind kind) => new(number, label, null, new())
        {
            Kind = kind
        };
        return command switch
        {
            "tap" => Step(FleetAction.Tap, new(X: Number(args, 0, 2), Y: Number(args, 1, 2))),
            "doubletap" => Step(FleetAction.DoubleTap, new(X: Number(args, 0, 2), Y: Number(args, 1, 2))),
            "longpress" => Step(FleetAction.LongPress, new(X: Number(args, 0, 2, 3), Y: Number(args, 1, 2, 3),
                Duration: args.Length > 2 ? Number(args, 2, 2, 3) : 1)),
            "swipe" => Swipe(Number(args, 0, 4, 5), Number(args, 1, 4, 5), Number(args, 2, 4, 5), Number(args, 3, 4, 5),
                args.Length > 4 ? Number(args, 4, 4, 5) : 0.4),
            "swipeup" => Swipe(.5, .78, .5, .25),
            "swipedown" => Swipe(.5, .25, .5, .78),
            "swipeleft" => Swipe(.8, .5, .2, .5),
            "swiperight" => Swipe(.2, .5, .8, .5),
            "back" => Swipe(0, .5, .7, .5, .05),
            "text" => Step(FleetAction.Text, new(Text: Required(rest))),
            "clipboard" => Step(FleetAction.Clipboard, new(Text: Required(rest))),
            "openurl" => Step(FleetAction.OpenUrl, new(Text: Required(rest))),
            "launch" => Step(FleetAction.Launch, new(BundleId: Required(rest))),
            "terminate" => Step(FleetAction.Terminate, new(BundleId: Required(rest))),
            "home" => Step(FleetAction.Home, new()),
            "lock" => Step(FleetAction.Lock, new()),
            "unlock" => Step(FleetAction.Unlock, new()),
            "volumeup" => Step(FleetAction.VolumeUp, new()),
            "volumedown" => Step(FleetAction.VolumeDown, new()),
            "appswitcher" => Step(FleetAction.AppSwitcher, new()),
            "wait" => new(number, label, null, new(), Wait(Number(args, 0, 1))),
            "stop" => args.Length == 0 ? Special(ScriptStepKind.Stop) : throw new ArgumentException("「停止」后面不能有其他内容。"),
            "fail" => Special(ScriptStepKind.Fail) with { Text = rest.Length == 0 ? null : FleetValidation.Text(rest, 200, "失败原因") },
            "randomwait" => RandomWait(Special(ScriptStepKind.RandomWait), args),
            "log" => Special(ScriptStepKind.Log) with { Text = FleetValidation.Text(Required(rest), 200, "日志内容") },
            "capture" => Special(ScriptStepKind.Capture) with { Text = CaptureName(args) },
            "tapelement" => Special(ScriptStepKind.TapProbe) with { Probe = SoleElement(rest) },
            "waitelement" or "waitgone" => ElementWait(Special(command == "waitelement" ? ScriptStepKind.WaitProbe
                : ScriptStepKind.WaitProbeGone), rest),
            "inputto" => InputTo(Special(ScriptStepKind.InputToElement), rest),
            "tapcolor" => Special(ScriptStepKind.TapProbe) with { Probe = ColorSearch(args) },
            "tapimage" => Special(ScriptStepKind.TapProbe) with { Probe = ImageProbe(args, 1, 2) },
            "waitimage" => ImageWait(Special(ScriptStepKind.WaitProbe), args),
            _ => throw new ArgumentException($"无法识别的命令「{word}」。")
        };
    }

    private static ScriptStep RandomWait(ScriptStep step, string[] args)
    {
        double low = Wait(Number(args, 0, 2)), high = Wait(Number(args, 1, 2));
        if (low > high)
            throw new ArgumentException("随机等待的最短时间不能大于最长时间。");
        return step with
        {
            WaitSeconds = low,
            MaxWaitSeconds = high
        };
    }

    // 等待元素 <选择器> [超时秒]；等待消失同理。
    private static ScriptStep ElementWait(ScriptStep step, string rest)
    {
        var probe = ElementProbe(rest, out var left);
        var args = left.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        return step with
        {
            Probe = probe,
            TimeoutSeconds = args.Length == 0 ? DefaultTimeout : Timeout(Number(args, 0, 1))
        };
    }

    private static ScriptStep InputTo(ScriptStep step, string rest)
    {
        var probe = ElementProbe(rest, out var left);
        return step with
        {
            Probe = probe,
            Text = FleetValidation.Text(left.Length > 0 ? left : throw new ArgumentException("「输入到」的选择器后面缺少要输入的文字。"),
                1000, "输入文本")
        };
    }

    // 等待图片 名称 [超时秒] [相似度]
    private static ScriptStep ImageWait(ScriptStep step, string[] args)
    {
        if (args.Length is < 1 or > 3)
            throw new ArgumentException("格式为：等待图片 名称 [超时秒] [相似度]。");
        return step with
        {
            Probe = ImageProbe(args.Length == 3 ? [args[0], args[2]] : args[..1], 1, 2),
            TimeoutSeconds = args.Length > 1 ? Timeout(Number(args[1..2], 0, 1)) : DefaultTimeout
        };
    }

    private static string? CaptureName(string[] args) => args.Length switch
    {
        0 => null,
        1 => TemplateName(args[0]),
        _ => throw new ArgumentException("截图名称不能包含空格。")
    };

    private static string Required(string value) =>
        value.Length > 0 ? value : throw new ArgumentException("命令后面缺少内容。");

    private static double Number(string[] args, int index, params int[] allowedCounts)
    {
        if (!allowedCounts.Contains(args.Length))
            throw new ArgumentException($"参数个数应为 {string.Join(" 或 ", allowedCounts)} 个。");
        return ParseNumber(args[index]);
    }

    private static double ParseNumber(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)
            ? value : throw new ArgumentException($"「{text}」不是有效数字。");

    private static int Integer(string text, int min, int max, string label) =>
        int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= min && value <= max
            ? value : throw new ArgumentException($"{label}须为 {min} 至 {max} 的整数。");

    private static double Wait(double seconds) => seconds is >= 0.05 and <= 3600
        ? seconds : throw new ArgumentException("等待时间须为 0.05 至 3600 秒。");

    private static double Timeout(double seconds) => seconds is >= 0.5 and <= 3600
        ? seconds : throw new ArgumentException("超时时间须为 0.5 至 3600 秒。");
}
