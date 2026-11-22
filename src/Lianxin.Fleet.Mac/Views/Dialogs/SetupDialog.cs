using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Platform.Storage;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Mac.ViewModels;
using Lianxin.Fleet.Mac.Views.Center;

namespace Lianxin.Fleet.Mac.Views.Dialogs;

// 一键配置：对勾选的 USB 设备依次开启开发者模式、挂载开发者镜像、安装批准目录里的 WDA，最多 4 台同时进行。
public sealed class SetupDialog : Window
{
    private readonly CenterWindow center;
    private readonly List<SetupItem> items;
    private readonly CheckBox developer, image, runner;
    private readonly TextBox package;
    private readonly TextBlock status;
    private readonly Button start;
    private CancellationTokenSource? running;
    private bool closeConfirmed;

    public SetupDialog(CenterWindow center)
    {
        this.center = center;
        var panel = DialogKit.Prepare(this, "一键配置", 760);
        var selected = center.SelectedRows();
        items = (selected.Count > 0 ? selected : center.Rows.Where(row => row.Connection == "USB").ToList())
            .Select(row => new SetupItem(row)).ToList();
        DialogKit.Note(panel, "配置前请先关闭手机锁屏密码和面容 ID，否则无法自动开启开发者模式。手机弹出确认时请点「允许」或「打开」。"
            + "\n开发者模式开启后手机会重启；手机每次重启后开发者镜像会失效，需要重新挂载。");
        developer = Check(panel, "开启开发者模式（iOS 16 及以上）");
        image = Check(panel, "挂载开发者镜像（首次需要联网下载）");
        runner = Check(panel, "安装 WDA（必须是批准目录里已签名的 .ipa）");
        var row = new DockPanel();
        var browse = new Button { Content = "选择…", Margin = new Thickness(8, 0, 0, 0) };
        DockPanel.SetDock(browse, Dock.Right);
        row.Children.Add(browse);
        package = new TextBox { Text = center.Settings.RunnerPackage ?? "" };
        row.Children.Add(package);
        DialogKit.Field(panel, "WDA 安装包", row);
        browse.Click += async (_, _) => await BrowseAsync();
        var grid = new DataGrid { Height = 260, IsReadOnly = true, ItemsSource = items };
        grid.Columns.Add(Column("设备", nameof(SetupItem.Name), new DataGridLength(170)));
        grid.Columns.Add(Column("系统", nameof(SetupItem.Os), new DataGridLength(70)));
        grid.Columns.Add(Column("连接", nameof(SetupItem.Connection), new DataGridLength(60)));
        grid.Columns.Add(Column("进度", nameof(SetupItem.State), new DataGridLength(1, DataGridLengthUnitType.Star)));
        DialogKit.Field(panel, items.Count == 0 ? "没有可配置的设备：请勾选设备，或用 USB 连接手机。" : $"待配置设备（{items.Count} 台）", grid);
        status = DialogKit.Note(panel, "");
        var buttons = DialogKit.Buttons(this, panel, "开始配置", "关闭");
        start = buttons[0];
        start.IsEnabled = items.Count > 0;
        start.Click += async (_, _) => await StartAsync();
        Closing += ConfirmClose;
    }

    // 配置进行中关闭会中断未完成的设备，先确认。
    private async void ConfirmClose(object? sender, WindowClosingEventArgs e)
    {
        if (running is null || closeConfirmed)
        {
            running?.Cancel();
            return;
        }
        e.Cancel = true;
        if (!await DialogKit.ConfirmAsync(this, "一键配置", "配置仍在进行，关闭会中断未完成的设备。确定关闭吗？", "关闭"))
            return;
        closeConfirmed = true;
        running?.Cancel();
        Close();
    }

    internal static DataGridTextColumn Column(string header, string path, DataGridLength width) =>
        new()
        {
            Header = header,
            Binding = new Binding(path),
            Width = width
        };

    private static CheckBox Check(Panel panel, string text)
    {
        var box = new CheckBox { Content = text, IsChecked = true };
        panel.Children.Add(box);
        return box;
    }

    private async Task BrowseAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "选择批准目录里已签名的 WDA 安装包",
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(center.Runtime.AppDirectory),
            FileTypeFilter = [new FilePickerFileType("已签名 WDA 安装包") { Patterns = ["*.ipa"] }]
        });
        if (files.FirstOrDefault()?.TryGetLocalPath() is { } path)
            package.Text = path;
    }

    private async Task StartAsync()
    {
        SetupOptions options;
        try
        {
            options = center.Runtime.Setup.Validate(new(developer.IsChecked == true, image.IsChecked == true, runner.IsChecked == true,
                package.Text?.Trim()));
        }
        catch (Exception error) when (error is ArgumentException or UnauthorizedAccessException or IOException)
        {
            status.Foreground = DialogKit.ErrorBrush;
            status.Text = error.Message + (runner.IsChecked == true ? $"（批准目录：{center.Runtime.AppDirectory}）" : "");
            return;
        }
        if (options.RunnerPackage is { } path && path != center.Settings.RunnerPackage)
            center.SaveSettings(center.Settings with
            {
                RunnerPackage = path
            });
        start.IsEnabled = false;
        status.Foreground = DialogKit.MutedBrush;
        status.Text = "正在配置…";
        using var source = CancellationTokenSource.CreateLinkedTokenSource(center.Lifetime);
        running = source;
        using var slots = new SemaphoreSlim(4);
        await Task.WhenAll(items.Select(item => ConfigureAsync(item, options, slots, source.Token)));
        running = null;
        var ok = items.Count(item => item.Succeeded);
        status.Text = $"配置完成：成功 {ok} 台，失败 {items.Count - ok} 台。WDA 安装后到手机「设置 → 通用 → VPN 与设备管理」信任开发者，再回中控启动自动化。";
        start.IsEnabled = true;
    }

    private async Task ConfigureAsync(SetupItem item, SetupOptions options, SemaphoreSlim slots, CancellationToken token)
    {
        item.Report("排队中");
        try
        {
            await slots.WaitAsync(token);
            try
            {
                // 进度回调在界面线程执行（Progress 捕获当前同步上下文）。
                await center.Runtime.Setup.SetupAsync(item.Row.Device, options, new Progress<string>(item.Report), token);
                item.Succeeded = true;
                item.Report("✓ 配置完成");
            }
            finally { slots.Release(); }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            item.Report("已中断");
        }
        catch (Exception error)
        {
            item.Report("✗ " + error.Message);
        }
        item.Row.SetNote(item.Succeeded ? null : "一键配置：" + item.State);
    }

    private sealed class SetupItem(DeviceRow row) : INotifyPropertyChanged
    {
        public DeviceRow Row { get; } = row;
        // 同型号手机常常同名，附上设备 ID 前 8 位以便区分。
        public string Name => (Row.Alias.Length > 0 ? Row.Alias : Row.Name) + " · " + Row.Udid[..Math.Min(8, Row.Udid.Length)];
        public string Os => Row.OsVersion;
        public string Connection => Row.Connection;
        public string State { get; private set; } = "待配置";
        public bool Succeeded
        {
            get; set;
        }
        public event PropertyChangedEventHandler? PropertyChanged;

        public void Report(string text)
        {
            State = text;
            Row.SetNote("一键配置：" + text);
            PropertyChanged?.Invoke(this, new(nameof(State)));
        }
    }
}
