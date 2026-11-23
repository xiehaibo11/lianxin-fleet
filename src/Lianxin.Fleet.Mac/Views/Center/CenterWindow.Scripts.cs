namespace Lianxin.Fleet.Mac.Views.Center;

// 工具栏的暂停 / 继续脚本：与「停止脚本」相同，作用于勾选的设备，没有勾选时作用于全部正在运行脚本的设备。
// 暂停在当前这一步执行完后生效，暂停中仍可停止；「消息」列显示「已暂停」。
public sealed partial class CenterWindow
{
    public void PauseScripts()
    {
        var paused = Runtime.Scripts.Pause(ScriptTargets());
        ApplySnapshot(Runtime.Service.GetSnapshot());
        Report(paused > 0 ? $"已暂停 {paused} 台设备上的脚本，当前这一步执行完后停住；点「继续脚本」恢复。"
            : "没有可以暂停的脚本（没有在运行，或已经暂停）。");
    }

    public void ResumeScripts()
    {
        var resumed = Runtime.Scripts.Resume(ScriptTargets());
        ApplySnapshot(Runtime.Service.GetSnapshot());
        Report(resumed > 0 ? $"已继续 {resumed} 台设备上的脚本。" : "没有暂停中的脚本。");
    }

    private long[] ScriptTargets()
    {
        var selected = SelectedRows();
        return selected.Count > 0 ? selected.Select(row => row.Id).ToArray() : Runtime.Scripts.Snapshot().Keys.ToArray();
    }
}
