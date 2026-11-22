using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Mac.ViewModels;

namespace Lianxin.Fleet.Mac.Views.Dialogs;

// 新建或编辑定时任务。保存失败时留在对话框里显示原因。
public sealed class ScheduleDialog : Window
{
    private static readonly string[] DayNames = ["日", "一", "二", "三", "四", "五", "六"];

    private ScheduleDialog(FleetStore store, FleetSchedule? existing, IReadOnlyList<long> checkedIds)
    {
        var panel = DialogKit.Prepare(this, existing is null ? "新建定时任务" : "编辑定时任务", 520);
        var name = DialogKit.Field(panel, "名称", new TextBox { Text = existing?.Name ?? "", MaxLength = 60 });
        // ScriptItem 的 ToString 是脚本名，列表和读屏软件显示的都是名称。
        var scripts = store.GetScripts().Select(item => new ScriptItem(item)).ToArray();
        var script = DialogKit.Field(panel, "运行脚本", new ComboBox { ItemsSource = scripts, MinWidth = 240 });
        script.SelectedItem = scripts.FirstOrDefault(item => item.Id == existing?.ScriptId) ?? scripts.FirstOrDefault();

        panel.Children.Add(new Label { Content = "目标设备", Padding = new Thickness(0, 8, 0, 2) });
        var all = Radio(panel, "全部设备", "Target");
        var byGroup = Radio(panel, "分组：", "Target");
        var groupNames = store.GetGroups();
        var groups = new ComboBox { ItemsSource = groupNames, Width = 200, Margin = new Thickness(22, 0, 0, 6) };
        AutomationProperties.SetName(groups, "目标分组");
        panel.Children.Add(groups);
        var fixedIds = existing?.TargetIds.Count > 0 ? existing.TargetIds : checkedIds;
        var byIds = Radio(panel, existing?.TargetIds.Count > 0 ? $"保持原来指定的 {fixedIds.Count} 台设备" : $"当前勾选的 {checkedIds.Count} 台设备", "Target");
        byIds.IsEnabled = fixedIds.Count > 0;
        (existing?.TargetIds.Count > 0 ? byIds : existing?.TargetGroup is not null ? byGroup : all).IsChecked = true;
        groups.SelectedItem = existing?.TargetGroup ?? groupNames.FirstOrDefault();

        panel.Children.Add(new Label { Content = "执行规则（按本机时间，中控运行期间生效）", Padding = new Thickness(0, 10, 0, 2) });
        var daily = Radio(panel, "每天定时", "Kind");
        var timeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(22, 0, 0, 4) };
        var at = new TextBox { Text = (existing?.At ?? new TimeOnly(9, 0)).ToString("HH:mm", CultureInfo.InvariantCulture), Width = 76 };
        AutomationProperties.SetName(at, "每天时间");
        timeRow.Children.Add(at);
        var days = DayNames.Select((label, index) => new CheckBox
        {
            Content = label,
            MinWidth = 0,
            IsChecked = ((existing?.Kind == ScheduleKind.Daily ? existing.Weekdays : 127) & (1 << index)) != 0
        }).ToArray();
        foreach (var day in days)
            timeRow.Children.Add(day);
        panel.Children.Add(timeRow);
        var interval = Radio(panel, "每隔一段时间（分钟）", "Kind");
        var minutes = new TextBox
        {
            Text = (existing?.Kind == ScheduleKind.Interval ? existing.IntervalMinutes : 60).ToString(CultureInfo.InvariantCulture),
            Width = 76,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(22, 0, 0, 4)
        };
        AutomationProperties.SetName(minutes, "间隔分钟");
        ToolTip.SetTip(minutes, "分钟");
        panel.Children.Add(minutes);
        (existing?.Kind == ScheduleKind.Interval ? interval : daily).IsChecked = true;
        var enabled = new CheckBox { Content = "启用", IsChecked = existing?.Enabled ?? true, Margin = new Thickness(0, 10, 0, 0) };
        panel.Children.Add(enabled);
        var error = DialogKit.Note(panel, "", DialogKit.ErrorBrush);
        var buttons = DialogKit.Buttons(this, panel, "保存", "取消");
        buttons[0].Click += (_, _) =>
        {
            try
            {
                if (script.SelectedItem is not ScriptItem chosen)
                    throw new ArgumentException("请先在设备页新建脚本。");
                if (!TimeOnly.TryParseExact(at.Text?.Trim(), ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                    throw new ArgumentException("时间格式应为 09:30。");
                if (!int.TryParse(minutes.Text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var every))
                    every = 0;
                var mask = days.Select((day, index) => day.IsChecked == true ? 1 << index : 0).Sum();
                Close(store.SaveSchedule(new FleetSchedule(existing?.Id ?? 0, name.Text ?? "", chosen.Id,
                    interval.IsChecked == true ? ScheduleKind.Interval : ScheduleKind.Daily, time, mask, every,
                    byGroup.IsChecked == true ? groups.SelectedItem as string : null,
                    byIds.IsChecked == true ? fixedIds : [], enabled.IsChecked == true, DateTimeOffset.UtcNow)));
            }
            catch (Exception failure) when (failure is ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                error.Text = failure.Message;
            }
        };
    }

    private static RadioButton Radio(Panel panel, string text, string group)
    {
        var radio = new RadioButton { Content = text, GroupName = group };
        panel.Children.Add(radio);
        return radio;
    }

    public static Task<FleetSchedule?> EditAsync(Window owner, FleetStore store, FleetSchedule? existing, IReadOnlyList<long> checkedIds) =>
        new ScheduleDialog(store, existing, checkedIds).ShowDialog<FleetSchedule?>(owner);
}
