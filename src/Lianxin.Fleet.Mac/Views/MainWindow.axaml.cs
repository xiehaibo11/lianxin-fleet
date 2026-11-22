using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Mac.ViewModels;

namespace Lianxin.Fleet.Mac.Views;

public sealed partial class MainWindow : Window
{
    private const string AllGroups = "所有分组";
    private readonly string appDirectory;
    private readonly FleetRuntime runtime;
    private readonly FleetService service;
    private readonly WdaHost host;
    private readonly Action<string> hostLog;
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

    // 集控投屏窗口与中控共用同一个运行时（数据库、任务队列、WDA 通道），关闭窗口不会停止它们。
    public MainWindow(FleetRuntime runtime, string runnerBundleId)
    {
        InitializeComponent();
        this.runtime = runtime;
        WdaBundleInput.Text = runnerBundleId;
        appDirectory = runtime.AppDirectory;
        service = runtime.Service;
        host = runtime.Host;
        hostLog = message => Dispatcher.UIThread.Post(() =>
        {
            if (!closing)
                WdaLog.Text = message;
        });
        runtime.HostLog += hostLog;
        EnvironmentLabel.Text = "集控投屏 · 与中控共用设备、任务和自动化通道";
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
        await StopForCloseAsync();
        closed = true;
        // 清理可能同步完成，此时仍在 Closing 事件里，不能直接再次 Close。
        Dispatcher.UIThread.Post(Close);
    }

    // 中控关闭时调用：先结束画面循环，再关闭窗口；共享服务由中控统一释放。
    public async Task ShutdownAsync()
    {
        if (closed)
            return;
        await StopForCloseAsync();
        closed = true;
        Close();
    }

    private async Task StopForCloseAsync()
    {
        if (closing)
            return;
        closing = true;
        IsEnabled = false;
        timer.Stop();
        runtime.HostLog -= hostLog;
        lifetime.Cancel();
        try
        {
            // 读流循环必须先结束，之后才能释放设备卡片的图片。
            await StopCastingAsync();
            await Task.WhenAll(operation, refresh);
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
        }
        MasterImage.Source = null;
        foreach (var card in cards.Values)
            card.Dispose();
        lifetime.Dispose();
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
