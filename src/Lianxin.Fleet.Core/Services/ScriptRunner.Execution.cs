using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Services;

// 语句执行：块与流程控制、等待、日志。设备上的查找与图色在 ScriptRunner.Probes.cs。
public sealed partial class ScriptRunner
{
    // 「当」循环每次进入最多执行的轮数，防止条件一直成立时无休止地查询手机。
    public const int MaxLoopIterations = 10_000;

    private enum Flow
    {
        Next, Break, Stop
    }

    // 「失败」命令：脚本按用户给出的原因以失败结束。
    private sealed class ScriptFailedException(int line, string message) : Exception(message)
    {
        public int Line { get; } = line;
    }

    private sealed class RunContext(FleetDevice device, Session session, CancellationToken token, int topCount)
    {
        public FleetDevice Device { get; } = device;
        public Session Session { get; } = session;
        public CancellationToken Token { get; } = token;
        public int TopCount { get; } = topCount;
        public string Progress = "";
        public string StepText = "";
        public string? Log;
        public int Line;
        public int Captures;
        // 是否占着并发名额（MaxParallelDevices）；暂停期间让出。
        public bool HoldsSlot;
    }

    private async Task<Flow> RunBlockAsync(IReadOnlyList<ScriptStep> steps, RunContext run, bool top)
    {
        for (var index = 0; index < steps.Count; index++)
        {
            await CheckpointAsync(run).ConfigureAwait(false);
            var step = steps[index];
            Show(step, run, top ? index + 1 : 0);
            var flow = await RunStepAsync(step, run, top ? index + 1 : 0).ConfigureAwait(false);
            if (flow != Flow.Next)
                return flow;
        }
        return Flow.Next;
    }

    // 两条语句之间检查停止和暂停。暂停期间让出并发名额，排队的其他设备可以先跑；继续后重新排队取得名额。
    private async Task CheckpointAsync(RunContext run)
    {
        run.Token.ThrowIfCancellationRequested();
        while (run.Session.Pause is { } pause)
        {
            if (run.HoldsSlot)
            {
                run.HoldsSlot = false;
                slots.Release();
            }
            await pause.Task.WaitAsync(run.Token).ConfigureAwait(false);
        }
        if (!run.HoldsSlot)
        {
            await slots.WaitAsync(run.Token).ConfigureAwait(false);
            run.HoldsSlot = true;
        }
    }

    // 顶层语句显示「第 i/n 步」，块内语句显示行号；用过「日志」后显示最近一条日志。
    private static void Show(ScriptStep step, RunContext run, int topIndex)
    {
        run.Line = step.Line;
        run.StepText = run.Progress + (topIndex > 0 ? $"第 {topIndex}/{run.TopCount} 步 · {step.Label}"
            : $"第 {step.Line} 行 · {step.Label}");
        run.Session.Message = run.Log is null ? run.StepText : run.Progress + run.Log;
    }

    private async Task<Flow> RunStepAsync(ScriptStep step, RunContext run, int topIndex)
    {
        var token = run.Token;
        switch (step.Kind)
        {
            case ScriptStepKind.Command:
                if (step.Action is { } action)
                    await bridge.ExecuteAsync(run.Device, action, step.Parameters, token).ConfigureAwait(false);
                if (step.WaitSeconds > 0)
                    await Task.Delay(TimeSpan.FromSeconds(step.WaitSeconds), token).ConfigureAwait(false);
                return Flow.Next;
            case ScriptStepKind.Repeat:
                for (var round = 0; round < step.Count; round++)
                {
                    if (round > 0)
                    {
                        await CheckpointAsync(run).ConfigureAwait(false);
                        Show(step, run, topIndex);
                    }
                    var flow = await RunBlockAsync(step.Body, run, top: false).ConfigureAwait(false);
                    if (flow == Flow.Break)
                        break;
                    if (flow == Flow.Stop)
                        return flow;
                }
                return Flow.Next;
            case ScriptStepKind.If:
                return await RunBlockAsync(await TestAsync(step.Condition!, run).ConfigureAwait(false) ? step.Body : step.ElseBody,
                    run, top: false).ConfigureAwait(false);
            case ScriptStepKind.While:
                for (var round = 0; ; round++)
                {
                    if (round > 0)
                    {
                        await CheckpointAsync(run).ConfigureAwait(false);
                        Show(step, run, topIndex);
                    }
                    if (!await TestAsync(step.Condition!, run).ConfigureAwait(false))
                        return Flow.Next;
                    if (round >= MaxLoopIterations)
                        throw new InvalidOperationException($"「当」循环已执行 {MaxLoopIterations} 次条件仍成立，请检查条件或加入「跳出」。");
                    var flow = await RunBlockAsync(step.Body, run, top: false).ConfigureAwait(false);
                    if (flow == Flow.Break)
                        return Flow.Next;
                    if (flow == Flow.Stop)
                        return flow;
                }
            case ScriptStepKind.Break:
                return Flow.Break;
            case ScriptStepKind.Stop:
                return Flow.Stop;
            case ScriptStepKind.Fail:
                throw new ScriptFailedException(step.Line, step.Text ?? "脚本主动结束。");
            case ScriptStepKind.RandomWait:
                var seconds = step.WaitSeconds + Random.Shared.NextDouble() * (step.MaxWaitSeconds - step.WaitSeconds);
                await Task.Delay(TimeSpan.FromSeconds(seconds), token).ConfigureAwait(false);
                return Flow.Next;
            case ScriptStepKind.Log:
                run.Log = step.Text;
                run.Session.Message = run.Progress + run.Log;
                return Flow.Next;
            case ScriptStepKind.Capture:
                await CaptureAsync(step, run).ConfigureAwait(false);
                return Flow.Next;
            case ScriptStepKind.TapProbe:
                await TapAsync(step.Probe!, run).ConfigureAwait(false);
                return Flow.Next;
            case ScriptStepKind.WaitProbe or ScriptStepKind.WaitProbeGone:
                await WaitForAsync(step, run, present: step.Kind == ScriptStepKind.WaitProbe).ConfigureAwait(false);
                return Flow.Next;
            case ScriptStepKind.InputToElement:
                await InputAsync(step, run).ConfigureAwait(false);
                return Flow.Next;
            default:
                throw new InvalidOperationException("不支持的脚本语句。");
        }
    }
}
