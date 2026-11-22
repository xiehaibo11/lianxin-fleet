using System.Diagnostics;
using System.IO;
using System.Windows;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;
using Lianxin.Fleet.Core.Validation;
using Lianxin.Fleet.Desktop.ViewModels;
using Lianxin.Fleet.Desktop.Views.Dialogs;
using Lianxin.Fleet.Desktop.Views.Main;
using Microsoft.Win32;

namespace Lianxin.Fleet.Desktop.Views.Center;

// 工具栏动作：自动化启停、脚本、重置 USB、投屏、安装与导出。
public partial class CenterWindow
{
    private MainWindow? mirror;

    public string RunnerBundleFile => Path.Combine(Runtime.DataDirectory, "wda-runner.txt");

    // 与集控投屏窗口共用同一个文件，两边修改都生效。
    public string LoadRunnerBundleId()
    {
        try
        {
            return FleetValidation.BundleId(File.ReadAllText(RunnerBundleFile).Trim());
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return WdaHost.DefaultRunnerBundleId;
        }
    }

    public void SaveRunnerBundleId(string bundleId)
    {
        bundleId = FleetValidation.BundleId(bundleId);
        File.WriteAllText(RunnerBundleFile + ".tmp", bundleId);
        File.Move(RunnerBundleFile + ".tmp", RunnerBundleFile, overwrite: true);
    }

    // 勾选了设备就用勾选的；否则用全部（可按条件过滤）。
    private IReadOnlyList<DeviceRow> Targets(Func<DeviceRow, bool> eligible)
    {
        var selected = SelectedRows();
        return (selected.Count > 0 ? selected : Rows).Where(eligible).ToArray();
    }

