using System.Text.Json;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Devices;

// 输入文字和打开链接的真机细节。这些方法都在设备的串行锁里调用，不能再调用带锁的公开方法。
public sealed partial class DeviceBridge
{
    private const string SafariBundleId = "com.apple.mobilesafari";

    // iOS 15 的 Safari：竖屏底部地址栏是输入框 TabBarItemTitle，横屏是按钮 UnifiedTabBarItemView；单标签页布局和更早的系统叫 URL。
    private const string SafariAddressBar = "(type == 'XCUIElementTypeTextField' OR type == 'XCUIElementTypeButton') AND "
        + "(name IN {'TabBarItemTitle', 'URL', 'Address'} OR name BEGINSWITH 'UnifiedTabBarItemView?isSelected=true')";

    // 真机实测：没有输入焦点时 /wda/keys 会让 WDA 等约 60 秒，期间这台手机的其他请求全部排队超时。
    // 先确认有获得焦点的元素或弹出的键盘（两种查法都约 0.1 秒），都没有就直接报错。
    private async Task RequireTypingTargetAsync(FleetDevice device, string session, CancellationToken cancellationToken)
    {
        if (!await HasTypingTargetAsync(device, session, cancellationToken).ConfigureAwait(false))
            throw new DeviceOperationException("手机上没有正在输入的输入框（键盘没有弹出），请先点中输入框再输入。");
    }

    private async Task<bool> HasTypingTargetAsync(FleetDevice device, string session, CancellationToken cancellationToken)
    {
        try
        {
            await RequestAsync(device, $"/session/{session}/element/active", cancellationToken: cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DeviceOperationException)
        {
            // 没有焦点元素时 WDA 返回 no such element；第三方键盘可能不报告焦点，再看键盘本身。
        }
        return await FirstElementAsync(device, session, "class name", "XCUIElementTypeKeyboard", cancellationToken)
            .ConfigureAwait(false) is not null;
    }

    private async Task<string?> FirstElementAsync(FleetDevice device, string session, string strategy, string value,
        CancellationToken cancellationToken)
    {
        var found = await RequestAsync(device, $"/session/{session}/elements", new
        {
            @using = strategy,
            value
        }, limit: 2 * 1024 * 1024, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (found.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var item in found.EnumerateArray())
            if (item.ValueKind == JsonValueKind.Object && (item.TryGetProperty("ELEMENT", out var id)
                || item.TryGetProperty(W3cElementKey, out id)) && id.ValueKind == JsonValueKind.String && ValidElementId(id.GetString()))
                return id.GetString();
        return null;
    }

    private async Task<bool> PollAsync(Func<Task<bool>> condition, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            if (await condition().ConfigureAwait(false))
                return true;
            if (DateTime.UtcNow >= deadline)
                return false;
            await Task.Delay(300, cancellationToken).ConfigureAwait(false);
        }
    }

    // WDA 的 /url 需要 iOS 16.4 以上；更早的系统报「Device does not have the capability to open the default app from a url」，
    // 这时改用 Safari：点地址栏、输入链接并回车。
    // 自定义协议（weixin:// 等）在 Safari 里会弹「在某应用中打开此页？」，真机实测这个确认框不在 XCTest 能读到的界面树里，
    // WDA 点不到，只能留在屏幕上挡住后面的步骤，所以旧系统上直接拒绝。
    private async Task OpenUrlAsync(FleetDevice device, string session, string url, CancellationToken cancellationToken)
    {
        try
        {
            await RequestAsync(device, $"/session/{session}/url", new
            {
                url
            }, action: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (DeviceOperationException error) when (!error.Uncertain
            && error.Message.Contains("capability to open", StringComparison.OrdinalIgnoreCase))
        {
        }
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new DeviceOperationException("这台手机的系统低于 iOS 16.4，WDA 只能经 Safari 打开 http/https 链接；"
                + "App 自定义协议（如 weixin://）会弹出确认框而 WDA 点不到，请改用「启动 BundleID」打开应用。");
        await RequestAsync(device, $"/session/{session}/wda/apps/launch", new
        {
            bundleId = SafariBundleId
        }, action: true,
            timeoutSeconds: 30, cancellationToken: cancellationToken).ConfigureAwait(false);
        string? bar = null;
        if (!await PollAsync(async () => (bar = await FirstElementAsync(device, session, "predicate string", SafariAddressBar,
            cancellationToken).ConfigureAwait(false)) is not null, TimeSpan.FromSeconds(6), cancellationToken).ConfigureAwait(false))
            throw new DeviceOperationException("这台手机的系统低于 iOS 16.4，要经 Safari 打开链接，但 Safari 已打开后没有找到地址栏，链接没有打开。");
        await RequestAsync(device, $"/session/{session}/element/{bar}/click", new
        {
        }, action: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!await PollAsync(() => HasTypingTargetAsync(device, session, cancellationToken), TimeSpan.FromSeconds(3), cancellationToken)
            .ConfigureAwait(false))
            throw new DeviceOperationException("Safari 地址栏没有进入输入状态，链接没有打开。");
        await RequestAsync(device, $"/session/{session}/wda/keys", new
        {
            value = new[] { url + "\n" }
        }, action: true,
            timeoutSeconds: 30, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
