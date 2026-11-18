using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Mac.ViewModels;

namespace Lianxin.Fleet.Mac.Views;

public sealed partial class MainWindow : Window
{
    private const string AllGroups = "所有分组";
    private readonly string appDirectory;
    private readonly FleetStore store;
    private readonly IDeviceBridge bridge;
    private readonly FleetService service;
    private readonly WdaHost host;
    private readonly CancellationTokenSource lifetime = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly Dictionary<long, DeviceCard> cards = [];
    private DeviceCard[] visible = [];
    private string[] groups = [];
    private JobCard[] jobs = [];
    private DeviceCard? master;
    private Task operation = Task.CompletedTask;
    private Task refresh = Task.CompletedTask;
    private bool ready, applyingFilter, closing, closed;

    public MainWindow()
    {
        InitializeComponent();
        WdaBundleInput.Text = WdaHost.DefaultRunnerBundleId;
        // FLEET_DATA_DIR 用于隔离测试或多工作站；macOS 上 .NET 不按 HOME 解析 LocalApplicationData。
        var dataDirectory = Path.GetFullPath(Environment.GetEnvironmentVariable("FLEET_DATA_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LianxinFleet.Mac"));
        appDirectory = Path.GetFullPath(Environment.GetEnvironmentVariable("FLEET_APP_DIR") ?? Path.Combine(dataDirectory, "apps"));
        Directory.CreateDirectory(dataDirectory);
        Directory.CreateDirectory(appDirectory);
        store = new FleetStore(Path.Combine(dataDirectory, "fleet.sqlite"));
        bridge = new DeviceBridge();
        try
        {
            service = new FleetService(store, bridge, appDirectory);
        }
        catch
        {
            (bridge as IDisposable)?.Dispose();
            store.Dispose();
            throw;
        }
        host = new WdaHost(log: message => Dispatcher.UIThread.Post(() =>
        {
            if (!closing)
                WdaLog.Text = message;
        }));
        EnvironmentLabel.Text = "macOS · USB 真机";
        StatusLabel.Text = "连接设备并确认信任后，点击「扫描 USB」。";
        UpdateHostButtons();
        Opened += (_, _) =>
        {
            BeginRefresh();
            timer.Start();
        };
        Closing += CloseWindow;
        timer.Tick += (_, _) => BeginRefresh();
        ready = true;
        ApplySnapshot(service.GetSnapshot());
    }

    private async void CloseWindow(object? sender, WindowClosingEventArgs args)
    {
        if (closed)
            return;
        args.Cancel = true;
        if (closing)
            return;
        closing = true;
        IsEnabled = false;
        timer.Stop();
        lifetime.Cancel();
        try
        {
            // 读流循环必须先结束，之后才能释放服务和设备卡片的图片。
            await StopCastingAsync();
            await Task.WhenAll(operation, refresh);
            await host.DisposeAsync();
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
        }
        finally
        {
            try
            {
                await service.DisposeAsync();
            }
            finally
            {
                MasterImage.Source = null;
                foreach (var card in cards.Values)
                    card.Dispose();
                (bridge as IDisposable)?.Dispose();
                store.Dispose();
                lifetime.Dispose();
                closed = true;
                Close();
            }
        }
    }

