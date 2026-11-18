using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Data;
using System.Windows.Threading;
using System.Windows;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Desktop.ViewModels;

namespace Lianxin.Fleet.Desktop.Views.Main;

public partial class MainWindow : Window
{
    private readonly string _appDirectory;
    private readonly FleetStore _store;
    private readonly FleetService _service;
    private readonly IDisposable? _bridgeLifetime;
    private readonly WdaHost _host;
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

    public MainWindow()
    {
        DeviceView = CollectionViewSource.GetDefaultView(DeviceCards);
        DeviceView.Filter = Matches;
        InitializeComponent();
        WdaBundleInput.Text = WdaHost.DefaultRunnerBundleId;
        var dataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LianxinFleet");
        Directory.CreateDirectory(dataDirectory);
        _appDirectory = Environment.GetEnvironmentVariable("FLEET_APP_DIR") ?? Path.Combine(dataDirectory, "apps");
        Directory.CreateDirectory(_appDirectory);
        _store = new FleetStore(Path.Combine(dataDirectory, "fleet.sqlite"));
        var bridge = new DeviceBridge();
        _bridgeLifetime = bridge;
        _service = new FleetService(_store, bridge, _appDirectory);
        _host = new WdaHost(log: message =>
        {
            if (!_closing && !Dispatcher.HasShutdownStarted)
                Dispatcher.BeginInvoke(new Action(() => HostLog(message)));
        });
        DataContext = this;
        GroupFilter.SelectedIndex = 0;
        EnvironmentLabel.Text = "Windows 原生工作站 · USB 直连";
        _timer.Tick += async (_, _) => await RefreshAsync();
        ApplySnapshot(_service.GetSnapshot());
    }

    private async void WindowLoaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
        if (!_closing)
            _timer.Start();
    }

    private async void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (_closed)
            return;
        e.Cancel = true;
        if (_closing)
            return;
        _closing = true;
        IsEnabled = false;
        _timer.Stop();
        _lifetime.Cancel();
        var errors = new List<string>();
        try
        {
            // 画面循环使用服务，必须在释放服务前全部结束。
            await StopCastingAsync();
        }
        catch (Exception error) { errors.Add(error.Message); }
        try
        {
            await _host.DisposeAsync();
        }
        catch (Exception error) { errors.Add(error.Message); }
        try
        {
            await _service.DisposeAsync();
        }
        catch (Exception error) { errors.Add(error.Message); }
        try
        {
            _bridgeLifetime?.Dispose();
        }
        catch (Exception error) { errors.Add(error.Message); }
        try
        {
            _store.Dispose();
        }
        catch (Exception error) { errors.Add(error.Message); }
        if (errors.Count > 0)
            MessageBox.Show(string.Join("\n", errors), "关闭工作站时发生错误");
        _lifetime.Dispose();
        _closed = true;
        Close();
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
                1 => card.Status.Online,
                2 => !card.Status.Online,
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
        EmptyLabel.Text = DeviceCards.Count == 0 ? "连接第一台 iPhone：通过 USB 连接并解锁信任，然后扫描设备。" : "没有符合当前筛选的设备。";
        EmptyLabel.Visibility = visible.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        CountsLabel.Text = $"{DeviceCards.Count} 台设备 · {DeviceCards.Count(card => card.Status.Online)} 台可控制 · {DeviceCards.Count(card => card.IsSelected)} 台已选";
        TargetLabel.Text = SyncEnabled.IsChecked == true ? $"同步目标：{DeviceCards.Count(card => card.IsSelected)} 台选中设备" : $"操作目标：{_master?.Name ?? "请选择主控"}";
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
