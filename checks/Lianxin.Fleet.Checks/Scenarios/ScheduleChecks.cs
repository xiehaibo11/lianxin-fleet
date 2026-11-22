using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static class ScheduleChecks
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(8);

    private static DateTimeOffset At(int day, int hour, int minute) => new(2026, 11, day, hour, minute, 0, Offset);

    public static Task DueRules()
    {
        // 2026-11-23 是星期一。
        var daily = new FleetSchedule(1, "早班", 1, ScheduleKind.Daily, new TimeOnly(9, 0), 127, 0, null, [], true, At(22, 12, 0));
        Assert.True(!ScheduleService.IsDue(daily, At(23, 8, 59)), "before slot");
        Assert.True(ScheduleService.IsDue(daily, At(23, 9, 0)), "at slot");
        Assert.True(ScheduleService.IsDue(daily, At(23, 9, 9)), "inside grace");
        Assert.True(!ScheduleService.IsDue(daily, At(23, 9, 10)), "grace expired");
        Assert.True(!ScheduleService.IsDue(daily with
        {
            LastRunAt = At(23, 9, 0)
        }, At(23, 9, 5)), "already ran today");
        Assert.True(ScheduleService.IsDue(daily with
        {
            LastRunAt = At(22, 9, 0)
        }, At(23, 9, 5)), "ran yesterday");
        Assert.True(!ScheduleService.IsDue(daily with
        {
            Weekdays = 1 << (int)DayOfWeek.Sunday
        }, At(23, 9, 1)), "wrong weekday");
        Assert.True(!ScheduleService.IsDue(daily with
        {
            Enabled = false
        }, At(23, 9, 1)), "disabled");
        // 当天 9:05 才创建的任务不会马上补跑 9:00 的那一次。
        Assert.True(!ScheduleService.IsDue(daily with
        {
            LastRunAt = At(23, 9, 5)
        }, At(23, 9, 6)), "created after slot");

        var interval = new FleetSchedule(2, "巡检", 1, ScheduleKind.Interval, TimeOnly.MinValue, 0, 30, null, [], true,
            At(23, 10, 0), At(23, 10, 0));
        Assert.True(!ScheduleService.IsDue(interval, At(23, 10, 29)), "interval not reached");
        Assert.True(ScheduleService.IsDue(interval, At(23, 10, 30)), "interval reached");
        Assert.True(!ScheduleService.IsDue(interval with
        {
            LastRunAt = At(23, 10, 30)
        }, At(23, 10, 59)), "interval resets");
        return Task.CompletedTask;
    }

    // 两个任务同时到点、目标是同一台手机：后一个等前一个结束后自动补跑，不会丢失。
    public static async Task Collision()
    {
        using var store = new FleetStore(":memory:");
        var phone = store.RegisterDevice("A", "00008030-0012345678900001", 8101);
        var hold = new TaskCompletionSource();
        var bridge = new FakeBridge((device, action, parameters, token) => hold.Task.WaitAsync(token));
        await using var runner = new ScriptRunner(store, bridge);
        var script = store.SaveScript(null, "回主屏", "主屏", 1);
        var now = DateTimeOffset.Now;
        await using var schedules = new ScheduleService(store, runner, () => now);
        var first = store.SaveSchedule(new FleetSchedule(0, "甲", script.Id, ScheduleKind.Interval, TimeOnly.MinValue, 0, 1, null, [], true, now));
        store.SaveSchedule(first with
        {
            Id = 0,
            Name = "乙"
        });
        now = now.AddMinutes(1).AddSeconds(5);
        var firings = schedules.CheckDue();
        Assert.True(firings.Count == 2 && firings[0].Result!.Started.Count == 1 && firings[1].Result!.Busy.Count == 1, "second waits");
        Assert.Equal(1, schedules.PendingCount);
        Assert.Equal(0, schedules.CheckDue().Count);
        Assert.Equal(1, schedules.PendingCount);
        hold.SetResult();
        await ScriptChecks.WaitFor(() => runner.RunningCount == 0);
        Assert.Equal("乙", schedules.CheckDue().Single().Name);
        Assert.Equal(0, schedules.PendingCount);
        await ScriptChecks.WaitFor(() => runner.RunningCount == 0);
        Assert.Equal("定时：乙", store.GetLatestScriptRuns()[phone.Id].Trigger);
    }

    public static async Task Firing()
    {
        using var store = new FleetStore(":memory:");
        var a = store.RegisterDevice("A", "00008030-0012345678900001", 8101, "早班组");
        var b = store.RegisterDevice("B", "00008030-0012345678900002", 8102, "早班组");
        var c = store.RegisterDevice("C", "00008030-0012345678900003", 8103);
        var executed = new List<long>();
        var bridge = new FakeBridge((device, action, parameters, token) =>
        {
            lock (executed)
                executed.Add(device.Id);
            return Task.CompletedTask;
        });
        await using var runner = new ScriptRunner(store, bridge);
        var script = store.SaveScript(null, "回主屏", "主屏", 1);
        var now = DateTimeOffset.Now;
        await using var schedules = new ScheduleService(store, runner, () => now);

        var saved = store.SaveSchedule(new FleetSchedule(0, "分组任务", script.Id, ScheduleKind.Interval, TimeOnly.MinValue,
            0, 5, "早班组", [], true, now));
        Assert.True(saved.Id > 0 && saved.LastRunAt is not null, "new interval schedule starts counting now");
        Assert.Equal(0, schedules.CheckDue().Count);
        // 新建时间由数据库按真实时钟记录，比测试时钟稍晚，多推进几秒。
        now = now.AddMinutes(5).AddSeconds(5);
        var firing = schedules.CheckDue().Single();
        Assert.True(firing.Result!.Started.Order().SequenceEqual([a.Id, b.Id]), "group targets only");
        Assert.Equal(0, schedules.CheckDue().Count);
        await ScriptChecks.WaitFor(() => runner.RunningCount == 0);
        Assert.True(executed.Order().SequenceEqual([a.Id, b.Id]), "both group devices ran");

        var direct = store.SaveSchedule(saved with
        {
            Id = 0,
            Name = "指定设备",
            TargetGroup = null,
            TargetIds = [c.Id, 999]
        });
        Assert.True(schedules.RunNow(direct.Id).Result!.Started.SequenceEqual([c.Id]), "missing device ids are ignored");
        Assert.True(schedules.LastMessage.Contains("指定设备"), schedules.LastMessage);

        store.SetScheduleEnabled(saved.Id, false);
        now = now.AddHours(1);
        Assert.True(schedules.CheckDue().All(item => item.ScheduleId != saved.Id), "disabled schedule skipped");

        Assert.Throws<InvalidOperationException>(() => store.DeleteScript(script.Id));
        Assert.Throws<InvalidOperationException>(() => store.DeleteGroup("早班组"));
        Assert.Throws<ArgumentException>(() => store.SaveSchedule(saved with { Id = 0, Kind = ScheduleKind.Daily, Weekdays = 0 }));
        Assert.Throws<KeyNotFoundException>(() => store.SaveSchedule(saved with { Id = 0, ScriptId = 12345 }));
        store.DeleteSchedule(saved.Id);
        store.DeleteSchedule(direct.Id);
        store.DeleteScript(script.Id);
        Assert.Equal(0, store.GetScripts().Count);
    }
}
