using System.Globalization;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Core.OpenApi;

// 中控接口（advance/openapi/center）：设备列表、脚本启停与暂停、别名、USB 与开发者模式。
internal static class CenterRoutes
{
    public static void Register(OpenApiRouter router) => router
        .Post("/openapi/deviceList", DeviceList)
        .Post("/openapi/startScript", StartScript)
        .Post("/openapi/stopScript", StopScript)
        .Post("/openapi/refreshMsg", RefreshMessages)
        .Post("/openapi/setDeviceAlias", SetAlias)
        .Post("/openapi/reconnectUsb", ReconnectUsb)
        .Post("/openapi/enableDeveloperMode", EnableDeveloperMode)
        .Post("/openapi/pauseScript", PauseScript)
        .Post("/openapi/continueScript", ContinueScript)
        .Post("/openapi/isScriptPause", IsScriptPaused)
        .Unsupported("/openapi/erasePhone", "恢复出厂（会清空手机全部数据，联信不通过接口提供）。")
        .Unsupported("/openapi/getOrientationNoAuto", "无自动化获取屏幕方向，请在启动自动化后使用 getOrientation。");

    // deviceId 按 UDID 模糊匹配；groupId 是联信的分组名称。
    private static Task<OpenApiReply> DeviceList(OpenApiCall call)
    {
        var filter = Usbmux.Normalize(call.Text("deviceId")?.Trim() ?? "");
        var group = call.Text("groupId")?.Trim() ?? "";
        var snapshot = call.Backend.Service.GetSnapshot();
        var list = snapshot.Devices.Where(item => filter.Length == 0 || Usbmux.Normalize(item.Device.Udid).Contains(filter, StringComparison.Ordinal)
                || item.Device.Id.ToString(CultureInfo.InvariantCulture) == filter)
            .Where(item => group.Length == 0 || item.Device.GroupName == group)
            .Select(item => new
            {
                bridgeUrl = "",
                deviceAlias = item.Device.Alias ?? "",
                groupId = item.Device.GroupName,
                deviceNo = item.Device.Udid,
                deviceId = item.Device.Udid,
                deviceName = item.Device.Name,
                serialNo = "",
                groupName = item.Device.GroupName,
                productVersion = item.Device.OsVersion ?? "",
                // 以下为联信补充字段。
                id = item.Device.Id,
                productType = item.Device.ProductType ?? "",
                wdaPort = item.Device.WdaPort,
                serviceOk = item.Status.Online,
                usbConnected = item.UsbConnected,
                wifiConnected = item.NetworkConnected
            }).ToArray();
        return Task.FromResult(OpenApiReply.Ok(new
        {
            list,
            total = list.Length,
            page = 1
        }));
    }

    // name 对应联信「脚本」里保存的脚本名称（也可直接传 scriptId）；iecPath 是 EasyClick 工程包路径，联信不使用。
    // 与中控界面一样只在自动化已就绪的设备上启动，先探测一次这些设备的 WDA 状态。
    private static async Task<OpenApiReply> StartScript(OpenApiCall call)
    {
        var script = FindScript(call);
        var devices = call.Devices();
        var snapshot = await call.Backend.Service.RefreshDeviceStatusesAsync(devices.Select(device => device.Id).ToArray(),
            call.Cancellation).ConfigureAwait(false);
        var ready = snapshot.Devices.Where(item => item.Status.Online && devices.Any(device => device.Id == item.Device.Id))
            .Select(item => item.Device.Id).ToArray();
        if (ready.Length == 0)
            return OpenApiReply.Fail(OpenApiCodes.DeviceFailed, "所选设备都没有启动自动化，请先调用 startAutoEnv。");
        var result = call.Backend.Scripts.Start(script.Id, ready, "开放接口");
        if (result.Started.Count == 0)
            return OpenApiReply.Fail(OpenApiCodes.Busy, "所选设备都在运行其他脚本，请先 stopScript。");
        var message = $"脚本「{script.Name}」已在 {result.Started.Count} 台设备启动"
            + (result.Busy.Count > 0 ? $"，{result.Busy.Count} 台正在运行其他脚本已跳过" : "")
            + (devices.Count > ready.Length ? $"，{devices.Count - ready.Length} 台自动化未就绪已跳过" : "") + "。";
        return OpenApiReply.Ok(true, message);
    }

