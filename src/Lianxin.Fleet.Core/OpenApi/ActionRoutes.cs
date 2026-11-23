using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.OpenApi;

// 动作接口（advance/openapi/action）与辅助助手里联信能用 WDA 完成的剪贴板、打开链接。
// 坐标是截图像素（随横竖屏变化），时长单位为毫秒；直接经设备桥接执行，不进任务队列。
internal static class ActionRoutes
{
    public static void Register(OpenApiRouter router) => router
        .Post("/openapi/clickPoint", call => Point(call, FleetAction.Tap))
        .Post("/openapi/doubleClickPoint", call => Point(call, FleetAction.DoubleTap))
        .Post("/openapi/longClickPoint", Press)
        .Post("/openapi/press", Press)
        .Post("/openapi/swipeToPoint", Swipe)
        .Post("/openapi/typingText", call => Text(call, FleetAction.Text))
        .Post("/openapi/inputText", call => Text(call, FleetAction.Text))
        .Post("/openapi/appLaunch", call => App(call, FleetAction.Launch, 0, "已启动应用（联信不返回进程号，data 固定为 0）。"))
        .Post("/openapi/openApp", call => App(call, FleetAction.Launch, true, "已打开应用。"))
        .Post("/openapi/stopApp", call => App(call, FleetAction.Terminate, true, "已关闭应用。"))
        .Post("/openapi/appKillByBundleId", call => App(call, FleetAction.Terminate, true, "已结束应用。"))
        .Post("/openapi/installApp", Install)
        .Post("/openapi/home", call => Simple(call, FleetAction.Home))
        .Post("/openapi/homeScreen", HomeScreen)
        .Post("/openapi/lockScreen", call => Simple(call, FleetAction.Lock))
        .Post("/openapi/unlockScreen", call => Simple(call, FleetAction.Unlock))
        .Post("/openapi/getOrientation", Orientation)
        .Post("/openapi/adjustScreenOrientation", AdjustOrientation)
        .Post("/openapi/appHelper/setPasteboard", Pasteboard)
        .Post("/openapi/appHelper/openUrl", OpenUrl)
        .Post("/openapi/uninstallApp", Uninstall)
        .Post("/openapi/reboot", Reboot)
        .Post("/openapi/dumpXml", DumpXml)
        .Post("/openapi/isLocked", IsLocked)
        .Post("/openapi/setOrientation", SetOrientation)
        .Unsupported("/openapi/setAssistiveTouch", "开关小白点（需要 EasyClick 代理 IPA）。")
        .Unsupported("/openapi/multiTouch", "多点触摸（WDA 通道目前只提供单指手势）。")
        .Unsupported("/openapi/ioHIDEvent", "HID 键盘事件（需要 EasyClick 代理 IPA）。");

    private static async Task<OpenApiReply> Point(OpenApiCall call, FleetAction action)
    {
        var device = call.Device();
        double x = call.RequiredNumber("x"), y = call.RequiredNumber("y");
        var screen = await call.Backend.Geometry.MeasureAsync(device, call.Cancellation).ConfigureAwait(false);
        await Execute(call, device, action, new(X: screen.X(x), Y: screen.Y(y))).ConfigureAwait(false);
        return OpenApiReply.Ok();
    }

    // 长按时长 0.2–5 秒（200–5000 毫秒），缺省 1000 毫秒。
    private static async Task<OpenApiReply> Press(OpenApiCall call)
    {
        var device = call.Device();
        double x = call.RequiredNumber("x"), y = call.RequiredNumber("y");
        var seconds = (call.Number("duration") ?? 1000) / 1000;
        var screen = await call.Backend.Geometry.MeasureAsync(device, call.Cancellation).ConfigureAwait(false);
        await Execute(call, device, FleetAction.LongPress, new(X: screen.X(x), Y: screen.Y(y), Duration: seconds)).ConfigureAwait(false);
        return OpenApiReply.Ok();
    }

    // 滑动时长 0.05–5 秒（50–5000 毫秒），缺省 500 毫秒。
    private static async Task<OpenApiReply> Swipe(OpenApiCall call)
    {
        var device = call.Device();
        double fromX = call.RequiredNumber("startX"), fromY = call.RequiredNumber("startY");
        double toX = call.RequiredNumber("endX"), toY = call.RequiredNumber("endY");
        var seconds = (call.Number("duration") ?? 500) / 1000;
        var screen = await call.Backend.Geometry.MeasureAsync(device, call.Cancellation).ConfigureAwait(false);
        await Execute(call, device, FleetAction.Swipe, new(FromX: screen.X(fromX), FromY: screen.Y(fromY),
            ToX: screen.X(toX), ToY: screen.Y(toY), Duration: seconds)).ConfigureAwait(false);
        return OpenApiReply.Ok();
    }

