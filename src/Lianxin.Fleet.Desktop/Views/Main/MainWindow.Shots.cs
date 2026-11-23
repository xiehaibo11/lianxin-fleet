using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Lianxin.Fleet.Desktop.Interaction;
using Lianxin.Fleet.Desktop.ViewModels;
using Microsoft.Win32;

namespace Lianxin.Fleet.Desktop.Views.Main;

// 截图保存、小屏幕缩放和文字框快捷键。
public partial class MainWindow
{
    private const double MinZoom = 0.6, MaxZoom = 2;
    private double _zoom = 1;
    private string? _shotFolder;

    private async void SaveMasterShotClicked(object sender, RoutedEventArgs e) =>
        await RunAsync(() => SaveShotsAsync(_master is null ? [] : [_master]));

    private async void SaveSelectedShotsClicked(object sender, RoutedEventArgs e) =>
        await RunAsync(() => SaveShotsAsync(DeviceCards.Where(card => card.IsSelected).ToArray()));

    private async void TileScreenshot(DeviceCard card) => await RunAsync(() => SaveShotsAsync([card]));

    // 优先读取设备的原始截图（USB，其次 WDA）；都失败时保存界面上正在显示的画面。
    private async Task SaveShotsAsync(IReadOnlyList<DeviceCard> cards)
    {
        if (cards.Count == 0)
            throw new InvalidOperationException("请先选择主控设备，或勾选要截图的设备。");
        var targets = cards.Where(card => card.CanPreview || card.Preview is not null).ToArray();
        if (targets.Length == 0)
            throw new InvalidOperationException("选中的设备都没有画面，无法截图。");
        var picker = new OpenFolderDialog
        {
            Title = $"选择截图保存位置（{targets.Length} 台设备）",
            InitialDirectory = _shotFolder ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };
        if (picker.ShowDialog(this) != true || _closing)
        {
            StatusLabel.Text = "已取消保存截图。";
            return;
        }
        _shotFolder = picker.FolderName;
        StatusLabel.Text = $"正在保存 {targets.Length} 台设备的截图…";
        var at = DateTime.Now;
        using var slots = new SemaphoreSlim(4);
        var results = await Task.WhenAll(targets.Select(card => SaveShotAsync(card, _shotFolder, at, slots)));
        var failures = results.Where(result => result.Error is not null).Select(result => result.Error).ToArray();
        var frames = results.Count(result => result.Error is null && result.FromFrame);
        StatusLabel.Text = $"已保存 {targets.Length - failures.Length}/{targets.Length} 张截图到 {_shotFolder}"
            + (frames > 0 ? $"（{frames} 张取不到原始截图，保存的是约 30% 尺寸的预览画面，文件名带 _preview，不能用来截取找图模板）" : "") + (failures.Length > 0 ? "。失败：" + string.Join("；", failures) : "。");
        if (cards.Count > targets.Length)
            StatusLabel.Text += $" 已跳过 {cards.Count - targets.Length} 台没有画面的设备。";
    }

    private async Task<(string? Error, bool FromFrame)> SaveShotAsync(DeviceCard card, string folder, DateTime at, SemaphoreSlim slots)
    {
        await slots.WaitAsync(_lifetime.Token);
        try
        {
            byte[] png;
            var fromFrame = false;
            try
            {
                png = ScreenshotFiles.ToPng(await CaptureShotAsync(card));
            }
            catch (Exception) when (!_lifetime.IsCancellationRequested && card.Preview is BitmapSource frame)
            {
                png = ScreenshotFiles.ToPng(frame);
                fromFrame = true;
            }
            // 预览画面只有约 30% 尺寸，文件名带 _preview，免得被当成原始截图拿去截模板。
            await ScreenshotFiles.WriteAsync(folder, card.Name, at, png, _lifetime.Token, fromFrame ? "_preview" : "");
            return (null, fromFrame);
        }
        catch (Exception error) when (!_lifetime.IsCancellationRequested)
        {
            return ($"{card.Name}：{error.Message}", false);
        }
        finally { slots.Release(); }
    }

    private async Task<byte[]> CaptureShotAsync(DeviceCard card)
    {
        if (card.UsbConnected == true)
        {
            try
            {
                return await _service.GetUsbScreenshotAsync(card.Id, _lifetime.Token);
            }
            catch (Exception) when (card.CanControl && !_lifetime.IsCancellationRequested)
            {
                // USB 截图服务不可用时，已就绪的 WDA 仍能截图。
            }
        }
        return await _service.GetScreenshotAsync(card.Id, _lifetime.Token);
    }

    // Ctrl+滚轮缩放设备墙上的小屏幕。
    private void WallWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control)
            return;
        e.Handled = true;
        var zoom = Math.Clamp(Math.Round(_zoom + (e.Delta > 0 ? 0.1 : -0.1), 1), MinZoom, MaxZoom);
        if (zoom == _zoom)
            return;
        _zoom = zoom;
        foreach (var card in DeviceCards)
            card.SetZoom(zoom);
        StatusLabel.Text = $"小屏幕缩放 {zoom:P0}";
    }

    // 多行文字框里回车换行，Ctrl+回车输入到目标设备。
    private async void PhoneTextKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.Control)
            return;
        e.Handled = true;
        await RunAsync(() => QueueCommandAsync("Text"));
    }

    private async void UrlKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None)
            return;
        e.Handled = true;
        await RunAsync(() => QueueCommandAsync("OpenUrl"));
    }
}
