using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.OpenApi;

// 启停自动化、设备信息与电量（advance/openapi/auto-env）和开发者镜像（dev-image）。联信的「自动化」就是手机上的 WDA Runner。
internal static class AutomationRoutes
{
    public static void Register(OpenApiRouter router) => router
        .Post("/openapi/startAutoEnv", StartAutomation)
        .Post("/openapi/closeEnv", CloseAutomation)
        .Post("/openapi/resetUsbConn", ResetConnection)
        .Post("/openapi/isServiceOk", IsServiceOk)
        .Post("/openapi/getDeviceInfo", DeviceInfo)
        .Post("/openapi/mountDevImage", MountImage)
        .Post("/openapi/getBattery", Battery)
        .Post("/openapi/isCharging", Charging)
        .Post("/openapi/unmountDevImage", UnmountImage)
        .Post("/openapi/checkDevImage", CheckImage)
        .Unsupported("/openapi/setAgentSetting", "代理 IPA 设置（联信使用 WDA，不使用 EasyClick 代理 IPA）。")
        .Unsupported("/openapi/startTunnel", "iOS 17+ 隧道（目前只支持 USB 与 iOS 16 及以下的无线连接）。")
        .Unsupported("/openapi/closeTunnel", "iOS 17+ 隧道（目前只支持 USB 与 iOS 16 及以下的无线连接）。");

    private static async Task<OpenApiReply> StartAutomation(OpenApiCall call)
    {
        var device = call.Device();
        var backend = call.Backend;
        if (await backend.IsServiceOkAsync(device, call.Cancellation).ConfigureAwait(false))
            return OpenApiReply.Ok(true, "自动化已在运行。");
        return await LaunchAsync(call, device).ConfigureAwait(false);
    }

    private static async Task<OpenApiReply> LaunchAsync(OpenApiCall call, FleetDevice device)
    {
        var backend = call.Backend;
        var started = await backend.Host.EnsureStartedAsync([device], backend.RunnerBundleId(), call.Cancellation).ConfigureAwait(false);
        if (started.Count == 0)
            return OpenApiReply.Fail(OpenApiCodes.DeviceFailed,
                "WDA 进程未能启动：请在中控「设置」核对 Python 路径和 WDA Runner Bundle ID，确认手机已安装 WDA 并挂载开发者镜像，且本机端口未被占用。");
        if (await backend.WaitReadyAsync(device, call.Cancellation).ConfigureAwait(false))
            return OpenApiReply.Ok(true, "自动化已就绪。");
        return OpenApiReply.Fail(OpenApiCodes.DeviceFailed,
            $"WDA 进程已启动，但 {backend.AutomationReadyTimeout.TotalSeconds:0} 秒内未就绪。请确认手机已解锁并信任开发者，稍后用 isServiceOk 查询。");
    }

    // 停止该设备的脚本和由本中控建立的 WDA 通道；其他设备不受影响。
    private static async Task<OpenApiReply> CloseAutomation(OpenApiCall call)
    {
        var device = call.Device();
        call.Backend.Scripts.Stop([device.Id]);
        var stopped = await call.Backend.Host.StopDevicesAsync([device]).ConfigureAwait(false);
        return OpenApiReply.Ok(true, stopped > 0 ? "已释放自动化，脚本一并停止。" : "该设备没有由本中控启动的自动化通道。");
    }

    // 只重建这一台手机的 USB 端口转发和 WDA 进程；重启整个 Apple 设备服务请用 reconnectUsb。
    private static async Task<OpenApiReply> ResetConnection(OpenApiCall call)
    {
        var device = call.Device();
        var backend = call.Backend;
        if (!backend.HasChannel(device))
            return OpenApiReply.Ok(true, "该设备没有由本中控建立的自动化通道，无需重置。");
        backend.Scripts.Stop([device.Id]);
        await backend.Host.StopDevicesAsync([device]).ConfigureAwait(false);
        return await LaunchAsync(call, device).ConfigureAwait(false);
    }

    private static async Task<OpenApiReply> IsServiceOk(OpenApiCall call)
    {
        var ok = await call.Backend.IsServiceOkAsync(call.Device(), call.Cancellation).ConfigureAwait(false);
        return OpenApiReply.Ok(ok, ok ? "自动化服务正常。" : "自动化未就绪。");
    }

    // 屏幕尺寸为截图像素（与点击坐标一致）；电量与充电状态读不到时为 null。
    private static async Task<OpenApiReply> DeviceInfo(OpenApiCall call)
    {
        var device = call.Device();
        var geometry = await call.Backend.Geometry.MeasureAsync(device, call.Cancellation).ConfigureAwait(false);
        var battery = await ReadBatteryAsync(call, device, call.Backend.Manager, fresh: false).ConfigureAwait(false);
        return OpenApiReply.Ok(new
        {
            deviceName = device.Name,
            deviceId = device.Udid,
            serialNo = "",
            deviceAlias = device.Alias ?? "",
            battery = battery?.Level,
            orientation = geometry.Orientation,
            orientationClick = geometry.Orientation,
            screenWidth = geometry.PixelWidth,
            screenHeight = geometry.PixelHeight,
            productVersion = device.OsVersion ?? "",
            model = device.ProductType ?? "iPhone",
            charging = IsCharging(battery),
            // 以下为联信补充字段：WDA 点坐标尺寸与像素比例。
            pointWidth = geometry.PointWidth,
            pointHeight = geometry.PointHeight,
            scale = Math.Round(geometry.Ratio, 4),
            ecid = device.Ecid ?? ""
        });
    }

