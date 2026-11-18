using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;
using Lianxin.Fleet.Desktop.ViewModels;

namespace Lianxin.Fleet.Desktop.Views.Main;

public partial class MainWindow
{
    private async Task RefreshAsync()
    {
        if (_refreshing || _closing)
            return;
        _refreshing = true;
        try
        {
            ApplySnapshot(await _service.RefreshAsync(_lifetime.Token));
            await RefreshScreensAsync();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
        catch (Exception error) { if (!_closing) StatusLabel.Text = "工作站更新失败，当前画面可能过期：" + error.Message; }
        finally { _refreshing = false; }
    }

    private void ApplySnapshot(FleetSnapshot snapshot)
    {
        foreach (var current in snapshot.Devices)
        {
            var card = DeviceCards.FirstOrDefault(item => item.Id == current.Device.Id);
            if (card is null)
            {
                card = new DeviceCard(current, SetMaster, QuickAction);
                card.PropertyChanged += CardChanged;
                DeviceCards.Add(card);
            }
            else
                card.Update(current);
        }
        foreach (var card in DeviceCards.Where(card => snapshot.Devices.All(item => item.Device.Id != card.Id)).ToArray())
        {
            card.PropertyChanged -= CardChanged;
            DeviceCards.Remove(card);
        }
        var groups = snapshot.Devices.Select(item => item.Device.GroupName).Distinct().Order().ToArray();
        foreach (var group in groups)
            if (!Groups.Contains(group))
                Groups.Add(group);
        foreach (var group in Groups.Where(group => group != "所有分组" && !groups.Contains(group)).ToArray())
            Groups.Remove(group);
        if (GroupFilter.SelectedItem is null)
            GroupFilter.SelectedIndex = 0;
        var jobs = snapshot.Jobs.Select(JobCard.From).ToArray();
        for (var index = 0; index < jobs.Length; index++)
        {
            if (index >= Jobs.Count)
                Jobs.Add(jobs[index]);
            else if (Jobs[index] != jobs[index])
                Jobs[index] = jobs[index];
        }
        while (Jobs.Count > jobs.Length)
            Jobs.RemoveAt(Jobs.Count - 1);
        if (_master is null || !DeviceCards.Contains(_master))
            _master = DeviceCards.FirstOrDefault(card => card.Status.Online) ?? DeviceCards.FirstOrDefault();
        EmptyLabel.Visibility = DeviceCards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        DeviceView.Refresh();
        UpdateMaster();
        UpdateSelection();
        StatusLabel.Text = _casting ? "工作站已更新 · 实时投屏中" : "工作站已更新 · 屏幕快照每 3 秒刷新";
        SyncCasting();
    }

    // 投屏关闭时的后备预览：每 3 秒快照，最多 12 台。
    private async Task RefreshScreensAsync()
    {
        if (_closing || _casting)
            return;
        var candidates = (_master is null ? Array.Empty<DeviceCard>() : [_master])
            .Concat(DeviceView.Cast<DeviceCard>()).Where(card => card.Status.Online).Distinct().Take(12).ToArray();
        foreach (var card in DeviceCards.Except(candidates))
            card.Preview = null;
        using var slots = new SemaphoreSlim(4);
        await Task.WhenAll(candidates.Select(async card =>
        {
            await slots.WaitAsync(_lifetime.Token);
            try
            {
                var data = await _service.GetScreenshotAsync(card.Id, _lifetime.Token);
                if (_closing || _casting)
                    return;
                card.Preview = Decode(data);
                card.SetPreviewError(null);
                if (_master == card)
                    UpdateMaster();
            }
            catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { }
            catch (Exception error)
            {
                if (!_closing)
                {
                    card.Preview = null;
                    card.SetPreviewError("屏幕读取失败：" + error.Message);
                    if (_master == card)
                        UpdateMaster();
                }
            }
            finally { slots.Release(); }
        }));
    }
    private void SetMaster(DeviceCard card)
    {
        _master = card;
        UpdateMaster();
        UpdateSelection();
    }
    private void UpdateMaster()
    {
        if (_gestureStart is not null)
            return;
        MasterLabel.Text = _master?.Name ?? "选择一台主控设备";
        MasterDetails.Text = _master is null ? "" : $"设备实际名：{_master.Status.DeviceName} · 端口 {_master.Device.WdaPort}\n{_master.Error}";
        MasterImage.Source = _master?.Preview;
        MasterPlaceholder.Text = _master?.Placeholder ?? "设备屏幕尚未就绪";
        MasterPlaceholder.Visibility = _master?.Preview is null ? Visibility.Visible : Visibility.Collapsed;
        MasterSurface.IsEnabled = _master?.Status.Online == true;
    }

    private ScreenPoint? _gestureStart;
    private long _gestureDevice;
    private long _gestureStarted;
    private ScreenPoint? MapPoint(MouseEventArgs e, bool clamp = false)
    {
        if (_master is null)
            return null;
        var point = e.GetPosition(MasterSurface);
        var image = MasterImage.Source as BitmapSource;
        return ScreenCoordinates.Map(point.X, point.Y, MasterSurface.ActualWidth, MasterSurface.ActualHeight,
            image?.PixelWidth ?? _master.Status.Width, image?.PixelHeight ?? _master.Status.Height, clamp);
    }
    private void ScreenDown(object sender, MouseButtonEventArgs e)
    {
        if (_busy || _closing || _master is null || !_master.Status.Online)
            return;
        if (_master.Preview is null)
        {
            StatusLabel.Text = "等待真实屏幕快照后再操作。";
            return;
        }
        _gestureStart = MapPoint(e);
        if (_gestureStart is null)
            return;
        _gestureDevice = _master.Id;
        _gestureStarted = Stopwatch.GetTimestamp();
        MasterSurface.Focus();
        MasterSurface.CaptureMouse();
        e.Handled = true;
    }
    private async void ScreenUp(object sender, MouseButtonEventArgs e)
    {
        var start = _gestureStart;
        var device = _gestureDevice;
        var end = MapPoint(e, true);
        var duration = Math.Clamp(Stopwatch.GetElapsedTime(_gestureStarted).TotalSeconds, .15, 2);
        _gestureStart = null;
        MasterSurface.ReleaseMouseCapture();
        if (start is null || end is null || _master?.Id != device)
            return;
        e.Handled = true;
        await RunAsync(() => Math.Sqrt(Math.Pow(end.Value.X - start.Value.X, 2) + Math.Pow(end.Value.Y - start.Value.Y, 2)) < .025
            ? QueuePointAsync(start.Value)
            : QueueAsync(FleetAction.Swipe, new(FromX: start.Value.X, FromY: start.Value.Y,
                ToX: end.Value.X, ToY: end.Value.Y, Duration: duration)));
    }
    // 点击方式：单击 / 双击 / 长按 1 秒。
    private Task QueuePointAsync(ScreenPoint point) => ClickMode.SelectedIndex switch
    {
        1 => QueueAsync(FleetAction.DoubleTap, new(X: point.X, Y: point.Y)),
        2 => QueueAsync(FleetAction.LongPress, new(X: point.X, Y: point.Y, Duration: 1)),
        _ => QueueAsync(FleetAction.Tap, new(X: point.X, Y: point.Y))
    };
    private void ScreenLostCapture(object sender, MouseEventArgs e)
    {
        _gestureStart = null;
        if (!_closing)
            UpdateMaster();
    }
    private async void ScreenKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter && e.Key != Key.Space)
            return;
        if (_master?.Preview is null)
        {
            StatusLabel.Text = "等待真实屏幕快照后再操作。";
            return;
        }
        e.Handled = true;
        await RunAsync(() => QueuePointAsync(new(.5, .5)));
    }
}
