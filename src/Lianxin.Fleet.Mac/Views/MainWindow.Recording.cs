using System.Globalization;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Mac.Views.Dialogs;

namespace Lianxin.Fleet.Mac.Views;

// 脚本录制：记录主控画面和右侧按钮提交成功的操作，停止后在对话框里核对、修改并保存为中控脚本。
public sealed partial class MainWindow
{
    private const string RecordHint = "录制主控画面和本栏按钮发出的操作；小屏幕上的发送、⌂ 只作用于那一台，不录入。";
    private readonly ScriptRecorder recorder = new();
    private readonly DispatcherTimer recordTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private string? recordingMaster;
    private bool recorded, recordingSaved = true;

    private void InitializeRecording()
    {
        RecordLabel.Text = RecordHint;
        recordTimer.Tick += (_, _) => UpdateRecordLabel();
        DeviceTextInput.AddHandler(KeyDownEvent, TextKeyDown, RoutingStrategies.Tunnel);
        UrlInput.AddHandler(KeyDownEvent, TextKeyDown, RoutingStrategies.Tunnel);
        DeviceList.AddHandler(PointerWheelChangedEvent, WallWheel, RoutingStrategies.Tunnel);
        RefreshScripts();
    }

    // 停止录制不等其他请求结束；核对对话框随后打开，打不开时可点「保存录制」。
    private void RecordClicked(object? sender, RoutedEventArgs args)
    {
        if (recorder.IsRecording)
        {
            StopRecording();
            Run(async () =>
            {
                await WaitForPendingRecordsAsync();
                await ReviewRecordingAsync();
            });
            return;
        }
        Run(StartRecordingAsync);
    }

    private async Task StartRecordingAsync()
    {
        if (!recordingSaved && recorder.Count > 0 && !await DialogKit.ConfirmAsync(this, "重新录制",
            "上次录制的操作还没有保存为脚本，重新录制会丢弃它们。继续吗？", "重新录制"))
        {
            StatusLabel.Text = "";
            return;
        }
        recorder.Start();
        recorded = true;
        recordingSaved = false;
        recordingMaster = master?.Name;
        RecordButton.Content = "■ 停止录制";
        ReviewRecordingButton.IsEnabled = false;
        recordTimer.Start();
        UpdateRecordLabel();
        StatusLabel.Text = "开始录制：在主控画面上点击、拖动，或使用右侧按钮和快捷键，提交成功的操作会写入脚本。";
    }

    private void StopRecording()
    {
        recorder.Stop();
        recordTimer.Stop();
        RecordButton.Content = "● 开始录制";
        ReviewRecordingButton.IsEnabled = true;
        UpdateRecordLabel();
    }

    private void ReviewRecordingClicked(object? sender, RoutedEventArgs args) => Run(ReviewRecordingAsync);

