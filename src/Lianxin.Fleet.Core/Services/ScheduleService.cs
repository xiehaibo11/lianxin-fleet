using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;

namespace Lianxin.Fleet.Core.Services;

public sealed record ScheduleFiring(long ScheduleId, string Name, ScriptStartResult? Result, string Message);

// 程序运行期间按本机时间检查定时任务；程序关闭期间错过的任务不补跑。
public sealed class ScheduleService : IAsyncDisposable
{
    // 每天模式：到点后这段时间内仍会触发（比如检查间隔或电脑短暂休眠），超过后当天跳过。
    public static readonly TimeSpan DailyGrace = TimeSpan.FromMinutes(10);
    private readonly FleetStore store;
    private readonly ScriptRunner runner;
    private readonly Func<DateTimeOffset> clock;
    private readonly TimeSpan period;
    private readonly CancellationTokenSource lifetime = new();
    private readonly object gate = new();
    private readonly object firingGate = new();
    // 触发时正在运行其他脚本的设备：之后每次检查重试，直到截止时间（每天模式为宽限期，间隔模式不超过一个间隔）。
    private readonly Dictionary<long, Pending> pending = [];
    private Task loop = Task.CompletedTask;
    private volatile string lastMessage = "";

    private sealed record Pending(long ScriptId, string Name, HashSet<long> Devices, DateTimeOffset Until);

    public ScheduleService(FleetStore store, ScriptRunner runner, Func<DateTimeOffset>? clock = null, TimeSpan? period = null)
    {
        this.store = store;
        this.runner = runner;
        this.clock = clock ?? (() => DateTimeOffset.Now);
        this.period = period ?? TimeSpan.FromSeconds(20);
    }

    public string LastMessage => lastMessage;

    public void Start()
    {
        lock (gate)
            if (loop.IsCompleted && !lifetime.IsCancellationRequested)
                loop = RunAsync(lifetime.Token);
    }

    private async Task RunAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(period);
        try
        {
            do
            {
                try
                {
                    CheckDue();
                }
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    lastMessage = "定时任务检查失败：" + error.Message;
                }
            }
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    public static bool IsDue(FleetSchedule schedule, DateTimeOffset now)
    {
        if (!schedule.Enabled)
            return false;
        if (schedule.Kind == ScheduleKind.Interval)
            return now - (schedule.LastRunAt ?? schedule.CreatedAt) >= TimeSpan.FromMinutes(schedule.IntervalMinutes);
        if ((schedule.Weekdays & (1 << (int)now.DayOfWeek)) == 0)
            return false;
        var slot = new DateTimeOffset(now.Date + schedule.At.ToTimeSpan(), now.Offset);
        return now >= slot && now - slot < DailyGrace && (schedule.LastRunAt is null || schedule.LastRunAt < slot);
    }

    public IReadOnlyList<ScheduleFiring> CheckDue()
    {
        lock (firingGate)
        {
            var now = clock();
            var firings = RetryPending(now);
            foreach (var schedule in store.GetSchedules().Where(item => IsDue(item, now)))
                firings.Add(Fire(schedule, now));
            return firings;
        }
    }

    public ScheduleFiring RunNow(long scheduleId)
    {
        lock (firingGate)
            return Fire(store.GetSchedule(scheduleId), clock());
    }

    public int PendingCount
    {
        get
        {
            lock (firingGate)
                return pending.Values.Sum(item => item.Devices.Count);
        }
    }

    private List<ScheduleFiring> RetryPending(DateTimeOffset now)
    {
        var firings = new List<ScheduleFiring>();
        foreach (var (id, item) in pending.ToArray())
        {
            if (now > item.Until)
            {
                pending.Remove(id);
                lastMessage = $"{now.LocalDateTime:HH:mm} 定时任务「{item.Name}」有 {item.Devices.Count} 台设备一直在运行其他脚本，本次已跳过。";
                continue;
            }
            try
            {
                var result = runner.Start(item.ScriptId, item.Devices.ToArray(), "定时：" + item.Name);
                item.Devices.ExceptWith(result.Started);
                if (result.Started.Count > 0)
                {
                    var firing = new ScheduleFiring(id, item.Name, result, $"定时任务「{item.Name}」等待其他脚本结束后，已在 {result.Started.Count} 台设备启动。");
                    lastMessage = $"{now.LocalDateTime:HH:mm} {firing.Message}";
                    firings.Add(firing);
                }
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                item.Devices.Clear();
                lastMessage = $"{now.LocalDateTime:HH:mm} 定时任务「{item.Name}」重试失败：{error.Message}";
            }
            if (item.Devices.Count == 0)
                pending.Remove(id);
        }
        return firings;
    }

    private ScheduleFiring Fire(FleetSchedule schedule, DateTimeOffset now)
    {
        // 先记录本次触发，即使启动失败也不会在下一次检查时反复触发。
        store.MarkScheduleRun(schedule.Id, now);
        ScheduleFiring firing;
        try
        {
            var targets = ResolveTargets(schedule);
            if (targets.Count == 0)
                firing = new(schedule.Id, schedule.Name, null, $"定时任务「{schedule.Name}」没有可用的目标设备。");
            else
            {
                var result = runner.Start(schedule.ScriptId, targets, "定时：" + schedule.Name);
                pending.Remove(schedule.Id);
                if (result.Busy.Count > 0)
                {
                    var window = schedule.Kind == ScheduleKind.Daily ? DailyGrace
                        : TimeSpan.FromMinutes(Math.Min(schedule.IntervalMinutes, DailyGrace.TotalMinutes));
                    pending[schedule.Id] = new(schedule.ScriptId, schedule.Name, result.Busy.ToHashSet(), now + window);
                }
                firing = new(schedule.Id, schedule.Name, result, $"定时任务「{schedule.Name}」已在 {result.Started.Count} 台设备启动"
                    + (result.Busy.Count > 0 ? $"，{result.Busy.Count} 台正在运行其他脚本，结束后自动补跑。" : "。"));
            }
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            firing = new(schedule.Id, schedule.Name, null, $"定时任务「{schedule.Name}」启动失败：{error.Message}");
        }
        lastMessage = $"{now.LocalDateTime:HH:mm} {firing.Message}";
        return firing;
    }

    private IReadOnlyList<long> ResolveTargets(FleetSchedule schedule)
    {
        var devices = store.GetDevices();
        if (schedule.TargetIds.Count > 0)
            return devices.Where(device => schedule.TargetIds.Contains(device.Id)).Select(device => device.Id).ToArray();
        return devices.Where(device => schedule.TargetGroup is null || device.GroupName == schedule.TargetGroup)
            .Select(device => device.Id).ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        Task running;
        lock (gate)
        {
            if (lifetime.IsCancellationRequested)
                return;
            lifetime.Cancel();
            running = loop;
        }
        await running.ConfigureAwait(false);
        lifetime.Dispose();
    }
}
