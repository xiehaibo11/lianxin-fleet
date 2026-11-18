using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Mac.ViewModels;

public sealed record JobCard(long Id, string Summary, string Details)
{
    public static JobCard From(FleetJob job) => new(job.Id,
        $"#{job.Id}  {Action(job.Action)} · {State(job.State)} · {job.Results.Count(result => result.State == JobState.Succeeded)}/{job.Results.Count} 台 · {job.CreatedAt.LocalDateTime:HH:mm:ss}",
        (job.Results.Any(result => result.State == JobState.Unknown) ? "结果可能已执行，请核对设备后再决定是否重发。\n" : "") +
        string.Join("\n", job.Results.Select(result => $"{result.Name}  {State(result.State)}  {result.Error}")));

    private static string Action(FleetAction action) => action switch
    {
        FleetAction.Tap => "屏幕点击",
        FleetAction.Swipe => "屏幕滑动",
        FleetAction.Home => "返回主屏幕",
        FleetAction.Text => "文本输入",
        FleetAction.Launch => "启动应用",
        FleetAction.Install => "安装应用",
        FleetAction.DoubleTap => "双击",
        FleetAction.LongPress => "长按",
        FleetAction.Lock => "锁屏",
        FleetAction.Unlock => "解锁",
        FleetAction.VolumeUp => "音量加",
        FleetAction.VolumeDown => "音量减",
        FleetAction.OpenUrl => "打开链接",
        FleetAction.Clipboard => "写入剪贴板",
        FleetAction.Terminate => "关闭应用",
        FleetAction.AppSwitcher => "多任务",
        _ => action.ToString()
    };

    private static string State(JobState state) => state switch
    {
        JobState.Queued => "排队中",
        JobState.Running => "执行中",
        JobState.Succeeded => "已完成",
        JobState.Partial => "部分完成",
        JobState.Failed => "失败",
        JobState.Unknown => "结果待确认",
        JobState.Cancelled => "已取消",
        _ => state.ToString()
    };
}
