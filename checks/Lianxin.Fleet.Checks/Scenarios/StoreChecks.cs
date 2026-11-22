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
    public static async Task ConnectedDiscovery()
    {
        using var folder = new TemporaryFolder();
        using var store = new FleetStore(":memory:");
        var offline = store.RegisterDevice("离线手机", "00008030-0012345678900001", 8100);
        var known = store.RegisterDevice("已命名手机", "00008030-00123456789000ab", 8102, "测试组");
        var newcomer = new DiscoveredDevice("00008030-0012345678900003", "新手机");
        IReadOnlyList<DiscoveredDevice> attached = [new("0000803000123456789000AB", "扫描名称"), newcomer, newcomer];
        var bridge = new FakeBridge(discover: () => attached);
        await using var service = new FleetService(store, bridge, folder.Path);

        var connected = await service.DiscoverConnectedAsync();
        Assert.Equal(2, connected.Count);
        Assert.Equal<bool?>(false, service.GetSnapshot().Devices.Single(item => item.Device.Id == offline.Id).UsbConnected);
        Assert.Equal<bool?>(true, service.GetSnapshot().Devices.Single(item => item.Device.Id == known.Id).UsbConnected);
        Assert.Equal(known, connected.Single(device => device.Udid == known.Udid));
        Assert.Equal(8101, connected.Single(device => device.Udid == newcomer.Udid).WdaPort);
        Assert.True(connected.SequenceEqual(await service.DiscoverConnectedAsync()));

        attached = [];
        Assert.Equal(0, (await service.DiscoverConnectedAsync()).Count);
        var registry = await service.DiscoverAsync();
        Assert.Equal(3, registry.Count);
        Assert.Equal(offline, registry.Single(device => device.Udid == offline.Udid));
    }

    public static async Task TargetedStatusRefresh()
    {
        using var folder = new TemporaryFolder();
        using var store = new FleetStore(":memory:");
        var selected = store.RegisterDevice("已连接", "00008030-0012345678900001", 8100);
        var other = store.RegisterDevice("其他设备", "00008030-0012345678900002", 8101);
        var calls = new ConcurrentQueue<long>();
        var ready = new DeviceStatus(true, Width: 375, Height: 667);
        var bridge = new FakeBridge(status: (device, _) =>
        {
            calls.Enqueue(device.Id);
            return Task.FromResult(ready);
        });
        await using var service = new FleetService(store, bridge, folder.Path);

        var snapshot = await service.RefreshDeviceStatusesAsync([selected.Id, selected.Id]);
        Assert.Equal(selected.Id, calls.Single());
        Assert.Equal(ready, snapshot.Devices.Single(item => item.Device.Id == selected.Id).Status);
        Assert.True(!snapshot.Devices.Single(item => item.Device.Id == other.Id).Status.Online);
        Assert.Equal(667, store.GetDevice(selected.Id).Height);
        Assert.Equal(other, store.GetDevice(other.Id));
    }

    public static async Task UsbPresenceAndCapture()
    {
        using var folder = new TemporaryFolder();
        using var store = new FleetStore(":memory:");
        var usbPhone = store.RegisterDevice("USB", "00008030-00123456789000ab", 8100);
        var wdaPhone = store.RegisterDevice("WDA", "00008030-0012345678900002", 8101);
        IReadOnlyList<string>? attached = ["0000803000123456789000AB"];
        byte[] frame = [1, 2, 3];
        var bridge = new FakeBridge(
            status: (device, _) => Task.FromResult(new DeviceStatus(device.Id == wdaPhone.Id)),
            usbPresence: _ => Task.FromResult(attached ?? throw new IOException("Apple USB 服务不可用")),
            usbScreenshot: (device, _) =>
            {
                Assert.Equal(usbPhone.Id, device.Id);
                return Task.FromResult(frame);
            });
        await using var service = new FleetService(store, bridge, folder.Path);

        Assert.True(service.GetSnapshot().Devices.All(item => item.UsbConnected is null));
        var snapshot = await service.RefreshAsync();
        var usb = snapshot.Devices.Single(item => item.Device.Id == usbPhone.Id);
        Assert.Equal<bool?>(true, usb.UsbConnected);
        Assert.True(!usb.Status.Online);
        Assert.True((await service.GetUsbScreenshotAsync(usbPhone.Id)).SequenceEqual(frame));

        var wda = snapshot.Devices.Single(item => item.Device.Id == wdaPhone.Id);
        Assert.Equal<bool?>(false, wda.UsbConnected);
        Assert.True(wda.Status.Online);

        attached = [];
        snapshot = await service.RefreshDeviceStatusesAsync([usbPhone.Id]);
        Assert.Equal(2, snapshot.Devices.Count);
        Assert.True(snapshot.Devices.All(item => item.UsbConnected is false));
        Assert.True(snapshot.Devices.Single(item => item.Device.Id == wdaPhone.Id).Status.Online);

        attached = null;
        snapshot = await service.RefreshAsync();
        Assert.True(snapshot.Devices.All(item => item.UsbConnected is null));
        Assert.True(snapshot.Devices.Single(item => item.Device.Id == wdaPhone.Id).Status.Online);
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