    private static FleetScript FindScript(OpenApiCall call)
    {
        var scripts = call.Backend.Store.GetScripts();
        if (call.Number("scriptId") is { } id)
            return scripts.FirstOrDefault(script => script.Id == (long)id)
                ?? throw new OpenApiException(OpenApiCodes.ScriptNotFound, $"脚本编号 {id} 不存在。");
        var name = call.Text("name")?.Trim();
        if (string.IsNullOrEmpty(name))
            throw OpenApiCall.Invalid("缺少参数 name（联信脚本名称）或 scriptId。");
        return scripts.FirstOrDefault(script => script.Name == name)
            ?? scripts.FirstOrDefault(script => string.Equals(script.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new OpenApiException(OpenApiCodes.ScriptNotFound,
                $"没有名为「{OpenApiBackend.Short(name)}」的脚本。请先在中控「脚本」里保存同名脚本（EasyClick 的 iec 工程不能直接运行）。");
    }

    private static Task<OpenApiReply> StopScript(OpenApiCall call)
    {
        var devices = call.Devices();
        var stopped = call.Backend.Scripts.Stop(devices.Select(device => device.Id).ToArray());
        return Task.FromResult(OpenApiReply.Ok(true, stopped > 0 ? $"正在停止 {stopped} 台设备上的脚本。" : "所选设备没有正在运行的脚本。"));
    }

    // 运行中的脚本报告当前步骤；否则报告最近一次运行的结果。
    private static Task<OpenApiReply> RefreshMessages(OpenApiCall call)
    {
        var devices = call.Devices(emptyMeansAll: true);
        var activity = call.Backend.Scripts.Snapshot();
        var runs = call.Backend.Store.GetLatestScriptRuns();
        var online = call.Backend.Service.GetSnapshot().Devices.ToDictionary(item => item.Device.Id, item => item.Status.Online);
        var data = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var device in devices)
        {
            var running = activity.GetValueOrDefault(device.Id);
            var last = runs.GetValueOrDefault(device.Id);
            var time = running is not null ? DateTimeOffset.Now : last?.FinishedAt ?? last?.StartedAt;
            data[device.Udid] = new
            {
                msg = running?.Message ?? last?.Message ?? "",
                deviceNo = device.Udid,
                deviceId = device.Udid,
                scriptName = running?.ScriptName ?? last?.ScriptName ?? "",
                serviceOk = online.GetValueOrDefault(device.Id) ? "1" : "0",
                reportTime = time?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "",
                running = running is not null,
                paused = running?.Paused == true,
                state = running is not null ? running.Paused ? "paused" : "running" : last?.State.ToString().ToLowerInvariant() ?? ""
            };
        }
        return Task.FromResult(OpenApiReply.Ok(data));
    }

    private static Task<OpenApiReply> SetAlias(OpenApiCall call)
    {
        var device = call.Device();
        var alias = call.Text("deviceAlias")?.Trim();
        call.Backend.Store.SetAlias(device.Id, alias);
        return Task.FromResult(OpenApiReply.Ok(true, string.IsNullOrEmpty(alias) ? "已清除别名。" : "别名已保存。"));
    }

    // 重启 Apple 设备服务（Windows：Apple Mobile Device Service；macOS：usbmuxd），会断开所有手机，需要电脑前有人确认管理员授权。
    private static async Task<OpenApiReply> ReconnectUsb(OpenApiCall call)
    {
        call.Device();
        var backend = call.Backend;
        if (!await backend.UsbGate.WaitAsync(0, call.Cancellation).ConfigureAwait(false))
            return OpenApiReply.Fail(OpenApiCodes.Busy, "USB 服务正在重启，请稍候。");
        try
        {
            backend.Scripts.StopAll();
            await backend.Host.StopAsync().ConfigureAwait(false);
            try
            {
                await backend.RestartUsbService(call.Cancellation).ConfigureAwait(false);
            }
            // 脚本和自动化已经停了：失败（包括在授权框点取消）时要把这个副作用告诉调用方。
            catch (Exception error) when (!call.Cancellation.IsCancellationRequested)
            {
                return OpenApiReply.Fail(OpenApiCodes.DeviceFailed,
                    $"已停止全部脚本和自动化通道，但 {UsbServiceControl.ServiceLabel} 没有重启：{error.Message}");
            }
            return OpenApiReply.Ok(true, $"{UsbServiceControl.ServiceLabel} 已重启，所有手机会短暂断开；重新出现后请再次启动自动化。");
        }
        finally { backend.UsbGate.Release(); }
    }

    private static async Task<OpenApiReply> EnableDeveloperMode(OpenApiCall call)
    {
        var device = call.Device();
        var progress = new MessageLog();
        await call.Backend.Setup.SetupAsync(device, new(DeveloperMode: true, MountImage: false, InstallRunner: false), progress,
            call.Cancellation).ConfigureAwait(false);
        return OpenApiReply.Ok(true, progress.Last ?? "开发者模式已开启。");
    }

    // 暂停在当前这一步执行完后生效，暂停中仍可 stopScript；没有运行脚本的设备不计入。
    private static Task<OpenApiReply> PauseScript(OpenApiCall call)
    {
        var ids = call.Devices().Select(device => device.Id).ToArray();
        var scripts = call.Backend.Scripts;
        var paused = scripts.Pause(ids);
        var already = ids.Count(scripts.IsPaused) - paused;
        return Task.FromResult(OpenApiReply.Ok(true, paused > 0
            ? $"已暂停 {paused} 台设备上的脚本（当前这一步执行完后停住）" + (already > 0 ? $"，{already} 台原本已暂停" : "") + "。"
            : already > 0 ? "所选设备上的脚本已经是暂停状态。" : "所选设备没有正在运行的脚本。"));
    }

    private static Task<OpenApiReply> ContinueScript(OpenApiCall call)
    {
        var resumed = call.Backend.Scripts.Resume(call.Devices().Select(device => device.Id).ToArray());
        return Task.FromResult(OpenApiReply.Ok(true, resumed > 0 ? $"已继续 {resumed} 台设备上的脚本。" : "所选设备没有暂停中的脚本。"));
    }

    // 文档示例里的 deviceAlias 不参与查询，以 deviceId 为准。
    private static Task<OpenApiReply> IsScriptPaused(OpenApiCall call)
    {
        var paused = call.Backend.Scripts.IsPaused(call.Device().Id);
        return Task.FromResult(OpenApiReply.Ok(paused, paused ? "脚本暂停中。" : "脚本不在暂停状态。"));
    }
}

// 同步记录一键配置的进度消息，返回最后一条给调用方。
internal sealed class MessageLog : IProgress<string>
{
    public string? Last
    {
        get; private set;
    }
    public void Report(string value) => Last = value;
}
