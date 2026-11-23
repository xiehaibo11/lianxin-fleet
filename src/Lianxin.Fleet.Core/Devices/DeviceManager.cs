using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Core.Devices;

// 设备管理：详情与电量、应用、媒体区文件、开发者镜像和电源。查询走 USB 锁屏服务，不需要自动化；
// 自动化在线时电量和屏幕改用 WDA 的实时数据。文件与电源操作见 DeviceManager.Files.cs。
public sealed partial class DeviceManager
{
    private readonly IDeviceBridge bridge;
    private readonly HostDeviceTools host;
    private readonly TimeProvider time;
    private readonly SemaphoreSlim batteryGate = new(1, 1);
    private readonly ConcurrentDictionary<string, (BatteryInfo? Battery, DateTimeOffset At)> batteries = new(StringComparer.Ordinal);

    public DeviceManager(IDeviceBridge bridge, string? pythonPath = null, IProcessRunner? processRunner = null,
        string? platform = null, TimeProvider? timeProvider = null)
    {
        this.bridge = bridge;
        time = timeProvider ?? TimeProvider.System;
        host = new HostDeviceTools(pythonPath, platform ?? (OperatingSystem.IsWindows() ? "win32"
            : OperatingSystem.IsMacOS() ? "darwin" : "linux"), processRunner ?? new ProcessRunner());
    }

    // 电量缓存有效期：期内重复调用 GetBatteriesAsync 不再启动子进程。
    public TimeSpan BatteryInterval { get; init; } = TimeSpan.FromSeconds(60);

