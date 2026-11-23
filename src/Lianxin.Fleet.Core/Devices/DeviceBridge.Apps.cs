using System.Text.Json;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Devices;

// 关闭应用的真机细节。在设备的串行锁里调用。
public sealed partial class DeviceBridge
{
    // XCUIApplication 的状态：1 未运行，2 后台挂起，3 后台运行，4 前台。
    private const int AppNotRunning = 1;
    private const int AppForeground = 4;

    // 真机实测（iOS 15.8.8，9 台同时启动）：WDA 刚启动时直接结束一个此前就挂在后台的应用，XCTest 会卡 60 秒，
    // 期间这台手机的所有请求超时，脚本以「待确认」结束。先把它切到前台（XCTest 就认识这个进程了）再结束，只要 1–2 秒；
    // 没在运行的应用不发送结束请求，直接算成功。
    private async Task TerminateAsync(FleetDevice device, string session, string bundleId, CancellationToken cancellationToken)
    {
        var body = new
        {
            bundleId
        };
        var state = await AppStateAsync(device, session, bundleId, cancellationToken).ConfigureAwait(false);
        if (state == AppNotRunning)
            return;
        if (state is > AppNotRunning and < AppForeground)
            await RequestAsync(device, $"/session/{session}/wda/apps/launch", body, action: true, timeoutSeconds: 30,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        await RequestAsync(device, $"/session/{session}/wda/apps/terminate", body, action: true, timeoutSeconds: 30,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    // 读不到状态时返回 0，按原来的方式直接结束。
    private async Task<int> AppStateAsync(FleetDevice device, string session, string bundleId, CancellationToken cancellationToken)
    {
        try
        {
            var value = await RequestAsync(device, $"/session/{session}/wda/apps/state", new
            {
                bundleId
            },
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var state) ? state : 0;
        }
        catch (DeviceOperationException)
        {
            return 0;
        }
    }
}
