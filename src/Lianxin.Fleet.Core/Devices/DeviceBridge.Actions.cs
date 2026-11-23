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
                ? await DimensionsAsync(device, 8, cancellationToken).ConfigureAwait(false) : (Width: 1, Height: 1);
            var (path, body, session) = Route(action, parameters, size);
            var prefix = session ? "/session/" + await SessionAsync(device, cancellationToken).ConfigureAwait(false) : "";
            await RequestAsync(device, prefix + path, body, action: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
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
            FleetAction.Home => ("/wda/homescreen", new { }, false),
            FleetAction.Lock => ("/wda/lock", new { }, false),
            FleetAction.Unlock => ("/wda/unlock", new { }, false),
            FleetAction.Clipboard => ("/wda/setPasteboard", new { content = Convert.ToBase64String(Encoding.UTF8.GetBytes(p.Text!)), contentType = "plaintext" }, false),
            FleetAction.Tap => ("/wda/tap", new { x = X(p.X), y = Y(p.Y) }, true),
            FleetAction.DoubleTap => ("/wda/doubleTap", new { x = X(p.X), y = Y(p.Y) }, true),
            FleetAction.LongPress => ("/wda/touchAndHold", new { x = X(p.X), y = Y(p.Y), duration = p.Duration }, true),
            FleetAction.Swipe => ("/actions", SwipeGesture(X(p.FromX), Y(p.FromY), X(p.ToX), Y(p.ToY), p.Duration), true),
            // shortcut: 从底边上滑后停住以打开多任务，参数未在真机校准；若打开的是主屏，调大 holdDuration 或降低 velocity。
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
