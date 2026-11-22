using System.IO;
using System.Windows;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Desktop.Views.Main;

public partial class MainWindow
{
    private readonly string _runnerBundleFile;

    private async void ConnectClicked(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        var selected = SelectedIds().ToHashSet();
        var bundleId = FleetValidation.BundleId(WdaBundleInput.Text.Trim());
        _timer.Stop();
        try
        {
            StatusLabel.Text = "正在识别 USB 设备…";
            var connected = await _service.DiscoverConnectedAsync(_lifetime.Token);
            ApplySnapshot(_service.GetSnapshot());
            var targets = connected.Where(device => selected.Count == 0 || selected.Contains(device.Id)).ToArray();
            if (targets.Length == 0)
            {
                StatusLabel.Text = selected.Count == 0
                    ? "未发现 USB 设备。请连接并解锁 iPhone，按手机提示信任这台电脑。"
                    : "勾选的设备当前未通过 USB 连接。";
                return;
            }

            SetCasting(true);
            StatusLabel.Text = $"正在启动 {targets.Length} 台设备的 WDA 控制，预览独立加载…";
            var started = await _host.EnsureStartedAsync(targets, bundleId, _lifetime.Token);
            if (started.Count == 0)
            {
                StatusLabel.Text = "正在加载 USB 预览。未能启动鼠标控制，请查看 WDA 日志。";
                return;
            }

            var ids = started.Select(device => device.Id).ToArray();
            await WaitForWdaAsync(ids);
            var ready = DeviceCards.Where(card => card.CanControl && ids.Contains(card.Id)).ToArray();
            if (ready.Length > 0)
            {
                if (_master is null || !ready.Contains(_master))
                    SetMaster(ready[0]);
                var saved = RememberRunnerBundleId(bundleId);
                StatusLabel.Text = $"本次连接完成，启动了 {ready.Length}/{targets.Length} 台设备的 WDA 控制。"
                    + (ready.Length < targets.Length ? "其余设备请查看状态与 WDA 日志。" : "")
                    + (saved ? "" : "本次 Bundle ID 未能保存。");
            }
            else
            {
                StatusLabel.Text = "正在尝试 USB 预览。鼠标控制需要已解锁的手机和匹配的签名 WDA Runner。";
            }
        }
        finally
        {
            if (!_closing)
                _timer.Start();
        }
    });

    private async Task WaitForWdaAsync(IReadOnlyList<long> ids)
    {
        var targets = ids.ToHashSet();
        using var readiness = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        readiness.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            while (true)
            {
                var snapshot = await _service.RefreshDeviceStatusesAsync(ids, readiness.Token);
                ApplySnapshot(snapshot);
                var readyCount = snapshot.Devices.Count(item =>
                    item.Status.Online && item.UsbConnected != false && targets.Contains(item.Device.Id));
                if (readyCount == targets.Count)
                    return;
                StatusLabel.Text = $"正在等待 WDA 就绪 {readyCount}/{targets.Count} 台…";
                await Task.Delay(TimeSpan.FromSeconds(1), readiness.Token);
            }
        }
        catch (OperationCanceledException) when (readiness.IsCancellationRequested && !_lifetime.IsCancellationRequested)
        {
            ApplySnapshot(_service.GetSnapshot());
        }
    }

    private static string LoadRunnerBundleId(string path)
    {
        try
        {
            return FleetValidation.BundleId(File.ReadAllText(path).Trim());
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return WdaHost.DefaultRunnerBundleId;
        }
    }

    private bool RememberRunnerBundleId(string bundleId)
    {
        var pendingPath = _runnerBundleFile + ".tmp";
        try
        {
            File.WriteAllText(pendingPath, bundleId);
            File.Move(pendingPath, _runnerBundleFile, overwrite: true);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
