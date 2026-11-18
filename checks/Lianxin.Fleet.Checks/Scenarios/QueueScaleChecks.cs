using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static class QueueScaleChecks
{
    public static async Task Run()
    {
        using var folder = new TemporaryFolder();
        using var store = new FleetStore(":memory:");
        var devices = Enumerable.Range(1, 300).Select(i => store.RegisterDevice("Phone " + i, $"00008030-{i:000000}", 8000 + i)).ToArray();
        var gate = new object();
        var busy = new HashSet<long>();
        var executed = new List<(long Device, string Text)>();
        var violations = 0;
        var peak = 0;
        var bridge = new FakeBridge(async (device, action, parameters, token) =>
        {
            lock (gate)
            {
                if (!busy.Add(device.Id))
                    violations++;
                peak = Math.Max(peak, busy.Count);
                executed.Add((device.Id, parameters.Text!));
            }
            await Task.Delay(1, token);
            lock (gate)
                busy.Remove(device.Id);
        });
        await using var service = new FleetService(store, bridge, folder.Path);
        var ids = devices.Select(d => d.Id).ToArray();
        await service.SubmitAsync(ids, FleetAction.Text, new(Text: "a"), "scale-a");
        await service.SubmitAsync(ids[..10], FleetAction.Text, new(Text: "b"), "scale-b");
        await service.SubmitAsync(ids[..5], FleetAction.Text, new(Text: "c"), "scale-c");
        for (var i = 0; i < 3000 && service.GetSnapshot().Jobs.Any(j => j.State is JobState.Queued or JobState.Running); i++)
            await Task.Delay(10);
        var snapshot = service.GetSnapshot();
        Assert.Equal(3, snapshot.Jobs.Count);
        Assert.True(snapshot.Jobs.All(j => j.State == JobState.Succeeded), "Jobs did not all finish successfully");
        Assert.Equal(315, executed.Count);
        Assert.Equal(0, violations);
        Assert.True(peak >= 1 && peak <= FleetService.MaxParallelDevices, $"Peak concurrency {peak}");
        for (var i = 0; i < devices.Length; i++)
        {
            var expected = i < 5 ? "abc" : i < 10 ? "ab" : "a";
            var actual = string.Concat(executed.Where(e => e.Device == devices[i].Id).Select(e => e.Text));
            Assert.Equal(expected, actual);
        }
    }
}
