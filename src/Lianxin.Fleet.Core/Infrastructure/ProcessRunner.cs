using System.Diagnostics;
using System.Text;

namespace Lianxin.Fleet.Core.Infrastructure;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string file, IReadOnlyList<string> arguments, TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public sealed class ProcessRunException(string message, bool interrupted, Exception inner) : Exception(message, inner)
{
    public bool Interrupted { get; } = interrupted;
}

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(string file, IReadOnlyList<string> arguments, TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var process = new Process { StartInfo = CreateStartInfo(file, arguments) };
        var started = false;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            if (!process.Start())
                throw new InvalidOperationException("设备工具未能启动。");
            started = true;
            var output = ReadBoundedAsync(process.StandardOutput, deadline.Token);
            var errors = ReadBoundedAsync(process.StandardError, deadline.Token);
            var exit = process.WaitForExitAsync(deadline.Token);
            var pending = new List<Task> { output, errors, exit };
            while (pending.Count > 0)
            {
                var completed = await Task.WhenAny(pending).ConfigureAwait(false);
                await completed.ConfigureAwait(false);
                pending.Remove(completed);
            }
            return new ProcessResult(process.ExitCode, await output.ConfigureAwait(false), await errors.ConfigureAwait(false));
        }
        catch (Exception error)
        {
            if (started)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
            }
            throw new ProcessRunException(started ? "设备工具执行中断或输出过大，请核对设备状态。"
                : "设备工具无法启动，请检查 Python 路径及安装。", started, error);
        }
    }

    internal static ProcessStartInfo CreateStartInfo(string file, IReadOnlyList<string> arguments)
    {
        if (string.IsNullOrWhiteSpace(file) || file.Contains('\0'))
            throw new ArgumentException("程序路径无效。");
        var info = new ProcessStartInfo(file)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        info.Environment["NO_COLOR"] = "1";
        info.Environment["PYTHONUTF8"] = "1";
        return info;
    }

    private static async Task<string> ReadBoundedAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        var result = new StringBuilder();
        var buffer = new char[4096];
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (result.Length + count > 1024 * 1024)
                throw new InvalidDataException("设备工具输出超过 1 MB。");
            result.Append(buffer, 0, count);
        }
        return result.ToString();
    }
}
