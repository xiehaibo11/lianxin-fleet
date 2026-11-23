using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Desktop.ViewModels;
using Microsoft.Win32;

namespace Lianxin.Fleet.Desktop.Views.Dialogs;

// 文件标签：浏览手机媒体区（/var/mobile/Media），上传、下载、新建文件夹和删除。
public sealed partial class DeviceDetailDialog
{
    private DataGrid fileGrid = null!;
    private TextBox pathBox = null!;
    private string currentPath = "/";
    private DeviceFileItem[] fileItems = [];

    private FrameworkElement BuildFiles()
    {
        var page = new DockPanel();
        var top = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
        var buttons = Toolbar(ActionButton("↑ 上级", async () => await LoadFilesAsync(ParentPath(currentPath))),
            ActionButton("刷新", async () => await LoadFilesAsync(currentPath)));
        DockPanel.SetDock(buttons, Dock.Right);
        top.Children.Add(buttons);
        pathBox = new TextBox { Text = "/", Margin = new Thickness(0, 0, 8, 6), ToolTip = "手机路径，按回车前往" };
        System.Windows.Automation.AutomationProperties.SetName(pathBox, "手机路径");
        pathBox.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
                await LoadFilesAsync(pathBox.Text.Trim());
        };
        top.Children.Add(pathBox);
        DockPanel.SetDock(top, Dock.Top);
        page.Children.Add(top);
        var bottom = Toolbar(ActionButton("上传文件…", async () => await UploadAsync()),
            ActionButton("下载选中…", async () => await DownloadAsync()),
            ActionButton("新建文件夹…", async () => await CreateFolderAsync()),
            ActionButton("删除选中…", async () => await DeleteAsync()));
        bottom.Children.Add(Hint("只能访问媒体区：相册 DCIM、Downloads 等。双击文件夹进入。"));
        DockPanel.SetDock(bottom, Dock.Bottom);
        page.Children.Add(bottom);
        fileGrid = new DataGrid { IsReadOnly = true, SelectionMode = DataGridSelectionMode.Single, RowHeight = 30 };
        fileGrid.Columns.Add(new DataGridTextColumn { Header = "名称", Binding = new Binding(nameof(DeviceFileItem.Name)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        fileGrid.Columns.Add(new DataGridTextColumn { Header = "类型", Binding = new Binding(nameof(DeviceFileItem.Kind)), Width = 70 });
        fileGrid.Columns.Add(new DataGridTextColumn { Header = "大小", Binding = new Binding(nameof(DeviceFileItem.Size)), Width = 90 });
        fileGrid.Columns.Add(new DataGridTextColumn { Header = "修改时间", Binding = new Binding(nameof(DeviceFileItem.Modified)), Width = 140 });
        fileGrid.MouseDoubleClick += async (_, _) =>
        {
            if (fileGrid.SelectedItem is DeviceFileItem { Entry.IsDirectory: true } item)
                await LoadFilesAsync(item.Entry.Path);
        };
        System.Windows.Automation.AutomationProperties.SetName(fileGrid, "手机文件");
        page.Children.Add(fileGrid);
        return page;
    }

    private static string ParentPath(string path) => path.LastIndexOf('/') is > 0 and var index ? path[..index] : "/";

    private Task LoadFilesAsync(string path) => RunAsync("正在读取文件列表…", async token =>
    {
        var listing = await center.Runtime.DeviceManager.ListFilesAsync(row.Device, path, token);
        currentPath = listing.Path;
        pathBox.Text = listing.Path;
        fileItems = listing.Entries.Select(entry => new DeviceFileItem(entry)).ToArray();
        fileGrid.ItemsSource = fileItems;
        Show($"{listing.Path} · {listing.Entries.Count} 项" + (listing.Truncated ? "（只显示前 1000 项）" : ""), false);
    }, readOnly: true);

    private DeviceFileItem? SelectedFile()
    {
        if (fileGrid.SelectedItem is DeviceFileItem item)
            return item;
        Show("请先在列表里选中文件或文件夹。", true);
        return null;
    }

    private async Task UploadAsync()
    {
        var picker = new OpenFileDialog { Title = $"上传到手机 {currentPath}", CheckFileExists = true, Multiselect = true };
        if (picker.ShowDialog(this) != true)
            return;
        var target = currentPath;
        // 直接问手机每个目标是否已存在：列表可能是旧的，也最多只显示 1000 项。
        var found = new List<DeviceFileEntry>();
        if (!await RunAsync("正在检查手机上的同名文件…", async token =>
        {
            foreach (var file in picker.FileNames)
                if (await center.Runtime.DeviceManager.GetFileAsync(row.Device, DeviceManager.JoinDevicePath(target,
                    Path.GetFileName(file)), token) is { } entry)
                    found.Add(entry);
        }, readOnly: true))
            return;
        if (found.FirstOrDefault(entry => entry.IsDirectory) is { } folder)
        {
            Show($"手机上已有名为「{folder.Name}」的文件夹，请先改名。", true);
            return;
        }
        var existing = found.Select(entry => entry.Name).ToArray();
        if (existing.Length > 0 && !Confirm("上传文件", $"手机上已有 {string.Join("、", existing.Take(3))}{(existing.Length > 3 ? " 等" : "")}，上传会覆盖。继续吗？"))
            return;
        var done = await RunAsync($"正在上传 {picker.FileNames.Length} 个文件…", async token =>
        {
            foreach (var (file, index) in picker.FileNames.Select((file, index) => (file, index)))
            {
                Show($"正在上传 {index + 1}/{picker.FileNames.Length}：{Path.GetFileName(file)}…", false);
                await center.Runtime.DeviceManager.PushFileAsync(row.Device, file, target, token);
            }
        });
        if (done)
        {
            await LoadFilesAsync(target);
            Show($"已上传 {picker.FileNames.Length} 个文件到 {target}。", false);
        }
    }

    private async Task DownloadAsync()
    {
        if (SelectedFile() is not { } item)
            return;
        var picker = new OpenFolderDialog { Title = $"把「{item.Name}」下载到本机文件夹" };
        if (picker.ShowDialog(this) != true)
            return;
        await RunAsync($"正在下载「{item.Name}」…", async token =>
        {
            var saved = await center.Runtime.DeviceManager.PullFileAsync(row.Device, item.Entry.Path, picker.FolderName, token);
            Show($"已下载到 {saved}", false);
        });
    }

    private async Task CreateFolderAsync()
    {
        var name = TextPromptDialog.Ask(this, "新建文件夹", $"在 {currentPath} 下新建文件夹", "");
        if (name is null)
            return;
        if (!DeviceManager.ValidDeviceName(name))
        {
            Show("文件夹名称不能包含 / 或 \\，也不能是 . 或 ..。", true);
            return;
        }
        var target = currentPath;
        if (await RunAsync("正在新建文件夹…", token => center.Runtime.DeviceManager.CreateDirectoryAsync(row.Device,
            DeviceManager.JoinDevicePath(target, name), token)))
        {
            await LoadFilesAsync(target);
            Show($"已新建文件夹「{name}」。", false);
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedFile() is not { } item || !Confirm("删除", item.Entry.IsDirectory
            ? $"删除手机上的文件夹「{item.Entry.Path}」及其中全部内容？无法恢复。" : $"删除手机上的文件「{item.Entry.Path}」？无法恢复。"))
            return;
        var target = currentPath;
        if (await RunAsync($"正在删除「{item.Name}」…", token => center.Runtime.DeviceManager.DeleteFileAsync(row.Device, item.Entry.Path, token)))
        {
            await LoadFilesAsync(target);
            Show($"已删除「{item.Name}」。", false);
        }
    }
}
