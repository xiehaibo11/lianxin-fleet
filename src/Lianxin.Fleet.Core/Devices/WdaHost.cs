using System.Diagnostics;
using System.Text.RegularExpressions;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Core.Devices;

public sealed record WdaCommand(string Label, string File, IReadOnlyList<string> Arguments);

public sealed class WdaHost(string? pythonPath = null, Action<string>? log = null) : IAsyncDisposable
{
    public const string DefaultRunnerBundleId = "com.lianxin.fleet.webdriveragent.xctrunner";

    private readonly object sync = new();
    private readonly List<Process> children = [];
    private readonly List<Task> streams = [];
    private readonly List<UsbPortForwarder> forwarders = [];
    private CancellationTokenSource? lifetime;
    private Task? starting;
    private Task? stopping;
    private bool disposed;
    private readonly string python = pythonPath ?? Environment.GetEnvironmentVariable("PYTHON_PATH")
        ?? (OperatingSystem.IsWindows() ? "python" : "python3");

    public bool IsRunning
    {
        get
        {
            lock (sync)
                return lifetime is not null;
        }
    }

    public static IReadOnlyList<WdaCommand> BuildPlan(IReadOnlyList<FleetDevice> devices, string bundleId, string pythonPath)
    {
        if (!Regex.IsMatch(bundleId, "^[A-Za-z0-9-]+(?:\\.[A-Za-z0-9-]+)+$") || bundleId.Length > 160)
            throw new ArgumentException($"请输入手机上已安装的 WDA Runner Bundle ID，例如 {DefaultRunnerBundleId}。");
        if (string.IsNullOrWhiteSpace(pythonPath) || pythonPath.Contains('\0'))
            throw new ArgumentException("Python 路径无效。");
        if (devices.Count is < 1 or > 1000)
            throw new ArgumentException("每次启动 1 至 1000 台设备。");
        if (devices.Select(d => d.Udid).Distinct().Count() != devices.Count
            || devices.Select(d => d.WdaPort).Distinct().Count() != devices.Count)
            throw new ArgumentException("设备 UDID 或端口重复。");
        var commands = new List<WdaCommand>();
        foreach (var device in devices)
        {
            if (!DeviceBridge.ValidUdid(device.Udid) || device.WdaPort is < 1024 or > 65535)
                throw new ArgumentException("设备 UDID 或本机端口无效。");
            if (device.Name.Length > 80 || device.Name.IndexOfAny(['\r', '\n', '\0']) >= 0)
                throw new ArgumentException("设备名称无效。");
            // 每台手机只剩一个 xcuitest 进程；本机端口到手机 8100 的转发在进程内完成（UsbPortForwarder）。
            commands.Add(new WdaCommand(device.Name + "/WDA", pythonPath, ["-m", "pymobiledevice3", "--no-color",
                "developer", "dvt", "xcuitest", "--udid", device.Udid, "--env", $"USE_PORT={DeviceBridge.WdaDevicePort}", bundleId]));
        }
        return commands;
    }

    public Task StartAsync(IReadOnlyList<FleetDevice> devices, string bundleId, CancellationToken cancellationToken = default)
    {
        var commands = BuildPlan(devices, bundleId, python);
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (lifetime is not null)
                throw new InvalidOperationException("请先停止当前 WDA 主机进程。");
            lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            stopping = null;
            starting = LaunchBatchesAsync(commands, devices, lifetime.Token);
            return StartAndCleanupAsync(starting);
        }
    }

    private void Forward(FleetDevice device)
    {
        try
        {
            var forwarder = new UsbPortForwarder(device.WdaPort,
                token => Usbmux.ConnectAsync(device.Udid, DeviceBridge.WdaDevicePort, token));
            lock (sync)
                forwarders.Add(forwarder);
        }
        catch (System.Net.Sockets.SocketException)
        {
            Log(device.Name + "/USB", $"本机端口 {device.WdaPort} 已被占用，请关闭其他转发程序。");
        }
    }

    private async Task StartAndCleanupAsync(Task startup)
    {
        try
        {
            await startup.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { await StopAsync().ConfigureAwait(false); throw; }
    }

    private async Task LaunchBatchesAsync(IReadOnlyList<WdaCommand> commands, IReadOnlyList<FleetDevice> devices,
        CancellationToken cancellationToken)
    {
        for (var offset = 0; offset < commands.Count; offset += 8)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var index = offset; index < Math.Min(offset + 8, commands.Count); index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Forward(devices[index]);
                Launch(commands[index], cancellationToken);
            }
            if (offset + 8 < commands.Count)
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }
    }

    private void Launch(WdaCommand command, CancellationToken cancellationToken)
    {
        var process = new Process { StartInfo = ProcessRunner.CreateStartInfo(command.File, command.Arguments), EnableRaisingEvents = true };
        try
        {
            process.Exited += (_, _) =>
            {
                lock (sync)
                    if (lifetime?.IsCancellationRequested == false)
                        Log(command.Label, "进程已退出；不会自动重启，请检查设备和工具。");
            };
            if (!process.Start())
                throw new InvalidOperationException("无法启动设备进程。");
            lock (sync)
            {
                children.Add(process);
                streams.Add(PumpOutputAsync(process.StandardOutput, command.Label, cancellationToken));
                streams.Add(PumpOutputAsync(process.StandardError, command.Label, cancellationToken));
            }
            Log(command.Label, "进程已启动，设备在线状态以控制台回报为准。");
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            process.Dispose();
            Log(command.Label, "启动失败，请检查 Python 与 pymobiledevice3；不会自动重试。");
        }
    }

    private async Task PumpOutputAsync(StreamReader reader, string label, CancellationToken cancellationToken)
    {
        var buffer = new char[1000];
        try
        {
            int count;
            while ((count = await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                Log(label, new string(buffer, 0, count).TrimEnd());
        }
        catch (Exception error) when (error is OperationCanceledException or IOException or ObjectDisposedException) { }
    }

    private void Log(string label, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        try
        {
            log?.Invoke($"[{label}] {(message.Length > 1000 ? message[..1000] : message)}");
        }
        catch { /* A UI log callback must not terminate device process cleanup. */ }
    }

    public Task StopAsync()
    {
        lock (sync)
            return stopping ??= StopCoreAsync();
    }

    private async Task StopCoreAsync()
    {
        CancellationTokenSource? source;
        Task? startup;
        lock (sync)
        {
            source = lifetime;
            startup = starting;
            source?.Cancel();
        }
        if (startup is not null)
        {
            try
            {
                await startup.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
        }
        Process[] owned;
        lock (sync)
        {
            owned = children.ToArray();
            children.Clear();
        }
        foreach (var process in owned)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            }
            catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or TimeoutException)
            {
                Log("主机", "停止自建子进程失败，请核对当前进程状态。");
            }
            finally { process.Dispose(); }
        }
        Task[] readers;
        lock (sync)
        {
            readers = streams.ToArray();
            streams.Clear();
        }
        await Task.WhenAll(readers).ConfigureAwait(false);
        UsbPortForwarder[] ports;
        lock (sync)
        {
            ports = forwarders.ToArray();
            forwarders.Clear();
        }
        foreach (var port in ports)
            await port.DisposeAsync().ConfigureAwait(false);
        lock (sync)
        {
            lifetime = null;
            starting = null;
        }
        source?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        disposed = true;
        await StopAsync().ConfigureAwait(false);
    }
}