    private void ApplySnapshot(FleetSnapshot snapshot)
    {
        foreach (var item in snapshot.Devices)
        {
            if (cards.TryGetValue(item.Device.Id, out var current))
            {
                current.Update(item);
                continue;
            }
            var card = new DeviceCard(item);
            card.PropertyChanged += CardChanged;
            cards.Add(card.Id, card);
        }
        master ??= cards.Values.FirstOrDefault(card => card.Status.Online) ?? cards.Values.FirstOrDefault();
        var currentGroups = new[] { AllGroups }.Concat(cards.Values.Select(card => card.GroupName).Distinct().Order()).ToArray();
        if (!currentGroups.SequenceEqual(groups))
        {
            var selected = GroupFilter.SelectedItem as string;
            groups = currentGroups;
            GroupFilter.ItemsSource = groups;
            GroupFilter.SelectedItem = selected is not null && groups.Contains(selected) ? selected : AllGroups;
        }
        var currentJobs = snapshot.Jobs.Select(JobCard.From).ToArray();
        if (!currentJobs.SequenceEqual(jobs))
        {
            var selected = (JobList.SelectedItem as JobCard)?.Id;
            jobs = currentJobs;
            JobList.ItemsSource = jobs;
            JobList.SelectedItem = jobs.FirstOrDefault(job => job.Id == selected) ?? jobs.FirstOrDefault();
        }
        ApplyFilter();
        UpdateMaster();
        SyncCasting();
    }

    private void ApplyFilter()
    {
        if (!ready)
            return;
        var group = GroupFilter.SelectedItem as string;
        var query = SearchInput.Text?.Trim() ?? "";
        var status = StateFilter.SelectedIndex;
        var matches = cards.Values.Where(card =>
            (group is null or AllGroups || card.GroupName == group) &&
            (card.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || card.Device.Udid.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (status <= 0 || card.Status.Online == (status == 1))).ToArray();
        if (!matches.SequenceEqual(visible))
        {
            applyingFilter = true;
            try
            {
                visible = matches;
                DeviceList.ItemsSource = visible;
                DeviceList.SelectedItem = visible.Contains(master) ? master : null;
            }
            finally { applyingFilter = false; }
        }
        EmptyLabel.IsVisible = visible.Length == 0;
        EmptyLabel.Text = cards.Count == 0 ? "通过 USB 连接并解锁 iPhone，然后扫描设备。" : "没有符合当前筛选的设备。";
        UpdateSelection();
    }

    private void CardChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(DeviceCard.IsSelected))
            return;
        ClearStaleFailure();
        UpdateSelection();
    }

    // 改变选择后，上一次"请先选择目标设备"之类的失败提示已经过时。
    private void ClearStaleFailure()
    {
        if (StatusLabel.Text?.StartsWith("操作失败", StringComparison.Ordinal) == true)
            StatusLabel.Text = "";
    }

    private void UpdateSelection()
    {
        if (!ready)
            return;
        var selected = cards.Values.Count(card => card.IsSelected);
        CountsLabel.Text = $"{cards.Count} 台设备 · {cards.Values.Count(card => card.Status.Online)} 台可控制 · {selected} 台已选";
        TargetLabel.Text = SyncEnabled.IsChecked == true ? $"同步到 {selected} 台已选设备" : $"操作目标：{master?.Name ?? "请选择主控"}";
    }

    private void SelectVisibleClicked(object? sender, RoutedEventArgs args)
    {
        foreach (var card in visible)
            card.IsSelected = true;
    }

    private void ClearSelectionClicked(object? sender, RoutedEventArgs args)
    {
        foreach (var card in cards.Values)
            card.IsSelected = false;
    }

    private void MasterChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (!ready || applyingFilter || DeviceList.SelectedItem is not DeviceCard card)
            return;
        master = card;
        ClearStaleFailure();
        UpdateMaster();
        UpdateSelection();
    }

    private void JobChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (ready)
            JobDetails.Text = (JobList.SelectedItem as JobCard)?.Details ?? "选择任务可查看每台设备的执行结果。";
    }

    private void FilterChanged(object? sender, SelectionChangedEventArgs args) => ApplyFilter();
    private void SearchChanged(object? sender, TextChangedEventArgs args) => ApplyFilter();
    private void SyncChanged(object? sender, RoutedEventArgs args)
    {
        ClearStaleFailure();
        UpdateSelection();
    }
    private long[] SelectedIds() => cards.Values.Where(card => card.IsSelected).Select(card => card.Id).ToArray();
    private long[] TargetIds() => SyncEnabled.IsChecked == true ? SelectedIds() : master is null ? [] : [master.Id];
}
