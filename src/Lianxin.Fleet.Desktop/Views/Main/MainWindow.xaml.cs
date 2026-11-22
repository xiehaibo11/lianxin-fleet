using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Desktop.ViewModels;

namespace Lianxin.Fleet.Desktop.Views.Main;

public partial class MainWindow : Window
{
    private readonly string _appDirectory;
    private readonly FleetRuntime _runtime;
    private readonly FleetService _service;
    private readonly WdaHost _host;
    private readonly Action<string> _hostLog;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly Dictionary<string, string> _requests = [];
    private DeviceCard? _master;
    private bool _busy, _refreshing, _closing, _closed;
    public ObservableCollection<DeviceCard> DeviceCards { get; } = [];
    public ObservableCollection<string> Groups { get; } = ["所有分组"];
    public ObservableCollection<JobCard> Jobs { get; } = [];
    public ICollectionView DeviceView
    {
        get;
    }

    // 集控投屏窗口与中控共用同一个运行时（数据库、任务队列、WDA 通道），关闭窗口不会停止它们。
    public MainWindow(FleetRuntime runtime)
    {
        DeviceView = CollectionViewSource.GetDefaultView(DeviceCards);
        DeviceView.Filter = Matches;
        InitializeComponent();
        _runtime = runtime;
        _runnerBundleFile = Path.Combine(runtime.DataDirectory, "wda-runner.txt");
        WdaBundleInput.Text = LoadRunnerBundleId(_runnerBundleFile);
        _appDirectory = runtime.AppDirectory;
        _service = runtime.Service;
        _host = runtime.Host;
        _hostLog = message =>
        {
            if (!_closing && !Dispatcher.HasShutdownStarted)
                Dispatcher.BeginInvoke(new Action(() => HostLog(message)));
        };
        runtime.HostLog += _hostLog;
        DataContext = this;
        GroupFilter.SelectedIndex = 0;
        EnvironmentLabel.Text = "集控投屏 · 与中控共用设备、任务和自动化通道";
        _timer.Tick += async (_, _) => await RefreshAsync();
        ApplySnapshot(_service.GetSnapshot());
    }

    private async void WindowLoaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
        if (!_closing)
        {
            if (StatusLabel.Text == "正在初始化…")
                StatusLabel.Text = "工作站就绪";
            _timer.Start();
        }
    }

    private async void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (_closed)
            return;
        e.Cancel = true;
        if (_closing)
            return;
        await StopForCloseAsync();
        _closed = true;
        // 清理可能同步完成，此时仍在 Closing 事件里，不能直接再次 Close。
        _ = Dispatcher.BeginInvoke(new Action(Close));
    }

    // 中控关闭时调用：先结束画面循环，再关闭窗口；共享服务由中控统一释放。
    public async Task ShutdownAsync()
    {
        if (_closed)
            return;
        await StopForCloseAsync();
        _closed = true;
        Close();
    }

    private async Task StopForCloseAsync()
    {
        if (_closing)
            return;
        _closing = true;
        IsEnabled = false;
        _timer.Stop();
        _runtime.HostLog -= _hostLog;
        _lifetime.Cancel();
        try
        {
            // 画面循环使用服务，必须在服务释放前全部结束。
            await StopCastingAsync();
        }
        catch (Exception error) when (error is OperationCanceledException or ObjectDisposedException) { }
        _lifetime.Dispose();
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (_closing)
            return;
        if (_busy)
        {
            StatusLabel.Text = "已有操作请求正在处理中，请稍候。";
            return;
        }
        _busy = true;
        Cursor = System.Windows.Input.Cursors.Wait;
        StatusLabel.Text = "正在处理操作请求…";
        try
        {
            await action();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception error) { StatusLabel.Text = "操作失败：" + error.Message; MessageBox.Show(this, error.Message, "联信工作站", MessageBoxButton.OK, MessageBoxImage.Warning); }
        finally { _busy = false; Cursor = null; }
    }

    private bool Matches(object item)
    {
        if (item is not DeviceCard card)
            return false;
        var group = GroupFilter?.SelectedItem as string;
        var query = SearchInput?.Text.Trim() ?? "";
        return (string.IsNullOrEmpty(group) || group == "所有分组" || card.GroupName == group) &&
            (query.Length == 0 || (card.Name + " " + card.Device.Udid).Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (StateFilter?.SelectedIndex switch
            {
                1 => card.UsbConnected == true,
                2 => card.CanControl,
                3 => !card.CanControl,
                _ => true
            });
    }
    private void FilterChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => FilterDevices();
    private void SearchChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => FilterDevices();
    private void FilterDevices()
    {
        DeviceView?.Refresh();
        if (CountsLabel is not null)
            UpdateSelection();
    }
    private void SelectionChanged(object sender, RoutedEventArgs e) => UpdateSelection();
    private void CardChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeviceCard.IsSelected))
            UpdateSelection();
        else if (e.PropertyName == nameof(DeviceCard.PlaceholderVisibility))
            UpdateCounts();
    }
    private void SelectVisibleClicked(object sender, RoutedEventArgs e)
    {
        var selected = SelectVisible.IsChecked == true;
        foreach (var card in DeviceView.Cast<DeviceCard>())
            card.IsSelected = selected;
        UpdateSelection();
    }
    private void UpdateSelection()
    {
        if (CountsLabel is null)
            return;
        var visible = DeviceView.Cast<DeviceCard>().ToArray();
        var selected = visible.Count(card => card.IsSelected);
        SelectVisible.IsChecked = selected == 0 ? false : selected == visible.Length ? true : null;
        EmptyLabel.Text = DeviceCards.Count == 0 ? "连接并解锁 iPhone，按手机提示信任这台电脑，然后点击一键连接。" : "没有符合当前筛选的设备。";
        EmptyLabel.Visibility = visible.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateCounts();
        TargetLabel.Text = SyncEnabled.IsChecked == true ? $"同步目标：{DeviceCards.Count(card => card.IsSelected)} 台选中设备" : $"操作目标：{_master?.Name ?? "请选择主控"}";
    }
    private void UpdateCounts()
    {
        CountsLabel.Text = $"{DeviceCards.Count(card => card.UsbConnected == true)} 台接入 USB · "
            + $"{DeviceCards.Count(card => card.Preview is not null)} 台有画面 · "
            + $"{DeviceCards.Count(card => card.CanControl)} 台可控制 · {DeviceCards.Count(card => card.IsSelected)} 台已选";
    }
    private IReadOnlyList<long> SelectedIds() => DeviceCards.Where(card => card.IsSelected).Select(card => card.Id).ToArray();
    private IReadOnlyList<long> TargetIds() => SyncEnabled.IsChecked == true ? SelectedIds() : _master is null ? [] : [_master.Id];
    private void HostLog(string message)
    {
        if (_closing)
            return;
        WdaLog.Text = message.Length > 700 ? message[..700] : message;
    }
}
