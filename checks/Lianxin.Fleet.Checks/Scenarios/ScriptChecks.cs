using System.Collections.Concurrent;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Core.Validation;
using Microsoft.Data.Sqlite;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class ScriptChecks
{
    public static Task Parser()
    {
        var steps = ScriptParser.Parse("""
            # 中英文命令混写
            启动 com.example.app
            等待 0.5
            点击 0.5 0.25
            longpress 0.1 0.2 1.5
            滑动 0 0.5 1 0.5
            上滑
            返回
            输入 你好 世界
            openurl weixin://dl/chat
            主屏
            """);
        Assert.Equal(10, steps.Count);
        Assert.Equal(FleetAction.Launch, steps[0].Action);
        Assert.True(steps[1].Action is null && steps[1].WaitSeconds == 0.5, "wait step");
        Assert.Equal(0.25, steps[2].Parameters.Y);
        Assert.Equal(1.5, steps[3].Parameters.Duration);
        Assert.Equal(0.4, steps[4].Parameters.Duration);
        Assert.Equal(0.05, steps[6].Parameters.Duration);
        Assert.Equal("你好 世界", steps[7].Parameters.Text);
        Assert.Equal(4, steps[2].Line);
        Expect("点击 0.5 1.2", "第 1 行");
        Expect("主屏\n点击 0.5", "第 2 行");
        Expect("等待 0", "等待时间");
        Expect("安装 C:\\a.ipa", "无法识别");
        Expect("启动 not a bundle", "Bundle ID");
        Expect("# 只有注释", "没有可执行");
        Expect(string.Join('\n', Enumerable.Repeat("主屏", ScriptParser.MaxSteps + 1)), "最多");
        return Task.CompletedTask;
    }

    private static void Expect(string source, string fragment)
    {
        try
        {
            ScriptParser.Parse(source);
        }
        catch (ArgumentException error)
        {
            Assert.True(error.Message.Contains(fragment, StringComparison.Ordinal), $"Unexpected message: {error.Message}");
            return;
        }
        throw new InvalidOperationException("Script should have been rejected: " + source);
    }

    // 旧版数据库没有别名等列和脚本表，打开后补齐结构且保留原有设备。
    public static Task Migration()
    {
        using var folder = new TemporaryFolder();
        var path = Path.Combine(folder.Path, "fleet.sqlite");
        using (var legacy = new SqliteConnection($"Data Source={path}"))
        {
            legacy.Open();
            using var command = legacy.CreateCommand();
            command.CommandText = """
                CREATE TABLE native_config (id INTEGER PRIMARY KEY CHECK(id=1), mode TEXT NOT NULL);
                INSERT INTO native_config VALUES(1,'real');
                CREATE TABLE native_devices (id INTEGER PRIMARY KEY, name TEXT NOT NULL, udid TEXT NOT NULL UNIQUE,
                  wda_port INTEGER NOT NULL UNIQUE, group_name TEXT NOT NULL, width INTEGER NOT NULL DEFAULT 390,
                  height INTEGER NOT NULL DEFAULT 844, last_seen INTEGER);
                INSERT INTO native_devices(name,udid,wda_port,group_name) VALUES('Old phone','00008030-0012345678900001',8100,'老分组');
                """;
            command.ExecuteNonQuery();
        }
        SqliteConnection.ClearAllPools();
        using (var store = new FleetStore(path))
        {
            var device = store.GetDevices().Single();
            Assert.Equal("Old phone", device.Name);
            Assert.Equal("老分组", device.GroupName);
            Assert.True(device.Alias is null && device.Ecid is null, "new columns start empty");
            store.SetAlias(device.Id, "一号机");
            // 发现结果里的 UDID 去掉连字符后同样能匹配到已登记设备。
            store.UpdateDeviceInfo(new DiscoveredDevice("000080300012345678900001", "x", "000A68AE18D20000", "iPhone8,1", "15.8.8"));
            store.UpdateDeviceInfo(new DiscoveredDevice("00008030-0012345678900001", "x", null, null, "15.8.9"));
            device = store.GetDevice(device.Id);
            Assert.Equal("一号机", device.DisplayName);
            Assert.Equal("000A68AE18D20000", device.Ecid);
            Assert.Equal("15.8.9", device.OsVersion);
            Assert.Equal("iPhone8,1", device.ProductType);
            store.AddGroup("空分组");
            Assert.True(store.GetGroups().ToHashSet().SetEquals(["未分组", "老分组", "空分组"]), string.Join(",", store.GetGroups()));
            store.DeleteGroup("老分组");
            Assert.Equal(FleetStore.DefaultGroup, store.GetDevice(device.Id).GroupName);
            Assert.Throws<InvalidOperationException>(() => store.DeleteGroup(FleetStore.DefaultGroup));
        }
        SqliteConnection.ClearAllPools();
        using (var reopened = new FleetStore(path))
            Assert.Equal("一号机", reopened.GetDevices().Single().Alias);
        return Task.CompletedTask;
    }

    public static async Task Runner()
    {
        using var store = new FleetStore(":memory:");
        var devices = Enumerable.Range(1, 3).Select(i => store.RegisterDevice("Phone " + i, $"00008030-001234567890000{i}", 8100 + i)).ToArray();
        var active = new ConcurrentDictionary<long, bool>();
        var calls = new ConcurrentQueue<(long Device, FleetAction Action)>();
        var bridge = new FakeBridge(async (device, action, parameters, token) =>
        {
            Assert.True(active.TryAdd(device.Id, true), "Concurrent script steps on one phone");
            calls.Enqueue((device.Id, action));
            try
            {
                await Task.Delay(5, token);
                if (action == FleetAction.Text)
                    throw new DeviceOperationException("WDA 拒绝执行：boom");
            }
            finally { active.TryRemove(device.Id, out _); }
        });
        await using var runner = new ScriptRunner(store, bridge);
        var twice = store.SaveScript(null, "两轮", "启动 com.example.app\n等待 0.05\n点击 0.5 0.5", 2);
        Assert.Throws<InvalidOperationException>(() => store.SaveScript(null, "两轮", "主屏", 1));
        Assert.Throws<ArgumentException>(() => store.SaveScript(null, "坏脚本", "点击 2 2", 1));
        var started = runner.Start(twice.Id, devices.Select(d => d.Id).ToArray(), "手动");
        Assert.Equal(3, started.Started.Count);
        await WaitFor(() => runner.RunningCount == 0);
        Assert.Equal(12, calls.Count);
        var runs = store.GetLatestScriptRuns();
        Assert.True(devices.All(d => runs[d.Id].State == ScriptRunState.Succeeded && runs[d.Id].ScriptName == "两轮"), "all succeeded");

        var loop = store.SaveScript(null, "循环", "主屏\n等待 0.05", 0);
        runner.Start(loop.Id, [devices[0].Id], "手动");
        var again = runner.Start(twice.Id, [devices[0].Id, devices[1].Id], "手动");
        Assert.True(again.Busy.SequenceEqual([devices[0].Id]) && again.Started.SequenceEqual([devices[1].Id]), "busy device skipped");
        await WaitFor(() => runner.Snapshot().TryGetValue(devices[0].Id, out var activity) && activity.Message.Contains("第 2 轮"));
        Assert.Equal(1, runner.Stop([devices[0].Id, devices[2].Id]));
        await WaitFor(() => runner.RunningCount == 0);
        Assert.Equal(ScriptRunState.Stopped, store.GetLatestScriptRuns()[devices[0].Id].State);

        var failing = store.SaveScript(null, "会失败", "主屏\n输入 hi\n主屏", 1);
        runner.Start(failing.Id, [devices[2].Id], "手动");
        await WaitFor(() => runner.RunningCount == 0);
        var failed = store.GetLatestScriptRuns()[devices[2].Id];
        Assert.Equal(ScriptRunState.Failed, failed.State);
        Assert.True(failed.Message!.Contains("第 2/3 步") && failed.Message.Contains("boom"), failed.Message);

        // 进程中断后，运行中的记录在下次启动时标为待确认，不会自动重跑。
        store.StartScriptRun(twice, devices[1].Id, "手动");
        await using (new ScriptRunner(store, bridge))
            Assert.Equal(ScriptRunState.Unknown, store.GetLatestScriptRuns()[devices[1].Id].State);
    }

    public static Task Settings()
    {
        using var folder = new TemporaryFolder();
        var path = Path.Combine(folder.Path, "settings.json");
        Assert.Equal(3, FleetSettings.Load(path).RefreshSeconds);
        new FleetSettings { RefreshSeconds = 999, HiddenColumns = ["Ecid"], PythonPath = @"C:\Py\python.exe" }.Save(path);
        var loaded = FleetSettings.Load(path);
        Assert.Equal(60, loaded.RefreshSeconds);
        Assert.True(loaded.HiddenColumns.SequenceEqual(["Ecid"]) && loaded.PythonPath == @"C:\Py\python.exe", "settings round trip");
        File.WriteAllText(path, "{broken");
        Assert.Equal(3, FleetSettings.Load(path).RefreshSeconds);
        return Task.CompletedTask;
    }

    internal static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 250; i++)
        {
            if (condition())
                return;
            await Task.Delay(20);
        }
        throw new TimeoutException("Condition was not reached in time");
    }
}
