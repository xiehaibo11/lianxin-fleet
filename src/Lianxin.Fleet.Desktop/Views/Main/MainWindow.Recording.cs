using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Desktop.Views.Dialogs;

namespace Lianxin.Fleet.Desktop.Views.Main;

// 脚本录制：记录主控画面和右侧按钮提交成功的操作，停止后在对话框里核对、修改并保存为中控脚本。
public partial class MainWindow
{
    private const string RecordHint = "录制主控画面和本栏按钮发出的操作；小屏幕上的发送、Home 只作用于那一台，不录入。";
    private readonly ScriptRecorder _recorder = new();
    private readonly DispatcherTimer _recordTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private string? _recordingMaster;
    private bool _recorded, _recordingSaved = true;

    private void InitializeRecording()
    {
        RecordLabel.Text = RecordHint;
        _recordTimer.Tick += (_, _) => UpdateRecordLabel();
        RefreshScripts();
    }

    private async void RecordClicked(object sender, RoutedEventArgs e)
    {
        if (_recorder.IsRecording)
        {
            StopRecording();
            await WaitForPendingRecordsAsync();
            ReviewRecording();
            return;
        }
        if (!_recordingSaved && _recorder.Count > 0 && MessageBox.Show(this, "上次录制的操作还没有保存为脚本，重新录制会丢弃它们。继续吗？",
            "联信工作站", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        _recorder.Start();
        _recorded = true;
        _recordingSaved = false;
        _recordingMaster = _master?.Name;
        RecordButton.Content = "■ 停止录制";
        ReviewRecordingButton.IsEnabled = false;
        _recordTimer.Start();
        UpdateRecordLabel();
        StatusLabel.Text = "开始录制：在主控画面上点击、拖动，或使用右侧按钮和快捷键，提交成功的操作会写入脚本。";
    }

    private void StopRecording()
    {
        _recorder.Stop();
        _recordTimer.Stop();
        RecordButton.Content = "● 开始录制";
        ReviewRecordingButton.IsEnabled = true;
        UpdateRecordLabel();
    }

    private void ReviewRecordingClicked(object sender, RoutedEventArgs e) => ReviewRecording();

    // 由 RecordWhenDoneAsync 在主控执行成功后调用；at 是用户操作那一刻的时间戳。
    private void Record(FleetAction action, ActionParameters parameters, long at)
    {
        switch (_recorder.Record(action, parameters, at))
        {
            case RecordOutcome.Recorded:
                UpdateRecordLabel();
                break;
            case RecordOutcome.Unsupported:
                StatusLabel.Text += " 这个操作不能写入脚本，未录制。";
                break;
            case RecordOutcome.Full:
                StopRecording();
                StatusLabel.Text = "脚本已达到步数或长度上限，录制已自动停止，最后这个操作没有录入。请点「保存录制」保存。";
                break;
        }
    }

    private void ReviewRecording()
    {
        if (_recorder.IsRecording || _closing)
            return;
        var elapsed = _recorder.Elapsed;
        var comments = new[]
        {
            $"录制于 {Now("yyyy-MM-dd HH:mm")} · 主控 {_recordingMaster ?? "未选择"} · {_recorder.Count} 个操作 · 用时 {Clock(elapsed)}",
            "坐标是 0–1 的相对位置；等待按录制时的操作间隔生成，可按需修改"
        };
        double? limit = IdleLimit.SelectedIndex switch
        {
            1 => 3,
            2 => 10,
            3 => 30,
            _ => null
        };
        var saved = RecordedScriptDialog.Review(this, "录制 " + Now("yyyy-MM-dd HH:mm:ss"), _recorder.Render(limit, comments),
            _recorder.Count, draft => _runtime.Store.SaveScript(null, draft.Name, draft.Source, draft.Repeat));
        if (saved is null)
        {
            StatusLabel.Text = _recorder.Count == 0 ? "本次没有录到操作。" : "录制尚未保存，可点「保存录制」重新打开。";
            return;
        }
        _recordingSaved = true;
        RefreshScripts(saved.Id);
        StatusLabel.Text = $"已保存脚本「{saved.Name}」，可在「运行脚本」或中控设备页运行。";
    }

    private void UpdateRecordLabel()
    {
        if (!_recorded)
            return;
        var last = _recorder.LastLine is { } line ? " · 最近：" + (line.Length > 28 ? line[..28] + "…" : line) : "";
        RecordLabel.Text = (_recorder.IsRecording ? "● 录制中 " : "上次录制 ") + Clock(_recorder.Elapsed)
            + $" · {_recorder.Count} 个操作" + last;
    }

    private static string Now(string format) => DateTime.Now.ToString(format, CultureInfo.InvariantCulture);

    private static string Clock(TimeSpan elapsed) => $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";

    private void ScriptPickerOpened(object? sender, EventArgs e) => RefreshScripts();

    private void RefreshScripts(long? select = null)
    {
        var current = select ?? (ScriptPicker.SelectedItem as FleetScript)?.Id;
        var scripts = _runtime.Store.GetScripts();
        // 列表没变时不重设，避免打断正在展开的下拉框。
        if (ScriptPicker.ItemsSource is IReadOnlyList<FleetScript> shown && shown.SequenceEqual(scripts) && select is null)
            return;
        ScriptPicker.ItemsSource = scripts;
        ScriptPicker.SelectedItem = scripts.FirstOrDefault(script => script.Id == current) ?? scripts.FirstOrDefault();
    }

    // 脚本在主控（或勾选同步时的全部选中设备）上运行，进度显示在中控设备页的「脚本」「消息」列。
    private async void RunScriptClicked(object sender, RoutedEventArgs e) => await RunAsync(() =>
    {
        if (ScriptPicker.SelectedItem is not FleetScript script)
            throw new InvalidOperationException("请先选择脚本；录制保存后的脚本也在这里。");
        if (_recorder.IsRecording)
            throw new InvalidOperationException("录制中不能运行脚本，请先停止录制。");
        var ids = TargetIds();
        if (ids.Count == 0)
            throw new InvalidOperationException("请先选择目标设备。");
        var ready = ids.Where(id => DeviceCards.Any(card => card.Id == id && card.CanControl)).ToArray();
        if (ready.Length == 0)
            throw new InvalidOperationException("目标设备的 WDA 控制尚未就绪。");
        var result = _runtime.Scripts.Start(script.Id, ready, "集控投屏");
        StatusLabel.Text = $"脚本「{script.Name}」已在 {result.Started.Count} 台设备启动"
            + (result.Busy.Count > 0 ? $"，{result.Busy.Count} 台正在运行其他脚本" : "")
            + (ids.Count > ready.Length ? $"，跳过 {ids.Count - ready.Length} 台控制未就绪的设备" : "") + "。";
        return Task.CompletedTask;
    });

    private async void StopScriptClicked(object sender, RoutedEventArgs e) => await RunAsync(() =>
    {
        var ids = TargetIds();
        if (ids.Count == 0)
            throw new InvalidOperationException("请先选择目标设备。");
        StatusLabel.Text = $"已请求停止 {_runtime.Scripts.Stop(ids)} 台设备上的脚本。";
        return Task.CompletedTask;
    });

    // 暂停在当前这一步执行完后生效；进度和「已暂停」见中控设备页的「消息」列。
    private async void PauseScriptClicked(object sender, RoutedEventArgs e) => await RunAsync(() =>
    {
        var ids = TargetIds();
        if (ids.Count == 0)
            throw new InvalidOperationException("请先选择目标设备。");
        StatusLabel.Text = $"已暂停 {_runtime.Scripts.Pause(ids)} 台设备上的脚本，当前这一步执行完后停住。";
        return Task.CompletedTask;
    });

    private async void ResumeScriptClicked(object sender, RoutedEventArgs e) => await RunAsync(() =>
    {
        var ids = TargetIds();
        if (ids.Count == 0)
            throw new InvalidOperationException("请先选择目标设备。");
        StatusLabel.Text = $"已继续 {_runtime.Scripts.Resume(ids)} 台设备上的脚本。";
        return Task.CompletedTask;
    });

    private readonly List<Task> _pendingRecords = [];

    // 录制时：等主控（同步时没有主控就取第一台目标）上这一步执行成功才写入脚本，失败或待确认的不写；时间仍按操作的时刻。
    private void TrackRecording(FleetJob job, IReadOnlyList<long> ids, FleetAction action, ActionParameters parameters, long stamp)
    {
        if (!_recorder.IsRecording)
            return;
        if (!_recorder.Accepts(action, parameters))
        {
            StatusLabel.Text += " 这个操作不能写入脚本，未录制。";
            return;
        }
        var device = _master is { } master && ids.Contains(master.Id) ? master.Id : ids[0];
        var pending = RecordWhenDoneAsync(job.Id, device, action, parameters, stamp);
        _pendingRecords.Add(pending);
        _ = pending.ContinueWith(done => _pendingRecords.Remove(done), TaskScheduler.FromCurrentSynchronizationContext());
    }

    private async Task RecordWhenDoneAsync(long jobId, long device, FleetAction action, ActionParameters parameters, long stamp)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(2);
        JobState state;
        try
        {
            while ((state = _runtime.Store.GetJob(jobId).Results.FirstOrDefault(target => target.DeviceId == device)?.State
                ?? JobState.Failed) is JobState.Queued or JobState.Running)
            {
                if (DateTime.UtcNow > deadline)
                    break;
                await Task.Delay(150, _lifetime.Token);
            }
        }
        catch (Exception error) when (error is OperationCanceledException or ObjectDisposedException) { return; }
        if (_closing)
            return;
        if (state == JobState.Succeeded)
            Record(action, parameters, stamp);
        else
            StatusLabel.Text = $"任务 #{jobId} 在主控上{(state == JobState.Unknown ? "结果待确认" : state is JobState.Queued or JobState.Running ? "两分钟内没有完成" : "没有执行成功")}，这一步没有写入脚本。";
    }

    // 停止录制后先等还在执行的操作（最多 10 秒），它们成功了也要写进这次的脚本。
    private async Task WaitForPendingRecordsAsync()
    {
        if (_pendingRecords.Count == 0)
            return;
        StatusLabel.Text = $"正在等待最后 {_pendingRecords.Count} 个操作执行完…";
        try
        {
            await Task.WhenAll(_pendingRecords.ToArray()).WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException) { }
    }

    // 关闭投屏窗口前：有没保存的录制时先问。选「是」打开核对窗口，保存了才关闭；选「否」丢弃并关闭。
    private bool ConfirmCloseRecording()
    {
        if (_recordingSaved || _recorder.Count == 0)
            return true;
        var answer = MessageBox.Show(this, $"录制的 {_recorder.Count} 个操作还没有保存为脚本，关闭窗口会丢弃它们。\n\n是：先核对并保存\n否：不保存，直接关闭\n取消：回到投屏窗口",
            "联信工作站", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
        if (answer == MessageBoxResult.No)
            return true;
        if (answer != MessageBoxResult.Yes)
            return false;
        if (_recorder.IsRecording)
            StopRecording();
        ReviewRecording();
        return _recordingSaved;
    }
}
