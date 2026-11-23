using System.Windows;
using System.Windows.Controls;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Desktop.ViewModels;
using Lianxin.Fleet.Desktop.Views.Dialogs;

namespace Lianxin.Fleet.Desktop.Views.Center;

// 设备页的分组、脚本侧栏和工具栏按钮。
public partial class DevicesPage
{
    private const string AllKey = "\0all";
    private string[] groupSignature = [];
    private bool updatingGroups;
    private bool sortByName = true;

    public long? SelectedScriptId => (ScriptList.SelectedItem as ScriptItem)?.Id;

    private bool InSelectedGroups(DeviceRow row)
    {
        var selected = GroupList.SelectedItems.OfType<DeviceGroupItem>().ToArray();
        return selected.Length == 0 || selected.Any(item => item.IsAll) || selected.Any(item => item.Name == row.Group);
    }

    private static string GroupKey(DeviceGroupItem item) => item.IsAll ? AllKey : item.Name;

    // 只有分组或数量变化时才重建列表，保留当前选择。
    private void UpdateGroups()
    {
        var counts = center.Rows.GroupBy(row => row.Group).ToDictionary(group => group.Key, group => group.Count());
        var filter = GroupSearch.Text.Trim();
        var items = new List<DeviceGroupItem> { new("", center.Rows.Count, true) };
        items.AddRange(center.Runtime.Store.GetGroups().Where(name => filter.Length == 0 || name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .Select(name => new DeviceGroupItem(name, counts.GetValueOrDefault(name))));
        var signature = items.Select(item => $"{GroupKey(item)}:{item.Count}").ToArray();
        if (signature.SequenceEqual(groupSignature))
            return;
        groupSignature = signature;
        var selected = GroupList.SelectedItems.OfType<DeviceGroupItem>().Select(GroupKey).ToHashSet();
        updatingGroups = true;
        try
        {
            GroupList.ItemsSource = items;
            var matches = items.Where(item => selected.Contains(GroupKey(item))).ToArray();
            if (GroupList.SelectionMode == SelectionMode.Single)
                GroupList.SelectedItem = matches.FirstOrDefault() ?? items[0];
            else
            {
                foreach (var item in matches)
                    GroupList.SelectedItems.Add(item);
                if (matches.Length == 0)
                    GroupList.SelectedIndex = 0;
            }
        }
        finally { updatingGroups = false; }
    }

    private void GroupSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!updatingGroups)
            ApplyFilter();
    }

    private void GroupSearchChanged(object sender, TextChangedEventArgs e)
    {
        groupSignature = [];
        UpdateGroups();
    }

    private void MultiGroupClicked(object sender, RoutedEventArgs e)
    {
        GroupList.SelectionMode = MultiGroupToggle.IsChecked == true ? SelectionMode.Multiple : SelectionMode.Single;
        GroupList.SelectedIndex = 0;
        ApplyFilter();
    }

    private void AddGroupClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        var name = TextPromptDialog.Ask(Window.GetWindow(this), "添加分组", "分组名称", "");
        if (name is not null)
        {
            center.Runtime.Store.AddGroup(name);
            groupSignature = [];
            Refreshed();
        }
        return Task.CompletedTask;
    });

    private DeviceGroupItem SelectedGroup() => GroupList.SelectedItem is DeviceGroupItem { IsAll: false } item
        ? item : throw new InvalidOperationException("请先在左侧选中一个分组（不是「所有设备」）。");

    private void MoveToGroupClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        var group = SelectedGroup();
        var ids = center.Rows.Where(row => row.IsSelected).Select(row => row.Id).ToArray();
        if (ids.Length == 0)
            throw new InvalidOperationException("请先勾选要移入的设备。");
        center.Runtime.Service.AssignGroup(ids, group.Name);
        center.ApplySnapshot(center.Runtime.Service.GetSnapshot());
        ApplyFilter();
        center.Report($"已把 {ids.Length} 台设备移入「{group.Name}」。");
        return Task.CompletedTask;
    });

    private void DeleteGroupClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        var group = SelectedGroup();
        if (group.Name == FleetStore.DefaultGroup)
            throw new InvalidOperationException("「未分组」不能删除。");
        if (MessageBox.Show(Window.GetWindow(this), $"删除分组「{group.Name}」？组内 {group.Count} 台设备会回到「未分组」。", "删除分组",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return Task.CompletedTask;
        center.Runtime.Store.DeleteGroup(group.Name);
        center.ApplySnapshot(center.Runtime.Service.GetSnapshot());
        GroupList.SelectedIndex = 0;
        ApplyFilter();
        return Task.CompletedTask;
    });

    public void LoadScripts()
    {
        var filter = ScriptSearch.Text.Trim();
        var scripts = center.Runtime.Store.GetScripts().Where(script => filter.Length == 0 || script.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        var items = (sortByName ? scripts.OrderBy(script => script.Name, StringComparer.CurrentCulture)
            : scripts.OrderByDescending(script => script.UpdatedAt)).Select(script => new ScriptItem(script)).ToArray();
        var selected = SelectedScriptId;
        ScriptList.ItemsSource = items;
        ScriptList.SelectedItem = items.FirstOrDefault(item => item.Id == selected);
        NoScriptsLabel.Visibility = items.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        center.NotifyScriptsChanged();
    }

    private void ScriptSearchChanged(object sender, TextChangedEventArgs e) => LoadScripts();

    private void SortScriptsClicked(object sender, RoutedEventArgs e)
    {
        sortByName = !sortByName;
        SortButton.Content = sortByName ? "按名称" : "按更新";
        LoadScripts();
    }

    private void AddScriptClicked(object sender, RoutedEventArgs e)
    {
        if (ScriptEditorDialog.Edit(Window.GetWindow(this), null, draft => center.Runtime.Store.SaveScript(null, draft.Name, draft.Source, draft.Repeat)) is { } saved)
        {
            LoadScripts();
            ScriptList.SelectedItem = ScriptList.Items.OfType<ScriptItem>().FirstOrDefault(item => item.Id == saved.Id);
        }
    }

    private void EditScriptClicked(object sender, RoutedEventArgs e)
    {
        if (ScriptList.SelectedItem is not ScriptItem item)
            return;
        if (ScriptEditorDialog.Edit(Window.GetWindow(this), item.Script,
            draft => center.Runtime.Store.SaveScript(item.Id, draft.Name, draft.Source, draft.Repeat)) is not null)
            LoadScripts();
    }

    private void DeleteScriptClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        if (ScriptList.SelectedItem is not ScriptItem item)
            throw new InvalidOperationException("请先选中脚本。");
        if (MessageBox.Show(Window.GetWindow(this), $"删除脚本「{item.Name}」？运行记录会保留。", "删除脚本",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            center.Runtime.Store.DeleteScript(item.Id);
            LoadScripts();
        }
        return Task.CompletedTask;
    });

    private void RunScriptClicked(object sender, RoutedEventArgs e) => center.RunScript(SelectedScriptId);
    private void StopScriptClicked(object sender, RoutedEventArgs e) => center.StopScripts();
    private void PauseScriptClicked(object sender, RoutedEventArgs e) => center.PauseScripts();
    private void ResumeScriptClicked(object sender, RoutedEventArgs e) => center.ResumeScripts();
    private void StartAutomationClicked(object sender, RoutedEventArgs e) => center.StartAutomation();
    private void StopAutomationClicked(object sender, RoutedEventArgs e) => center.StopAutomation();
    private void ResetUsbClicked(object sender, RoutedEventArgs e) => center.ResetUsb();
    private void MirrorClicked(object sender, RoutedEventArgs e) => center.OpenMirror();
    private void WirelessClicked(object sender, RoutedEventArgs e) => center.OpenWireless();
    private void SetupClicked(object sender, RoutedEventArgs e) => center.OpenSetup();
    private void DiscoverClicked(object sender, RoutedEventArgs e) => center.Discover();
    private void RegisterClicked(object sender, RoutedEventArgs e) => center.RegisterDevice();
    private void InstallClicked(object sender, RoutedEventArgs e) => center.InstallApp();
    private void ExportClicked(object sender, RoutedEventArgs e) => center.ExportConfig();
    private void OpenAppsClicked(object sender, RoutedEventArgs e) => center.OpenApprovedFolder();
}
