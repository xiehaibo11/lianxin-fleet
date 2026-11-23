using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Desktop.ViewModels;

namespace Lianxin.Fleet.Desktop.Views.Center;

// 中控主窗口：持有共享的运行时，定时刷新设备表格和状态栏。各标签页通过它访问服务。
public partial class CenterWindow : Window
{
    private readonly DispatcherTimer timer = new();
    private readonly CancellationTokenSource lifetime = new();
    private readonly Dictionary<long, DeviceRow> rowsById = [];
    private bool refreshing, discovering, busy, closing, closed;
    private DateTime lastAutoDiscover = DateTime.MinValue;
    private string lastScheduleMessage = "";

    public CenterWindow(FleetRuntime runtime)
    {
        Runtime = runtime;
        Settings = FleetSettings.Load(runtime.SettingsPath);
        InitializeComponent();
        DataLabel.Text = "数据目录 " + runtime.DataDirectory;
        VersionText.Text = "版本 " + (Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "?");
        runtime.HostLog += message => Dispatcher.BeginInvoke(() =>
        {
            if (!closing)
                Report(message);
        });
        timer.Tick += async (_, _) => await RefreshAsync();
        SelectRefresh(Settings.RefreshSeconds);
        DevicesView.Attach(this);
        AgentView.Attach(this);
        SchedulesView.Attach(this);
        SettingsView.Attach(this);
    }

    public FleetRuntime Runtime
    {
        get;
    }
    public FleetSettings Settings
    {
        get; private set;
    }
    public ObservableCollection<DeviceRow> Rows { get; } = [];
    public CancellationToken Lifetime => lifetime.Token;
    public bool IsClosing => closing;
    public event Action? Refreshed;
    public event Action? ScriptsChanged;

    public void NotifyScriptsChanged() => ScriptsChanged?.Invoke();

    public void SaveSettings(FleetSettings settings)
    {
        settings.Save(Runtime.SettingsPath);
        Settings = settings;
        SelectRefresh(settings.RefreshSeconds);
    }

    public void Report(string message) => StatusText.Text = message.Length > 400 ? message[..400] : message;

