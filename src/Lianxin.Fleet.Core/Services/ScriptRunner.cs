using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Services;

public sealed record ScriptActivity(long DeviceId, long RunId, string ScriptName, string Message);
public sealed record ScriptStartResult(IReadOnlyList<long> Started, IReadOnlyList<long> Busy);

// 每台手机同一时间只跑一个脚本；不同手机最多并行 MaxParallelDevices 台。
public sealed class ScriptRunner : IAsyncDisposable
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
        public Task Work = Task.CompletedTask;
    }

    public ScriptRunner(FleetStore store, IDeviceBridge bridge)
    {
        this.store = store;
        this.bridge = bridge;
        store.RecoverInterruptedScriptRuns();
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
            return sessions.ToDictionary(item => item.Key,
                item => new ScriptActivity(item.Key, item.Value.RunId, item.Value.ScriptName, item.Value.Message));
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

    private async Task RunAsync(FleetDevice device, FleetScript script, IReadOnlyList<ScriptStep> steps, Session session)
    {
        // 让出调用方持有的锁，脚本主体在线程池上执行。
        await Task.Yield();
        var token = session.Stop.Token;
        var state = ScriptRunState.Succeeded;
        string? message;
        var acquired = false;
        try
        {
            await slots.WaitAsync(token).ConfigureAwait(false);
            acquired = true;
            var rounds = script.Repeat == 0 ? int.MaxValue : script.Repeat;
            for (var round = 1; round <= rounds; round++)
                for (var index = 0; index < steps.Count; index++)
                {
                    token.ThrowIfCancellationRequested();
                    var step = steps[index];
                    var progress = script.Repeat == 1 ? "" : script.Repeat == 0 ? $"第 {round} 轮 · " : $"第 {round}/{script.Repeat} 轮 · ";
                    session.Message = $"{progress}第 {index + 1}/{steps.Count} 步 · {step.Label}";
                    if (step.Action is { } action)
                        await bridge.ExecuteAsync(device, action, step.Parameters, token).ConfigureAwait(false);
                    if (step.WaitSeconds > 0)
                        await Task.Delay(TimeSpan.FromSeconds(step.WaitSeconds), token).ConfigureAwait(false);
                }
            message = script.Repeat == 1 ? "脚本已完成。" : $"脚本已完成 {script.Repeat} 轮。";
        }
        catch (Exception) when (token.IsCancellationRequested)
        {
            state = ScriptRunState.Stopped;
            message = lifetime.IsCancellationRequested ? "程序关闭，脚本已停止。" : "已手动停止。";
        }
        catch (Exception error)
        {
            state = error is DeviceOperationException { Uncertain: true } ? ScriptRunState.Unknown : ScriptRunState.Failed;
            message = $"{session.Message} · 失败：{error.Message}";
        }
        finally
        {
            if (acquired)
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
