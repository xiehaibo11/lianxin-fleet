using System.Globalization;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;

namespace Lianxin.Fleet.Core.OpenApi;

// 开放接口使用的运行时服务。界面层用 FromRuntime 从共享的 FleetRuntime 构造，检查用假桥接直接构造。
// 这里只引用服务，不负责释放它们。
public sealed class OpenApiBackend
{
    public OpenApiBackend(FleetStore store, IDeviceBridge bridge, FleetService service, ScriptRunner scripts, WdaHost host,
        DeviceSetup setup, string appDirectory)
    {
        Store = store;
        Bridge = bridge;
        Service = service;
        Scripts = scripts;
        Host = host;
        Setup = setup;
        AppDirectory = Path.GetFullPath(appDirectory);
        Geometry = new ScreenGeometry(bridge);
    }

    public static OpenApiBackend FromRuntime(FleetRuntime runtime, Func<string> runnerBundleId,
        Func<byte[], int, byte[]>? jpegEncoder = null) => new(runtime.Store, runtime.Bridge, runtime.Service,
        runtime.Scripts, runtime.Host, runtime.Setup, runtime.AppDirectory)
        {
            RunnerBundleId = runnerBundleId,
            JpegEncoder = jpegEncoder,
            Manager = runtime.DeviceManager,
            TransferDirectory = TransferPath(runtime.DataDirectory)
        };

    // 数据目录下的 transfer：开放接口与电脑交换文件的唯一位置（界面设置页显示同一路径）。
    public static string TransferPath(string dataDirectory) => Path.Combine(Path.GetFullPath(dataDirectory), "transfer");

    public FleetStore Store
    {
        get;
    }
    public IDeviceBridge Bridge
    {
        get;
    }
    public FleetService Service
    {
        get;
    }
    public ScriptRunner Scripts
    {
        get;
    }
    public WdaHost Host
    {
        get;
    }
    public DeviceSetup Setup
    {
        get;
    }
    // 应用批准目录：installApp 只能安装其中的 .ipa/.app。
    public string AppDirectory
    {
        get;
    }
    // 启动自动化用的 WDA Runner Bundle ID（中控设置里的值）。
    public Func<string> RunnerBundleId { get; init; } = () => WdaHost.DefaultRunnerBundleId;
    // 重启 Apple 设备服务（Windows 需要管理员授权，macOS 需要管理员密码）。
    public Func<CancellationToken, Task> RestartUsbService { get; init; } = UsbServiceControl.RestartAppleServiceAsync;
    // PNG 转 JPG（参数：PNG 字节、质量 1–100）。Core 不带图片编码库，由界面层提供；为空时 JPG 接口返回不支持。
    public Func<byte[], int, byte[]>? JpegEncoder
    {
        get; init;
    }
    public TimeSpan AutomationReadyTimeout { get; init; } = TimeSpan.FromSeconds(40);
    // 设备管理（电量、应用、文件、开发者镜像与电源）。为空时这些接口返回不支持。
    public DeviceManager? Manager
    {
        get; init;
    }
    // 文件推送与拉取的电脑端中转目录：fsyncPullPush 只读写其中的文件。为空时电脑端路径一律拒绝。
    public string? TransferDirectory
    {
        get; init;
    }
    internal ScreenGeometry Geometry
    {
        get;
    }
    internal SemaphoreSlim UsbGate { get; } = new(1, 1);

    // deviceId 可以是 UDID（大小写、连字符不敏感）或中控里的设备编号。
    public FleetDevice ResolveDevice(string deviceId)
    {
        var key = Usbmux.Normalize(deviceId.Trim());
        var devices = Store.GetDevices();
        var match = devices.FirstOrDefault(device => Usbmux.Normalize(device.Udid) == key);
        if (match is null && long.TryParse(deviceId.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var id))
            match = devices.FirstOrDefault(device => device.Id == id);
        return match ?? throw new OpenApiException(OpenApiCodes.DeviceNotFound, $"设备 {Short(deviceId)} 未在中控登记。");
    }

    // 任意一台找不到就整体拒绝，不会只对部分设备执行。
    public IReadOnlyList<FleetDevice> ResolveDevices(IEnumerable<string> deviceIds)
    {
        var devices = new List<FleetDevice>();
        var missing = new List<string>();
        foreach (var id in deviceIds)
        {
            try
            {
                devices.Add(ResolveDevice(id));
            }
            catch (OpenApiException) { missing.Add(Short(id)); }
        }
        if (missing.Count > 0)
            throw new OpenApiException(OpenApiCodes.DeviceNotFound, "以下设备未在中控登记：" + string.Join("、", missing.Take(20)));
        return devices.DistinctBy(device => device.Id).ToArray();
    }

    public bool HasChannel(FleetDevice device) => Host.ChannelUdids.Contains(Usbmux.Normalize(device.Udid));

    internal DeviceManager RequireManager() => Manager ?? throw new OpenApiException(OpenApiCodes.Unsupported,
        "联信 Fleet 暂不支持设备管理接口（当前中控没有设备管理模块）。");

    // 卸载镜像、重启前：停止这台手机的脚本和由本中控启动的自动化通道，返回停止的通道数。
    // 之后的设备操作失败时用 AfterReleaseAsync 把「脚本和自动化已经停了」这个副作用写进错误信息。
    internal async Task<int> ReleaseAsync(FleetDevice device)
    {
        Scripts.Stop([device.Id]);
        return await Host.StopDevicesAsync([device]).ConfigureAwait(false);
    }

    internal static async Task AfterReleaseAsync(Task operation, string verb) =>
        await AfterReleaseAsync(Wrap(operation), verb).ConfigureAwait(false);

    internal static async Task<T> AfterReleaseAsync<T>(Task<T> operation, string verb)
    {
        try
        {
            return await operation.ConfigureAwait(false);
        }
        catch (DeviceOperationException error)
        {
            throw new DeviceOperationException($"已停止这台手机的脚本和自动化，但{verb}没有完成：{error.Message}", error.Uncertain, error);
        }
    }

    private static async Task<bool> Wrap(Task operation)
    {
        await operation.ConfigureAwait(false);
        return true;
    }

    // 轮询直到 WDA 就绪或超时；状态同时写回中控，界面表格随之更新。
    public async Task<bool> WaitReadyAsync(FleetDevice device, CancellationToken cancellationToken)
    {
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        wait.CancelAfter(AutomationReadyTimeout);
        try
        {
            while (true)
            {
                var snapshot = await Service.RefreshDeviceStatusesAsync([device.Id], wait.Token).ConfigureAwait(false);
                if (snapshot.Devices.FirstOrDefault(item => item.Device.Id == device.Id)?.Status.Online == true)
                    return true;
                await Task.Delay(1000, wait.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    public async Task<bool> IsServiceOkAsync(FleetDevice device, CancellationToken cancellationToken)
    {
        try
        {
            return (await Bridge.GetStatusAsync(device, cancellationToken).ConfigureAwait(false)).Online;
        }
        catch (Exception error) when (error is DeviceOperationException or ArgumentException)
        {
            return false;
        }
    }

    internal static string Short(string value) => value.Length > 60 ? value[..60] + "…" : value;
}