    // automationReady：WDA 已就绪时电量与屏幕取 WDA；USB 读不到时也能只用 WDA 的信息。
    public async Task<DeviceInfo> GetInfoAsync(FleetDevice device, bool automationReady = false,
        CancellationToken cancellationToken = default)
    {
        Check(device);
        var live = automationReady ? WdaAsync(device, cancellationToken) : Task.FromResult<WdaDeviceInfo?>(null);
        DeviceInfo? info = null;
        ExceptionDispatchInfo? failure = null;
        try
        {
            info = await host.GetDeviceInfoAsync(device.Udid, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (error is DeviceOperationException or InvalidDataException)
        {
            failure = ExceptionDispatchInfo.Capture(error);
        }
        var wda = await live.ConfigureAwait(false);
        if (info is null && wda is null)
            failure!.Throw();
        info ??= new DeviceInfo(device.Udid, ProductType: device.ProductType, OsVersion: device.OsVersion, Source: "WDA");
        if (wda is not null)
            info = info with
            {
                DeviceName = info.DeviceName ?? wda.Name,
                ScreenWidth = info.ScreenWidth ?? (int)Math.Round(wda.ScreenWidth * (wda.Scale ?? 1)),
                ScreenHeight = info.ScreenHeight ?? (int)Math.Round(wda.ScreenHeight * (wda.Scale ?? 1)),
                ScreenScale = info.ScreenScale ?? wda.Scale,
                Battery = wda.Battery ?? info.Battery,
                Source = info.Source == "USB" ? "USB+WDA" : "WDA"
            };
        if (info.Battery is not null)
            batteries[Usbmux.Normalize(device.Udid)] = (info.Battery, time.GetUtcNow());
        return info;
    }

    public BatteryInfo? GetCachedBattery(string udid) =>
        batteries.TryGetValue(Usbmux.Normalize(udid), out var entry) ? entry.Battery : null;

    // 表格电量列：一个子进程读取全部在线设备，BatteryInterval 内直接返回缓存。USB 读不到而 WDA 在线的设备改问 WDA。
    // 返回键为设备 Id；离线或读不到电量的设备不在结果里。
    public async Task<IReadOnlyDictionary<long, BatteryInfo>> GetBatteriesAsync(IReadOnlyList<DeviceSnapshot> devices,
        bool force = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(devices);
        var online = devices.Where(item => DeviceBridge.ValidUdid(item.Device.Udid)
                && (item.UsbConnected == true || item.NetworkConnected == true || item.Status.Online))
            .DistinctBy(item => Usbmux.Normalize(item.Device.Udid)).ToArray();
        await batteryGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = time.GetUtcNow();
            if (online.Length > 0 && (force || online.Any(item => !batteries.TryGetValue(Usbmux.Normalize(item.Device.Udid),
                    out var entry) || now - entry.At >= BatteryInterval)))
                await ReadBatteriesAsync(online, now, cancellationToken).ConfigureAwait(false);
        }
        finally { batteryGate.Release(); }
        var result = new Dictionary<long, BatteryInfo>();
        var keys = online.Select(item => Usbmux.Normalize(item.Device.Udid)).ToHashSet(StringComparer.Ordinal);
        foreach (var item in devices)
            if (keys.Contains(Usbmux.Normalize(item.Device.Udid)) && GetCachedBattery(item.Device.Udid) is { } battery)
                result[item.Device.Id] = battery;
        return result;
    }

    private async Task ReadBatteriesAsync(IReadOnlyList<DeviceSnapshot> online, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, BatteryInfo> read;
        var lockdown = online.Where(item => item.UsbConnected == true || item.NetworkConnected == true)
            .Select(item => item.Device.Udid).ToArray();
        try
        {
            read = await host.GetBatteriesAsync(lockdown, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (error is DeviceOperationException or InvalidDataException)
        {
            read = new Dictionary<string, BatteryInfo>();
        }
        cancellationToken.ThrowIfCancellationRequested();
        var missing = online.Where(item => item.Status.Online && !read.ContainsKey(Usbmux.Normalize(item.Device.Udid))).ToArray();
        var live = await Task.WhenAll(missing.Select(item => WdaAsync(item.Device, cancellationToken))).ConfigureAwait(false);
        var fallback = missing.Zip(live).Where(pair => pair.Second?.Battery is not null)
            .ToDictionary(pair => Usbmux.Normalize(pair.First.Device.Udid), pair => pair.Second!.Battery!);
        foreach (var item in online)
        {
            var key = Usbmux.Normalize(item.Device.Udid);
            batteries[key] = (read.TryGetValue(key, out var battery) ? battery : fallback.GetValueOrDefault(key), now);
        }
    }

    // WDA 查询失败（未启动、端口映射断开）时返回 null，由调用方回退到 USB。
    private async Task<WdaDeviceInfo?> WdaAsync(FleetDevice device, CancellationToken cancellationToken)
    {
        try
        {
            return await bridge.GetWdaInfoAsync(device, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (error is DeviceOperationException or InvalidDataException or ArgumentException)
        {
            return null;
        }
    }

    public Task<IReadOnlyList<DeviceApp>> GetAppsAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.GetAppsAsync(device.Udid, cancellationToken);
    }

    // 卸载同时删除应用数据；系统应用（com.apple.*）不允许卸载。启动、关闭应用用 IDeviceBridge.ExecuteAsync。
    public Task UninstallAppAsync(FleetDevice device, string bundleId, CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.UninstallAppAsync(device.Udid, bundleId, cancellationToken);
    }

    public Task<DeveloperImageStatus> GetDeveloperImageAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.GetDeveloperImageAsync(device.Udid, cancellationToken);
    }

    // 按 iOS 版本自动选择并挂载（本机没有时联网下载）；已挂载时直接成功。
    public Task MountDeveloperImageAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.MountDeveloperImageAsync(device.Udid, cancellationToken);
    }

    // 返回 false 表示本来就没有挂载。卸载后依赖镜像的自动化会中断，需要重新挂载。
    public async Task<bool> UnmountDeveloperImageAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        var status = await GetDeveloperImageAsync(device, cancellationToken).ConfigureAwait(false);
        if (!status.Mounted)
            return false;
        await host.UnmountDeveloperImageAsync(device.Udid, status.ImageType == "Personalized", cancellationToken)
            .ConfigureAwait(false);
        return true;
    }

    // 批量执行：最多 maxParallel 台同时进行，每台完成后报告一次，结果按输入顺序返回。
    public static async Task<IReadOnlyList<DeviceBatchResult>> RunBatchAsync(IReadOnlyList<FleetDevice> devices,
        Func<FleetDevice, CancellationToken, Task> action, IProgress<DeviceBatchResult>? progress = null,
        int maxParallel = 4, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(action);
        using var slots = new SemaphoreSlim(Math.Clamp(maxParallel, 1, 32));
        async Task<DeviceBatchResult> RunOneAsync(FleetDevice device)
        {
            var started = false;
            DeviceBatchResult result;
            try
            {
                await slots.WaitAsync(cancellationToken).ConfigureAwait(false);
                started = true;
                try
                {
                    await action(device, cancellationToken).ConfigureAwait(false);
                }
                finally { slots.Release(); }
                result = new(device.Id, device.DisplayName, true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                result = new(device.Id, device.DisplayName, false, started, started ? "已中断，结果待确认。" : "已取消。");
            }
            catch (Exception error)
            {
                result = new(device.Id, device.DisplayName, false, error is DeviceOperationException { Uncertain: true },
                    error.Message);
            }
            progress?.Report(result);
            return result;
        }
        return await Task.WhenAll(devices.Select(RunOneAsync)).ConfigureAwait(false);
    }

    private static void Check(FleetDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (!DeviceBridge.ValidUdid(device.Udid))
            throw new ArgumentException("设备 UDID 无效。");
    }
}
