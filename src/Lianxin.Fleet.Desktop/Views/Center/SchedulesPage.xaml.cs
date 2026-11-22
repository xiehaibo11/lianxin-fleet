using System.Windows;
using System.Windows.Controls;
using Lianxin.Fleet.Desktop.ViewModels;
using Lianxin.Fleet.Desktop.Views.Dialogs;

namespace Lianxin.Fleet.Desktop.Views.Center;

public partial class SchedulesPage : UserControl
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
        IsVisibleChanged += (_, _) => Reload();
        Reload();
    }

    private void Reload()
    {
        // 关闭窗口时 WPF 仍会触发可见性变化，此时数据库已经释放。
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

    private void AddClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        if (ScheduleDialog.Edit(Window.GetWindow(this), center.Runtime.Store, null, center.SelectedRows().Select(row => row.Id).ToArray()) is not null)
            Reload();
        return Task.CompletedTask;
    });

    private void EditClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        var item = Selected();
        if (ScheduleDialog.Edit(Window.GetWindow(this), center.Runtime.Store, item.Schedule, center.SelectedRows().Select(row => row.Id).ToArray()) is not null)
            Reload();
        return Task.CompletedTask;
    });

    private void ToggleClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        var item = Selected();
        center.Runtime.Store.SetScheduleEnabled(item.Id, !item.Schedule.Enabled);
        Reload();
        return Task.CompletedTask;
    });

    private void RunNowClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        var firing = center.Runtime.Schedules.RunNow(Selected().Id);
        center.Report(firing.Message);
        Reload();
        return Task.CompletedTask;
    });

    private void DeleteClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        var item = Selected();
        if (MessageBox.Show(Window.GetWindow(this), $"删除定时任务「{item.Name}」？", "删除定时任务", MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            center.Runtime.Store.DeleteSchedule(item.Id);
            Reload();
        }
        return Task.CompletedTask;
    });
}
