using System.Collections.Concurrent;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Lianxin.Fleet.Mac.ViewModels;

namespace Lianxin.Fleet.Mac.Views;

// 实时投屏：每台在线设备一个读流循环，在后台线程解码，只把最新一帧交给界面线程。
public sealed partial class MainWindow
{
    private readonly Dictionary<long, CancellationTokenSource> casts = [];
    private readonly List<Task> castLoops = [];
    private readonly ConcurrentDictionary<long, byte> framesInFlight = new();
    private bool casting = true;

    private void CastClicked(object? sender, RoutedEventArgs args)
    {
        casting = !casting;
        CastButton.Content = casting ? "停止投屏" : "开始投屏";
        StatusLabel.Text = casting ? "已开始实时投屏。" : "已停止投屏，改为每 3 秒刷新快照。";
        SyncCasting();
        BeginRefresh();
    }

    // 在界面线程调用：按在线状态和投屏开关启动或取消每台设备的读流循环。
    private void SyncCasting()
    {
        castLoops.RemoveAll(loop => loop.IsCompleted);
        foreach (var card in cards.Values)
        {
            var wanted = casting && !closing && card.Status.Online;
            if (wanted && !casts.ContainsKey(card.Id))
            {
                var stop = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                casts[card.Id] = stop;
                castLoops.Add(Task.Run(() => CastAsync(card, stop.Token)));
            }
            else if (!wanted && casts.Remove(card.Id, out var stop))
            {
                stop.Cancel();
                stop.Dispose();
            }
        }
    }

    private async Task CastAsync(DeviceCard card, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await foreach (var frame in service.StreamAsync(card.Id, token).ConfigureAwait(false))
                {
                    // 上一帧还没画到界面就丢弃这一帧，不积压。
                    if (!framesInFlight.TryAdd(card.Id, 0))
                        continue;
                    Bitmap bitmap;
                    try
                    {
                        using var stream = new MemoryStream(frame);
                        bitmap = new Bitmap(stream);
                    }
                    catch
                    {
                        framesInFlight.TryRemove(card.Id, out _);
                        throw;
                    }
                    Dispatcher.UIThread.Post(() => ShowFrame(card, bitmap));
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception error)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (closing || !casts.ContainsKey(card.Id))
                        return;
                    card.SetPreview(null, "画面读取失败：" + error.Message);
                    if (card == master)
                        UpdateMaster();
                });
            }
            try
            {
                await Task.Delay(3000, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private void ShowFrame(DeviceCard card, Bitmap bitmap)
    {
        framesInFlight.TryRemove(card.Id, out _);
        // 拖动主控画面时不替换主控图片，避免手势中途画面跳动。
        if (closing || !casts.ContainsKey(card.Id) || (gesture is not null && card == master))
        {
            bitmap.Dispose();
            return;
        }
        card.SetPreview(bitmap);
        if (card == master)
            UpdateMaster();
    }

    private async Task StopCastingAsync()
    {
        foreach (var stop in casts.Values)
            stop.Cancel();
        await Task.WhenAll(castLoops);
        foreach (var stop in casts.Values)
            stop.Dispose();
        casts.Clear();
    }
}