    // 按系统版本自动选择并挂载开发者镜像，首次需要联网下载，已挂载时直接成功。
    private static async Task<OpenApiReply> MountImage(OpenApiCall call)
    {
        var device = call.Device();
        var progress = new MessageLog();
        await call.Backend.Setup.SetupAsync(device, new(DeveloperMode: false, MountImage: true, InstallRunner: false), progress,
            call.Cancellation).ConfigureAwait(false);
        return OpenApiReply.Ok(true, progress.Last ?? "开发者镜像已就绪。");
    }

    // 卸载前先停止这台手机的脚本和由本中控启动的自动化（它们依赖镜像）；本来就没有挂载时也返回成功。
    private static async Task<OpenApiReply> UnmountImage(OpenApiCall call)
    {
        var device = call.Device();
        var manager = call.Backend.RequireManager();
        await call.Backend.ReleaseAsync(device).ConfigureAwait(false);
        var unmounted = await OpenApiBackend.AfterReleaseAsync(manager.UnmountDeveloperImageAsync(device, call.Cancellation), "卸载开发者镜像")
            .ConfigureAwait(false);
        return OpenApiReply.Ok(true, unmounted ? "开发者镜像已卸载，这台手机的脚本和自动化已停止，重新挂载后才能再启动自动化。"
            : "开发者镜像本来就没有挂载。");
    }

    // 只查询，不会自动挂载。path 是镜像在手机上的挂载点（如 /Developer），联信不保存镜像文件在电脑上的路径。
    private static async Task<OpenApiReply> CheckImage(OpenApiCall call)
    {
        var status = await call.Backend.RequireManager().GetDeveloperImageAsync(call.Device(), call.Cancellation)
            .ConfigureAwait(false);
        return OpenApiReply.Ok(new
        {
            mounted = status.Mounted,
            result = status.Mounted,
            path = status.Mounted ? status.MountPath ?? "" : "",
            // 联信补充字段：Developer（iOS 16 及以下）或 Personalized（iOS 17 及以上）。
            imageType = status.Mounted ? status.ImageType ?? "" : ""
        }, status.Mounted ? "开发者镜像已挂载。" : "开发者镜像没有挂载。");
    }

    // 电量 0–100。默认与设备页电量列共用读数（最多 60 秒前），refresh 为 true 时立即重新读取。
    private static async Task<OpenApiReply> Battery(OpenApiCall call)
    {
        var device = call.Device();
        var battery = await ReadBatteryAsync(call, device, call.Backend.RequireManager(), call.Flag("refresh") == true)
            .ConfigureAwait(false);
        return battery?.Level is { } level ? OpenApiReply.Ok(level, $"电量 {level}%。")
            : OpenApiReply.Fail(OpenApiCodes.DeviceFailed, NoBattery);
    }

    private static async Task<OpenApiReply> Charging(OpenApiCall call)
    {
        var device = call.Device();
        var battery = await ReadBatteryAsync(call, device, call.Backend.RequireManager(), call.Flag("refresh") == true)
            .ConfigureAwait(false);
        return IsCharging(battery) is { } charging ? OpenApiReply.Ok(charging, charging ? "正在充电。" : "没有在充电。")
            : OpenApiReply.Fail(OpenApiCodes.DeviceFailed, NoBattery);
    }

    private const string NoBattery = "读不到电量：请用数据线连接手机并信任此电脑（或开启 Wi-Fi 连接），或者先启动自动化。";

    // 不需要自动化：先探测这一台的 USB 与 WDA 状态，再按电量列的规则读取（锁屏服务优先，读不到且 WDA 在线时问 WDA）。
    private static async Task<BatteryInfo?> ReadBatteryAsync(OpenApiCall call, FleetDevice device, DeviceManager? manager,
        bool fresh)
    {
        if (manager is null)
            return null;
        var snapshot = (await call.Backend.Service.RefreshDeviceStatusesAsync([device.Id], call.Cancellation).ConfigureAwait(false))
            .Devices.FirstOrDefault(item => item.Device.Id == device.Id);
        if (snapshot is null)
            return null;
        var batteries = await manager.GetBatteriesAsync([snapshot], fresh, call.Cancellation).ConfigureAwait(false);
        return batteries.GetValueOrDefault(device.Id);
    }

    // 充满后仍接着电源时 iOS 报告「未充电」；只有缺少充电标志时才看是否接着电源。
    private static bool? IsCharging(BatteryInfo? battery) => battery?.Charging ?? battery?.ExternalPower;
}