    // 界面操作统一入口：同一时间只处理一个请求，错误显示在状态栏并弹窗。
    public async void Run(Func<Task> action)
    {
        if (closing)
            return;
        if (busy)
        {
            Report("上一项操作仍在进行，请稍候。");
            return;
        }
        busy = true;
        Cursor = System.Windows.Input.Cursors.AppStarting;
        try
        {
            await action();
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            Report("操作失败：" + error.Message);
            if (!closing)
                MessageBox.Show(this, error.Message, "联信中控", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            busy = false;
            Cursor = null;
        }
    }

    private async void WindowLoaded(object sender, RoutedEventArgs e)
    {
        Runtime.Schedules.Start();
        await ApplyOpenApiAsync();
        await RefreshAsync();
        await DiscoverAsync(quiet: true);
        if (!closing)
        {
            timer.Start();
            if (StatusText.Text == "正在初始化…")
                Report("中控就绪。用数据线连接 iPhone 并信任电脑后会自动登记。");
        }
    }

    public async Task RefreshAsync()
    {
        if (refreshing || closing)
            return;
        refreshing = true;
        try
        {
            var snapshot = await Runtime.Service.RefreshAsync(Lifetime);
            var plugged = await ProbeBridgeAsync();
            ApplySnapshot(snapshot);
            SetChip(CenterChip, CenterText, "中控 正常", true, null);
            // 有新插入但还没登记的手机时自动扫描，最多 30 秒一次。
            if (plugged is not null && plugged.Any(udid => !rowsById.Values.Any(row => Same(row.Udid, udid)))
                && DateTime.UtcNow - lastAutoDiscover > TimeSpan.FromSeconds(30))
                _ = DiscoverAsync(quiet: true);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            SetChip(CenterChip, CenterText, "中控 异常", false, error.Message);
            Report("刷新失败，画面可能过期：" + error.Message);
        }
        finally { refreshing = false; }
    }

    private async Task<IReadOnlyList<string>?> ProbeBridgeAsync()
    {
        try
        {
            var devices = await Usbmux.ListAsync(Lifetime);
            SetChip(BridgeChip, BridgeText, "桥接 正常", true, $"Apple 设备服务在线，{devices.Count} 个连接。");
            return devices.Select(device => device.Udid).ToArray();
        }
        catch (Exception error) when (error is IOException or InvalidDataException or System.Xml.XmlException)
        {
            SetChip(BridgeChip, BridgeText, "桥接 异常", false, error.Message);
            return null;
        }
    }

    public async Task DiscoverAsync(bool quiet)
    {
        if (discovering || closing)
            return;
        discovering = true;
        lastAutoDiscover = DateTime.UtcNow;
        try
        {
            var before = rowsById.Count;
            await Runtime.Service.DiscoverAsync(Lifetime);
            ApplySnapshot(Runtime.Service.GetSnapshot());
            var added = rowsById.Count - before;
            if (!quiet || added > 0)
                Report(added > 0 ? $"已自动登记 {added} 台新设备。" : $"扫描完成，共 {rowsById.Count} 台已登记设备。");
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            Report("扫描设备失败：" + error.Message);
            if (!quiet)
                MessageBox.Show(this, error.Message, "扫描设备", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally { discovering = false; }
    }

    public void ApplySnapshot(FleetSnapshot snapshot)
    {
        if (closing)
            return;
        var activity = Runtime.Scripts.Snapshot();
        var runs = Runtime.Store.GetLatestScriptRuns();
        var channels = Runtime.Host.ChannelUdids.ToHashSet(StringComparer.Ordinal);
        foreach (var item in snapshot.Devices)
        {
            if (!rowsById.TryGetValue(item.Device.Id, out var row))
            {
                row = new DeviceRow(item);
                rowsById[item.Device.Id] = row;
                Rows.Add(row);
            }
            row.Update(item, channels.Contains(Normalize(item.Device.Udid)), activity.GetValueOrDefault(item.Device.Id),
                runs.GetValueOrDefault(item.Device.Id));
        }
        foreach (var gone in rowsById.Keys.Except(snapshot.Devices.Select(item => item.Device.Id)).ToArray())
        {
            Rows.Remove(rowsById[gone]);
            rowsById.Remove(gone);
        }
        TotalText.Text = $"总数 {Rows.Count}";
        var errors = Rows.Count(row => row.Online && !row.AutomationReady);
        ErrorText.Text = $"未就绪 {errors}";
        ErrorChip.Visibility = errors > 0 ? Visibility.Visible : Visibility.Collapsed;
        OnlineText.Text = $"在线 {Rows.Count(row => row.Online)}";
        ScriptsText.Text = $"脚本 {Runtime.Scripts.RunningCount}";
        if (Runtime.Schedules.LastMessage is { Length: > 0 } scheduled && scheduled != lastScheduleMessage)
        {
            lastScheduleMessage = scheduled;
            Report(scheduled);
        }
        Refreshed?.Invoke();
    }

    public IReadOnlyList<DeviceRow> SelectedRows() => Rows.Where(row => row.IsSelected).ToArray();

    internal static string Normalize(string udid) => udid.Replace("-", "", StringComparison.Ordinal).ToUpperInvariant();
    private static bool Same(string left, string right) => Normalize(left) == Normalize(right);

    private static void SetChip(Border chip, TextBlock text, string label, bool healthy, string? tip)
    {
        text.Text = label;
        chip.Background = new SolidColorBrush(healthy ? Color.FromRgb(0x23, 0x30, 0x24) : Color.FromRgb(0x3A, 0x1F, 0x24));
        chip.BorderBrush = new SolidColorBrush(healthy ? Color.FromRgb(0x3E, 0x6B, 0x47) : Color.FromRgb(0x7A, 0x3A, 0x44));
        chip.ToolTip = tip;
    }

    private void SelectRefresh(int seconds)
    {
        var item = RefreshBox.Items.OfType<ComboBoxItem>().FirstOrDefault(entry => (string)entry.Tag == seconds.ToString())
            ?? RefreshBox.Items.OfType<ComboBoxItem>().First(entry => (string)entry.Tag == "3");
        RefreshBox.SelectedItem = item;
        timer.Interval = TimeSpan.FromSeconds(int.Parse((string)item.Tag));
    }

    private void RefreshIntervalChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RefreshBox.SelectedItem is not ComboBoxItem { Tag: string tag } || !IsLoaded)
            return;
        var seconds = int.Parse(tag);
        timer.Interval = TimeSpan.FromSeconds(seconds);
        if (seconds != Settings.RefreshSeconds)
            SaveSettings(Settings with
            {
                RefreshSeconds = seconds
            });
    }

    private async void ReloadClicked(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
        await DiscoverAsync(quiet: false);
    }

    private void TabChanged(object sender, RoutedEventArgs e)
    {
        if (DevicesView is null)
            return;
        DevicesView.Visibility = DevicesTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        AgentView.Visibility = AgentTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SchedulesView.Visibility = SchedulesTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = SettingsTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (closed)
            return;
        e.Cancel = true;
        if (closing)
            return;
        var running = Runtime.Scripts.RunningCount;
        if (running > 0 && MessageBox.Show(this, $"还有 {running} 台设备在运行脚本，关闭中控会全部停止。确定关闭吗？", "关闭中控",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        closing = true;
        IsEnabled = false;
        timer.Stop();
        lifetime.Cancel();
        AgentView.Stop();
        await StopOpenApiAsync();
        await CloseMirrorAsync();
        try
        {
            await Runtime.DisposeAsync();
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "关闭中控时发生错误");
        }
        lifetime.Dispose();
        closed = true;
        // 清理可能同步完成，此时仍在 Closing 事件里，不能直接再次 Close。
        _ = Dispatcher.BeginInvoke(new Action(Close));
    }
}