    public void StartAutomation() => Run(async () =>
    {
        var targets = Targets(row => row.Online);
        if (targets.Count == 0)
            throw new InvalidOperationException("没有在线设备。请连接并信任手机；勾选设备时只启动勾选的在线设备。");
        var bundle = LoadRunnerBundleId();
        Report($"正在为 {targets.Count} 台设备启动自动化（WDA {bundle}）…");
        var started = await Runtime.Host.EnsureStartedAsync(targets.Select(row => row.Device).ToArray(), bundle, Lifetime);
        if (started.Count == 0)
        {
            Report("没有设备成功启动 WDA 进程。请在「设置」核对 Python 路径和 WDA Bundle ID，或先运行一键配置安装 WDA。");
            return;
        }
        var ids = started.Select(device => device.Id).ToHashSet();
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(Lifetime);
        wait.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            while (true)
            {
                ApplySnapshot(await Runtime.Service.RefreshDeviceStatusesAsync(ids.ToArray(), wait.Token));
                var ready = Rows.Count(row => ids.Contains(row.Id) && row.AutomationReady);
                Report($"正在等待自动化就绪 {ready}/{ids.Count} 台…");
                if (ready == ids.Count)
                    break;
                await Task.Delay(1000, wait.Token);
            }
        }
        catch (OperationCanceledException) when (!Lifetime.IsCancellationRequested) { }
        var done = Rows.Count(row => ids.Contains(row.Id) && row.AutomationReady);
        Report($"自动化已就绪 {done}/{targets.Count} 台。" + (done < targets.Count ? "未就绪的设备请把鼠标停在「自动化」列查看原因。" : ""));
    });

    public void StopAutomation() => Run(async () =>
    {
        var channels = Runtime.Host.ChannelUdids.ToHashSet(StringComparer.Ordinal);
        var targets = Targets(row => channels.Contains(Normalize(row.Udid)));
        if (targets.Count == 0)
        {
            Report("没有由本中控启动的自动化通道。");
            return;
        }
        Runtime.Scripts.Stop(targets.Select(row => row.Id).ToArray());
        var stopped = await Runtime.Host.StopDevicesAsync(targets.Select(row => row.Device).ToArray());
        await RefreshAsync();
        Report($"已停止 {stopped} 台设备的自动化，脚本一并停止。");
    });

    public void RunScript(long? scriptId) => Run(() =>
    {
        if (scriptId is null)
            throw new InvalidOperationException("请先在左侧「脚本」列表选中一个脚本。");
        var selected = SelectedRows();
        if (selected.Count == 0)
            throw new InvalidOperationException("请先勾选要运行脚本的设备。");
        var ready = selected.Where(row => row.AutomationReady).ToArray();
        if (ready.Length == 0)
            throw new InvalidOperationException("勾选的设备都没有启动自动化，请先点「启动自动化」。");
        var result = Runtime.Scripts.Start(scriptId.Value, ready.Select(row => row.Id).ToArray(), "手动");
        ApplySnapshot(Runtime.Service.GetSnapshot());
        Report($"脚本已在 {result.Started.Count} 台设备启动"
            + (result.Busy.Count > 0 ? $"，{result.Busy.Count} 台正在运行其他脚本已跳过" : "")
            + (selected.Count > ready.Length ? $"，{selected.Count - ready.Length} 台自动化未就绪已跳过" : "") + "。");
        return Task.CompletedTask;
    });

    public void StopScripts()
    {
        var selected = SelectedRows();
        var stopped = selected.Count > 0 ? Runtime.Scripts.Stop(selected.Select(row => row.Id).ToArray()) : Runtime.Scripts.StopAll();
        Report(stopped > 0 ? $"正在停止 {stopped} 台设备上的脚本…" : "没有正在运行的脚本。");
    }

    public void ResetUsb() => Run(async () =>
    {
        if (MessageBox.Show(this, "重置 USB 会停止所有脚本和自动化通道，并重启 Apple Mobile Device Service（需要管理员授权）。所有手机会短暂断开。继续吗？",
            "重置 USB", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        Runtime.Scripts.StopAll();
        await Runtime.Host.StopAsync();
        Report("正在重启 Apple Mobile Device Service…");
        await UsbServiceControl.RestartAppleServiceAsync(Lifetime);
        await Task.Delay(3000, Lifetime);
        await RefreshAsync();
        await DiscoverAsync(quiet: true);
        Report("USB 服务已重启。手机重新出现后请再次启动自动化。");
    });

    public void OpenMirror()
    {
        if (mirror is null)
        {
            mirror = new MainWindow(Runtime);
            mirror.Closed += (_, _) => mirror = null;
            mirror.Show();
        }
        else
            mirror.Activate();
    }

    private async Task CloseMirrorAsync()
    {
        if (mirror is not null)
            await mirror.ShutdownAsync();
    }

    public void OpenWireless() => new WirelessDialog(this) { Owner = this }.ShowDialog();
    public void OpenSetup() => new SetupDialog(this) { Owner = this }.ShowDialog();
    public void Discover() => Run(() => DiscoverAsync(quiet: false));

    public void RegisterDevice() => Run(() =>
    {
        var form = new DeviceRegistrationDialog { Owner = this };
        if (form.ShowDialog() == true)
        {
            Runtime.Service.RegisterDevice(form.DeviceName, form.Udid, form.WdaPort, form.GroupName);
            ApplySnapshot(Runtime.Service.GetSnapshot());
        }
        return Task.CompletedTask;
    });

    // 安装走持久任务队列：同一台手机依次执行，结果在任务记录里逐台可查。
    public void InstallApp() => Run(async () =>
    {
        var targets = SelectedRows();
        if (targets.Count == 0)
            throw new InvalidOperationException("请先勾选要安装的设备。");
        var picker = new OpenFileDialog { Filter = "已签名 iPhone 应用|*.ipa", InitialDirectory = Runtime.AppDirectory, CheckFileExists = true };
        if (picker.ShowDialog(this) != true)
            return;
        var job = await Runtime.Service.SubmitAsync(targets.Select(row => row.Id).ToArray(), FleetAction.Install,
            new ActionParameters(AppPath: picker.FileName), Guid.NewGuid().ToString("N"), Lifetime);
        Report($"安装任务 #{job.Id} 已提交到 {targets.Count} 台设备，结果可在集控投屏窗口的任务记录里查看。");
    });

    public void ExportConfig() => Run(async () =>
    {
        var save = new SaveFileDialog { Filter = "JSON 设备配置|*.json", FileName = "wda-devices.json", OverwritePrompt = true };
        if (save.ShowDialog(this) != true)
            return;
        var ids = SelectedRows().Select(row => row.Id).ToArray();
        await File.WriteAllTextAsync(save.FileName, Runtime.Service.ExportConfig(ids.Length == 0 ? null : ids), Lifetime);
        Report("连接配置已导出。");
    });

    public void OpenApprovedFolder() =>
        Process.Start(new ProcessStartInfo(Runtime.AppDirectory) { UseShellExecute = true });
}
