using Avalonia.Controls;
using Avalonia.Interactivity;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Mac.ViewModels;
using Lianxin.Fleet.Mac.Views.Dialogs;

namespace Lianxin.Fleet.Mac.Views.Center;

// 设备页的分组、脚本侧栏和工具栏按钮。
public sealed partial class DevicesPage
{
    private const string AllKey = "\0all";
    private string[] groupSignature = [];
    private bool updatingGroups;
    private bool sortByName = true;

    public long? SelectedScriptId => (ScriptList.SelectedItem as ScriptItem)?.Id;

    private DeviceGroupItem[] SelectedGroups() => GroupList.SelectedItems?.OfType<DeviceGroupItem>().ToArray() ?? [];

    private bool InSelectedGroups(DeviceRow row)
    {
        var selected = SelectedGroups();
        return selected.Length == 0 || selected.Any(item => item.IsAll) || selected.Any(item => item.Name == row.Group);
    }

    private static string GroupKey(DeviceGroupItem item) => item.IsAll ? AllKey : item.Name;

    // 只有分组或数量变化时才重建列表，保留当前选择。
    private void UpdateGroups()
    {
        var counts = center.Rows.GroupBy(row => row.Group).ToDictionary(group => group.Key, group => group.Count());
        var filter = GroupSearch.Text?.Trim() ?? "";
        var items = new List<DeviceGroupItem> { new("", center.Rows.Count, true) };
        items.AddRange(center.Runtime.Store.GetGroups().Where(name => filter.Length == 0 || name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .Select(name => new DeviceGroupItem(name, counts.GetValueOrDefault(name))));
        var signature = items.Select(item => $"{GroupKey(item)}:{item.Count}").ToArray();
        if (signature.SequenceEqual(groupSignature))
            return;
        groupSignature = signature;
        var selected = SelectedGroups().Select(GroupKey).ToHashSet();
        updatingGroups = true;
        try
        {
            GroupList.ItemsSource = items;
            var matches = items.Where(item => selected.Contains(GroupKey(item))).ToArray();
            if (GroupList.SelectionMode.HasFlag(SelectionMode.Multiple))
            {
                foreach (var item in matches)
                    GroupList.SelectedItems?.Add(item);
                if (matches.Length == 0)
                    GroupList.SelectedIndex = 0;
            }
            else
                GroupList.SelectedItem = matches.FirstOrDefault() ?? items[0];
        }
        finally { updatingGroups = false; }
    }

    private void GroupSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!updatingGroups && center is not null)
            ApplyFilter();
    }

    private void GroupSearchChanged(object? sender, TextChangedEventArgs e)
    {
        if (center is null)
            return;
        groupSignature = [];
        UpdateGroups();
    }

    private void MultiGroupClicked(object? sender, RoutedEventArgs e)
    {
        GroupList.SelectionMode = MultiGroupToggle.IsChecked == true ? SelectionMode.Multiple | SelectionMode.Toggle : SelectionMode.Single;
        GroupList.SelectedIndex = 0;
        ApplyFilter();
    }

    private void AddGroupClicked(object? sender, RoutedEventArgs e) => center.Run(async () =>
    {
        var name = await TextPromptDialog.AskAsync(center, "添加分组", "分组名称", "");
        if (name is null || center.IsClosing)
            return;
        center.Runtime.Store.AddGroup(name);
        groupSignature = [];
        Refreshed();
    });

    private DeviceGroupItem SelectedGroup() => GroupList.SelectedItem is DeviceGroupItem { IsAll: false } item
        ? item : throw new InvalidOperationException("请先在左侧选中一个分组（不是「所有设备」）。");

    private void MoveToGroupClicked(object? sender, RoutedEventArgs e) => center.Run(() =>
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

    private void DeleteGroupClicked(object? sender, RoutedEventArgs e) => center.Run(async () =>
    {
        var group = SelectedGroup();
        if (group.Name == FleetStore.DefaultGroup)
            throw new InvalidOperationException("「未分组」不能删除。");
        if (!await DialogKit.ConfirmAsync(center, "删除分组", $"删除分组「{group.Name}」？组内 {group.Count} 台设备会回到「未分组」。", "删除")
            || center.IsClosing)
            return;
        center.Runtime.Store.DeleteGroup(group.Name);
        center.ApplySnapshot(center.Runtime.Service.GetSnapshot());
        GroupList.SelectedIndex = 0;
        ApplyFilter();
    });

    public void LoadScripts()
    {
        var filter = ScriptSearch.Text?.Trim() ?? "";
        var scripts = center.Runtime.Store.GetScripts().Where(script => filter.Length == 0 || script.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        var items = (sortByName ? scripts.OrderBy(script => script.Name, StringComparer.CurrentCulture)
            : scripts.OrderByDescending(script => script.UpdatedAt)).Select(script => new ScriptItem(script)).ToArray();
        var selected = SelectedScriptId;
        ScriptList.ItemsSource = items;
        ScriptList.SelectedItem = items.FirstOrDefault(item => item.Id == selected);
        NoScriptsLabel.IsVisible = items.Length == 0;
        center.NotifyScriptsChanged();
    }

    private void ScriptSearchChanged(object? sender, TextChangedEventArgs e)
    {
        if (center is not null)
            LoadScripts();
    }

    private void SortScriptsClicked(object? sender, RoutedEventArgs e)
    {
        sortByName = !sortByName;
        SortButton.Content = sortByName ? "按名称" : "按更新";
        LoadScripts();
    }

    private async void AddScriptClicked(object? sender, RoutedEventArgs e)
    {
        if (await ScriptEditorDialog.EditAsync(center, null, draft => center.Runtime.Store.SaveScript(null, draft.Name, draft.Source, draft.Repeat)) is { } saved
            && !center.IsClosing)
        {
            LoadScripts();
            ScriptList.SelectedItem = ScriptList.Items.OfType<ScriptItem>().FirstOrDefault(item => item.Id == saved.Id);
        }
    }

    private async void EditScriptClicked(object? sender, RoutedEventArgs e)
    {
        if (ScriptList.SelectedItem is not ScriptItem item)
            return;
        if (await ScriptEditorDialog.EditAsync(center, item.Script,
            draft => center.Runtime.Store.SaveScript(item.Id, draft.Name, draft.Source, draft.Repeat)) is not null && !center.IsClosing)
            LoadScripts();
    }

    private void DeleteScriptClicked(object? sender, RoutedEventArgs e) => center.Run(async () =>
    {
        if (ScriptList.SelectedItem is not ScriptItem item)
            throw new InvalidOperationException("请先选中脚本。");
        if (!await DialogKit.ConfirmAsync(center, "删除脚本", $"删除脚本「{item.Name}」？运行记录会保留。", "删除") || center.IsClosing)
            return;
        center.Runtime.Store.DeleteScript(item.Id);
        LoadScripts();
    });

    private void RunScriptClicked(object? sender, RoutedEventArgs e) => center.RunScript(SelectedScriptId);
    private void StopScriptClicked(object? sender, RoutedEventArgs e) => center.StopScripts();
    private void StartAutomationClicked(object? sender, RoutedEventArgs e) => center.StartAutomation();
    private void StopAutomationClicked(object? sender, RoutedEventArgs e) => center.StopAutomation();
    private void ResetUsbClicked(object? sender, RoutedEventArgs e) => center.ResetUsb();
    private void MirrorClicked(object? sender, RoutedEventArgs e) => center.OpenMirror();
    private void WirelessClicked(object? sender, RoutedEventArgs e) => center.OpenWireless();
    private void SetupClicked(object? sender, RoutedEventArgs e) => center.OpenSetup();
    private void DiscoverClicked(object? sender, RoutedEventArgs e) => center.Discover();
    private void RegisterClicked(object? sender, RoutedEventArgs e) => center.RegisterDevice();
    private void InstallClicked(object? sender, RoutedEventArgs e) => center.InstallApp();
    private void ExportClicked(object? sender, RoutedEventArgs e) => center.ExportConfig();
    private void OpenAppsClicked(object? sender, RoutedEventArgs e) => center.OpenApprovedFolder();
}
