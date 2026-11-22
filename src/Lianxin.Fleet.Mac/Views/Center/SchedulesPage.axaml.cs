using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Lianxin.Fleet.Mac.ViewModels;
using Lianxin.Fleet.Mac.Views.Dialogs;

namespace Lianxin.Fleet.Mac.Views.Center;

public sealed partial class SchedulesPage : UserControl
{
    private CenterWindow center = null!;
    private string signature = "";

    public SchedulesPage()
    {
        InitializeComponent();
    }

    public void Attach(CenterWindow host)
    {
        center = host;
        center.ScriptsChanged += Reload;
        // 定时触发会更新「上次运行」，随设备刷新一起检查，内容不变时不重建列表。
        center.Refreshed += () =>
        {
            if (IsVisible)
                Reload();
        };
        Reload();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty && center is not null)
            Reload();
    }

    private void Reload()
    {
        // 关闭窗口后数据库已经释放。
        if (center.IsClosing)
            return;
        var names = center.Runtime.Store.GetScripts().ToDictionary(script => script.Id, script => script.Name);
        var items = center.Runtime.Store.GetSchedules().Select(schedule =>
            new ScheduleItem(schedule, names.GetValueOrDefault(schedule.ScriptId, "（脚本已删除）"))).ToArray();
        LastLabel.Text = center.Runtime.Schedules.LastMessage;
        var current = string.Join("|", items.Select(item => item.ToString()));
        if (current == signature)
            return;
        signature = current;
        var selected = (ScheduleGrid.SelectedItem as ScheduleItem)?.Id;
        ScheduleGrid.ItemsSource = items;
        ScheduleGrid.SelectedItem = items.FirstOrDefault(item => item.Id == selected);
    }

    private ScheduleItem Selected() => ScheduleGrid.SelectedItem as ScheduleItem
        ?? throw new InvalidOperationException("请先选中一个定时任务。");

    private long[] CheckedIds() => center.SelectedRows().Select(row => row.Id).ToArray();

    private void AddClicked(object? sender, RoutedEventArgs e) => center.Run(async () =>
    {
        if (await ScheduleDialog.EditAsync(center, center.Runtime.Store, null, CheckedIds()) is not null && !center.IsClosing)
            Reload();
    });

    private void EditClicked(object? sender, RoutedEventArgs e) => center.Run(async () =>
    {
        var item = Selected();
        if (await ScheduleDialog.EditAsync(center, center.Runtime.Store, item.Schedule, CheckedIds()) is not null && !center.IsClosing)
            Reload();
    });

    private void ToggleClicked(object? sender, RoutedEventArgs e) => center.Run(() =>
    {
        var item = Selected();
        center.Runtime.Store.SetScheduleEnabled(item.Id, !item.Schedule.Enabled);
        Reload();
        return Task.CompletedTask;
    });

    private void RunNowClicked(object? sender, RoutedEventArgs e) => center.Run(() =>
    {
        var firing = center.Runtime.Schedules.RunNow(Selected().Id);
        center.Report(firing.Message);
        Reload();
        return Task.CompletedTask;
    });

    private void DeleteClicked(object? sender, RoutedEventArgs e) => center.Run(async () =>
    {
        var item = Selected();
        if (!await DialogKit.ConfirmAsync(center, "删除定时任务", $"删除定时任务「{item.Name}」？", "删除") || center.IsClosing)
            return;
        center.Runtime.Store.DeleteSchedule(item.Id);
        Reload();
    });
}
