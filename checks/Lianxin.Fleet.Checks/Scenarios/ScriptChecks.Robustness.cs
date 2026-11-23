using System.Diagnostics;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class ScriptChecks
{
    // 上万个连写的「非」：不递归，在只有 256 KB 栈的线程上也很快解析完，奇偶数决定是否取反。
    public static Task NegationChain()
    {
        Assert.True(ScriptParser.Parse("如果 非 非 非 存在 文字=a\n  主屏\n结束")[0].Condition is { Negate: true }, "odd count negates");
        Assert.True(ScriptParser.Parse("如果 not 非 NOT 非 不存在 文字=a\n  主屏\n结束")[0].Condition is { Negate: true }, "even count keeps 不存在");
        Assert.Throws<ArgumentException>(() => ScriptParser.Parse("如果 非 非\n  主屏\n结束"));
        Exception? failure = null;
        var elapsed = 0L;
        var source = "如果 " + string.Concat(Enumerable.Repeat("非 ", 20_000)) + "存在 文字=a\n  主屏\n结束\n";
        var thread = new Thread(() =>
        {
            var timer = Stopwatch.StartNew();
            try
            {
                Assert.True(ScriptParser.Parse(source)[0].Condition is { Negate: false }, "20000 × 非");
            }
            catch (Exception error) { failure = error; }
            elapsed = timer.ElapsedMilliseconds;
        }, 256 * 1024);
        thread.Start();
        thread.Join();
        Assert.True(failure is null && elapsed < 2000, $"deep chain: {failure?.Message} in {elapsed} ms");
        return Task.CompletedTask;
    }

    // 暂停中的脚本让出并发名额：32 台都在跑时第 33 台排队，前 32 台暂停后它立即开始；继续后重新排队取得名额。
    public static async Task PausedSlot()
    {
        using var store = new FleetStore(":memory:");
        var devices = Enumerable.Range(1, ScriptRunner.MaxParallelDevices + 1)
            .Select(i => store.RegisterDevice("Phone " + i, $"00008030-00123456789{i:00000}", 9000 + i)).ToArray();
        var bridge = new ScriptBridge();
        await using var runner = new ScriptRunner(store, bridge);
        var script = store.SaveScript(null, "长循环", "重复 100000\n  点击 0.5 0.5\n  等待 0.05\n结束", 1);
        var first = devices[..^1].Select(device => device.Id).ToArray();
        var last = devices[^1].Id;
        runner.Start(script.Id, first, "手动");
        await WaitFor(() => first.All(id => runner.Snapshot()[id].Message != "排队中"));
        runner.Start(script.Id, [last], "手动");
        await Task.Delay(300);
        Assert.Equal("排队中", runner.Snapshot()[last].Message);
        Assert.Equal(ScriptRunner.MaxParallelDevices, runner.Pause(first));
        await WaitFor(() => runner.Snapshot()[last].Message != "排队中");
        Assert.Equal(ScriptRunner.MaxParallelDevices, runner.Resume(first));
        var taps = bridge.Count("tap");
        await WaitFor(() => bridge.Count("tap") > taps + 40);
        Assert.Equal(ScriptRunner.MaxParallelDevices + 1, runner.StopAll());
        await WaitFor(() => runner.RunningCount == 0);
    }

    // 等待元素：单次查询失败（WDA 短暂无响应）继续查到超时；一直失败时报告最后一次的错误，不当成「没出现」。
    public static async Task WaitRetries()
    {
        using var store = new FleetStore(":memory:");
        var device = store.RegisterDevice("Phone", "00008030-0012345678900001", 8101);
        var bridge = new ScriptBridge();
        await using var runner = new ScriptRunner(store, bridge);
        var attempts = 0;
        bridge.Find = _ => ++attempts <= 2 ? throw new DeviceOperationException("无法确认 WDA 响应，请检查设备及本机 USB 端口映射。") : ["el-1"];
        var flaky = await RunScript(store, runner, device.Id, "等待元素 文字=登录 5\n点击元素 文字=登录");
        Assert.True(flaky.State == ScriptRunState.Succeeded && attempts >= 3 && bridge.Count("click el-1") == 1, flaky.Message ?? "");
        bridge.Find = _ => throw new DeviceOperationException("无法确认 WDA 响应，请检查设备及本机 USB 端口映射。");
        var broken = await RunScript(store, runner, device.Id, "等待元素 文字=登录 1");
        Assert.True(broken.State == ScriptRunState.Failed && broken.Message!.Contains("最后一次查询失败") && broken.Message.Contains("WDA"),
            broken.Message ?? "");
        bridge.Find = _ => [];
        var missing = await RunScript(store, runner, device.Id, "等待元素 文字=登录 1");
        Assert.True(missing.State == ScriptRunState.Failed && missing.Message!.Contains("仍未出现"), missing.Message ?? "");
    }

    // 抓取节点的选择器：太长或含换行的文字改用「包含」片段；同样的文字在别处也有时标为不唯一，不会悄悄点到别的元素。
    public static Task Suggestions()
    {
        var longLabel = "协议" + new string('长', 600);
        var page = $"""
            <XCUIElementTypeApplication type="XCUIElementTypeApplication" name="App" label="App" visible="true" x="0" y="0" width="375" height="667">
              <XCUIElementTypeStaticText type="XCUIElementTypeStaticText" label="{longLabel}" visible="true" x="0" y="10" width="300" height="20"/>
              <XCUIElementTypeButton type="XCUIElementTypeButton" label="第一行&#10;第二行比较长" visible="true" x="0" y="40" width="100" height="40"/>
              <XCUIElementTypeButton type="XCUIElementTypeButton" label="确定" visible="true" x="0" y="100" width="100" height="40"/>
              <XCUIElementTypeButton type="XCUIElementTypeButton" label="确定" visible="true" x="0" y="160" width="100" height="40"/>
              <XCUIElementTypeButton type="XCUIElementTypeButton" visible="true" x="0" y="220" width="40" height="40"/>
            </XCUIElementTypeApplication>
            """;
        var elements = ScriptParser.DescribeElements(page);
        Assert.Equal(5, elements.Count);
        Assert.True(elements[0].Selector.StartsWith("包含=协议长", StringComparison.Ordinal) && elements[0].Selector.Length < 120
            && elements[0].Unique, elements[0].Selector);
        Assert.True(elements[1].Selector == "包含=第二行比较长" && elements[1].Unique, elements[1].Selector);
        Assert.True(elements[2].Selector == "文字=确定" && !elements[2].Unique && !elements[3].Unique, elements[2].Selector);
        Assert.True(ScriptParser.Summary(elements[2]).Contains("不唯一"), ScriptParser.Summary(elements[2]));
        // 没有文字的按钮：同类型还有别的按钮，按类型会点到第一个，所以标为不唯一。
        Assert.True(elements[4].Selector == "类型=Button" && !elements[4].Unique, elements[4].Selector);
        foreach (var element in elements)
            ScriptParser.Parse("点击元素 " + element.Selector);
        return Task.CompletedTask;
    }

    private static async Task<ScriptRun> RunScript(FleetStore store, ScriptRunner runner, long deviceId, string source)
    {
        var script = store.SaveScript(null, "检查 " + Guid.NewGuid().ToString("N")[..8], source, 1);
        runner.Start(script.Id, [deviceId], "检查");
        await WaitFor(() => runner.RunningCount == 0);
        return store.GetLatestScriptRuns()[deviceId];
    }
}
