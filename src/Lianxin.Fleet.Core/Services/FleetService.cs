using System.Collections.Concurrent;
using System.Text.Json;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Serialization;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Services;

public sealed class FleetService : IAsyncDisposable
{
    private readonly FleetStore store;
    private readonly IDeviceBridge bridge;
    private readonly string appDirectory;
    private readonly CancellationTokenSource lifetime = new();
    private readonly SemaphoreSlim refreshGate = new(1, 1);
    private readonly ConcurrentDictionary<long, DeviceStatus> statuses = new();
    private IReadOnlySet<string>? connectedUsbUdids;
    private IReadOnlySet<string>? connectedNetworkUdids;
    private bool disposed;

    public FleetService(FleetStore store, IDeviceBridge bridge, string appDirectory)
    {
        this.store = store;
        this.bridge = bridge;
        this.appDirectory = Path.GetFullPath(appDirectory);
        store.RecoverInterruptedJobs();
    }
    public FleetSnapshot GetSnapshot()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var usbUdids = Volatile.Read(ref connectedUsbUdids);
        var networkUdids = Volatile.Read(ref connectedNetworkUdids);
        return new(store.GetDevices().Select(device => new DeviceSnapshot(device,
            statuses.TryGetValue(device.Id, out var status) ? status : new(false, Error: "等待状态检查。"),
            usbUdids?.Contains(Usbmux.Normalize(device.Udid)), networkUdids?.Contains(Usbmux.Normalize(device.Udid))))
            .ToArray(), store.GetJobs());
    }
    public Task<FleetSnapshot> RefreshAsync(CancellationToken cancellationToken = default)
        => RefreshStatusesAsync(ids: null, cancellationToken);

    public Task<FleetSnapshot> RefreshDeviceStatusesAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        return RefreshStatusesAsync(ids, cancellationToken);
    }

    private async Task<FleetSnapshot> RefreshStatusesAsync(IReadOnlyList<long>? ids, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        if (!await refreshGate.WaitAsync(ids is null ? 0 : Timeout.Infinite, linked.Token))
            return GetSnapshot();
        try
        {
            var devices = ids is null ? store.GetDevices() : store.ResolveDevices(ids);
            var usbProbe = RefreshUsbPresenceAsync(linked.Token);
            var wdaProbe = Parallel.ForEachAsync(devices, new ParallelOptions { MaxDegreeOfParallelism = 16, CancellationToken = linked.Token },
                async (device, token) =>
                {
                    DeviceStatus status;
                    try
                    {
                        status = await bridge.GetStatusAsync(device, token);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                    catch (Exception error) { status = new(false, Error: error.Message); }
                    if (disposed)
                        return;
                    statuses[device.Id] = status;
                    store.UpdateDeviceStatus(device.Id, status);
                });
            await Task.WhenAll(usbProbe, wdaProbe);
            return GetSnapshot();
        }
        finally { refreshGate.Release(); }
    }

    private async Task<IReadOnlySet<string>?> RefreshUsbPresenceAsync(CancellationToken cancellationToken)
    {
        IReadOnlySet<string>? connected = null;
        IReadOnlySet<string>? network = null;
        try
        {
            var connections = await bridge.GetConnectionsAsync(cancellationToken);
            connected = connections.Where(item => item.ConnectionType == "USB")
                .Select(item => Usbmux.Normalize(item.Udid)).ToHashSet(StringComparer.Ordinal);
            network = connections.Where(item => item.ConnectionType == "Network")
                .Select(item => Usbmux.Normalize(item.Udid)).Where(udid => !connected.Contains(udid)).ToHashSet(StringComparer.Ordinal);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception) { /* USB presence remains unknown when the transport cannot report it. */ }
        cancellationToken.ThrowIfCancellationRequested();
        Volatile.Write(ref connectedUsbUdids, connected);
        Volatile.Write(ref connectedNetworkUdids, network);
        return connected is null ? null : network is null ? connected : connected.Union(network).ToHashSet(StringComparer.Ordinal);
    }

    public Task<IReadOnlyList<FleetDevice>> DiscoverAsync(CancellationToken cancellationToken = default)
        => DiscoverAndRegisterAsync(connectedOnly: false, cancellationToken);

    public Task<IReadOnlyList<FleetDevice>> DiscoverConnectedAsync(CancellationToken cancellationToken = default)
        => DiscoverAndRegisterAsync(connectedOnly: true, cancellationToken);

    private async Task<IReadOnlyList<FleetDevice>> DiscoverAndRegisterAsync(bool connectedOnly, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        var discovered = await bridge.DiscoverAsync(linked.Token);
        IReadOnlySet<string>? connectedUdids = null;
        if (connectedOnly)
        {
            await refreshGate.WaitAsync(linked.Token);
            try
            {
                connectedUdids = await RefreshUsbPresenceAsync(linked.Token);
            }
            finally { refreshGate.Release(); }
        }
        lock (queueGate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            linked.Token.ThrowIfCancellationRequested();
            var registered = store.GetDevices();
            var udids = registered.Select(d => Usbmux.Normalize(d.Udid)).ToHashSet(StringComparer.Ordinal);
            var ports = registered.Select(d => d.WdaPort).ToHashSet();
            foreach (var device in discovered.Where(device => udids.Add(Usbmux.Normalize(device.Udid))))
            {
                var port = 8100;
                while (ports.Contains(port))
                    port++;
                store.RegisterDevice(device.Name, device.Udid, port);
                ports.Add(port);
            }
            foreach (var device in discovered)
                store.UpdateDeviceInfo(device);
            var devices = store.GetDevices();
            if (!connectedOnly)
                return devices;
            connectedUdids ??= discovered.Select(device => Usbmux.Normalize(device.Udid)).ToHashSet(StringComparer.Ordinal);
            return devices.Where(device => connectedUdids.Contains(Usbmux.Normalize(device.Udid)))
                .DistinctBy(device => Usbmux.Normalize(device.Udid)).ToArray();
        }
    }
    public FleetDevice RegisterDevice(string name, string udid, int wdaPort, string groupName = "未分组")
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        lock (queueGate)
            return store.RegisterDevice(name, udid, wdaPort, groupName);
    }
    public void AssignGroup(IReadOnlyList<long> ids, string groupName)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        store.AssignGroup(ids, groupName);
    }
    public Task<byte[]> GetScreenshotAsync(long id, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return bridge.GetScreenshotAsync(store.GetDevice(id), cancellationToken);
    }
    public Task<byte[]> GetUsbScreenshotAsync(long id, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return bridge.GetUsbScreenshotAsync(store.GetDevice(id), cancellationToken);
    }
    public IAsyncEnumerable<byte[]> StreamAsync(long id, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return bridge.StreamAsync(store.GetDevice(id), cancellationToken);
    }
    public string ExportConfig(IReadOnlyList<long>? ids = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var devices = ids is null ? store.GetDevices() : store.ResolveDevices(ids);
        return JsonSerializer.Serialize(new
        {
            devices = devices.Select(d => new { d.Udid, d.Name, d.WdaPort })
        }, FleetJson.Options);
    }
    // 同一台手机仍串行；不同手机最多并行这么多台，主控同步才能接近同时落到所有手机。
    public const int MaxParallelDevices = 32;
    private readonly object queueGate = new();
    private readonly HashSet<long> activeDevices = [];
    private readonly HashSet<Task> activeTasks = [];
    private Exception? queueFailure;

    public Task<FleetJob> SubmitAsync(IReadOnlyList<long> ids, FleetAction action, ActionParameters parameters,
        string requestKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (queueGate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (queueFailure is not null)
                throw new InvalidOperationException("任务队列写入失败，请重启并核对设备。", queueFailure);
            var validated = FleetValidation.Action(action, parameters, appDirectory);
            var job = store.Enqueue(requestKey, ids, action, validated);
            Pump();
            return Task.FromResult(job);
        }
    }
    private void Pump()
    {
        if (disposed || queueFailure is not null)
            return;
        foreach (var (jobId, deviceId) in store.GetNextQueuedTargets(MaxParallelDevices - activeDevices.Count))
        {
            if (!activeDevices.Add(deviceId))
                continue;
            FleetJob job;
            FleetDevice device;
            try
            {
                job = store.GetJobHeader(jobId);
                device = store.GetDevice(deviceId);
                store.StartTarget(jobId, deviceId);
            }
            catch { activeDevices.Remove(deviceId); throw; }
            var task = RunTargetAsync(job, device);
            activeTasks.Add(task);
            _ = task.ContinueWith(completed =>
            {
                lock (queueGate)
                {
                    activeTasks.Remove(completed);
                    activeDevices.Remove(deviceId);
                    if (completed.IsFaulted)
                        queueFailure = completed.Exception;
                    try
                    {
                        Pump();
                    }
                    catch (Exception error) { queueFailure = error; }
                }
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        }
    }
    private async Task RunTargetAsync(FleetJob job, FleetDevice device)
    {
        var state = JobState.Succeeded;
        string? message = null;
        try
        {
            if (job.Action == FleetAction.Install)
                FleetValidation.Action(job.Action, job.Parameters, appDirectory);
            await bridge.ExecuteAsync(device, job.Action, job.Parameters, lifetime.Token);
        }
        catch (Exception error)
        {
            state = error is DeviceOperationException { Uncertain: true } || error is OperationCanceledException
                ? JobState.Unknown : JobState.Failed;
            message = error.Message;
        }
        lock (queueGate)
            if (!disposed)
                store.FinishTarget(job.Id, device.Id, state, message);
    }
    public async ValueTask DisposeAsync()
    {
        Task[] tasks;
        lock (queueGate)
        {
            if (disposed)
                return;
            disposed = true;
            lifetime.Cancel();
            tasks = activeTasks.ToArray();
        }
        try
        {
            await Task.WhenAll(tasks);
        }
        catch { /* Preserve interrupted tasks for the next startup audit. */ }
        await refreshGate.WaitAsync();
        refreshGate.Release();
        lifetime.Dispose();
    }
}
