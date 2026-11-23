using System.Globalization;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class RecorderChecks
{
    // 手动拨动的时钟，录制间隔完全由检查控制。
    private sealed class ManualClock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => ticks;
        public void Advance(double seconds) => ticks += (long)Math.Round(seconds * TimeSpan.TicksPerSecond);
    }

    public static async Task Run()
    {
        await EveryAction();
        await Waits();
        await FormattingAndLimits();
    }

    // 每种动作都能写成脚本行，生成的文本能被现有解析器读回同样的动作和参数。
    private static Task EveryAction()
    {
        var clock = new ManualClock();
        var recorder = new ScriptRecorder(clock);
        Assert.Equal(RecordOutcome.NotRecording, recorder.Record(FleetAction.Home, new()));
        recorder.Start();
        var actions = new (FleetAction Action, ActionParameters Parameters, string Line)[]
        {
            (FleetAction.Tap, new(X: 0.5, Y: 0.3), "点击 0.5 0.3"),
            (FleetAction.DoubleTap, new(X: 0.12345, Y: 1), "双击 0.123 1"),
            (FleetAction.LongPress, new(X: 0, Y: 0.25, Duration: 1), "长按 0 0.25 1"),
            (FleetAction.Swipe, new(FromX: 0.1, FromY: 0.9, ToX: 0.1, ToY: 0.2, Duration: 0.4537), "滑动 0.1 0.9 0.1 0.2 0.45"),
            (FleetAction.Swipe, new(FromX: .5, FromY: .78, ToX: .5, ToY: .25), "上滑"),
            (FleetAction.Swipe, new(FromX: .5, FromY: .25, ToX: .5, ToY: .78), "下滑"),
            (FleetAction.Swipe, new(FromX: .8, FromY: .5, ToX: .2, ToY: .5), "左滑"),
            (FleetAction.Swipe, new(FromX: .2, FromY: .5, ToX: .8, ToY: .5), "右滑"),
            (FleetAction.Swipe, new(FromX: 0, FromY: .5, ToX: .7, ToY: .5, Duration: .05), "返回"),
            (FleetAction.Home, new(), "主屏"),
            (FleetAction.AppSwitcher, new(), "多任务"),
            (FleetAction.Lock, new(), "锁屏"),
            (FleetAction.Unlock, new(), "解锁"),
            (FleetAction.VolumeUp, new(), "音量加"),
            (FleetAction.VolumeDown, new(), "音量减"),
            (FleetAction.Text, new(Text: "  你好  world #1 "), "输入 你好  world #1"),
            (FleetAction.Clipboard, new(Text: "复制的内容"), "剪贴板 复制的内容"),
            (FleetAction.OpenUrl, new(Text: "https://example.com/a?b=1"), "打开 https://example.com/a?b=1"),
            (FleetAction.Launch, new(BundleId: "com.apple.Preferences"), "启动 com.apple.Preferences"),
            (FleetAction.Terminate, new(BundleId: "com.apple.Preferences"), "关闭 com.apple.Preferences")
        };
        foreach (var (action, parameters, line) in actions)
        {
            Assert.Equal(line, ScriptRecorder.Line(action, parameters));
            Assert.Equal(RecordOutcome.Recorded, recorder.Record(action, parameters));
            clock.Advance(1);
        }
        Assert.True(Enum.GetValues<FleetAction>().All(action => action == FleetAction.Install || actions.Any(item => item.Action == action)),
            "every scriptable action is covered");
        Assert.Equal(RecordOutcome.Unsupported, recorder.Record(FleetAction.Install, new(AppPath: "C:\\a.ipa")));
        Assert.Equal(null, ScriptRecorder.Line(FleetAction.Install, new(AppPath: "C:\\a.ipa")));
        Assert.Equal(RecordOutcome.Unsupported, recorder.Record((FleetAction)999, new()));
        Assert.Throws<ArgumentException>(() => recorder.Record(FleetAction.Tap, new(X: 1.5, Y: 0.5)));
        Assert.Equal(actions.Length, recorder.Count);
        Assert.Equal("关闭 com.apple.Preferences", recorder.LastLine);
        recorder.Stop();
        Assert.Equal(RecordOutcome.NotRecording, recorder.Record(FleetAction.Home, new()));

        var source = recorder.Render(comments: ["录制于测试\n第二行说明"]);
        Assert.True(source.StartsWith("# 录制于测试\n# 第二行说明\n点击 0.5 0.3\n等待 1\n", StringComparison.Ordinal), source);
        var steps = ScriptParser.Parse(source);
        Assert.Equal(actions.Length * 2 - 1, steps.Count);
        var commands = steps.Where(step => step.Action is not null).ToArray();
        for (var index = 0; index < actions.Length; index++)
        {
            var expected = FleetValidation.Action(actions[index].Action, actions[index].Parameters, "");
            var actual = commands[index].Parameters;
            Assert.Equal(actions[index].Action, commands[index].Action);
            Assert.True(Near(expected.X, actual.X) && Near(expected.Y, actual.Y) && Near(expected.FromX, actual.FromX)
                && Near(expected.ToY, actual.ToY) && Math.Abs(expected.Duration - actual.Duration) < 0.006
                && expected.Text == actual.Text && expected.BundleId == actual.BundleId, $"round trip of {actions[index].Line}");
        }
        return Task.CompletedTask;
    }

    private static bool Near(double? expected, double? actual) =>
        expected is null ? actual is null : actual is not null && Math.Abs(expected.Value - actual.Value) < 0.0006;

    // 间隔取整、最短 0.05 秒、最长 3600 秒；滑动和长按本身的时长从下一段等待里扣除；可压缩空闲。
    private static Task Waits()
    {
        Assert.Equal(0.05, ScriptRecorder.WaitSeconds(0));
        Assert.Equal(0.05, ScriptRecorder.WaitSeconds(-3));
        Assert.Equal(0.05, ScriptRecorder.WaitSeconds(0.04));
        Assert.Equal(0.05, ScriptRecorder.WaitSeconds(double.NaN));
        Assert.Equal(0.1, ScriptRecorder.WaitSeconds(0.06));
        Assert.Equal(1.3, ScriptRecorder.WaitSeconds(1.25));
        Assert.Equal(9.9, ScriptRecorder.WaitSeconds(9.94));
        Assert.Equal(13, ScriptRecorder.WaitSeconds(12.6));
        Assert.Equal(3600, ScriptRecorder.WaitSeconds(5000));
        Assert.Equal(3600, ScriptRecorder.WaitSeconds(double.PositiveInfinity));

        var clock = new ManualClock();
        var recorder = new ScriptRecorder(clock);
        recorder.Start();
        clock.Advance(30);
        recorder.Record(FleetAction.Tap, new(X: .5, Y: .5));
        clock.Advance(2.04);
        recorder.Record(FleetAction.Swipe, new(FromX: .5, FromY: .9, ToX: .5, ToY: .1, Duration: 1.5));
        clock.Advance(1.52);
        recorder.Record(FleetAction.LongPress, new(X: .2, Y: .2, Duration: 2));
        clock.Advance(500);
        recorder.Record(FleetAction.Home, new());
        clock.Advance(7200);
        recorder.Record(FleetAction.Lock, new());
        clock.Advance(0.01);
        recorder.Record(FleetAction.Unlock, new());
        clock.Advance(4);
        recorder.Stop();
        clock.Advance(100);
        Assert.True(Math.Abs(recorder.Elapsed.TotalSeconds - 7737.57) < 1e-6, $"elapsed {recorder.Elapsed}");
        // 开头的 30 秒和结尾的 4 秒不写入；滑动后实际空闲 0.02 秒，长按后 498 秒。
        Assert.Equal("点击 0.5 0.5\n等待 2\n滑动 0.5 0.9 0.5 0.1 1.5\n等待 0.05\n长按 0.2 0.2 2\n等待 498\n主屏\n等待 3600\n锁屏\n等待 0.05\n解锁\n",
            recorder.Render());
        var trimmed = recorder.Render(maxIdleSeconds: 10);
        Assert.True(trimmed.Contains("长按 0.2 0.2 2\n等待 10\n主屏\n等待 10\n锁屏", StringComparison.Ordinal), trimmed);
        Assert.True(trimmed.Contains("点击 0.5 0.5\n等待 2\n", StringComparison.Ordinal), trimmed);
        Assert.Equal(6, ScriptParser.Parse(trimmed).Count(step => step.Action is not null));
        Assert.Throws<ArgumentOutOfRangeException>(() => recorder.Render(maxIdleSeconds: 0.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => recorder.Render(maxIdleSeconds: double.NaN));

        recorder.Start();
        Assert.Equal(0, recorder.Count);
        Assert.Equal("", recorder.Render());
        Assert.Equal("# 只有注释\n", recorder.Render(comments: ["只有注释"]));
        return Task.CompletedTask;
    }

    // 数字与区域设置无关；文字换行改成空格并注明；超过脚本步数或长度上限时拒绝继续录制；结果可直接入库。
    private static Task FormattingAndLimits()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.Equal("0.5", ScriptRecorder.Number(0.5));
            Assert.Equal("0.333", ScriptRecorder.Number(1.0 / 3));
            Assert.Equal("1", ScriptRecorder.Number(0.99999));
            Assert.Equal("0", ScriptRecorder.Number(-0.0));
            Assert.Equal("1234.5", ScriptRecorder.Number(1234.5));
            Assert.Equal("点击 0.667 0.001", ScriptRecorder.Line(FleetAction.Tap, new(X: 2.0 / 3, Y: 0.0006)));
            Assert.Equal("滑动 0.25 0.75 0.5 0.5 2", ScriptRecorder.Line(FleetAction.Swipe, new(FromX: .25, FromY: .75, ToX: .5, ToY: .5, Duration: 2)));
            Assert.Equal("长按 0.5 0.5 0.2", ScriptRecorder.Line(FleetAction.LongPress, new(X: .5, Y: .5, Duration: 0.2001)));
        }
        finally { CultureInfo.CurrentCulture = culture; }

        var recorder = new ScriptRecorder(new ManualClock());
        recorder.Start();
        recorder.Record(FleetAction.Text, new(Text: "第一行\r\n第二行\n\n 第三行 "));
        recorder.Record(FleetAction.Clipboard, new(Text: "a\rb"));
        var source = recorder.Render();
        Assert.True(source.Contains("# 原文含换行", StringComparison.Ordinal), source);
        var steps = ScriptParser.Parse(source);
        Assert.Equal("第一行 第二行 第三行", steps[0].Parameters.Text);
        Assert.Equal("a b", steps[2].Parameters.Text);

        var full = new ScriptRecorder(new ManualClock());
        full.Start();
        var recorded = 0;
        while (full.Record(FleetAction.Home, new()) == RecordOutcome.Recorded)
            recorded++;
        Assert.Equal(ScriptParser.MaxSteps / 2, recorded);
        Assert.Equal(ScriptParser.MaxSteps - 1, ScriptParser.Parse(full.Render(comments: ["说明"])).Count);

        var longText = new ScriptRecorder(new ManualClock());
        longText.Start();
        var text = new string('字', 1000);
        while (longText.Record(FleetAction.Text, new(Text: text)) == RecordOutcome.Recorded)
        {
        }
        // 每行约 1000 字，行数随脚本长度上限变化。
        Assert.True(longText.Count > ScriptParser.MaxSourceLength / 2000 && longText.Count <= ScriptParser.MaxSourceLength / 1000,
            $"long text count {longText.Count}");
        var rendered = longText.Render(comments: [new string('注', 300)]);
        Assert.True(rendered.Length <= ScriptParser.MaxSourceLength, $"length {rendered.Length}");

        using var store = new FleetStore(":memory:");
        var saved = store.SaveScript(null, "录制 测试", recorder.Render(comments: ["录制于测试"]), 1);
        Assert.True(store.GetScripts().Single().Id == saved.Id && saved.Source.StartsWith("# 录制于测试", StringComparison.Ordinal), "saved");
        return Task.CompletedTask;
    }
}
