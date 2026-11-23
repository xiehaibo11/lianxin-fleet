using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Mac.ViewModels;

namespace Lianxin.Fleet.Mac.Views.Dialogs;

// 应用标签：列出用户安装的应用，启动、关闭（需要自动化）、卸载，以及从批准目录安装。
public sealed partial class DeviceDetailDialog
{
    private DataGrid appGrid = null!;

    private Control BuildApps()
    {
        var page = new DockPanel();
        var top = Toolbar(ActionButton("刷新列表", async () => await LoadAppsAsync()),
            ActionButton("▶ 启动", async () => await AppActionAsync(FleetAction.Launch)),
            ActionButton("■ 关闭", async () => await AppActionAsync(FleetAction.Terminate)),
            ActionButton("卸载…", async () => await UninstallAsync()),
            ActionButton("安装应用…", async () => await InstallAsync()));
        DockPanel.SetDock(top, Dock.Top);
        page.Children.Add(top);
        var hint = Hint("只列出用户安装的应用。启动、关闭需要自动化已就绪；安装包必须放在应用批准目录里。");
        DockPanel.SetDock(hint, Dock.Top);
        page.Children.Add(hint);
        appGrid = new DataGrid { IsReadOnly = true, SelectionMode = DataGridSelectionMode.Single };
        appGrid.Columns.Add(SetupDialog.Column("名称", nameof(DeviceAppItem.Name), new DataGridLength(1, DataGridLengthUnitType.Star)));
        appGrid.Columns.Add(SetupDialog.Column("Bundle ID", nameof(DeviceAppItem.BundleId), new DataGridLength(330)));
        appGrid.Columns.Add(SetupDialog.Column("版本", nameof(DeviceAppItem.Version), new DataGridLength(130)));
        AutomationProperties.SetName(appGrid, "应用列表");
        page.Children.Add(appGrid);
        return page;
    }

    private Task LoadAppsAsync() => RunAsync("正在读取应用列表…", async token =>
    {
        var apps = await center.Runtime.DeviceManager.GetAppsAsync(row.Device, token);
        appGrid.ItemsSource = apps.Select(app => new DeviceAppItem(app)).ToArray();
        Show($"共 {apps.Count} 个用户应用。", false);
    }, readOnly: true);

    private DeviceAppItem? SelectedApp()
    {
        if (appGrid.SelectedItem is DeviceAppItem item)
            return item;
        Show("请先在列表里选中一个应用。", true);
        return null;
    }

    // 启动与关闭走已有的设备动作（WDA），与脚本里的「启动」「关闭」相同。
    private async Task AppActionAsync(FleetAction action)
    {
        if (SelectedApp() is not { } app)
            return;
        if (!row.AutomationReady)
        {
            Show("启动、关闭应用需要先启动自动化。", true);
            return;
        }
        var verb = action == FleetAction.Launch ? "启动" : "关闭";
        await RunAsync($"正在{verb}「{app.Name}」…", async token =>
        {
            await center.Runtime.Bridge.ExecuteAsync(row.Device, action, new ActionParameters(BundleId: app.BundleId), token);
            Show($"已{verb}「{app.Name}」。", false);
        });
    }

    private async Task UninstallAsync()
    {
        if (SelectedApp() is not { } app
            || !await ConfirmAsync("卸载应用", $"从这台手机卸载「{app.Name}」（{app.BundleId}）？应用数据会一起删除，无法恢复。", "卸载"))
            return;
        if (await RunAsync($"正在卸载「{app.Name}」…", token => center.Runtime.DeviceManager.UninstallAppAsync(row.Device, app.BundleId, token)))
        {
            var name = app.Name;
            await LoadAppsAsync();
            Show($"已卸载「{name}」。", false);
        }
    }

    // 安装走持久任务队列和批准目录校验，与工具栏「安装应用到勾选设备」相同；这里等待结果后刷新列表。
    private async Task InstallAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择批准目录里已签名的 iPhone 应用",
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(center.Runtime.AppDirectory),
            FileTypeFilter = [new FilePickerFileType("已签名 iPhone 应用") { Patterns = ["*.ipa"] }]
        });
        if (files.FirstOrDefault()?.TryGetLocalPath() is not { } path)
            return;
        var finished = await RunAsync("正在安装…", async token =>
        {
            var job = await center.Runtime.Service.SubmitAsync([row.Id], FleetAction.Install,
                new ActionParameters(AppPath: path), Guid.NewGuid().ToString("N"), token);
            Show($"安装任务 #{job.Id} 已提交，正在等待结果…", false);
            var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(3);
            FleetJob result;
            // 按编号读取任务，不受任务记录条数的影响；超时和「待确认」都不是失败，安装可能仍会完成。
            while ((result = center.Runtime.Store.GetJob(job.Id)).State is JobState.Queued or JobState.Running)
            {
                if (DateTime.UtcNow > deadline)
                    throw new DeviceOperationException($"安装任务 #{job.Id} 仍在进行，请稍后刷新列表或在任务记录里查看。", uncertain: true);
                await Task.Delay(1500, token);
            }
            var error = result.Results.FirstOrDefault()?.Error ?? "请在集控投屏窗口的任务记录里查看。";
            if (result.State == JobState.Unknown)
                throw new DeviceOperationException($"安装任务 #{job.Id}：{error}", uncertain: true);
            if (result.State != JobState.Succeeded)
                throw new InvalidOperationException($"安装任务 #{job.Id}：{error}");
        });
        if (finished)
        {
            await LoadAppsAsync();
            Show("安装完成。", false);
        }
    }
}
