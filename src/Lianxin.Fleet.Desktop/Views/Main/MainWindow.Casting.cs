using System.IO;
using System.Windows.Media.Imaging;
using System.Windows;
using Lianxin.Fleet.Desktop.ViewModels;

namespace Lianxin.Fleet.Desktop.Views.Main;

public partial class MainWindow
{
    // 仅在 UI 线程读写；每台在线设备一个画面循环。
    private readonly Dictionary<long, (CancellationTokenSource Source, Task Loop)> _casts = [];
    private bool _casting = true;

    private void CastClicked(object sender, RoutedEventArgs e)
    {
        _casting = !_casting;
        CastButton.Content = _casting ? "停止投屏" : "开始投屏";
        SyncCasting();
        StatusLabel.Text = _casting ? "已开始投屏，在线设备显示实时画面。" : "已停止投屏，改为每 3 秒刷新快照。";
    }

    // 每次快照更新后调用：在线设备启动画面循环，离线、停止投屏或关闭时取消。
    private void SyncCasting()
    {
        foreach (var (id, cast) in _casts.ToArray())
        {
            var card = DeviceCards.FirstOrDefault(item => item.Id == id);
            if (_casting && !_closing && card?.Status.Online == true && !cast.Loop.IsCompleted)
                continue;
            cast.Source.Cancel();
            _casts.Remove(id);
            _ = cast.Loop.ContinueWith(_ => cast.Source.Dispose(), TaskScheduler.Default);
        }
        if (!_casting || _closing)
            return;
        foreach (var card in DeviceCards.Where(card => card.Status.Online && !_casts.ContainsKey(card.Id)))
        {
            var source = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            _casts[card.Id] = (source, Task.Run(() => CastAsync(card, source.Token)));
        }
    }

    // 在线程池运行：解码不占用 UI 线程，只把冻结后的图片交给 UI 线程显示。
    private async Task CastAsync(DeviceCard card, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Task? pending = null;
                    await foreach (var frame in _service.StreamAsync(card.Id, token))
                    {
                        // 上一帧尚未显示就丢弃本帧，避免几十台手机的画面在 UI 线程堆积。
                        if (pending is { IsCompleted: false })
                            continue;
                        var image = Decode(frame);
                        pending = Dispatcher.InvokeAsync(() => ShowFrame(card, image, null)).Task;
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { return; }
                catch (Exception error)
                {
                    _ = Dispatcher.InvokeAsync(() => ShowFrame(card, null, "画面中断：" + error.Message));
                }
                await Task.Delay(3000, token);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void ShowFrame(DeviceCard card, BitmapImage? image, string? error)
    {
        if (_closing || !_casting || !card.Status.Online)
            return;
        card.Preview = image;
        card.SetPreviewError(error);
        if (_master == card)
            UpdateMaster();
    }

    private async Task StopCastingAsync()
    {
        var casts = _casts.Values.ToArray();
        _casts.Clear();
        foreach (var cast in casts)
            cast.Source.Cancel();
        await Task.WhenAll(casts.Select(cast => cast.Loop));
        foreach (var cast in casts)
            cast.Source.Dispose();
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
