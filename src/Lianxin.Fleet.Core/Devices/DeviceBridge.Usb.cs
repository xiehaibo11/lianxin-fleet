using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Core.Devices;

public sealed partial class DeviceBridge
{
    public Task<byte[]> GetUsbScreenshotAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Validate(device, needsWda: false);
        return host.GetUsbScreenshotAsync(device.Udid, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetUsbConnectedUdidsAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var devices = await Usbmux.ListAsync(cancellationToken).ConfigureAwait(false);
        return devices.Where(device => device.ConnectionType == "USB" && ValidUdid(device.Udid))
            .Select(device => device.Udid).DistinctBy(Usbmux.Normalize).ToArray();
    }

    public async Task<IReadOnlyList<DeviceConnection>> GetConnectionsAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var devices = await Usbmux.ListAsync(cancellationToken).ConfigureAwait(false);
        return devices.Where(device => device.ConnectionType is "USB" or "Network" && ValidUdid(device.Udid))
            .Select(device => new DeviceConnection(device.Udid, device.ConnectionType)).ToArray();
    }
}
