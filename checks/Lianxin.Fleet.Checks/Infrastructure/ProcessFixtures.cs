using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Checks.Infrastructure;

// 记录每次调用的假进程，按参数返回预设输出，不启动真实 Python。
internal sealed class RecordingRunner(Func<IReadOnlyList<string>, ProcessResult> reply) : IProcessRunner
{
    public List<(string File, string[] Arguments, TimeSpan Timeout)> Calls { get; } = [];

    public Task<ProcessResult> RunAsync(string file, IReadOnlyList<string> arguments, TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (Calls)
            Calls.Add((file, arguments.ToArray(), timeout));
        return Task.FromResult(reply(arguments));
    }

    public static ProcessResult Ok(string output = "") => new(0, output, "");
}
