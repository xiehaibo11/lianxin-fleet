using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Devices;

public interface IDeviceBridge
{
    Task<IReadOnlyList<DiscoveredDevice>> DiscoverAsync(CancellationToken cancellationToken = default);
    Task<DeviceStatus> GetStatusAsync(FleetDevice device, CancellationToken cancellationToken = default);
    Task<byte[]> GetScreenshotAsync(FleetDevice device, CancellationToken cancellationToken = default);
    // 实时画面：逐帧返回图片字节，调用方取消即断开。
    IAsyncEnumerable<byte[]> StreamAsync(FleetDevice device, CancellationToken cancellationToken = default);
    Task ExecuteAsync(FleetDevice device, FleetAction action, ActionParameters parameters,
        CancellationToken cancellationToken = default);
}

public sealed class DeviceOperationException(string message, bool uncertain = false, Exception? inner = null)
    : Exception(message, inner)
{
    public bool Uncertain { get; } = uncertain;
}
