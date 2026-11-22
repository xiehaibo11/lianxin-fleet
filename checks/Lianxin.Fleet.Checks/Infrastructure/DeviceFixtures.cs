using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Checks.Infrastructure;

internal sealed class TemporaryFolder : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fleet-native-check-" + Guid.NewGuid());
    public TemporaryFolder() => Directory.CreateDirectory(Path);
    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(Path, true);
    }
}

internal sealed class FakeBridge(Func<FleetDevice, FleetAction, ActionParameters, CancellationToken, Task>? execute = null,
    Func<IReadOnlyList<DiscoveredDevice>>? discover = null,
    Func<FleetDevice, CancellationToken, Task<DeviceStatus>>? status = null,
    Func<CancellationToken, Task<IReadOnlyList<string>>>? usbPresence = null,
    Func<FleetDevice, CancellationToken, Task<byte[]>>? usbScreenshot = null) : IDeviceBridge
{
    public Task<IReadOnlyList<DiscoveredDevice>> DiscoverAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(discover?.Invoke() ?? []);
    public Task<IReadOnlyList<string>> GetUsbConnectedUdidsAsync(CancellationToken cancellationToken = default)
        => usbPresence?.Invoke(cancellationToken)
            ?? Task.FromResult<IReadOnlyList<string>>(discover?.Invoke().Select(device => device.Udid).ToArray() ?? []);
    public Task<DeviceStatus> GetStatusAsync(FleetDevice device, CancellationToken cancellationToken = default)
        => status?.Invoke(device, cancellationToken) ?? Task.FromResult(new DeviceStatus(true));
    public Task<byte[]> GetUsbScreenshotAsync(FleetDevice device, CancellationToken cancellationToken = default)
        => usbScreenshot?.Invoke(device, cancellationToken) ?? Task.FromResult(Array.Empty<byte>());
    public Task<byte[]> GetScreenshotAsync(FleetDevice device, CancellationToken cancellationToken = default) => Task.FromResult(Array.Empty<byte>());
    public IAsyncEnumerable<byte[]> StreamAsync(FleetDevice device, CancellationToken cancellationToken = default) => AsyncEnumerable.Empty<byte[]>();
    public Task ExecuteAsync(FleetDevice device, FleetAction action, ActionParameters parameters, CancellationToken cancellationToken = default)
        => execute?.Invoke(device, action, parameters, cancellationToken) ?? Task.CompletedTask;
}
