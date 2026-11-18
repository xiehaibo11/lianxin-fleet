using System.Collections.Concurrent;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static class StoreChecks
{
    public static Task Persistence()
    {
        using var folder = new TemporaryFolder();
        var database = Path.Combine(folder.Path, "fleet.sqlite");
        long firstId;
        using (var store = new FleetStore(database))
        {
            var first = store.RegisterDevice("iPhone 1", "00008030-0012345678900001", 8100);
            firstId = first.Id;
            var second = store.RegisterDevice("iPhone 2", "00008030-0012345678900002", 8101);
            store.AssignGroup([first.Id, second.Id], "一组");
            var job = store.Enqueue("once", [first.Id, second.Id], FleetAction.Home, new());
            Assert.Equal(job.Id, store.Enqueue("once", [second.Id, first.Id, first.Id], FleetAction.Home, new()).Id);
            Assert.Throws<InvalidOperationException>(() => store.Enqueue("once", [first.Id], FleetAction.Home, new()));
            store.StartTarget(job.Id, first.Id);
            Assert.Throws<InvalidOperationException>(() => store.StartTarget(job.Id, first.Id));
        }
        using (var store = new FleetStore(database))
        {
            store.RecoverInterruptedJobs();
            var job = store.GetJobs().Single();
            Assert.Equal(JobState.Unknown, job.State);
            Assert.Equal(JobState.Unknown, job.Results.Single(r => r.DeviceId == firstId).State);
            Assert.Equal(JobState.Cancelled, job.Results.Single(r => r.DeviceId != firstId).State);
            Assert.Equal("一组", store.GetDevices()[0].GroupName);
        }
        // 演示模式已移除：旧版演示库必须被拒绝，不能被当成真机数据继续使用。
        using (var legacy = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={database}"))
        {
            legacy.Open();
            using var command = legacy.CreateCommand();
            command.CommandText = "UPDATE native_config SET mode='demo'";
            command.ExecuteNonQuery();
        }
        Assert.Throws<InvalidOperationException>(() => { using var unused = new FleetStore(database); });
        return Task.CompletedTask;
    }
    public static async Task Queue()
    {
        using var folder = new TemporaryFolder();
        using var store = new FleetStore(":memory:");
        var devices = Enumerable.Range(1, 6).Select(i => store.RegisterDevice("Phone " + i, $"00008030-001234567890000{i}", 8100 + i)).ToArray();
        var active = new ConcurrentDictionary<long, bool>();
        var calls = new ConcurrentQueue<(long, FleetAction)>();
        var peak = 0;
        var bridge = new FakeBridge(async (device, action, parameters, token) =>
        {
            Assert.True(active.TryAdd(device.Id, true), "Concurrent actions on one phone");
            peak = Math.Max(peak, active.Count);
            calls.Enqueue((device.Id, action));
            await Task.Delay(25, token);
            active.TryRemove(device.Id, out _);
            if (action == FleetAction.Text)
                throw new DeviceOperationException("Response lost", true);
        });
        await using var service = new FleetService(store, bridge, folder.Path);
        var batch = await service.SubmitAsync(devices.Select(d => d.Id).ToArray(), FleetAction.Home, new(), "batch");
        await service.SubmitAsync([devices[0].Id], FleetAction.Launch, new(BundleId: "com.example.internal"), "launch");
        await service.SubmitAsync([devices[1].Id], FleetAction.Text, new(Text: "内部测试"), "text");
        await WaitFor(() => service.GetSnapshot().Jobs.All(j => j.State is not (JobState.Queued or JobState.Running)));
        Assert.Equal(JobState.Succeeded, service.GetSnapshot().Jobs.Single(j => j.Id == batch.Id).State);
        Assert.Equal(JobState.Unknown, service.GetSnapshot().Jobs.Single(j => j.RequestKey == "text").State);
        Assert.Equal(8, calls.Count);
        Assert.True(peak >= 2 && peak <= FleetService.MaxParallelDevices);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitAsync([devices[0].Id], FleetAction.Tap, new(X: double.NaN, Y: 0), "invalid"));
    }
    public static Task ApplicationBoundary()
    {
        using var folder = new TemporaryFolder();
        var approved = Directory.CreateDirectory(Path.Combine(folder.Path, "apps")).FullName;
        var app = Path.Combine(approved, "Internal.ipa");
        File.WriteAllText(app, "mock signed package");
        Assert.Equal("mock signed package", File.ReadAllText(FleetValidation.AppPath(app, approved)));
        var outside = Directory.CreateDirectory(Path.Combine(folder.Path, "Outside.app")).FullName;
        Assert.Throws<UnauthorizedAccessException>(() => FleetValidation.AppPath(outside, approved));
        if (!OperatingSystem.IsWindows())
        {
            var link = Path.Combine(approved, "Escape.app");
            Directory.CreateSymbolicLink(link, outside);
            Assert.Throws<UnauthorizedAccessException>(() => FleetValidation.AppPath(link, approved));
        }
        return Task.CompletedTask;
    }
    public static Task ExclusiveLease()
    {
        using var folder = new TemporaryFolder();
        var database = Path.Combine(folder.Path, "exclusive.sqlite");
        using (var owner = new FleetStore(database))
        {
            var device = owner.RegisterDevice("Owner", "00008030-0012345678900001", 8100);
            var job = owner.Enqueue("in-flight", [device.Id], FleetAction.Home, new());
            owner.StartTarget(job.Id, device.Id);
            Assert.Throws<InvalidOperationException>(() => { using var second = new FleetStore(database); });
            Assert.Equal(JobState.Running, owner.GetJobs().Single().State);
        }
        using var reopened = new FleetStore(database);
        reopened.RecoverInterruptedJobs();
        Assert.Equal(JobState.Unknown, reopened.GetJobs().Single().State);
        return Task.CompletedTask;
    }
    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 100; i++)
        {
            if (condition())
                return;
            await Task.Delay(20);
        }
        throw new TimeoutException("Task queue did not finish");
    }
}
