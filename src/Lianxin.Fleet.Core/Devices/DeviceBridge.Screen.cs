using System.Text.Json;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Devices;

// 锁屏状态与屏幕方向。查询锁屏不需要会话；设置方向与截图、动作共用会话并按设备串行。
public sealed partial class DeviceBridge
{
    public async Task<bool> IsLockedAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var value = await RequestAsync(device, "/wda/locked", timeoutSeconds: 4, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new DeviceOperationException("WDA 未返回锁屏状态。")
        };
    }

    // WDA 旋转模拟的是整台手机转动：当前界面不支持横屏（例如大多数机型的主屏）时请求会被拒绝或界面保持竖屏。
    // 旋转动画约需半秒，结束前 window/size 可能仍是旧方向。
    public Task SetOrientationAsync(FleetDevice device, ScreenOrientation orientation,
        CancellationToken cancellationToken = default)
    {
        var name = orientation switch
        {
            ScreenOrientation.Portrait => "PORTRAIT",
            ScreenOrientation.Landscape => "LANDSCAPE",
            _ => throw new ArgumentException("屏幕方向无效。")
        };
        return SerializedAsync(device, async () =>
        {
            var session = await SessionAsync(device, cancellationToken).ConfigureAwait(false);
            var before = await CurrentOrientationAsync(device, session, cancellationToken).ConfigureAwait(false);
            try
            {
                await RequestAsync(device, $"/session/{session}/orientation", new
                {
                    orientation = name
                }, action: true, timeoutSeconds: 15, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (DeviceOperationException error) when (!error.Uncertain && before is not null && before != name)
            {
                // 真机实测：主屏拒绝横屏后，这个横屏请求仍然留着，下一个支持横屏的应用一打开就是横的。
                // 把方向设回旋转前的值，撤销这个请求；撤销失败不影响报告原来的错误。
                try
                {
                    await RequestAsync(device, $"/session/{session}/orientation", new
                    {
                        orientation = before
                    }, action: true, timeoutSeconds: 15, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (DeviceOperationException)
                {
                }
                throw;
            }
            return true;
        }, cancellationToken);
    }

    // 旋转前的方向，只认 PORTRAIT / LANDSCAPE；读不到时不做撤销。
    private async Task<string?> CurrentOrientationAsync(FleetDevice device, string session, CancellationToken cancellationToken)
    {
        try
        {
            var value = await RequestAsync(device, $"/session/{session}/orientation", timeoutSeconds: 8,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return value.ValueKind == JsonValueKind.String && value.GetString() is "PORTRAIT" or "LANDSCAPE" ? value.GetString() : null;
        }
        catch (DeviceOperationException)
        {
            return null;
        }
    }
}
