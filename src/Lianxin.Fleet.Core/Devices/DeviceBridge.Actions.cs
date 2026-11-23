using System.Text;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Devices;

public sealed partial class DeviceBridge
{
    public async Task ExecuteAsync(FleetDevice device, FleetAction action, ActionParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Validate(device, action != FleetAction.Install);
        if (!Enum.IsDefined(action))
            throw new ArgumentException("不支持此设备动作。");
        ArgumentNullException.ThrowIfNull(parameters);
        if (action == FleetAction.Install)
        {
            await host.InstallAsync(device, parameters.AppPath, cancellationToken).ConfigureAwait(false);
            return;
        }
        // 与服务层同一套校验，非法参数不会到达手机；安装路径由 HostDeviceTools 单独校验。
        parameters = FleetValidation.Action(action, parameters, "");
        await SerializedAsync(device, async () =>
        {
            var size = action is FleetAction.Tap or FleetAction.DoubleTap or FleetAction.LongPress
                or FleetAction.Swipe or FleetAction.AppSwitcher
                ? await DimensionsAsync(device, ActionTimeoutSeconds, cancellationToken).ConfigureAwait(false) : (Width: 1, Height: 1);
            var (path, body, session) = Route(action, parameters, size);
            var id = session ? await SessionAsync(device, cancellationToken).ConfigureAwait(false) : "";
            if (action == FleetAction.OpenUrl)
            {
                await OpenUrlAsync(device, id, parameters.Text!, cancellationToken).ConfigureAwait(false);
                return true;
            }
            if (action == FleetAction.Terminate)
            {
                await TerminateAsync(device, id, parameters.BundleId!, cancellationToken).ConfigureAwait(false);
                return true;
            }
            if (action == FleetAction.Text)
                await RequireTypingTargetAsync(device, id, cancellationToken).ConfigureAwait(false);
            await RequestAsync(device, (session ? "/session/" + id : "") + path, body, action: true,
                timeoutSeconds: ActionTimeoutSeconds, cancellationToken: cancellationToken).ConfigureAwait(false);
            // 按 Home 键立即返回，但回主屏动画约 0.4 秒：真机实测紧接着启动应用会被动画吞掉（应用留在后台、前台仍是主屏），
            // 间隔 0.3 秒以上都正常。在设备锁内等一下，这台手机的下一个动作一定在主屏稳定后才发出。
            if (action == FleetAction.Home)
                await Task.Delay(HomeSettle, cancellationToken).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    private static (string Path, object Body, bool Session) Route(FleetAction action, ActionParameters p,
        (int Width, int Height) size)
    {
        int X(double? value) => (int)Math.Round(value!.Value * (size.Width - 1), MidpointRounding.AwayFromZero);
        int Y(double? value) => (int)Math.Round(value!.Value * (size.Height - 1), MidpointRounding.AwayFromZero);
        return action switch
        {
            // 真机实测：/wda/homescreen 按完 Home 还要等主屏，约四次有一次让 XCTest 卡住约 60 秒（之后截图、查询全部超时）；
            // 直接按一次 Home 键 30 次都正常，也更快。
            FleetAction.Home => ("/wda/pressButton", new { name = "home" }, true),
            FleetAction.Lock => ("/wda/lock", new { }, false),
            FleetAction.Unlock => ("/wda/unlock", new { }, false),
            FleetAction.Clipboard => ("/wda/setPasteboard", new { content = Convert.ToBase64String(Encoding.UTF8.GetBytes(p.Text!)), contentType = "plaintext" }, false),
            FleetAction.Tap => ("/wda/tap", new { x = X(p.X), y = Y(p.Y) }, true),
            FleetAction.DoubleTap => ("/wda/doubleTap", new { x = X(p.X), y = Y(p.Y) }, true),
            FleetAction.LongPress => ("/wda/touchAndHold", new { x = X(p.X), y = Y(p.Y), duration = p.Duration }, true),
            FleetAction.Swipe when IsSideEdge(X(p.FromX), size.Width) => ("/wda/pressAndDragWithVelocity",
                EdgeDrag(X(p.FromX), Y(p.FromY), X(p.ToX), Y(p.ToY), p.Duration), true),
            FleetAction.Swipe => ("/actions", SwipeGesture(X(p.FromX), Y(p.FromY), X(p.ToX), Y(p.ToY), p.Duration), true),
            // 有 Home 键的 iPhone 要连按两次 Home 才打开多任务；从底边上滑在这些机型上打开的是控制中心。
            // WDA 两次按键之间至少隔 0.3 秒，系统不认作连按，所以明确拒绝。
            FleetAction.AppSwitcher when HasHomeButton(size) => throw new DeviceOperationException(
                "这台 iPhone 有 Home 键，打开多任务要连按两次 Home 键，WDA 按不了这么快，暂不支持；从底边上滑在这类机型上会打开控制中心。"),
            // shortcut: 全面屏机型从底边上滑后停住以打开多任务，参数未在真机校准；若打开的是主屏，调大 holdDuration 或降低 velocity。
            FleetAction.AppSwitcher => ("/wda/pressAndDragWithVelocity", new { fromX = X(.5), fromY = Y(1), toX = X(.5), toY = Y(.6), pressDuration = .05, holdDuration = .8, velocity = 1000 }, true),
            FleetAction.Text => ("/wda/keys", new { value = new[] { p.Text } }, true),
            FleetAction.Launch => ("/wda/apps/launch", new { bundleId = p.BundleId }, true),
            FleetAction.Terminate => ("/wda/apps/terminate", new { bundleId = p.BundleId }, true),
            FleetAction.OpenUrl => ("/url", new { url = p.Text }, true),
            FleetAction.VolumeUp => ("/wda/pressButton", new { name = "volumeUp" }, true),
            FleetAction.VolumeDown => ("/wda/pressButton", new { name = "volumeDown" }, true),
            _ => throw new ArgumentException("不支持此设备动作。")
        };
    }

    private static readonly TimeSpan HomeSettle = TimeSpan.FromMilliseconds(600);

    // 真机实测（iPhone 6s，集控投屏开着）：XCTest 偶尔卡 5–13 秒后照常执行动作。8 秒就放弃会把这种慢成功记成「待确认」，
    // 脚本停下而手机随后又执行了；按 20 秒等待。
    private const int ActionTimeoutSeconds = 20;

    // 有 Home 键的 iPhone（SE、6s–8 及 Plus）点尺寸长边不超过 736，全面屏机型都在 812 以上。
    public static bool HasHomeButton((int Width, int Height) size) =>
        Math.Max(size.Width, size.Height) <= 736 && Math.Min(size.Width, size.Height) <= 414;

    // 从左右边缘 5 点以内开始的滑动（返回、Safari 前进）。
    public static bool IsSideEdge(int x, int width) => x <= 5 || x >= width - 6;

    // W3C 动作合成的触摸从屏幕边缘开始时，iOS 不识别为边缘手势（真机上「返回」没有反应）；
    // XCUICoordinate 的按住拖动会被识别。速度按请求的时长换算，限制在系统能识别的范围。
    private static object EdgeDrag(int fromX, int fromY, int toX, int toY, double seconds)
    {
        var distance = Math.Sqrt(Math.Pow(toX - fromX, 2) + Math.Pow(toY - fromY, 2));
        var velocity = Math.Clamp(distance / Math.Max(seconds, .01), 400, 2500);
        return new
        {
            fromX,
            fromY,
            toX,
            toY,
            pressDuration = .05,
            holdDuration = 0,
            velocity = Math.Round(velocity)
        };
    }

    private static object SwipeGesture(int fromX, int fromY, int toX, int toY, double seconds)
    {
        // The move takes the requested time. Keeping a finger pressed at the start can trigger iOS long-press.
        object[] steps =
        [
            new { type = "pointerMove", origin = "viewport", duration = 0, x = fromX, y = fromY },
            new { type = "pointerDown", button = 0 },
            new { type = "pointerMove", origin = "viewport", duration = (int)Math.Round(seconds * 1000), x = toX, y = toY },
            new { type = "pointerUp", button = 0 }
        ];
        return new
        {
            actions = new[] { new { type = "pointer", id = "finger", parameters = new { pointerType = "touch" }, actions = steps } }
        };
    }
}
