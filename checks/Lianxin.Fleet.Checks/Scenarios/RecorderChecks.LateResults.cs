using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class RecorderChecks
{
    // 投屏录制等主控执行成功才写入：时间按用户操作的时刻（Stamp），结果先后到达也按操作顺序排列；
    // 录制期间做的操作在停止后才执行完仍会写入，开始录制前的、不能写进脚本的操作不写。
    public static Task LateResults()
    {
        var clock = new ManualClock();
        var recorder = new ScriptRecorder(clock);
        clock.Advance(5);
        var beforeStart = recorder.Stamp();
        clock.Advance(1);
        recorder.Start();
        clock.Advance(1);
        var home = recorder.Stamp();
        clock.Advance(2);
        var tap = recorder.Stamp();
        clock.Advance(3);
        var press = recorder.Stamp();
        Assert.True(!recorder.Accepts(FleetAction.Install, new(AppPath: @"C:\a.ipa")) && recorder.Accepts(FleetAction.Home, new()), "accepts");
        // 点击先执行完，主屏后执行完：仍按操作时刻排成 主屏、点击。
        clock.Advance(4);
        Assert.Equal(RecordOutcome.Recorded, recorder.Record(FleetAction.Tap, new(X: 0.5, Y: 0.5), tap));
        Assert.Equal(RecordOutcome.Recorded, recorder.Record(FleetAction.Home, new(), home));
        Assert.Equal(RecordOutcome.NotRecording, recorder.Record(FleetAction.Lock, new(), beforeStart));
        recorder.Stop();
        clock.Advance(10);
        // 停止后才执行完的长按（操作发生在录制期间）照样写入；停止之后的新操作不写。
        Assert.Equal(RecordOutcome.Recorded, recorder.Record(FleetAction.LongPress, new(X: 0.2, Y: 0.3, Duration: 1), press));
        Assert.Equal(RecordOutcome.NotRecording, recorder.Record(FleetAction.Home, new(), recorder.Stamp()));
        Assert.Equal(RecordOutcome.NotRecording, recorder.Record(FleetAction.Home, new()));
        var lines = recorder.Render().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.SequenceEqual(["主屏", "等待 2", "点击 0.5 0.5", "等待 3", "长按 0.2 0.3 1"]), string.Join(" | ", lines));
        Assert.Equal(3, ScriptParser.Parse(recorder.Render()).Count(step => step.Action is not null));
        return Task.CompletedTask;
    }
}
