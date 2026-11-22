using System.Text.Json;
using Anthropic.Models.Beta.Messages;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Agents;

// 模型请求的一步操作：设备动作、等待或结束。坐标已换算为 0–1 相对位置并经过校验。
public sealed record AgentAction(string Description, FleetAction? Action, ActionParameters Parameters,
    double WaitSeconds = 0, bool Finished = false, bool Success = false, string? Summary = null);

public static class AgentTools
{
    private static JsonElement Schema(object value) => JsonSerializer.SerializeToElement(value);
    private static readonly object Pixel = new { type = "integer", minimum = 0, description = "Pixel coordinate in the latest screenshot" };

    private static BetaTool Tool(string name, string description, Dictionary<string, object> properties) => new()
    {
        Name = name,
        Description = description,
        InputSchema = new()
        {
            Properties = properties.ToDictionary(item => item.Key, item => Schema(item.Value)),
            Required = [.. properties.Keys],
        },
    };

    public static IReadOnlyList<BetaTool> Definitions
    {
        get;
    } =
    [
        Tool("tap", "Tap once at a point of the latest screenshot.", new() { ["x"] = Pixel, ["y"] = Pixel }),
        Tool("double_tap", "Double-tap at a point of the latest screenshot.", new() { ["x"] = Pixel, ["y"] = Pixel }),
        Tool("long_press", "Touch and hold at a point.", new()
        {
            ["x"] = Pixel, ["y"] = Pixel,
            ["seconds"] = new { type = "number", minimum = 0.2, maximum = 5, description = "Hold duration" }
        }),
        Tool("swipe", "Drag a finger from one point to another, e.g. to scroll. Scrolling down means swiping up.", new()
        {
            ["from_x"] = Pixel, ["from_y"] = Pixel, ["to_x"] = Pixel, ["to_y"] = Pixel,
            ["seconds"] = new { type = "number", minimum = 0.05, maximum = 5, description = "Swipe duration, 0.3 is typical" }
        }),
        Tool("type_text", "Type text into the focused text field. Tap the field first if no keyboard is visible.",
            new() { ["text"] = new { type = "string", maxLength = 1000 } }),
        Tool("press_button", "Press a system button or gesture.", new()
        {
            ["button"] = new { type = "string", @enum = new[] { "home", "back", "app_switcher", "lock", "unlock", "volume_up", "volume_down" } }
        }),
        Tool("launch_app", "Launch an installed app by bundle identifier, e.g. com.apple.Preferences.",
            new() { ["bundle_id"] = new { type = "string" } }),
        Tool("open_url", "Open a URL or app link on the phone.", new() { ["url"] = new { type = "string" } }),
        Tool("wait", "Wait for the screen to finish loading.",
            new() { ["seconds"] = new { type = "number", minimum = 0.5, maximum = 10 } }),
        Tool("finish", "Call when the task is done or cannot be done. Nothing else runs after this call.", new()
        {
            ["success"] = new { type = "boolean" },
            ["summary"] = new { type = "string", description = "Short result for the user, in the language of their instruction" }
        }),
    ];

    // 把模型的工具调用换算成设备动作；参数不合法时抛出 ArgumentException，由调用方作为错误结果回传给模型。
    public static AgentAction Parse(string name, IReadOnlyDictionary<string, JsonElement> input, int width, int height)
    {
        if (width < 2 || height < 2)
            throw new ArgumentException("Screenshot size is unknown.");
        double X(string key) => Coordinate(input, key, width);
        double Y(string key) => Coordinate(input, key, height);
        AgentAction Act(string description, FleetAction action, ActionParameters parameters) =>
            new(description, action, FleetValidation.Action(action, parameters, ""));
        return name switch
        {
            "tap" => Act($"点击 ({Int(input, "x")}, {Int(input, "y")})", FleetAction.Tap, new(X: X("x"), Y: Y("y"))),
            "double_tap" => Act($"双击 ({Int(input, "x")}, {Int(input, "y")})", FleetAction.DoubleTap, new(X: X("x"), Y: Y("y"))),
            "long_press" => Act($"长按 ({Int(input, "x")}, {Int(input, "y")})", FleetAction.LongPress,
                new(X: X("x"), Y: Y("y"), Duration: Number(input, "seconds"))),
            "swipe" => Act($"滑动 ({Int(input, "from_x")}, {Int(input, "from_y")}) → ({Int(input, "to_x")}, {Int(input, "to_y")})",
                FleetAction.Swipe, new(FromX: X("from_x"), FromY: Y("from_y"), ToX: X("to_x"), ToY: Y("to_y"),
                    Duration: Number(input, "seconds"))),
            "type_text" => Act("输入文字", FleetAction.Text, new(Text: Text(input, "text"))),
            "press_button" => Button(Text(input, "button")),
            "launch_app" => Act("启动 " + Text(input, "bundle_id"), FleetAction.Launch, new(BundleId: Text(input, "bundle_id"))),
            "open_url" => Act("打开链接", FleetAction.OpenUrl, new(Text: Text(input, "url"))),
            "wait" => new($"等待 {Number(input, "seconds"):0.#} 秒", null, new(), Range(Number(input, "seconds"), 0.5, 10)),
            "finish" => new("结束", null, new(), Finished: true,
                Success: input.TryGetValue("success", out var ok) && ok.ValueKind == JsonValueKind.True,
                Summary: Text(input, "summary")),
            _ => throw new ArgumentException($"Unknown tool {name}.")
        };
    }

    private static AgentAction Button(string button)
    {
        AgentAction Act(string description, FleetAction action, ActionParameters? parameters = null) =>
            new(description, action, FleetValidation.Action(action, parameters ?? new(), ""));
        return button switch
        {
            "home" => Act("回到主屏", FleetAction.Home),
            "back" => Act("返回", FleetAction.Swipe, new(FromX: 0, FromY: .5, ToX: .7, ToY: .5, Duration: .05)),
            "app_switcher" => Act("多任务", FleetAction.AppSwitcher),
            "lock" => Act("锁屏", FleetAction.Lock),
            "unlock" => Act("解锁", FleetAction.Unlock),
            "volume_up" => Act("音量加", FleetAction.VolumeUp),
            "volume_down" => Act("音量减", FleetAction.VolumeDown),
            _ => throw new ArgumentException($"Unknown button {button}.")
        };
    }

    private static double Coordinate(IReadOnlyDictionary<string, JsonElement> input, string key, int size)
    {
        var value = Int(input, key);
        if (value < 0 || value >= size)
            throw new ArgumentException($"{key}={value} is outside the screenshot (0-{size - 1}).");
        return (double)value / (size - 1);
    }

    private static int Int(IReadOnlyDictionary<string, JsonElement> input, string key) =>
        input.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number)
            && double.IsFinite(number) ? (int)Math.Round(number) : throw new ArgumentException($"Missing number {key}.");

    private static double Number(IReadOnlyDictionary<string, JsonElement> input, string key) =>
        input.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number)
            && double.IsFinite(number) ? number : throw new ArgumentException($"Missing number {key}.");

    private static string Text(IReadOnlyDictionary<string, JsonElement> input, string key) =>
        input.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text
            ? text : throw new ArgumentException($"Missing text {key}.");

    private static double Range(double value, double min, double max) =>
        value >= min && value <= max ? value : throw new ArgumentException($"Value {value} must be between {min} and {max}.");
}
