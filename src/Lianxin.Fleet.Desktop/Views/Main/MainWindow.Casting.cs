using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Lianxin.Fleet.Desktop.ViewModels;

namespace Lianxin.Fleet.Desktop.Views.Main;

public partial class MainWindow
{
    private static readonly TimeSpan SnapshotInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan StreamFrameTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StreamRetryInterval = TimeSpan.FromSeconds(15);
    private readonly SemaphoreSlim _snapshotSlots = new(4);
    private readonly Dictionary<long, PreviewSession> _casts = [];
    private readonly Dictionary<long, Task> _stoppingCasts = [];
    private bool _casting = true;

    private readonly record struct PreviewPolicy(bool Live, bool Usb, bool Wda)
    {
        public static PreviewPolicy From(DeviceCard card, bool live) =>
            new(live && card.CanControl, card.UsbConnected == true, card.CanControl);
    }

    private sealed record PreviewSession(PreviewPolicy Policy, CancellationTokenSource Source, Task Loop);

    private void CastClicked(object sender, RoutedEventArgs e)
    {
        SetCasting(!_casting);
        StatusLabel.Text = _casting ? "已启用实时投屏，USB 快照作为备用。" : "已切换为定时快照。";
    }

    private void SetCasting(bool enabled)
    {
        _casting = enabled;
        CastButton.Content = _casting ? "切换到快照" : "启用实时投屏";
        SyncCasting();
    }

    // Only the UI thread changes the session registry.
    private void SyncCasting()
    {
        if (_closing)
            return;
        var eligible = DeviceCards.Where(card => card.CanPreview).ToDictionary(card => card.Id);
        foreach (var (id, cast) in _casts.ToArray())
        {
            if (eligible.TryGetValue(id, out var card) && cast.Policy == PreviewPolicy.From(card, _casting))
                continue;
            _casts.Remove(id);
            cast.Source.Cancel();
            _stoppingCasts[id] = ReleaseCastAsync(cast);
        }
        foreach (var id in _stoppingCasts.Where(item => item.Value.IsCompletedSuccessfully).Select(item => item.Key).ToArray())
            _stoppingCasts.Remove(id);
        foreach (var (id, card) in eligible)
        {
            if (_casts.ContainsKey(id))
                continue;
            var policy = PreviewPolicy.From(card, _casting);
            var source = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            var predecessor = _stoppingCasts.GetValueOrDefault(id);
            var loop = Task.Run(async () =>
            {
                if (predecessor is not null)
                    await predecessor;
                await CastAsync(card, policy, source.Token);
            });
            _casts[id] = new(policy, source, loop);
        }
    }

    private async Task CastAsync(DeviceCard card, PreviewPolicy policy, CancellationToken token)
    {
        try
        {
            var retryStreamAt = 0L;
            while (!token.IsCancellationRequested)
            {
                if (policy.Live && Environment.TickCount64 >= retryStreamAt)
                {
                    try
                    {
                        await ShowStreamAsync(card, token);
                    }
                    catch (Exception error) when (!token.IsCancellationRequested)
                    {
                        retryStreamAt = Environment.TickCount64 + (long)StreamRetryInterval.TotalMilliseconds;
                        ShowPreviewError(card, "实时画面中断：" + error.Message, token);
                    }
                }

                try
                {
                    var (frame, usb) = await CapturePreviewAsync(card.Id, policy, token);
                    await Dispatcher.InvokeAsync(() => ShowFrame(card, frame, usb, token),
                        DispatcherPriority.Background, token).Task;
                }
                catch (Exception error) when (!token.IsCancellationRequested)
                {
                    ShowPreviewError(card, (policy.Usb ? "USB 快照读取失败：" : "快照读取失败：") + error.Message, token);
                }
                await Task.Delay(SnapshotInterval, token);
            }
        }
        catch (Exception) when (token.IsCancellationRequested) { }
    }

    private async Task ShowStreamAsync(DeviceCard card, CancellationToken token)
    {
        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(token);
        await using var frames = _service.StreamAsync(card.Id, attempt.Token).GetAsyncEnumerator();
        Task? pending = null;
        try
        {
            while (true)
            {
                attempt.CancelAfter(StreamFrameTimeout);
                if (!await frames.MoveNextAsync())
                    throw new IOException("画面通道已结束。");
                attempt.CancelAfter(Timeout.InfiniteTimeSpan);
                // At most one WDA frame waits in the dispatcher for each phone.
                if (pending is { IsCompleted: false })
                    continue;
                var image = Decode(frames.Current);
                pending = Dispatcher.InvokeAsync(() => ShowFrame(card, image, false, attempt.Token),
                    DispatcherPriority.Background, attempt.Token).Task;
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && attempt.IsCancellationRequested)
        {
            throw new TimeoutException("等待实时画面超时。");
        }
        finally
        {
            attempt.Cancel();
            if (pending is not null)
                try { await pending; } catch (OperationCanceledException) when (attempt.IsCancellationRequested) { }
        }
    }

    private async Task<(BitmapImage Image, bool Usb)> CapturePreviewAsync(long id, PreviewPolicy policy, CancellationToken token)
    {
        await _snapshotSlots.WaitAsync(token);
        try
        {
            if (policy.Usb)
            {
                try { return (Decode(await _service.GetUsbScreenshotAsync(id, token)), true); }
                catch (Exception) when (policy.Wda && !token.IsCancellationRequested)
                {
                    // A signed WDA can still provide a frame when the USB screenshot service is unavailable.
                }
            }
            return (Decode(await _service.GetScreenshotAsync(id, token)), false);
        }
        finally { _snapshotSlots.Release(); }
    }

    private void ShowFrame(DeviceCard card, BitmapImage image, bool usb, CancellationToken token)
    {
        if (_closing || token.IsCancellationRequested || !card.CanPreview)
            return;
        card.SetPreviewFrame(image, usb);
        if (_master == card)
            UpdateMaster();
    }

    private void ShowPreviewError(DeviceCard card, string error, CancellationToken token) =>
        Dispatcher.InvokeAsync(() =>
        {
            if (_closing || token.IsCancellationRequested || !card.CanPreview)
                return;
            card.SetPreviewError(error);
            if (_master == card)
                UpdateMaster();
        }, DispatcherPriority.Background, token);

    private static async Task ReleaseCastAsync(PreviewSession cast)
    {
        try { await cast.Loop; }
        finally { cast.Source.Dispose(); }
    }

    private async Task StopCastingAsync()
    {
        var casts = _casts.Values.ToArray();
        _casts.Clear();
        foreach (var cast in casts)
            cast.Source.Cancel();
        try { await Task.WhenAll(casts.Select(ReleaseCastAsync).Concat(_stoppingCasts.Values)); }
        finally { _snapshotSlots.Dispose(); }
    }

    private static BitmapImage Decode(byte[] data)
    {
        var image = new BitmapImage();
        using (var stream = new MemoryStream(data))
        {
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
        }
        image.Freeze();
        return image;
    }
}