    // 由 RecordWhenDoneAsync 在主控执行成功后调用；at 是用户操作那一刻的时间戳。
    private void Record(FleetAction action, ActionParameters parameters, long at)
    {
        switch (recorder.Record(action, parameters, at))
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

    private async Task ReviewRecordingAsync()
    {
        if (recorder.IsRecording || closing)
            return;
        var comments = new[]
        {
            $"录制于 {Now("yyyy-MM-dd HH:mm")} · 主控 {recordingMaster ?? "未选择"} · {recorder.Count} 个操作 · 用时 {Clock(recorder.Elapsed)}",
            "坐标是 0–1 的相对位置；等待按录制时的操作间隔生成，可按需修改"
        };
        double? limit = IdleLimit.SelectedIndex switch
        {
            1 => 3,
            2 => 10,
            3 => 30,
            _ => null
        };
        var saved = await RecordedScriptDialog.ReviewAsync(this, "录制 " + Now("yyyy-MM-dd HH:mm:ss"), recorder.Render(limit, comments),
            recorder.Count, draft => runtime.Store.SaveScript(null, draft.Name, draft.Source, draft.Repeat));
        if (closing)
            return;
        if (saved is null)
        {
            StatusLabel.Text = recorder.Count == 0 ? "本次没有录到操作。" : "录制尚未保存，可点「保存录制」重新打开。";
            return;
        }
        recordingSaved = true;
        RefreshScripts(saved.Id);
        StatusLabel.Text = $"已保存脚本「{saved.Name}」，可在「运行脚本」或中控设备页运行。";
    }

    private void UpdateRecordLabel()
    {
        if (!recorded || closing)
            return;
        var last = recorder.LastLine is { } line ? " · 最近：" + (line.Length > 24 ? line[..24] + "…" : line) : "";
        RecordLabel.Text = (recorder.IsRecording ? "● 录制中 " : "上次录制 ") + Clock(recorder.Elapsed) + $" · {recorder.Count} 个操作" + last;
    }

    private static string Now(string format) => DateTime.Now.ToString(format, CultureInfo.InvariantCulture);

    private static string Clock(TimeSpan elapsed) => $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";

    private void ScriptPickerOpened(object? sender, EventArgs args) => RefreshScripts();

    private void RefreshScripts(long? select = null)
    {
        var current = select ?? (ScriptPicker.SelectedItem as FleetScript)?.Id;
        var scripts = runtime.Store.GetScripts();
        // 列表没变时不重设，避免打断正在展开的下拉框。
        if (ScriptPicker.ItemsSource is IReadOnlyList<FleetScript> shown && shown.SequenceEqual(scripts) && select is null)
            return;
        ScriptPicker.ItemsSource = scripts;
        ScriptPicker.SelectedItem = scripts.FirstOrDefault(script => script.Id == current) ?? scripts.FirstOrDefault();
    }

    // 脚本在主控（或勾选同步时的全部选中设备）上运行，进度显示在中控设备页的「脚本」「消息」列。
    private void RunScriptClicked(object? sender, RoutedEventArgs args) => Run(() =>
    {
        if (ScriptPicker.SelectedItem is not FleetScript script)
            throw new InvalidOperationException("请先选择脚本；录制保存后的脚本也在这里。");
        if (recorder.IsRecording)
            throw new InvalidOperationException("录制中不能运行脚本，请先停止录制。");
        var ids = TargetIds();
        if (ids.Length == 0)
            throw new InvalidOperationException("请先选择目标设备。");
        var ready = ids.Where(id => cards[id].Status.Online).ToArray();
        if (ready.Length == 0)
            throw new InvalidOperationException("目标设备都未连接。");
        var result = runtime.Scripts.Start(script.Id, ready, "集控投屏");
        StatusLabel.Text = $"脚本「{script.Name}」已在 {result.Started.Count} 台设备启动"
            + (result.Busy.Count > 0 ? $"，{result.Busy.Count} 台正在运行其他脚本" : "")
            + (ids.Length > ready.Length ? $"，跳过 {ids.Length - ready.Length} 台未连接设备" : "") + "。";
        return Task.CompletedTask;
    });

    private void StopScriptClicked(object? sender, RoutedEventArgs args) => Run(() =>
    {
        var ids = TargetIds();
        if (ids.Length == 0)
            throw new InvalidOperationException("请先选择目标设备。");
        StatusLabel.Text = $"已请求停止 {runtime.Scripts.Stop(ids)} 台设备上的脚本。";
        return Task.CompletedTask;
    });

    // 暂停在当前这一步执行完后生效；进度和「已暂停」见中控设备页的「消息」列。
    private void PauseScriptClicked(object? sender, RoutedEventArgs args) => Run(() =>
    {
        var ids = TargetIds();
        if (ids.Length == 0)
            throw new InvalidOperationException("请先选择目标设备。");
        StatusLabel.Text = $"已暂停 {runtime.Scripts.Pause(ids)} 台设备上的脚本，当前这一步执行完后停住。";
        return Task.CompletedTask;
    });

    private void ResumeScriptClicked(object? sender, RoutedEventArgs args) => Run(() =>
    {
        var ids = TargetIds();
        if (ids.Length == 0)
            throw new InvalidOperationException("请先选择目标设备。");
        StatusLabel.Text = $"已继续 {runtime.Scripts.Resume(ids)} 台设备上的脚本。";
        return Task.CompletedTask;
    });

    private readonly List<Task> pendingRecords = [];

    // 录制时：等主控（同步时没有主控就取第一台目标）上这一步执行成功才写入脚本，失败或待确认的不写；时间仍按操作的时刻。
    private void TrackRecording(FleetJob job, long[] ids, FleetAction action, ActionParameters parameters, long stamp)
    {
        if (!recorder.IsRecording)
            return;
        if (!recorder.Accepts(action, parameters))
        {
            StatusLabel.Text += " 这个操作不能写入脚本，未录制。";
            return;
        }
        var device = master is { } current && ids.Contains(current.Id) ? current.Id : ids[0];
        var pending = RecordWhenDoneAsync(job.Id, device, action, parameters, stamp);
        pendingRecords.Add(pending);
        _ = pending.ContinueWith(done => pendingRecords.Remove(done), TaskScheduler.FromCurrentSynchronizationContext());
    }

    private async Task RecordWhenDoneAsync(long jobId, long device, FleetAction action, ActionParameters parameters, long stamp)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(2);
        JobState state;
        try
        {
            while ((state = runtime.Store.GetJob(jobId).Results.FirstOrDefault(target => target.DeviceId == device)?.State
                ?? JobState.Failed) is JobState.Queued or JobState.Running)
            {
                if (DateTime.UtcNow > deadline)
                    break;
                await Task.Delay(150, lifetime.Token);
            }
        }
        catch (Exception error) when (error is OperationCanceledException or ObjectDisposedException) { return; }
        if (closing)
            return;
        if (state == JobState.Succeeded)
            Record(action, parameters, stamp);
        else
            StatusLabel.Text = $"任务 #{jobId} 在主控上{(state == JobState.Unknown ? "结果待确认" : state is JobState.Queued or JobState.Running ? "两分钟内没有完成" : "没有执行成功")}，这一步没有写入脚本。";
    }

    // 停止录制后先等还在执行的操作（最多 10 秒），它们成功了也要写进这次的脚本。
    private async Task WaitForPendingRecordsAsync()
    {
        if (pendingRecords.Count == 0)
            return;
        StatusLabel.Text = $"正在等待最后 {pendingRecords.Count} 个操作执行完…";
        try
        {
            await Task.WhenAll(pendingRecords.ToArray()).WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException) { }
    }

    // 关闭投屏窗口前：有没保存的录制时先问。先核对保存，保存了才关闭；不保存要再确认一次丢弃。
    private async Task<bool> ConfirmCloseRecordingAsync()
    {
        if (recordingSaved || recorder.Count == 0)
            return true;
        if (await DialogKit.ConfirmAsync(this, "关闭集控投屏", $"录制的 {recorder.Count} 个操作还没有保存为脚本，关闭窗口会丢弃它们。先核对并保存吗？", "保存录制"))
        {
            if (recorder.IsRecording)
                StopRecording();
            await ReviewRecordingAsync();
            return recordingSaved;
        }
        return await DialogKit.ConfirmAsync(this, "关闭集控投屏", "不保存录制，直接关闭集控投屏？", "丢弃并关闭");
    }
}