    private static async Task<OpenApiReply> Text(OpenApiCall call, FleetAction action)
    {
        var device = call.Device();
        await Execute(call, device, action, new(Text: call.Text("content"))).ConfigureAwait(false);
        return OpenApiReply.Ok();
    }

    private static async Task<OpenApiReply> App(OpenApiCall call, FleetAction action, object data, string message)
    {
        var device = call.Device();
        await Execute(call, device, action, new(BundleId: call.Text("bundleId"))).ConfigureAwait(false);
        return OpenApiReply.Ok(data, message);
    }

    // path 必须位于中控的应用批准目录内；bundleId 只用于说明，安装以安装包为准。
    private static async Task<OpenApiReply> Install(OpenApiCall call)
    {
        var device = call.Device();
        ActionParameters parameters;
        try
        {
            parameters = FleetValidation.Action(FleetAction.Install, new(AppPath: call.Text("path")), call.Backend.AppDirectory);
        }
        // 路径校验的异常只带本程序自己的说明（目录外的路径在访问文件系统之前就被拒绝），作为参数错误返回。
        catch (Exception error) when (error is UnauthorizedAccessException or IOException)
        {
            throw new OpenApiException(OpenApiCodes.InvalidArgument, error.Message);
        }
        await Execute(call, device, FleetAction.Install, parameters).ConfigureAwait(false);
        return OpenApiReply.Ok("ok", "应用已安装。");
    }

    private static async Task<OpenApiReply> Simple(OpenApiCall call, FleetAction action)
    {
        await Execute(call, call.Device(), action, new()).ConfigureAwait(false);
        return OpenApiReply.Ok();
    }

    // 强制进入主页：先解锁（无锁屏密码时有效），再回到主屏。
    private static async Task<OpenApiReply> HomeScreen(OpenApiCall call)
    {
        var device = call.Device();
        await Execute(call, device, FleetAction.Unlock, new()).ConfigureAwait(false);
        try
        {
            await Execute(call, device, FleetAction.Home, new()).ConfigureAwait(false);
        }
        catch (DeviceOperationException error)
        {
            throw new DeviceOperationException("手机已解锁，但回到主屏失败：" + error.Message, error.Uncertain, error);
        }
        return OpenApiReply.Ok();
    }

    private static async Task<OpenApiReply> Orientation(OpenApiCall call)
    {
        var screen = await call.Backend.Geometry.MeasureAsync(call.Device(), call.Cancellation).ConfigureAwait(false);
        return OpenApiReply.Ok(screen.Orientation);
    }

    // 联信始终使用当前方向的坐标系：0（自动）或与当前方向一致时返回当前尺寸，强制另一方向不支持。
    private static async Task<OpenApiReply> AdjustOrientation(OpenApiCall call)
    {
        var device = call.Device();
        var wanted = (int)(call.Number("orientation") ?? 0);
        var screen = await call.Backend.Geometry.MeasureAsync(device, call.Cancellation).ConfigureAwait(false);
        if (wanted != 0 && wanted != screen.Orientation)
            return OpenApiReply.Fail(OpenApiCodes.Unsupported, "联信 Fleet 暂不支持强制坐标系方向，坐标始终跟随当前屏幕方向。");
        return OpenApiReply.Ok(new
        {
            orientation = screen.Orientation,
            screenWidth = screen.PixelWidth,
            screenHeight = screen.PixelHeight
        });
    }

    // 通过 WDA 写剪贴板，不需要也不会打开 EasyClick 辅助助手（openApp 参数被忽略）。
    private static async Task<OpenApiReply> Pasteboard(OpenApiCall call)
    {
        await Execute(call, call.Device(), FleetAction.Clipboard, new(Text: call.Text("content"))).ConfigureAwait(false);
        return OpenApiReply.Ok(0, "已写入剪贴板。");
    }

    private static async Task<OpenApiReply> OpenUrl(OpenApiCall call)
    {
        await Execute(call, call.Device(), FleetAction.OpenUrl, new(Text: call.Text("url"))).ConfigureAwait(false);
        return OpenApiReply.Ok(0, "已打开链接。");
    }

