using System.Collections.Concurrent;
using System.Globalization;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Checks.Scenarios;

// 脚本检查用的假设备：记录每个调用，元素查找与截图由检查用例提供。不连接任何真机。
internal sealed class ScriptBridge : IDeviceBridge
{
    public ConcurrentQueue<string> Calls { get; } = new();
    public Func<ElementSelector, IReadOnlyList<string>> Find { get; set; } = _ => [];
    public Func<byte[]> Screenshot { get; set; } = () => [];
    public Func<FleetAction, CancellationToken, Task>? OnExecute
    {
        get; set;
    }

    public int Count(string prefix) => Calls.Count(call => call.StartsWith(prefix, StringComparison.Ordinal));

    public Task<IReadOnlyList<DiscoveredDevice>> DiscoverAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DiscoveredDevice>>([]);
    public Task<IReadOnlyList<string>> GetUsbConnectedUdidsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);
    public Task<DeviceStatus> GetStatusAsync(FleetDevice device, CancellationToken cancellationToken = default) =>
        Task.FromResult(new DeviceStatus(true));
    public Task<byte[]> GetUsbScreenshotAsync(FleetDevice device, CancellationToken cancellationToken = default) =>
        Task.FromResult(Array.Empty<byte>());
    public IAsyncEnumerable<byte[]> StreamAsync(FleetDevice device, CancellationToken cancellationToken = default) =>
        AsyncEnumerable.Empty<byte[]>();

    public Task<byte[]> GetScreenshotAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        Calls.Enqueue("screenshot");
        return Task.FromResult(Screenshot());
    }

    public async Task ExecuteAsync(FleetDevice device, FleetAction action, ActionParameters parameters,
        CancellationToken cancellationToken = default)
    {
        Calls.Enqueue(action == FleetAction.Tap
            ? string.Create(CultureInfo.InvariantCulture, $"tap {parameters.X:0.###} {parameters.Y:0.###}")
            : action.ToString().ToLowerInvariant());
        if (OnExecute is not null)
            await OnExecute(action, cancellationToken);
    }

    public Task<IReadOnlyList<string>> FindElementsAsync(FleetDevice device, ElementSelector selector,
        CancellationToken cancellationToken = default)
    {
        Calls.Enqueue("find " + ScriptParser.FormatSelector(selector));
        return Task.FromResult(Find(selector));
    }

    public Task ClickElementAsync(FleetDevice device, string elementId, CancellationToken cancellationToken = default)
    {
        Calls.Enqueue("click " + elementId);
        return Task.CompletedTask;
    }

    public Task SetElementValueAsync(FleetDevice device, string elementId, string text,
        CancellationToken cancellationToken = default)
    {
        Calls.Enqueue($"value {elementId} {text}");
        return Task.CompletedTask;
    }
}
