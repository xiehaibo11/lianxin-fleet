using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Lianxin.Fleet.Mac.ViewModels;
using Lianxin.Fleet.Mac.Views.Dialogs;

namespace Lianxin.Fleet.Mac.Views.Center;

// 设备页：表格筛选、勾选、列设置和行右键操作。工具栏与侧栏见 DevicesPage.Panels.cs。
public sealed partial class DevicesPage : UserControl
{
    private CenterWindow center = null!;
    private DataGridCollectionView view = null!;
    private string query = "";

    public DevicesPage()
    {
        InitializeComponent();
        // 单行输入框自己不处理回车，在隧道阶段截获，按回车即搜索。
        SearchInput.AddHandler(KeyDownEvent, SearchKeyDown, RoutingStrategies.Tunnel);
    }

    public void Attach(CenterWindow host)
    {
        center = host;
        view = new DataGridCollectionView(center.Rows) { Filter = item => item is DeviceRow row && row.Matches(query) && InSelectedGroups(row) };
        DeviceGrid.ItemsSource = view;
        foreach (var column in DeviceGrid.Columns)
            if (column.Header is string { Length: > 0 } header && center.Settings.HiddenColumns.Contains(header))
                column.IsVisible = false;
        center.Refreshed += Refreshed;
        center.Refreshed += BatteryTick;
        LoadScripts();
        Refreshed();
    }

    private DeviceRow[] Visible() => view.Cast<DeviceRow>().ToArray();

    private void Refreshed()
    {
        UpdateGroups();
        var visible = Visible();
        CountLabel.Text = $"{visible.Length} / {center.Rows.Count} 台 · 已勾选 {center.Rows.Count(row => row.IsSelected)} 台";
        EmptyLabel.IsVisible = center.Rows.Count == 0;
        SelectAllButton.Content = visible.Length > 0 && visible.All(row => row.IsSelected) ? "☑ 全选" : "☐ 全选";
    }

    // 分组或搜索条件变化后重新筛选；刷新设备状态时不重排，避免表格跳动。
    private void ApplyFilter()
    {
        view.Refresh();
        Refreshed();
    }

    private void SearchClicked(object? sender, RoutedEventArgs e)
    {
        query = SearchInput.Text?.Trim() ?? "";
        ApplyFilter();
    }

    private void SearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        e.Handled = true;
        SearchClicked(sender, e);
    }

    private void SelectAllClicked(object? sender, RoutedEventArgs e)
    {
        var visible = Visible();
        var target = !visible.All(row => row.IsSelected);
        foreach (var row in visible)
            row.IsSelected = target;
        Refreshed();
    }

    private void InvertSelectionClicked(object? sender, RoutedEventArgs e)
    {
        foreach (var row in Visible())
            row.IsSelected = !row.IsSelected;
        Refreshed();
    }

    private void SelectOnlineClicked(object? sender, RoutedEventArgs e)
    {
        foreach (var row in Visible())
            row.IsSelected = row.Online;
        Refreshed();
    }

    private async Task CopyAsync(IReadOnlyCollection<string> ids)
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
            await clipboard.SetTextAsync(string.Join(Environment.NewLine, ids));
        center.Report($"已复制 {ids.Count} 个设备 ID。");
    }

    private async void CopyIdsClicked(object? sender, RoutedEventArgs e)
    {
        var ids = center.Rows.Where(row => row.IsSelected).Select(row => row.Udid).ToArray();
        if (ids.Length == 0)
            center.Report("请先勾选设备。");
        else
            await CopyAsync(ids);
    }

    // 列设置：勾选显示的列，结果保存到工作站设置。
    private void ColumnsClicked(object? sender, RoutedEventArgs e)
    {
        var menu = new MenuFlyout();
        foreach (var column in DeviceGrid.Columns.Where(column => column.Header is string { Length: > 0 }))
        {
            var item = new MenuItem { Header = column.Header, ToggleType = MenuItemToggleType.CheckBox, IsChecked = column.IsVisible };
            item.Click += (_, _) =>
            {
                column.IsVisible = !column.IsVisible;
                item.IsChecked = column.IsVisible;
                var hidden = DeviceGrid.Columns.Where(c => !c.IsVisible && c.Header is string { Length: > 0 })
                    .Select(c => (string)c.Header!).ToArray();
                center.SaveSettings(center.Settings with
                {
                    HiddenColumns = hidden
                });
            };
            menu.Items.Add(item);
        }
        menu.ShowAt(ColumnsButton);
    }

    private void GridDoubleClicked(object? sender, TappedEventArgs e)
    {
        if (DeviceGrid.SelectedItem is DeviceRow row)
        {
            row.IsSelected = !row.IsSelected;
            Refreshed();
        }
    }

    // 右键操作作用于表格中选中（高亮）的行；没有高亮行时用勾选的行。
    private IReadOnlyList<DeviceRow> ContextRows()
    {
        var highlighted = DeviceGrid.SelectedItems.OfType<DeviceRow>().ToArray();
        return highlighted.Length > 0 ? highlighted : center.Rows.Where(row => row.IsSelected).ToArray();
    }

    private void AliasClicked(object? sender, RoutedEventArgs e) => center.Run(async () =>
    {
        var rows = ContextRows();
        if (rows.Count != 1)
            throw new InvalidOperationException("请选中一台设备再设置别名。");
        var alias = await TextPromptDialog.AskAsync(center, "设置别名", $"「{rows[0].Name}」的别名（留空清除）", rows[0].Alias, allowEmpty: true);
        if (alias is null || center.IsClosing)
            return;
        center.Runtime.Store.SetAlias(rows[0].Id, alias);
        center.ApplySnapshot(center.Runtime.Service.GetSnapshot());
    });

    private void MoveRowsClicked(object? sender, RoutedEventArgs e) => center.Run(async () =>
    {
        var rows = ContextRows();
        if (rows.Count == 0)
            throw new InvalidOperationException("请先选中或勾选设备。");
        var group = await TextPromptDialog.AskAsync(center, "移到分组", $"把 {rows.Count} 台设备移到分组", "");
        if (group is null || center.IsClosing)
            return;
        center.Runtime.Service.AssignGroup(rows.Select(row => row.Id).ToArray(), group);
        center.ApplySnapshot(center.Runtime.Service.GetSnapshot());
        ApplyFilter();
    });

    private void TestAutomationClicked(object? sender, RoutedEventArgs e) => center.Run(async () =>
    {
        var rows = ContextRows();
        if (rows.Count == 0)
            throw new InvalidOperationException("请先选中或勾选设备。");
        center.ApplySnapshot(await center.Runtime.Service.RefreshDeviceStatusesAsync(rows.Select(row => row.Id).ToArray(), center.Lifetime));
        var failed = rows.Where(row => !row.AutomationReady).ToArray();
        center.Report(failed.Length == 0 ? $"{rows.Count} 台设备的自动化均正常。"
            : $"{failed.Length} 台自动化未就绪：" + string.Join("；", failed.Take(3).Select(row => $"{row.Name}：{row.AutomationTip}")));
    });

    private async void CopyRowIdClicked(object? sender, RoutedEventArgs e)
    {
        var rows = ContextRows();
        if (rows.Count > 0)
            await CopyAsync(rows.Select(row => row.Udid).ToArray());
    }
}
