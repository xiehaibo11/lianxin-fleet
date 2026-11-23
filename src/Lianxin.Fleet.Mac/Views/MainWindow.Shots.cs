using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Mac.Interaction;
using Lianxin.Fleet.Mac.ViewModels;

namespace Lianxin.Fleet.Mac.Views;

// 截图保存、小屏幕右键菜单与缩放、文字框回车发送。
public sealed partial class MainWindow
{
    private const double MinZoom = 0.6, MaxZoom = 2;
    private double zoom = 1;
    private string? shotFolder;

    private void SaveMasterShotClicked(object? sender, RoutedEventArgs args) =>
        Run(() => SaveShotsAsync(master is null ? [] : [master]));

    private void SaveSelectedShotsClicked(object? sender, RoutedEventArgs args) =>
        Run(() => SaveShotsAsync(cards.Values.Where(card => card.IsSelected).ToArray()));

    private void TileShotClicked(object? sender, RoutedEventArgs args)
    {
        if ((sender as Control)?.DataContext is DeviceCard card)
            Run(() => SaveShotsAsync([card]));
    }

    // 小屏幕右键菜单：Tag 为 FleetAction 名称，只作用于这一台，不录制。
    private void TileActionClicked(object? sender, RoutedEventArgs args)
    {
        if (sender is Control { DataContext: DeviceCard card, Tag: string tag })
            Run(() => QueueAsync(Enum.Parse<FleetAction>(tag), new ActionParameters(), [card.Id]));
    }

    private void TileMasterClicked(object? sender, RoutedEventArgs args)
    {
        if ((sender as Control)?.DataContext is DeviceCard card && visible.Contains(card))
            DeviceList.SelectedItem = card;
    }

    // 优先读取 WDA 原始截图；失败时保存界面上正在显示的画面。
    private async Task SaveShotsAsync(IReadOnlyList<DeviceCard> targets)
    {
        if (targets.Count == 0)
            throw new InvalidOperationException("请先选择主控设备，或勾选要截图的设备。");
        var ready = targets.Where(card => card.Status.Online || card.Preview is not null).ToArray();
        if (ready.Length == 0)
            throw new InvalidOperationException("选中的设备都没有画面，无法截图。");
        var start = shotFolder is null ? await StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Pictures)
            : await StorageProvider.TryGetFolderFromPathAsync(shotFolder);
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = $"选择截图保存位置（{ready.Length} 台设备）",
            SuggestedStartLocation = start
        });
        if (folders.FirstOrDefault()?.TryGetLocalPath() is not { } folder || closing)
        {
            StatusLabel.Text = "已取消保存截图。";
            return;
        }
        shotFolder = folder;
        StatusLabel.Text = $"正在保存 {ready.Length} 台设备的截图…";
        var at = DateTime.Now;
        using var slots = new SemaphoreSlim(4);
        var results = await Task.WhenAll(ready.Select(card => SaveShotAsync(card, folder, at, slots)));
        var failures = results.Where(result => result.Error is not null).Select(result => result.Error).ToArray();
        var frames = results.Count(result => result.Error is null && result.FromFrame);
        StatusLabel.Text = $"已保存 {ready.Length - failures.Length}/{ready.Length} 张截图到 {folder}"
            + (frames > 0 ? $"（{frames} 张取不到原始截图，保存的是约 30% 尺寸的预览画面，文件名带 _preview，不能用来截取找图模板）" : "") + (failures.Length > 0 ? "。失败：" + string.Join("；", failures) : "。")
            + (targets.Count > ready.Length ? $" 已跳过 {targets.Count - ready.Length} 台没有画面的设备。" : "");
    }

    private async Task<(string? Error, bool FromFrame)> SaveShotAsync(DeviceCard card, string folder, DateTime at, SemaphoreSlim slots)
    {
        await slots.WaitAsync(lifetime.Token);
        try
        {
            byte[] png;
            var fromFrame = false;
            try
            {
                png = ScreenshotFiles.ToPng(await service.GetScreenshotAsync(card.Id, lifetime.Token));
            }
            catch (Exception) when (!lifetime.IsCancellationRequested && card.Preview is { } frame)
            {
                // 在界面线程上同步编码，画面循环不会在这期间释放这张图。
                png = ScreenshotFiles.ToPng(frame);
                fromFrame = true;
            }
            // 预览画面只有约 30% 尺寸，文件名带 _preview，免得被当成原始截图拿去截模板。
            await ScreenshotFiles.WriteAsync(folder, card.Name, at, png, lifetime.Token, fromFrame ? "_preview" : "");
            return (null, fromFrame);
        }
        catch (Exception error) when (!lifetime.IsCancellationRequested)
        {
            return ($"{card.Name}：{error.Message}", false);
        }
        finally { slots.Release(); }
    }

    // Ctrl（⌘）+滚轮缩放设备墙上的小屏幕；在隧道阶段处理，避免列表先把滚轮当成滚动。
    private void WallWheel(object? sender, PointerWheelEventArgs args)
    {
        if ((args.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) == 0 || args.Delta.Y == 0)
            return;
        args.Handled = true;
        var next = Math.Clamp(Math.Round(zoom + (args.Delta.Y > 0 ? 0.1 : -0.1), 1), MinZoom, MaxZoom);
        if (next == zoom)
            return;
        zoom = next;
        foreach (var card in cards.Values)
            card.SetZoom(zoom);
        StatusLabel.Text = $"小屏幕缩放 {zoom:P0}";
    }

    // 文字框回车：信息框输入文字，链接框打开链接。
    private void TextKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key != Key.Enter || args.KeyModifiers != KeyModifiers.None)
            return;
        args.Handled = true;
        if (sender == UrlInput)
            OpenUrlClicked(sender, args);
        else
            Run(() => QueueAsync(FleetAction.Text, new ActionParameters(Text: DeviceTextInput.Text)));
    }

    private void TileTextKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key != Key.Enter || args.KeyModifiers != KeyModifiers.None)
            return;
        args.Handled = true;
        TileSendClicked(sender, args);
    }
}
