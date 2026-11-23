using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Services;

public sealed record ScriptActivity(long DeviceId, long RunId, string ScriptName, string Message, bool Paused = false);
public sealed record ScriptStartResult(IReadOnlyList<long> Started, IReadOnlyList<long> Busy);

// 每台手机同一时间只跑一个脚本；不同手机最多并行 MaxParallelDevices 台。
// 暂停在两条语句之间生效；暂停中仍可停止，程序关闭时同样会结束。
public sealed partial class ScriptRunner : IAsyncDisposable
{
    public const int MaxParallelDevices = 32;
    private readonly FleetStore store;
    private readonly IDeviceBridge bridge;
    private readonly SemaphoreSlim slots = new(MaxParallelDevices);
    private readonly CancellationTokenSource lifetime = new();
    private readonly object gate = new();
    private readonly Dictionary<long, Session> sessions = [];
    private bool disposed;

    private sealed class Session(long runId, string scriptName, CancellationTokenSource stop)
    {
        public long RunId { get; } = runId;
        public string ScriptName { get; } = scriptName;
        public CancellationTokenSource Stop { get; } = stop;
        public volatile string Message = "排队中";
        // 非空表示已暂停；继续时完成它。只在 gate 锁内替换。
        public volatile TaskCompletionSource? Pause;
        public Task Work = Task.CompletedTask;
    }

    // dataDirectory 为空时，图片模板与截图命令会以失败结束；其余命令不受影响。
    public ScriptRunner(FleetStore store, IDeviceBridge bridge, string? dataDirectory = null)
    {
        this.store = store;
        this.bridge = bridge;
        if (dataDirectory is not null)
        {
            var root = Path.GetFullPath(dataDirectory);
            TemplateDirectory = Path.Combine(root, "templates");
            CaptureDirectory = Path.Combine(root, "captures");
            Directory.CreateDirectory(TemplateDirectory);
        }
        store.RecoverInterruptedScriptRuns();
    }

    // 模板图片目录：点击图片 / 等待图片 / 找到图片 按名称读取其中的 名称.png。
    public string? TemplateDirectory
    {
        get;
    }
    // 截图命令保存的位置。
    public string? CaptureDirectory
    {
        get;
    }

    public int RunningCount
    {
        get
        {
            lock (gate)
                return sessions.Count;
        }
    }

    public IReadOnlyDictionary<long, ScriptActivity> Snapshot()
    {
        lock (gate)
            return sessions.ToDictionary(item => item.Key, item => new ScriptActivity(item.Key, item.Value.RunId,
                item.Value.ScriptName, item.Value.Message, item.Value.Pause is not null));
    }

    public ScriptStartResult Start(long scriptId, IReadOnlyList<long> deviceIds, string trigger)
    {
        var script = store.GetScript(scriptId);
        var steps = ScriptParser.Parse(script.Source);
        var devices = store.ResolveDevices(deviceIds);
        var started = new List<long>();
        var busy = new List<long>();
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            foreach (var device in devices)
            {
                if (sessions.ContainsKey(device.Id))
                {
                    busy.Add(device.Id);
                    continue;
                }
                var session = new Session(store.StartScriptRun(script, device.Id, trigger), script.Name,
                    CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token));
                sessions[device.Id] = session;
                session.Work = RunAsync(device, script, steps, session);
                started.Add(device.Id);
            }
        }
        return new(started, busy);
    }

    public int Stop(IReadOnlyList<long> deviceIds)
    {
        lock (gate)
        {
            var stopped = 0;
            foreach (var id in deviceIds.Distinct())
                if (sessions.TryGetValue(id, out var session) && !session.Stop.IsCancellationRequested)
                {
                    session.Stop.Cancel();
                    stopped++;
                }
            return stopped;
        }
    }

    public int StopAll()
    {
        lock (gate)
            return Stop(sessions.Keys.ToArray());
    }

    // 返回本次新暂停的设备数；没有运行脚本、已暂停或正在停止的设备不计。
    public int Pause(IReadOnlyList<long> deviceIds)
    {
        lock (gate)
        {
            var paused = 0;
            foreach (var id in deviceIds.Distinct())
                if (sessions.TryGetValue(id, out var session) && !session.Stop.IsCancellationRequested && session.Pause is null)
                {
                    session.Pause = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    paused++;
                }
            return paused;
        }
    }

    // 返回本次恢复运行的设备数。
    public int Resume(IReadOnlyList<long> deviceIds)
    {
        lock (gate)
        {
            var resumed = 0;
            foreach (var id in deviceIds.Distinct())
                if (sessions.TryGetValue(id, out var session) && session.Pause is { } pause)
                {
                    session.Pause = null;
                    pause.TrySetResult();
                    resumed++;
                }
            return resumed;
        }
    }

    public bool IsPaused(long deviceId)
    {
        lock (gate)
            return sessions.TryGetValue(deviceId, out var session) && session.Pause is not null;
    }

    private async Task RunAsync(FleetDevice device, FleetScript script, IReadOnlyList<ScriptStep> steps, Session session)
    {
        // 让出调用方持有的锁，脚本主体在线程池上执行。
        await Task.Yield();
        var token = session.Stop.Token;
        var run = new RunContext(device, session, token, steps.Count);
        var state = ScriptRunState.Succeeded;
        string? message;
        try
        {
            await slots.WaitAsync(token).ConfigureAwait(false);
            run.HoldsSlot = true;
            var rounds = script.Repeat == 0 ? int.MaxValue : script.Repeat;
            var stopped = false;
            for (var round = 1; round <= rounds && !stopped; round++)
            {
                run.Progress = script.Repeat == 1 ? "" : script.Repeat == 0 ? $"第 {round} 轮 · " : $"第 {round}/{script.Repeat} 轮 · ";
                stopped = await RunBlockAsync(steps, run, top: true).ConfigureAwait(false) == Flow.Stop;
            }
            message = stopped ? $"{run.Progress}第 {run.Line} 行执行「停止」，脚本正常结束。"
                : script.Repeat == 1 ? "脚本已完成。" : $"脚本已完成 {script.Repeat} 轮。";
        }
        catch (Exception) when (token.IsCancellationRequested)
        {
            state = ScriptRunState.Stopped;
            message = lifetime.IsCancellationRequested ? "程序关闭，脚本已停止。" : "已手动停止。";
        }
        catch (ScriptFailedException failure)
        {
            state = ScriptRunState.Failed;
            message = $"{run.Progress}失败：{failure.Message}（第 {failure.Line} 行）";
        }
        catch (Exception error)
        {
            state = error is DeviceOperationException { Uncertain: true } ? ScriptRunState.Unknown : ScriptRunState.Failed;
            message = $"{run.StepText} · 失败：{error.Message}";
        }
        finally
        {
            if (run.HoldsSlot)
                slots.Release();
        }
        try
        {
            store.FinishScriptRun(session.RunId, state, message);
        }
        catch (ObjectDisposedException) { /* 数据库已关闭时，下次启动会把这次运行标为待确认。 */ }
        lock (gate)
        {
            if (sessions.TryGetValue(device.Id, out var current) && ReferenceEquals(current, session))
                sessions.Remove(device.Id);
        }
        session.Stop.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        Task[] running;
        lock (gate)
        {
            if (disposed)
                return;
            disposed = true;
            lifetime.Cancel();
            running = sessions.Values.Select(session => session.Work).ToArray();
        }
        await Task.WhenAll(running).ConfigureAwait(false);
        lifetime.Dispose();
    }
}
