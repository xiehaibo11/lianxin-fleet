using System.Globalization;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Validation;

// 脚本按行书写，每行一个命令，# 或 // 开头为注释。坐标是 0–1 的相对位置，时间单位为秒。
public static class ScriptParser
{
    public const int MaxSourceLength = 20_000;
    public const int MaxSteps = 500;

    public const string Help = """
        每行一个命令，# 开头为注释；坐标是 0–1 的相对位置（左上角 0 0，右下角 1 1），时间单位为秒。
        点击 x y            双击 x y            长按 x y [秒]
        滑动 x1 y1 x2 y2 [秒]   上滑 / 下滑 / 左滑 / 右滑   返回
        输入 文字           剪贴板 文字         打开 https://example.com
        启动 BundleID       关闭 BundleID       等待 秒
        主屏  锁屏  解锁  音量加  音量减  多任务
        英文命令同样可用：tap doubletap longpress swipe text clipboard openurl launch terminate wait home lock unlock volumeup volumedown appswitcher back
        """;

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
        ["多任务"] = "appswitcher"
    };

    public static IReadOnlyList<ScriptStep> Parse(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("脚本内容不能为空。");
        if (source.Length > MaxSourceLength)
            throw new ArgumentException($"脚本最多 {MaxSourceLength} 个字符。");
        var steps = new List<ScriptStep>();
        var lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("//", StringComparison.Ordinal))
                continue;
            try
            {
                steps.Add(ParseLine(index + 1, line));
            }
            catch (Exception error) when (error is ArgumentException or FormatException)
            {
                throw new ArgumentException($"第 {index + 1} 行：{error.Message}", error);
            }
            if (steps.Count > MaxSteps)
                throw new ArgumentException($"脚本最多 {MaxSteps} 步。");
        }
        if (steps.Count == 0)
            throw new ArgumentException("脚本没有可执行的命令。");
        return steps;
    }

    private static ScriptStep ParseLine(int number, string line)
    {
        var space = line.IndexOfAny([' ', '\t']);
        var word = space < 0 ? line : line[..space];
        var rest = space < 0 ? "" : line[(space + 1)..].Trim();
        var command = Aliases.TryGetValue(word, out var alias) ? alias : word.ToLowerInvariant();
        var label = line.Length > 40 ? line[..40] + "…" : line;
        var args = rest.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        ScriptStep Step(FleetAction action, ActionParameters parameters) =>
            new(number, label, action, FleetValidation.Action(action, parameters, ""));
        ScriptStep Swipe(double fromX, double fromY, double toX, double toY, double seconds = 0.4) =>
            Step(FleetAction.Swipe, new(FromX: fromX, FromY: fromY, ToX: toX, ToY: toY, Duration: seconds));
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
            _ => throw new ArgumentException($"无法识别的命令「{word}」。")
        };
    }

    private static string Required(string value) =>
        value.Length > 0 ? value : throw new ArgumentException("命令后面缺少内容。");

    private static double Number(string[] args, int index, params int[] allowedCounts)
    {
        if (!allowedCounts.Contains(args.Length))
            throw new ArgumentException($"参数个数应为 {string.Join(" 或 ", allowedCounts)} 个。");
        if (!double.TryParse(args[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            || !double.IsFinite(value))
            throw new ArgumentException($"「{args[index]}」不是有效数字。");
        return value;
    }

    private static double Wait(double seconds) => seconds is >= 0.05 and <= 3600
        ? seconds : throw new ArgumentException("等待时间须为 0.05 至 3600 秒。");
}
