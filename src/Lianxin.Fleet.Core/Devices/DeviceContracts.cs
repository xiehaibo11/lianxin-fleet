using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Devices;

public interface IDeviceBridge
{
    Task<IReadOnlyList<DiscoveredDevice>> DiscoverAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetUsbConnectedUdidsAsync(CancellationToken cancellationToken = default);
    // USB 与 Wi-Fi（usbmuxd 网络设备）连接；默认实现只报告 USB。
    async Task<IReadOnlyList<DeviceConnection>> GetConnectionsAsync(CancellationToken cancellationToken = default)
        => (await GetUsbConnectedUdidsAsync(cancellationToken).ConfigureAwait(false))
            .Select(udid => new DeviceConnection(udid, "USB")).ToArray();
    Task<DeviceStatus> GetStatusAsync(FleetDevice device, CancellationToken cancellationToken = default);
    Task<byte[]> GetUsbScreenshotAsync(FleetDevice device, CancellationToken cancellationToken = default);
    Task<byte[]> GetScreenshotAsync(FleetDevice device, CancellationToken cancellationToken = default);
    // 实时画面：逐帧返回图片字节，调用方取消即断开。
    IAsyncEnumerable<byte[]> StreamAsync(FleetDevice device, CancellationToken cancellationToken = default);
    Task ExecuteAsync(FleetDevice device, FleetAction action, ActionParameters parameters,
        CancellationToken cancellationToken = default);

    // 界面元素（节点）。默认实现表示该通道不支持，脚本里的元素命令会以失败结束。
    // 查找结果是 WDA 元素编号，按界面顺序，最多 DeviceBridge.MaxElements 个；没有匹配时为空列表。
    Task<IReadOnlyList<string>> FindElementsAsync(FleetDevice device, ElementSelector selector,
        CancellationToken cancellationToken = default)
        => Task.FromException<IReadOnlyList<string>>(new NotSupportedException(UnsupportedElements));
    Task<ElementBounds> GetElementBoundsAsync(FleetDevice device, string elementId,
        CancellationToken cancellationToken = default)
        => Task.FromException<ElementBounds>(new NotSupportedException(UnsupportedElements));
    Task ClickElementAsync(FleetDevice device, string elementId, CancellationToken cancellationToken = default)
        => Task.FromException(new NotSupportedException(UnsupportedElements));
    Task SetElementValueAsync(FleetDevice device, string elementId, string text,
        CancellationToken cancellationToken = default)
        => Task.FromException(new NotSupportedException(UnsupportedElements));
    // 当前界面的完整元素树（XML），供抓取节点和以后的 dumpXml 使用。
    Task<string> GetPageSourceAsync(FleetDevice device, CancellationToken cancellationToken = default)
        => Task.FromException<string>(new NotSupportedException(UnsupportedElements));

    private const string UnsupportedElements = "当前设备通道不支持界面元素操作。";

    // WDA 实时的名称、屏幕与电量；默认实现返回 null，表示该通道不提供。
    Task<WdaDeviceInfo?> GetWdaInfoAsync(FleetDevice device, CancellationToken cancellationToken = default)
        => Task.FromResult<WdaDeviceInfo?>(null);

    // 锁屏状态与屏幕方向。默认实现表示该通道不支持；设置方向后屏幕尺寸随之互换，调用方需重新读取。
    Task<bool> IsLockedAsync(FleetDevice device, CancellationToken cancellationToken = default)
        => Task.FromException<bool>(new NotSupportedException("当前设备通道不支持查询锁屏状态。"));
    Task SetOrientationAsync(FleetDevice device, ScreenOrientation orientation, CancellationToken cancellationToken = default)
        => Task.FromException(new NotSupportedException("当前设备通道不支持设置屏幕方向。"));
}

public sealed class DeviceOperationException(string message, bool uncertain = false, Exception? inner = null)
    : Exception(message, inner)
{
    public bool Uncertain { get; } = uncertain;
}
