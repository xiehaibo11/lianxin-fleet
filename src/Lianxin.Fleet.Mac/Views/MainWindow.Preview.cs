using System.Diagnostics;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;
using Lianxin.Fleet.Mac.ViewModels;

namespace Lianxin.Fleet.Mac.Views;

public sealed partial class MainWindow
{
    private (ScreenPoint Point, long DeviceId, long Started)? gesture;

    private void BeginRefresh()
    {
        if (!closing && refresh.IsCompleted)
            refresh = RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            var snapshot = await service.RefreshAsync(lifetime.Token);
            if (closing)
                return;
            ApplySnapshot(snapshot);
            // 投屏时画面来自实时流；停止投屏后才退回 3 秒快照。
            if (casting)
                return;
            var candidates = (master is null ? Array.Empty<DeviceCard>() : [master])
                .Concat(visible).Where(card => card.Status.Online).Distinct().Take(12).ToArray();
            foreach (var card in cards.Values.Except(candidates))
                card.SetPreview(null);
            UpdateMaster();
            using var slots = new SemaphoreSlim(4);
            await Task.WhenAll(candidates.Select(card => ReadPreviewAsync(card, slots)));
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (!closing)
                StatusLabel.Text = "刷新失败：" + error.Message;
        }
    }

    private async Task ReadPreviewAsync(DeviceCard card, SemaphoreSlim slots)
    {
        await slots.WaitAsync(lifetime.Token);
        try
        {
            var data = await service.GetScreenshotAsync(card.Id, lifetime.Token);
            if (closing || (gesture is not null && card == master))
                return;
            using var stream = new MemoryStream(data);
            card.SetPreview(new Bitmap(stream));
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception error)
        {
            if (!closing)
                card.SetPreview(null, "屏幕读取失败：" + error.Message);
        }
        finally
        {
            slots.Release();
            if (!closing && card == master)
                UpdateMaster();
        }
    }

    private void UpdateMaster()
    {
        if (gesture is not null)
            return;
        MasterTitle.Text = master?.Name ?? "选择一台主控设备";
        MasterDetails.Text = master is null ? "" : $"设备名：{master.Status.DeviceName} · 端口 {master.Device.WdaPort}\n{master.Status.Error}";
        MasterImage.Source = master?.Preview;
        MasterPlaceholder.IsVisible = master?.Preview is null;
        MasterPlaceholder.Text = master?.PreviewError ?? "等待设备屏幕";
        MasterSurface.IsEnabled = CanUseScreen();
    }

    private bool CanUseScreen() => master?.Status.Online == true && master.Preview is not null;

    private ScreenPoint? MapPoint(PointerEventArgs args, bool clamp = false)
    {
        if (master is null)
            return null;
        var point = args.GetPosition(MasterSurface);
        var image = MasterImage.Source as Bitmap;
        var bounds = MasterSurface.Bounds;
        return ScreenCoordinates.Map(point.X, point.Y, bounds.Width, bounds.Height,
            image?.PixelSize.Width ?? master.Status.Width, image?.PixelSize.Height ?? master.Status.Height, clamp);
    }

    private void ScreenPressed(object? sender, PointerPressedEventArgs args)
    {
        if (closing || !operation.IsCompleted || !CanUseScreen() ||
            !args.GetCurrentPoint(MasterSurface).Properties.IsLeftButtonPressed || MapPoint(args) is not { } point)
            return;
        gesture = (point, master!.Id, Stopwatch.GetTimestamp());
        MasterSurface.Focus();
        args.Pointer.Capture(MasterSurface);
        args.Handled = true;
    }

    private void ScreenReleased(object? sender, PointerReleasedEventArgs args)
    {
        if (gesture is not { } start)
            return;
        var end = MapPoint(args, clamp: true);
        gesture = null;
        args.Pointer.Capture(null);
        UpdateMaster();
        if (end is not { } point || master?.Id != start.DeviceId || !CanUseScreen())
            return;
        args.Handled = true;
        var distance = Math.Sqrt(Math.Pow(point.X - start.Point.X, 2) + Math.Pow(point.Y - start.Point.Y, 2));
        var duration = Math.Clamp(Stopwatch.GetElapsedTime(start.Started).TotalSeconds, .15, 2);
        Run(() => distance < .025
            ? TapAsync(start.Point)
            : QueueAsync(FleetAction.Swipe, new ActionParameters(FromX: start.Point.X, FromY: start.Point.Y,
                ToX: point.X, ToY: point.Y, Duration: duration)));
    }

    // 点击方式：0 单击、1 双击、2 长按 1 秒。
    private Task TapAsync(ScreenPoint point) => TapMode.SelectedIndex switch
    {
        1 => QueueAsync(FleetAction.DoubleTap, new ActionParameters(X: point.X, Y: point.Y)),
        2 => QueueAsync(FleetAction.LongPress, new ActionParameters(X: point.X, Y: point.Y, Duration: 1)),
        _ => QueueAsync(FleetAction.Tap, new ActionParameters(X: point.X, Y: point.Y))
    };

    private void ScreenCaptureLost(object? sender, PointerCaptureLostEventArgs args)
    {
        gesture = null;
        if (!closing)
            UpdateMaster();
    }

    private void ScreenKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key is not (Key.Enter or Key.Space) || !CanUseScreen())
            return;
        args.Handled = true;
        Run(() => TapAsync(new ScreenPoint(.5, .5)));
    }
}