    // 参数在到达手机前统一校验（与脚本、任务队列相同的规则）。
    private static Task Execute(OpenApiCall call, FleetDevice device, FleetAction action, ActionParameters parameters)
    {
        var validated = FleetValidation.Action(action, parameters, call.Backend.AppDirectory);
        return call.Backend.Bridge.ExecuteAsync(device, action, validated, call.Cancellation);
    }

    // 经 USB 卸载，不需要自动化；应用数据一并删除，系统应用（com.apple.*）不能卸载。
    private static async Task<OpenApiReply> Uninstall(OpenApiCall call)
    {
        var device = call.Device();
        var bundleId = FleetValidation.BundleId(call.Text("bundleId"));
        await call.Backend.RequireManager().UninstallAppAsync(device, bundleId, call.Cancellation).ConfigureAwait(false);
        return OpenApiReply.Ok("ok", "应用已卸载。");
    }

    // 先停止这台手机的脚本和自动化。开机后开发者镜像失效，需要重新 mountDevImage 再 startAutoEnv。
    private static async Task<OpenApiReply> Reboot(OpenApiCall call)
    {
        var device = call.Device();
        var manager = call.Backend.RequireManager();
        await call.Backend.ReleaseAsync(device).ConfigureAwait(false);
        await OpenApiBackend.AfterReleaseAsync(manager.RestartAsync(device, call.Cancellation), "重启").ConfigureAwait(false);
        return OpenApiReply.Ok(true, "已发送重启指令，这台手机的脚本和自动化已停止。开机后请重新挂载开发者镜像并启动自动化。");
    }

    // 成功时响应体就是界面树 XML，失败时为 JSON 信封；节点坐标已换算成截图像素，可直接用于 clickPoint。
    private static async Task<OpenApiReply> DumpXml(OpenApiCall call)
    {
        var device = call.Device();
        var options = PageSourceDump.Options(call);
        var screen = await call.Backend.Geometry.MeasureAsync(device, call.Cancellation).ConfigureAwait(false);
        var source = await call.Backend.Bridge.GetPageSourceAsync(device, call.Cancellation).ConfigureAwait(false);
        return OpenApiReply.Xml(PageSourceDump.Filter(source, options, screen.Ratio));
    }

    private static async Task<OpenApiReply> IsLocked(OpenApiCall call)
    {
        var locked = await call.Backend.Bridge.IsLockedAsync(call.Device(), call.Cancellation).ConfigureAwait(false);
        return OpenApiReply.Ok(locked, locked ? "屏幕已锁定。" : "屏幕未锁定。");
    }

    // 1 竖屏，2 横屏。请求后等界面转完（最多约 3 秒）再返回，之后的坐标与截图都按新方向；
    // 当前界面不支持该方向（例如多数机型的主屏）或开启了竖排方向锁定时返回 1003。
    private static async Task<OpenApiReply> SetOrientation(OpenApiCall call)
    {
        var device = call.Device();
        var wanted = call.RequiredNumber("orientation") switch
        {
            1 => ScreenOrientation.Portrait,
            2 => ScreenOrientation.Landscape,
            _ => throw OpenApiCall.Invalid("orientation 只能是 1（竖屏）或 2（横屏）。")
        };
        var code = wanted == ScreenOrientation.Landscape ? 2 : 1;
        var geometry = call.Backend.Geometry;
        await geometry.MeasureAsync(device, call.Cancellation).ConfigureAwait(false);
        await call.Backend.Bridge.SetOrientationAsync(device, wanted, call.Cancellation).ConfigureAwait(false);
        for (var attempt = 0; attempt <= 12; attempt++)
        {
            var screen = await geometry.MeasureAsync(device, call.Cancellation).ConfigureAwait(false);
            if (screen.Orientation == code)
                return OpenApiReply.Ok(true, $"屏幕已{(code == 2 ? "横" : "竖")}屏，截图 {screen.PixelWidth}×{screen.PixelHeight}。");
            await Task.Delay(250, call.Cancellation).ConfigureAwait(false);
        }
        return OpenApiReply.Fail(OpenApiCodes.DeviceFailed,
            "已请求旋转，但界面 3 秒内没有转到该方向：当前应用可能不支持，或手机开启了竖排方向锁定。");
    }
}
