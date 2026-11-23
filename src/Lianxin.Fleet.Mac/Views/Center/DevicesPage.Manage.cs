using Avalonia.Interactivity;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Mac.ViewModels;
using Lianxin.Fleet.Mac.Views.Dialogs;

namespace Lianxin.Fleet.Mac.Views.Center;

// 设备管理：行右键「设备详情」、工具栏「更多」里的批量镜像与电源操作，以及电量列刷新。
public sealed partial class DevicesPage
{
    private bool batteryBusy, batchBusy;

    private DeviceManager Manager => center.Runtime.DeviceManager;

    // 每次刷新表格时调用；电量在 DeviceManager 里按分钟缓存，期内不会启动子进程。
    private void BatteryTick() => _ = RefreshBatteriesAsync(force: false);

    private async Task RefreshBatteriesAsync(bool force)
    {
        if (batteryBusy || center.IsClosing || center.Rows.Count == 0)
            return;
        batteryBusy = true;
        try
        {
            var rows = center.Rows.ToArray();
            var levels = await Manager.GetBatteriesAsync(rows.Select(row => row.Snapshot).ToArray(), force, center.Lifetime);
            foreach (var row in rows)
                row.SetBattery(levels.GetValueOrDefault(row.Id));
            if (force)
                center.Report($"已刷新电量：读到 {levels.Count} 台设备。");
        }
        catch (OperationCanceledException) when (center.Lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (force && !center.IsClosing)
                center.Report("刷新电量失败：" + error.Message);
        }
        finally { batteryBusy = false; }
    }

    private async void RefreshBatteryClicked(object? sender, RoutedEventArgs e)
    {
        if (batteryBusy)
        {
            center.Report("电量正在刷新，请稍候。");
            return;
        }
        center.Report("正在读取电量…");
        await RefreshBatteriesAsync(force: true);
    }

    private async void DetailsClicked(object? sender, RoutedEventArgs e)
    {
        if ((DeviceGrid.SelectedItem as DeviceRow ?? ContextRows().FirstOrDefault()) is not { } row)
        {
            center.Report("请先选中一台设备。");
            return;
        }
        await new DeviceDetailDialog(center, row).ShowDialog(center);
    }

    private void BatchMountClicked(object? sender, RoutedEventArgs e) =>
        RunBatch("挂载镜像", null, false, Manager.MountDeveloperImageAsync);

    private void BatchUnmountClicked(object? sender, RoutedEventArgs e) =>
        RunBatch("卸载镜像", count => $"卸载 {count} 台手机的开发者镜像？这些手机上的脚本和自动化会先停止，重新挂载后才能再启动自动化。",
            true, async (device, token) => await Manager.UnmountDeveloperImageAsync(device, token));

    private void BatchRestartClicked(object? sender, RoutedEventArgs e) =>
        RunBatch("重启", count => $"重启 {count} 台手机？这些手机上的脚本和自动化会先停止；重启后需要重新挂载开发者镜像并启动自动化。",
            true, Manager.RestartAsync);

    private void BatchShutdownClicked(object? sender, RoutedEventArgs e) =>
        RunBatch("关机", count => $"关闭 {count} 台手机？这些手机上的脚本和自动化会先停止，关机后需要在手机上按电源键开机。",
            true, Manager.ShutdownAsync);

    // 批量操作只针对勾选且在线的设备，最多 4 台同时进行；失败与待确认的原因留在「消息」列。
    private async void RunBatch(string label, Func<int, string>? confirm, bool stopAutomation,
        Func<FleetDevice, CancellationToken, Task> action)
    {
        if (batchBusy)
        {
            center.Report("上一批设备操作仍在进行，请稍候。");
            return;
        }
        var selected = center.SelectedRows();
        var rows = selected.Where(row => row.Online).ToArray();
        if (rows.Length == 0)
        {
            await DialogKit.AlertAsync(center, label, "请先勾选在线（USB 或 Wi-Fi 已连接）的设备。");
            return;
        }
        if (confirm is not null && (!await DialogKit.ConfirmAsync(center, label, confirm(rows.Length), label) || center.IsClosing))
            return;
        batchBusy = true;
        try
        {
            foreach (var row in rows)
                row.SetNote($"{label}：进行中…");
            if (stopAutomation)
            {
                center.Runtime.Scripts.Stop(rows.Select(row => row.Id).ToArray());
                await center.Runtime.Host.StopDevicesAsync(rows.Select(row => row.Device).ToArray());
            }
            center.Report($"正在对 {rows.Length} 台设备执行{label}…");
            var byId = rows.ToDictionary(row => row.Id);
            var results = await DeviceManager.RunBatchAsync(rows.Select(row => row.Device).ToArray(), action,
                new Progress<DeviceBatchResult>(result => byId[result.DeviceId].SetNote(result.Succeeded ? null
                    : $"{label}：{(result.Uncertain ? "结果待确认" : "失败")} · {result.Error}")), 4, center.Lifetime);
            center.Report(BatchSummary(label, results, selected.Count - rows.Length));
        }
        catch (OperationCanceledException) when (center.Lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (!center.IsClosing)
                center.Report($"{label}失败：{error.Message}");
        }
        finally { batchBusy = false; }
    }

    internal static string BatchSummary(string label, IReadOnlyList<DeviceBatchResult> results, int skipped)
    {
        var failed = results.Count(result => !result.Succeeded && !result.Uncertain);
        var uncertain = results.Count(result => result.Uncertain);
        return $"{label}完成：成功 {results.Count(result => result.Succeeded)} 台"
            + (failed > 0 ? $"，失败 {failed} 台" : "") + (uncertain > 0 ? $"，待确认 {uncertain} 台" : "")
            + (skipped > 0 ? $"，{skipped} 台不在线已跳过" : "") + (failed + uncertain > 0 ? "。原因见「消息」列。" : "。");
    }
}
