using System.Diagnostics;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Core.Devices;

public sealed record WdaCommand(string Label, string File, IReadOnlyList<string> Arguments);

public sealed partial class WdaHost(string? pythonPath = null, Action<string>? log = null) : IAsyncDisposable
{
    public const string DefaultRunnerBundleId = "com.lianxin.fleet.webdriveragent.xctrunner";
    private readonly object sync = new();
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private readonly Dictionary<string, DeviceChannel> channels = new(StringComparer.Ordinal);
    private CancellationTokenSource? lifetime;
    private Task? stopping;
    private bool disposed;
    private readonly string python = pythonPath ?? Environment.GetEnvironmentVariable("PYTHON_PATH")
        ?? (OperatingSystem.IsWindows() ? "python" : "python3");

    private sealed record DeviceChannel(string BundleId, Process Process, UsbPortForwarder Forwarder, Task OutputReaders);

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
        if (devices.Select(d => Usbmux.Normalize(d.Udid)).Distinct().Count() != devices.Count
            || devices.Select(d => d.WdaPort).Distinct().Count() != devices.Count)
            throw new ArgumentException("设备 UDID 或端口重复。");
        var commands = new List<WdaCommand>();
        foreach (var device in devices)
        {
            if (!DeviceBridge.ValidUdid(device.Udid) || device.WdaPort is < 1024 or > 65535)
                throw new ArgumentException("设备 UDID 或本机端口无效。");
            if (device.Name.Length > 80 || device.Name.IndexOfAny(['\r', '\n', '\0']) >= 0)
                throw new ArgumentException("设备名称无效。");
            // 每台手机只使用一个 xcuitest 进程，本机端口的转发在进程内完成。
            commands.Add(new WdaCommand(device.Name + "/WDA", pythonPath, ["-m", "pymobiledevice3", "--no-color",
                "developer", "dvt", "xcuitest", "--udid", device.Udid, "--env", $"USE_PORT={DeviceBridge.WdaDevicePort}", bundleId]));
        }
        return commands;
    }

    public Task StartAsync(IReadOnlyList<FleetDevice> devices, string bundleId, CancellationToken cancellationToken = default)
        => StartDevicesAsync(devices, bundleId, exclusive: true, cancellationToken);

    public Task<IReadOnlyList<FleetDevice>> EnsureStartedAsync(IReadOnlyList<FleetDevice> devices, string bundleId, CancellationToken cancellationToken = default)
        => StartDevicesAsync(devices, bundleId, exclusive: false, cancellationToken);

    private async Task<IReadOnlyList<FleetDevice>> StartDevicesAsync(IReadOnlyList<FleetDevice> devices, string bundleId, bool exclusive,
        CancellationToken cancellationToken)
    {
        var targets = devices.ToArray();
        var commands = BuildPlan(targets, bundleId, python);
        await operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        CancellationTokenSource? created = null;
        try
        {
            CancellationToken hostToken;
            lock (sync)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                if (stopping is { IsCompleted: false })
                    throw new InvalidOperationException("WDA 通道正在停止，请稍后再连接。");
                if (exclusive && lifetime is not null)
                    throw new InvalidOperationException("请先停止当前 WDA 主机进程。");
                if (lifetime is null)
                {
                    created = exclusive ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken) : new();
                    lifetime = created;
                    stopping = null;
                }
                hostToken = lifetime.Token;
            }
            using var request = CancellationTokenSource.CreateLinkedTokenSource(hostToken, cancellationToken);
            return await LaunchMissingAsync(commands, targets, bundleId, hostToken, request.Token).ConfigureAwait(false);
        }
        catch when (created is not null)
        {
            await ClearChannelsAsync().ConfigureAwait(false);
            throw;
        }
        finally { operationGate.Release(); }
    }

    private async Task<IReadOnlyList<FleetDevice>> LaunchMissingAsync(IReadOnlyList<WdaCommand> commands, IReadOnlyList<FleetDevice> devices,
        string bundleId, CancellationToken hostToken, CancellationToken requestToken)
    {
        foreach (var (id, channel) in channels.ToArray())
        {
            if (!channel.Process.HasExited)
                continue;
            channels.Remove(id);
            await StopChannelAsync(channel).ConfigureAwait(false);
        }
        Publish();

        requestToken.ThrowIfCancellationRequested();
        foreach (var device in devices)
        {
            if (channels.TryGetValue(Usbmux.Normalize(device.Udid), out var channel)
                && (channel.BundleId != bundleId || channel.Forwarder.LocalPort != device.WdaPort))
                throw new InvalidOperationException($"「{device.Name}」已有不同配置的 WDA 通道，请停止后再使用新配置。");
        }

        var startedInBatch = 0;
        for (var index = 0; index < devices.Count; index++)
        {
            requestToken.ThrowIfCancellationRequested();
            var device = devices[index];
            var key = Usbmux.Normalize(device.Udid);
            if (channels.ContainsKey(key))
                continue;
            if (startedInBatch == 8)
            {
                await Task.Delay(1000, requestToken).ConfigureAwait(false);
                startedInBatch = 0;
            }
            var channel = await LaunchAsync(commands[index], device, bundleId, hostToken).ConfigureAwait(false);
            if (channel is not null)
                channels.Add(key, channel);
            Publish();
            startedInBatch++;
        }
        return devices.Where(device => channels.TryGetValue(Usbmux.Normalize(device.Udid), out var channel)
            && !channel.Process.HasExited).ToArray();
    }

    private async Task<DeviceChannel?> LaunchAsync(WdaCommand command, FleetDevice device, string bundleId, CancellationToken hostToken)
    {
        UsbPortForwarder forwarder;
        try
        {
            forwarder = new UsbPortForwarder(device.WdaPort,
                token => Usbmux.ConnectAsync(device.Udid, DeviceBridge.WdaDevicePort, token));
        }
        catch (SocketException)
        {
            Log(device.Name + "/USB", $"本机端口 {device.WdaPort} 无法监听，请检查端口是否被占用。");
            return null;
        }

        var process = new Process { StartInfo = ProcessRunner.CreateStartInfo(command.File, command.Arguments), EnableRaisingEvents = true };
        try
        {
            process.Exited += (_, _) =>
            {
                if (!hostToken.IsCancellationRequested)
                    Log(command.Label, "进程已退出，可在设备重新接入后再次连接。");
            };
            if (!process.Start())
                throw new InvalidOperationException("无法启动设备进程。");
            var readers = Task.WhenAll(PumpOutputAsync(process.StandardOutput, command.Label, hostToken),
                PumpOutputAsync(process.StandardError, command.Label, hostToken));
            Log(command.Label, "进程已启动，设备在线状态以控制台回报为准。");
            return new(bundleId, process, forwarder, readers);
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            process.Dispose();
            await forwarder.DisposeAsync().ConfigureAwait(false);
            Log(command.Label, "启动失败，请检查 Python 与 pymobiledevice3，然后重新连接。");
            return null;
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

}
