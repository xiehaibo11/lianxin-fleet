using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Core.Devices;

public sealed partial class WdaHost
{
    private volatile string[] channelUdids = [];

    // 已建立通道的设备（规范化 UDID）；进程退出后要等下一次连接或停止才会移除。
    public IReadOnlyCollection<string> ChannelUdids => channelUdids;

    // 只在持有 operationGate 时修改 channels，修改后发布一份快照给界面线程读取。
    private void Publish() => channelUdids = channels.Keys.ToArray();

    public Task StopAsync()
    {
        lock (sync)
            return stopping ??= StopCoreAsync();
    }

    // 只停止指定设备的通道，其余设备继续运行。
    public async Task<int> StopDevicesAsync(IReadOnlyList<FleetDevice> devices)
    {
        var keys = devices.Select(device => Usbmux.Normalize(device.Udid)).ToHashSet(StringComparer.Ordinal);
        await operationGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var removed = channels.Where(item => keys.Contains(item.Key)).ToArray();
            foreach (var (key, _) in removed)
                channels.Remove(key);
            Publish();
            await Task.WhenAll(removed.Select(item => StopChannelAsync(item.Value))).ConfigureAwait(false);
            if (channels.Count == 0)
                await ClearChannelsAsync().ConfigureAwait(false);
            return removed.Length;
        }
        finally { operationGate.Release(); }
    }

    private async Task StopCoreAsync()
    {
        lock (sync)
            lifetime?.Cancel();
        await operationGate.WaitAsync().ConfigureAwait(false);
        try
        {
            await ClearChannelsAsync().ConfigureAwait(false);
        }
        finally { operationGate.Release(); }
    }

    private async Task ClearChannelsAsync()
    {
        CancellationTokenSource? source;
        lock (sync)
        {
            source = lifetime;
            source?.Cancel();
        }
        var owned = channels.Values.ToArray();
        channels.Clear();
        Publish();
        try
        {
            await Task.WhenAll(owned.Select(StopChannelAsync)).ConfigureAwait(false);
        }
        finally
        {
            lock (sync)
            {
                lifetime = null;
                source?.Dispose();
            }
        }
    }

    private async Task StopChannelAsync(DeviceChannel channel)
    {
        try
        {
            if (!channel.Process.HasExited)
                channel.Process.Kill(entireProcessTree: true);
            await channel.Process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or TimeoutException)
        {
            Log("主机", "停止自建子进程失败，请核对当前进程状态。");
        }
        finally
        {
            channel.Process.StandardOutput.Dispose();
            channel.Process.StandardError.Dispose();
            channel.Process.Dispose();
        }
        await channel.Forwarder.DisposeAsync().ConfigureAwait(false);
        await channel.OutputReaders.ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        lock (sync)
            disposed = true;
        await StopAsync().ConfigureAwait(false);
    }
}
