using System.Text.Json;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.OpenApi;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class OpenApiChecks
{
    // 动作接口：截图像素坐标换算到 WDA 点坐标、时长换算、参数校验与桥接错误。
    public static async Task Actions()
    {
        await using var api = await ApiHarness.StartAsync();
        var bridge = api.Bridge;
        Dictionary<string, object?> At(object x, object y, params (string, object?)[] more) => J([("deviceId", Udid), ("x", x), ("y", y), .. more]);
        // 390×844 点、截图 1170×2532（3 倍）：像素 (585, 1266) 对应点 (195, 422)。
        await api.OkAsync("/openapi/clickPoint", At(585, 1266));
        var tap = bridge.Last();
        Assert.Equal(FleetAction.Tap, tap.Action);
        Assert.Equal(195.0 / 389, tap.Parameters.X);
        Assert.Equal(422.0 / 843, tap.Parameters.Y);
        Assert.Equal(195, (int)Math.Round(tap.Parameters.X!.Value * 389, MidpointRounding.AwayFromZero));
        Assert.Equal(1, bridge.ScreenshotCount);
        await api.OkAsync("/openapi/doubleClickPoint", At("0", "2532"));
        Assert.True(bridge.Last() is { Action: FleetAction.DoubleTap, Parameters: { X: 0.0, Y: 1.0 } }, "edges and string numbers");
        Assert.Equal(1, bridge.ScreenshotCount);
        await api.OkAsync("/openapi/longClickPoint", At(300, 600, ("duration", 1500)));
        Assert.True(bridge.Last() is { Action: FleetAction.LongPress, Parameters.Duration: 1.5 }, "long press seconds");
        await api.OkAsync("/openapi/press", At(300, 600));
        Assert.Equal(1.0, bridge.Last().Parameters.Duration);
        await api.OkAsync("/openapi/swipeToPoint", J(("deviceId", Udid), ("startX", 300), ("startY", 2100), ("endX", 300), ("endY", 30), ("duration", 1000)));
        var swipe = bridge.Last();
        Assert.True(swipe.Action == FleetAction.Swipe && swipe.Parameters.Duration == 1.0, "swipe");
        Assert.Equal(100.0 / 389, swipe.Parameters.FromX);
        Assert.Equal(700.0 / 843, swipe.Parameters.FromY);
        Assert.Equal(10.0 / 843, swipe.Parameters.ToY);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/clickPoint", At(1171, 10))).Code);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/clickPoint", J(("deviceId", Udid), ("x", 10)))).Code);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/longClickPoint", At(1, 1, ("duration", 100)))).Code);

        // 横屏：点尺寸变为 844×390，比例沿用缓存，坐标系跟随当前方向。
        bridge.Status = new(true, 844, 390);
        await api.OkAsync("/openapi/clickPoint", At(2531, 1169));
        Assert.True(bridge.Last().Parameters is { X: 1.0, Y: 1.0 }, "landscape edge");
        Assert.Equal(1, bridge.ScreenshotCount);
        Assert.Equal(2, (await api.OkAsync("/openapi/getOrientation", J(("deviceId", Udid)))).GetInt32());
        var adjusted = await api.OkAsync("/openapi/adjustScreenOrientation", J(("deviceId", Udid), ("orientation", 0)));
        Assert.Equal(2532, adjusted.GetProperty("screenWidth").GetInt32());
        Assert.Equal(OpenApiCodes.Unsupported, (await api.PostAsync("/openapi/adjustScreenOrientation", J(("deviceId", Udid), ("orientation", 1)))).Code);
        // 放大显示：点尺寸长边变化后重新测量比例（2532/693 不是整数）。
        bridge.Status = new(true, 320, 693);
        var info = await api.OkAsync("/openapi/getDeviceInfo", J(("deviceId", Udid)));
        Assert.Equal(2, bridge.ScreenshotCount);
        Assert.Equal(1169, info.GetProperty("screenWidth").GetInt32());
        Assert.Equal(2532, info.GetProperty("screenHeight").GetInt32());
        Assert.Equal(1, info.GetProperty("orientation").GetInt32());
        Assert.Equal(JsonValueKind.Null, info.GetProperty("battery").ValueKind);
        await api.OkAsync("/openapi/clickPoint", At(585, 0));
        Assert.Equal(160, (int)Math.Round(bridge.Last().Parameters.X!.Value * 319, MidpointRounding.AwayFromZero));
        bridge.Status = new(true, 390, 844);

        await api.OkAsync("/openapi/inputText", J(("deviceId", Udid), ("content", "你好 hello"), ("duration", 1000)));
        Assert.True(bridge.Last() is { Action: FleetAction.Text, Parameters.Text: "你好 hello" }, "input");
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/typingText", J(("deviceId", Udid), ("content", "")))).Code);
        var app = J(("deviceId", Udid), ("bundleId", "rn.notes.best"));
        Assert.Equal(0, (await api.OkAsync("/openapi/appLaunch", app)).GetInt32());
        Assert.True(bridge.Last() is { Action: FleetAction.Launch, Parameters.BundleId: "rn.notes.best" }, "launch");
        Assert.True((await api.OkAsync("/openapi/stopApp", app)).GetBoolean(), "stop");
        Assert.Equal(FleetAction.Terminate, bridge.Last().Action);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/openApp", J(("deviceId", Udid), ("bundleId", "not a bundle")))).Code);
        foreach (var (path, action) in new[] { ("home", FleetAction.Home), ("lockScreen", FleetAction.Lock), ("unlockScreen", FleetAction.Unlock) })
        {
            await api.OkAsync("/openapi/" + path, J(("deviceId", Udid)));
            Assert.Equal(action, bridge.Last().Action);
        }
        var before = bridge.Calls.Count;
        await api.OkAsync("/openapi/homeScreen", J(("deviceId", Udid)));
        Assert.True(bridge.Calls.Skip(before).Select(call => call.Action).SequenceEqual([FleetAction.Unlock, FleetAction.Home]), "homeScreen");
        Assert.Equal(0, (await api.OkAsync("/openapi/appHelper/setPasteboard", J(("deviceId", Udid), ("content", "剪贴板"), ("openApp", true)))).GetInt32());
        Assert.True(bridge.Last() is { Action: FleetAction.Clipboard, Parameters.Text: "剪贴板" }, "pasteboard");
        await api.OkAsync("/openapi/appHelper/openUrl", J(("deviceId", Udid), ("url", "https://www.example.com"), ("openApp", true)));
        Assert.True(bridge.Last() is { Action: FleetAction.OpenUrl, Parameters.Text: "https://www.example.com" }, "url");

        // 安装只接受批准目录内的安装包，目录外的路径到不了桥接。
        var outside = Path.Combine(Path.GetDirectoryName(api.Apps)!, "outside.ipa");
        File.WriteAllText(outside, "x");
        before = bridge.Calls.Count;
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/installApp", J(("deviceId", Udid), ("bundleId", "a.b"), ("path", outside)))).Code);
        Assert.Equal(before, bridge.Calls.Count);
        var inside = Path.Combine(api.Apps, "央视频_2.4.3.ipa");
        File.WriteAllText(inside, "x");
        Assert.Equal("ok", (await api.OkAsync("/openapi/installApp", J(("deviceId", Udid), ("bundleId", "a.b"), ("path", inside)))).GetString());
        Assert.True(bridge.Last() is { Action: FleetAction.Install } install && install.Parameters.AppPath == Path.GetFullPath(inside), "install");

        // 自动化未就绪、桥接失败与结果待确认分别返回不同的 code。
        bridge.Status = new(false, Error: "WDA 尚未就绪。");
        var offline = await api.PostAsync("/openapi/clickPoint", At(1, 1));
        Assert.True(offline.Code == OpenApiCodes.DeviceFailed && offline.Msg.Contains("自动化"), offline.Text);
        Assert.True(!(await api.OkAsync("/openapi/isServiceOk", J(("deviceId", Udid)))).GetBoolean(), "service not ok");
        bridge.Status = new(true, 390, 844);
        Assert.True((await api.OkAsync("/openapi/isServiceOk", J(("deviceId", Udid)))).GetBoolean(), "service ok");
        bridge.Execute = (_, _) => throw new DeviceOperationException("WDA 拒绝执行：测试", uncertain: false);
        Assert.Equal(OpenApiCodes.DeviceFailed, (await api.PostAsync("/openapi/home", J(("deviceId", Udid)))).Code);
        bridge.Execute = (_, _) => throw new DeviceOperationException("无法确认 WDA 响应", uncertain: true);
        Assert.Equal(OpenApiCodes.Uncertain, (await api.PostAsync("/openapi/home", J(("deviceId", Udid)))).Code);
        bridge.Execute = null;
    }
}
