using System.Diagnostics;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;

namespace Lianxin.Fleet.Checks.Scenarios;

// 脚本 v2 执行：循环、条件、跳出/停止/失败、「当」循环上限、日志，以及暂停、继续与停止的配合。
internal static partial class ScriptChecks
{
    public static async Task Flow()
    {
        using var store = new FleetStore(":memory:");
        var device = store.RegisterDevice("Phone", "00008030-0012345678900001", 8101);
        var bridge = new ScriptBridge();
        await using var runner = new ScriptRunner(store, bridge);
        var finds = 0;
        void Reset()
        {
            bridge.Calls.Clear();
            finds = 0;
        }
        bridge.Find = selector => selector.Value switch
        {
            "第二次出现" => ++finds >= 2 ? ["e-1"] : [],
            "出现三次" => ++finds <= 3 ? ["e-2"] : [],
            "一直在" => ["e-3"],
            _ => []
        };

        var run = await RunScript(store, runner, device.Id, """
            重复 5
                点击 0.1 0.1
                如果 存在 文字=第二次出现
                    跳出
                结束
            结束
            主屏
            """);
        Assert.Equal(ScriptRunState.Succeeded, run.State);
        Assert.True(bridge.Count("tap") == 2 && bridge.Count("home") == 1, "break leaves the innermost loop only: " + string.Join(",", bridge.Calls));

        Reset();
        run = await RunScript(store, runner, device.Id, """
            当 存在 文字=出现三次
                点击 0.2 0.2
            结束
            如果 不存在 文字=出现三次
                锁屏
            否则
                解锁
            结束
            """);
        Assert.True(bridge.Count("tap") == 3 && bridge.Count("lock") == 1 && bridge.Count("unlock") == 0, "while + else");

        Reset();
        run = await RunScript(store, runner, device.Id, "重复 3\n  重复 2\n    主屏\n    停止\n  结束\n结束\n锁屏", repeat: 3);
        Assert.True(run.State == ScriptRunState.Succeeded && run.Message!.Contains("第 4 行执行「停止」"), run.Message ?? "");
        Assert.True(bridge.Count("home") == 1 && bridge.Count("lock") == 0, "stop ends every loop and every round");

        Reset();
        run = await RunScript(store, runner, device.Id, "主屏\n如果 存在 文字=一直在\n  失败 账号被封\n结束\n锁屏", repeat: 2);
        Assert.True(run.State == ScriptRunState.Failed && run.Message!.Contains("失败：账号被封（第 3 行）")
            && run.Message.Contains("第 1/2 轮"), run.Message ?? "");
        Assert.Equal(0, bridge.Count("lock"));

        // 条件一直成立、循环体为空时不会无限查询手机。
        Reset();
        var watch = Stopwatch.StartNew();
        run = await RunScript(store, runner, device.Id, "当 存在 文字=一直在\n结束");
        Assert.True(run.State == ScriptRunState.Failed && run.Message!.Contains($"{ScriptRunner.MaxLoopIterations} 次"), run.Message ?? "");
        Assert.Equal(ScriptRunner.MaxLoopIterations + 1, bridge.Count("find"));
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), "loop cap is reached quickly with a fast bridge");

        Reset();
        run = await RunScript(store, runner, device.Id, "当 存在 文字=一直在\n  主屏\n  跳出\n结束\n日志 收尾");
        Assert.True(run.State == ScriptRunState.Succeeded && bridge.Count("home") == 1, "break inside while");

        // 日志替代步骤描述显示在消息里；随机等待落在区间内；失败消息仍带行号。
        Reset();
        var script = store.SaveScript(null, "日志", "日志 正在刷视频\n随机等待 0.3 0.4\n点击元素 文字=没有", 1);
        watch.Restart();
        runner.Start(script.Id, [device.Id], "手动");
        await WaitFor(() => runner.Snapshot().TryGetValue(device.Id, out var activity) && activity.Message == "正在刷视频");
        await WaitFor(() => runner.RunningCount == 0);
        Assert.True(watch.Elapsed >= TimeSpan.FromSeconds(0.3), "random wait lower bound");
        run = store.GetLatestScriptRuns()[device.Id];
        Assert.True(run.State == ScriptRunState.Failed && run.Message!.Contains("第 3/3 步 · 点击元素 文字=没有")
            && run.Message.Contains("没有找到该元素"), run.Message ?? "");
    }

    public static async Task PauseResume()
    {
        using var store = new FleetStore(":memory:");
        var devices = Enumerable.Range(1, 2).Select(i => store.RegisterDevice("Phone " + i, $"00008030-001234567890000{i}", 8100 + i)).ToArray();
        var bridge = new ScriptBridge();
        await using var runner = new ScriptRunner(store, bridge);
        var script = store.SaveScript(null, "长循环", "重复 100000\n  点击 0.5 0.5\n  等待 0.05\n结束", 1);
        runner.Start(script.Id, [devices[0].Id], "手动");
        await WaitFor(() => bridge.Count("tap") >= 3);
        Assert.Equal(0, runner.Pause([devices[1].Id]));
        Assert.Equal(0, runner.Resume([devices[0].Id]));
        Assert.Equal(1, runner.Pause([devices[0].Id, devices[0].Id]));
        Assert.Equal(0, runner.Pause([devices[0].Id]));
        Assert.True(runner.IsPaused(devices[0].Id) && !runner.IsPaused(devices[1].Id), "paused flag");
        Assert.True(runner.Snapshot()[devices[0].Id].Paused, "snapshot shows paused");
        // 暂停在两条语句之间生效：正在执行的那一步可以完成，之后不再有新动作。
        await Task.Delay(150);
        var frozen = bridge.Calls.Count;
        await Task.Delay(300);
        Assert.Equal(frozen, bridge.Calls.Count);
        Assert.Equal(1, runner.RunningCount);
        Assert.Equal(1, runner.Resume([devices[0].Id]));
        Assert.True(!runner.IsPaused(devices[0].Id) && !runner.Snapshot()[devices[0].Id].Paused, "resumed");
        await WaitFor(() => bridge.Calls.Count > frozen + 2);

        // 暂停中可以停止，停止后不能再暂停或继续。
        Assert.Equal(1, runner.Pause([devices[0].Id]));
        Assert.Equal(1, runner.Stop([devices[0].Id]));
        await WaitFor(() => runner.RunningCount == 0);
        Assert.Equal(ScriptRunState.Stopped, store.GetLatestScriptRuns()[devices[0].Id].State);
        Assert.True(runner.Pause([devices[0].Id]) == 0 && runner.Resume([devices[0].Id]) == 0 && !runner.IsPaused(devices[0].Id), "finished run");

        // 刚启动、还没执行第一步时也能暂停；程序关闭时暂停中的脚本同样结束。
        runner.Start(script.Id, [devices[0].Id, devices[1].Id], "手动");
        Assert.Equal(2, runner.Pause([devices[0].Id, devices[1].Id]));
        var closing = runner.DisposeAsync().AsTask();
        Assert.True(await Task.WhenAny(closing, Task.Delay(5000)) == closing, "dispose does not hang on paused scripts");
        var runs = store.GetLatestScriptRuns();
        Assert.True(devices.All(d => runs[d.Id].State == ScriptRunState.Stopped && runs[d.Id].Message == "程序关闭，脚本已停止。"), "closed");
    }

    private static async Task<ScriptRun> RunScript(FleetStore store, ScriptRunner runner, long deviceId, string source, int repeat = 1)
    {
        var script = store.SaveScript(null, "检查 " + Guid.NewGuid().ToString("N")[..8], source, repeat);
        Assert.True(runner.Start(script.Id, [deviceId], "手动").Started.Count == 1, "started");
        await WaitFor(() => runner.RunningCount == 0);
        return store.GetLatestScriptRuns()[deviceId];
    }
}
