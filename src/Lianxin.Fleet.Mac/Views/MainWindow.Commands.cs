using System.Diagnostics;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Serialization;
using Lianxin.Fleet.Mac.ViewModels;

namespace Lianxin.Fleet.Mac.Views;

public sealed partial class MainWindow
{
    private (string Signature, string Key)? pendingRequest;

    private void Run(Func<Task> action)
    {
        if (closing)
            return;
        if (!operation.IsCompleted)
        {
            StatusLabel.Text = "上一项请求仍在处理。";
            return;
        }
        operation = RunAsync(action);
    }

    private async Task RunAsync(Func<Task> action)
    {
        StatusLabel.Text = "正在处理请求…";
        try
        {
            await action();
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
        catch (Exception error)
        {
            StatusLabel.Text = "操作失败：" + error.Message;
        }
        finally
        {
            UpdateHostButtons();
        }
    }

    private async Task QueueAsync(FleetAction action, ActionParameters parameters, long[]? targets = null)
    {
        targets ??= TargetIds();
        if (targets.Length == 0)
            throw new InvalidOperationException("请先选择目标设备。");
        var requested = targets.Length;
        // 同步时跳过未连接设备，不让一台掉线挡住整批；安装仍按勾选执行。
        if (action != FleetAction.Install)
            targets = targets.Where(id => cards[id].Status.Online).ToArray();
        if (targets.Length == 0)
            throw new InvalidOperationException("目标设备都未连接。");
        var signature = JsonSerializer.Serialize(new
        {
            Ids = targets.Order().ToArray(),
            Action = action,
            Parameters = parameters
        }, FleetJson.Options);
        if (pendingRequest is not { } previous || previous.Signature != signature)
            pendingRequest = (signature, Guid.NewGuid().ToString("N"));
        var job = await service.SubmitAsync(targets, action, parameters, pendingRequest.Value.Key, lifetime.Token);
        pendingRequest = null;
        ApplySnapshot(service.GetSnapshot());
        var skipped = requested - targets.Length;
        StatusLabel.Text = $"任务 #{job.Id} 已提交" + (skipped > 0 ? $"（已跳过 {skipped} 台未连接设备）" : "") + "。执行结果见任务记录。";
    }

    private void DiscoverClicked(object? sender, RoutedEventArgs args) => Run(async () =>
    {
        var known = cards.Count;
        var devices = await service.DiscoverAsync(lifetime.Token);
        ApplySnapshot(service.GetSnapshot());
        StatusLabel.Text = $"扫描完成，新增 {Math.Max(0, devices.Count - known)} 台设备，共 {devices.Count} 台已登记。";
        BeginRefresh();
    });

    private void RegisterClicked(object? sender, RoutedEventArgs args) => Run(async () =>
    {
        var port = 8100;
        var used = cards.Values.Select(card => card.Device.WdaPort).ToHashSet();
        while (used.Contains(port))
            port++;
        var dialog = new RegisterDeviceWindow(port);
        if (await dialog.ShowDialog<DeviceRegistration?>(this) is not { } registration || closing)
        {
            StatusLabel.Text = "已取消登记。";
            return;
        }
        var device = service.RegisterDevice(registration.Name, registration.Udid, registration.Port, registration.Group);
        ApplySnapshot(service.GetSnapshot());
        StatusLabel.Text = $"已登记 {device.Name}。";
        BeginRefresh();
    });

    private void GroupClicked(object? sender, RoutedEventArgs args) => Run(() =>
    {
        var ids = SelectedIds();
        if (ids.Length == 0)
            throw new InvalidOperationException("请勾选要分组的设备。");
        service.AssignGroup(ids, GroupNameInput.Text ?? "");
        ApplySnapshot(service.GetSnapshot());
        StatusLabel.Text = $"已更新 {ids.Length} 台设备的分组。";
        return Task.CompletedTask;
    });

    private void ExportClicked(object? sender, RoutedEventArgs args) => Run(async () =>
    {
        var ids = SelectedIds();
        var config = service.ExportConfig(ids.Length == 0 ? null : ids);
        var output = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出设备连接配置",
            SuggestedFileName = "wda-devices.json",
            DefaultExtension = "json",
            FileTypeChoices = [new FilePickerFileType("JSON 配置") { Patterns = ["*.json"] }],
            ShowOverwritePrompt = true
        });
        if (output?.TryGetLocalPath() is not { } path || closing)
        {
            StatusLabel.Text = "已取消导出。";
            return;
        }
        await File.WriteAllTextAsync(path, config, lifetime.Token);
        StatusLabel.Text = "连接配置已导出。";
    });

    // 按钮 Tag 即 FleetAction 名称，见 MainWindow.axaml。
    private static FleetAction TagAction(object? sender) => Enum.Parse<FleetAction>((string)((Control)sender!).Tag!);

    private void ActionClicked(object? sender, RoutedEventArgs args) =>
        Run(() => QueueAsync(TagAction(sender), new ActionParameters()));

    private void TextActionClicked(object? sender, RoutedEventArgs args) =>
        Run(() => QueueAsync(TagAction(sender), new ActionParameters(Text: DeviceTextInput.Text)));

    private void AppActionClicked(object? sender, RoutedEventArgs args) =>
        Run(() => QueueAsync(TagAction(sender), new ActionParameters(BundleId: LaunchBundleInput.Text)));

    private void OpenUrlClicked(object? sender, RoutedEventArgs args) =>
        Run(() => QueueAsync(FleetAction.OpenUrl, new ActionParameters(Text: UrlInput.Text)));

    private void BackClicked(object? sender, RoutedEventArgs args) =>
        Run(() => QueueAsync(FleetAction.Swipe, new ActionParameters(FromX: 0, FromY: .5, ToX: .7, ToY: .5, Duration: .05)));

    private void TileSendClicked(object? sender, RoutedEventArgs args)
    {
        if ((sender as Control)?.DataContext is not DeviceCard card)
            return;
        Run(async () =>
        {
            await QueueAsync(FleetAction.Text, new ActionParameters(Text: card.QuickText), [card.Id]);
            card.QuickText = "";
        });
    }

    private void TileHomeClicked(object? sender, RoutedEventArgs args)
    {
        if ((sender as Control)?.DataContext is DeviceCard card)
            Run(() => QueueAsync(FleetAction.Home, new ActionParameters(), [card.Id]));
    }

    private void InstallClicked(object? sender, RoutedEventArgs args) =>
        Run(() => QueueAsync(FleetAction.Install, new ActionParameters(AppPath: AppPathInput.Text), SelectedIds()));

    private void SwipeClicked(object? sender, RoutedEventArgs args) => Run(() =>
    {
        var (fromX, fromY, toX, toY) = (sender as Button)?.Tag switch
        {
            "up" => (.5, .78, .5, .25),
            "down" => (.5, .25, .5, .78),
            "left" => (.8, .5, .2, .5),
            _ => (.2, .5, .8, .5)
        };
        return QueueAsync(FleetAction.Swipe, new ActionParameters(FromX: fromX, FromY: fromY, ToX: toX, ToY: toY));
    });

    private void BrowsePackageClicked(object? sender, RoutedEventArgs args) => Run(async () =>
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择批准目录内的签名安装包",
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(appDirectory),
            FileTypeFilter = [new FilePickerFileType("iPhone 安装包") { Patterns = ["*.ipa", "*.app"] }]
        });
        if (!closing && files.FirstOrDefault()?.TryGetLocalPath() is { } path)
            AppPathInput.Text = path;
        StatusLabel.Text = "";
    });

    private void OpenApprovedClicked(object? sender, RoutedEventArgs args) => Run(() =>
    {
        var info = new ProcessStartInfo { FileName = "/usr/bin/open", UseShellExecute = false };
        info.ArgumentList.Add(appDirectory);
        using var process = Process.Start(info);
        StatusLabel.Text = "已请求在访达中打开批准目录。";
        return Task.CompletedTask;
    });

    private void StartWdaClicked(object? sender, RoutedEventArgs args) => Run(async () =>
    {
        var selected = cards.Values.Where(card => card.IsSelected).Select(card => card.Device).ToArray();
        var devices = selected.Length == 0 ? cards.Values.Select(card => card.Device).ToArray() : selected;
        await host.StartAsync(devices, WdaBundleInput.Text?.Trim() ?? "", lifetime.Token);
        StatusLabel.Text = "已请求启动通道，实际连接状态见设备列表。";
        BeginRefresh();
    });

    private void StopWdaClicked(object? sender, RoutedEventArgs args) => Run(async () =>
    {
        await host.StopAsync();
        StatusLabel.Text = "已停止本工作站启动的控制通道。";
        BeginRefresh();
    });

    private void UpdateHostButtons()
    {
        StartWdaButton.IsEnabled = !closing && !host.IsRunning;
        StopWdaButton.IsEnabled = !closing && host.IsRunning;
    }
}
