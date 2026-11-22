using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Serialization;
using Lianxin.Fleet.Desktop.ViewModels;
using Lianxin.Fleet.Desktop.Views.Dialogs;
using Microsoft.Win32;

namespace Lianxin.Fleet.Desktop.Views.Main;

public partial class MainWindow
{
    private async Task QueueAsync(FleetAction action, ActionParameters parameters, IReadOnlyList<long>? ids = null)
    {
        ids ??= TargetIds();
        if (ids.Count == 0)
            throw new InvalidOperationException("请先选择目标设备。");
        var skipped = 0;
        if (action != FleetAction.Install)
        {
            var controllable = ids.Where(id => DeviceCards.Any(card => card.Id == id && card.CanControl)).ToArray();
            skipped = ids.Count - controllable.Length;
            if (controllable.Length == 0)
                throw new InvalidOperationException("目标设备的 WDA 控制尚未就绪。");
            ids = controllable;
        }
        var signature = JsonSerializer.Serialize(new
        {
            Ids = ids.Order().ToArray(),
            Action = action,
            Parameters = parameters
        }, FleetJson.Options);
        if (!_requests.TryGetValue(signature, out var requestKey))
            _requests[signature] = requestKey = Guid.NewGuid().ToString("N");
        var job = await _service.SubmitAsync(ids, action, parameters, requestKey, _lifetime.Token);
        _requests.Remove(signature);
        ApplySnapshot(_service.GetSnapshot());
        StatusLabel.Text = $"任务 #{job.Id} 已提交" + (skipped > 0 ? $"（已跳过 {skipped} 台控制未就绪的设备）" : "")
            + "，请在任务记录核对逐设备执行结果。";
    }
    private async void QuickAction(DeviceCard card, FleetAction action) => await RunAsync(() =>
        QueueAsync(action, action == FleetAction.Text ? new(Text: card.QuickText) : new(), [card.Id]));
    private async void CommandClicked(object sender, RoutedEventArgs e) => await RunAsync(() =>
    {
        var tag = (sender as Button)?.Tag as string;
        if (tag == "Back")
            return QueueAsync(FleetAction.Swipe, new(FromX: 0, FromY: .5, ToX: .7, ToY: .5, Duration: .05));
        var action = Enum.Parse<FleetAction>(tag!);
        return QueueAsync(action, action switch
        {
            FleetAction.Text or FleetAction.Clipboard => new(Text: PhoneTextInput.Text),
            FleetAction.OpenUrl => new(Text: UrlInput.Text.Trim()),
            FleetAction.Launch or FleetAction.Terminate => new(BundleId: BundleInput.Text.Trim()),
            _ => new()
        });
    });
    private async void SwipeClicked(object sender, RoutedEventArgs e) => await RunAsync(() =>
    {
        var coordinates = ((sender as Button)?.Tag as string) switch
        {
            "up" => (.5, .78, .5, .25),
            "down" => (.5, .25, .5, .78),
            "left" => (.8, .5, .2, .5),
            _ => (.2, .5, .8, .5)
        };
        return QueueAsync(FleetAction.Swipe, new(FromX: coordinates.Item1, FromY: coordinates.Item2,
            ToX: coordinates.Item3, ToY: coordinates.Item4));
    });
    private async void DiscoverClicked(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        var devices = await _service.DiscoverAsync(_lifetime.Token);
        ApplySnapshot(_service.GetSnapshot());
        StatusLabel.Text = $"扫描完成，共 {devices.Count} 台已登记设备。新设备已分配端口，安装 WDA 后可启动控制通道。";
        await RefreshAsync();
    });
    private async void RegisterClicked(object sender, RoutedEventArgs e) => await RunAsync(() =>
    {
        var form = new DeviceRegistrationDialog { Owner = this };
        if (form.ShowDialog() != true)
            return Task.CompletedTask;
        _service.RegisterDevice(form.DeviceName, form.Udid, form.WdaPort, form.GroupName);
        ApplySnapshot(_service.GetSnapshot());
        return Task.CompletedTask;
    });
    private async void GroupClicked(object sender, RoutedEventArgs e) => await RunAsync(() =>
    {
        var ids = SelectedIds();
        if (ids.Count == 0)
            throw new InvalidOperationException("请先勾选要分组的设备。");
        _service.AssignGroup(ids, GroupNameInput.Text.Trim());
        ApplySnapshot(_service.GetSnapshot());
        return Task.CompletedTask;
    });
    private async void ExportClicked(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        var save = new SaveFileDialog { Filter = "JSON 设备配置|*.json", FileName = "wda-devices.json", OverwritePrompt = true };
        if (save.ShowDialog(this) != true)
            return;
        var ids = SelectedIds();
        await File.WriteAllTextAsync(save.FileName, _service.ExportConfig(ids.Count == 0 ? null : ids), _lifetime.Token);
        StatusLabel.Text = "连接配置已导出。";
    });
    private async void InstallClicked(object sender, RoutedEventArgs e) => await RunAsync(() => QueueAsync(FleetAction.Install, new(AppPath: AppPathInput.Text.Trim()), SelectedIds()));
    private void BrowseAppClicked(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Filter = "已签名 iPhone 应用|*.ipa", InitialDirectory = _appDirectory, CheckFileExists = true };
        if (picker.ShowDialog(this) == true)
            AppPathInput.Text = picker.FileName;
    }
    private async void OpenAppsClicked(object sender, RoutedEventArgs e) => await RunAsync(() =>
    {
        Process.Start(new ProcessStartInfo(_appDirectory) { UseShellExecute = true });
        return Task.CompletedTask;
    });
    private async void StartWdaClicked(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        var selected = DeviceCards.Where(card => card.IsSelected).Select(card => card.Device).ToArray();
        var devices = selected.Length == 0 ? DeviceCards.Select(card => card.Device).ToArray() : selected;
        // 中控可能已为部分设备建立通道，这里只补齐缺少的设备。
        await _host.EnsureStartedAsync(devices, WdaBundleInput.Text.Trim(), _lifetime.Token);
        StatusLabel.Text = "控制通道进程已启动，请核对设备在线状态及实际名称。";
        await RefreshAsync();
    });
    private async void StopWdaClicked(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        await _host.StopAsync();
        await RefreshAsync();
        StatusLabel.Text = "本次启动的 WDA 通道已停止。已连接的设备继续尝试 USB 快照。";
    });
}
