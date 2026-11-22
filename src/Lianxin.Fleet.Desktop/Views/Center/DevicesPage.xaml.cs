using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Lianxin.Fleet.Desktop.ViewModels;
using Lianxin.Fleet.Desktop.Views.Dialogs;

namespace Lianxin.Fleet.Desktop.Views.Center;

// 设备页：表格筛选、勾选、列设置和行右键操作。工具栏与侧栏见 DevicesPage.Panels.cs。
public partial class DevicesPage : UserControl
{
    private CenterWindow center = null!;
    private ICollectionView view = null!;
    private string query = "";

    public DevicesPage()
    {
        InitializeComponent();
    }

    public void Attach(CenterWindow host)
    {
        center = host;
        view = CollectionViewSource.GetDefaultView(center.Rows);
        view.Filter = item => item is DeviceRow row && row.Matches(query) && InSelectedGroups(row);
        DeviceGrid.ItemsSource = view;
        foreach (var column in DeviceGrid.Columns)
            if (column.Header is string { Length: > 0 } header && center.Settings.HiddenColumns.Contains(header))
                column.Visibility = Visibility.Collapsed;
        center.Refreshed += Refreshed;
        LoadScripts();
        Refreshed();
    }

    private void Refreshed()
    {
        UpdateGroups();
        var visible = view.Cast<DeviceRow>().ToArray();
        CountLabel.Text = $"{visible.Length} / {center.Rows.Count} 台 · 已勾选 {center.Rows.Count(row => row.IsSelected)} 台";
        EmptyLabel.Visibility = center.Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        SelectAllButton.Content = visible.Length > 0 && visible.All(row => row.IsSelected) ? "☑ 全选" : "☐ 全选";
    }

    // 分组或搜索条件变化后重新筛选；刷新设备状态时不重排，避免表格跳动。
    private void ApplyFilter()
    {
        view.Refresh();
        Refreshed();
    }

    private void SearchClicked(object sender, RoutedEventArgs e)
    {
        query = SearchInput.Text.Trim();
        ApplyFilter();
    }

    private void SearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            SearchClicked(sender, e);
    }

    private void SelectAllClicked(object sender, RoutedEventArgs e)
    {
        var visible = view.Cast<DeviceRow>().ToArray();
        var target = !visible.All(row => row.IsSelected);
        foreach (var row in visible)
            row.IsSelected = target;
        Refreshed();
    }

    private void InvertSelectionClicked(object sender, RoutedEventArgs e)
    {
        foreach (var row in view.Cast<DeviceRow>())
            row.IsSelected = !row.IsSelected;
        Refreshed();
    }

    private void SelectOnlineClicked(object sender, RoutedEventArgs e)
    {
        foreach (var row in view.Cast<DeviceRow>())
            row.IsSelected = row.Online;
        Refreshed();
    }

    private void CopyIdsClicked(object sender, RoutedEventArgs e)
    {
        var ids = center.Rows.Where(row => row.IsSelected).Select(row => row.Udid).ToArray();
        if (ids.Length == 0)
        {
            center.Report("请先勾选设备。");
            return;
        }
        Clipboard.SetText(string.Join(Environment.NewLine, ids));
        center.Report($"已复制 {ids.Length} 个设备 ID。");
    }

    private void ListMoreClicked(object sender, RoutedEventArgs e) => OpenMenu(ListMoreButton);
    private void ToolMoreClicked(object sender, RoutedEventArgs e) => OpenMenu(ToolMoreButton);

    private static void OpenMenu(Button button)
    {
        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.IsOpen = true;
    }

    // 列设置：勾选显示的列，结果保存到工作站设置。
    private void ColumnsClicked(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = ColumnsButton };
        foreach (var column in DeviceGrid.Columns.Where(column => column.Header is string { Length: > 0 }))
        {
            var item = new MenuItem { Header = column.Header, IsCheckable = true, IsChecked = column.Visibility == Visibility.Visible };
            item.Click += (_, _) =>
            {
                column.Visibility = item.IsChecked ? Visibility.Visible : Visibility.Collapsed;
                var hidden = DeviceGrid.Columns.Where(c => c.Visibility != Visibility.Visible && c.Header is string { Length: > 0 })
                    .Select(c => (string)c.Header).ToArray();
                center.SaveSettings(center.Settings with
                {
                    HiddenColumns = hidden
                });
            };
            menu.Items.Add(item);
        }
        menu.IsOpen = true;
    }

    private void GridDoubleClicked(object sender, MouseButtonEventArgs e)
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

    private void AliasClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        var rows = ContextRows();
        if (rows.Count != 1)
            throw new InvalidOperationException("请选中一台设备再设置别名。");
        var alias = TextPromptDialog.Ask(Window.GetWindow(this), "设置别名", $"「{rows[0].Name}」的别名（留空清除）", rows[0].Alias, allowEmpty: true);
        if (alias is null)
            return Task.CompletedTask;
        center.Runtime.Store.SetAlias(rows[0].Id, alias);
        center.ApplySnapshot(center.Runtime.Service.GetSnapshot());
        return Task.CompletedTask;
    });

    private void MoveRowsClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        var rows = ContextRows();
        if (rows.Count == 0)
            throw new InvalidOperationException("请先选中或勾选设备。");
        var group = TextPromptDialog.Ask(Window.GetWindow(this), "移到分组", $"把 {rows.Count} 台设备移到分组", "");
        if (group is null)
            return Task.CompletedTask;
        center.Runtime.Service.AssignGroup(rows.Select(row => row.Id).ToArray(), group);
        center.ApplySnapshot(center.Runtime.Service.GetSnapshot());
        ApplyFilter();
        return Task.CompletedTask;
    });

    private void TestAutomationClicked(object sender, RoutedEventArgs e) => center.Run(async () =>
    {
        var rows = ContextRows();
        if (rows.Count == 0)
            throw new InvalidOperationException("请先选中或勾选设备。");
        center.ApplySnapshot(await center.Runtime.Service.RefreshDeviceStatusesAsync(rows.Select(row => row.Id).ToArray(), center.Lifetime));
        var failed = rows.Where(row => !row.AutomationReady).ToArray();
        center.Report(failed.Length == 0 ? $"{rows.Count} 台设备的自动化均正常。"
            : $"{failed.Length} 台自动化未就绪：" + string.Join("；", failed.Take(3).Select(row => $"{row.Name}：{row.AutomationTip}")));
    });

    private void CopyRowIdClicked(object sender, RoutedEventArgs e)
    {
        var rows = ContextRows();
        if (rows.Count > 0)
            Clipboard.SetText(string.Join(Environment.NewLine, rows.Select(row => row.Udid)));
    }
}
