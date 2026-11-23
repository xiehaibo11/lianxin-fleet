using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Mac.Views.Dialogs;

// 录制结束后核对脚本：可改名称、次数和每一行，保存为新脚本；出错时留在对话框里显示原因。
public sealed class RecordedScriptDialog : Window
{
    private RecordedScriptDialog(string name, string source, int actions, Func<ScriptDraft, FleetScript> save)
    {
        var panel = DialogKit.Prepare(this, "保存录制的脚本", 720);
        DialogKit.Note(panel, actions == 0
            ? "这次没有录到操作。可以直接在下面编写脚本，或取消后重新录制。"
            : $"共录到 {actions} 个操作。保存前可以修改坐标、文字和等待时间，也可以删除不需要的行。");
        var title = DialogKit.Field(panel, "脚本名称", new TextBox { Text = name, MaxLength = 60 });
        var repeat = DialogKit.Field(panel, "运行次数（0 表示一直循环，直到手动停止）", new TextBox
        {
            Text = "1",
            MaxLength = 6,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left
        });
        var content = DialogKit.Field(panel, "脚本内容", new TextBox
        {
            Text = source,
            AcceptsReturn = true,
            AcceptsTab = true,
            Height = 320,
            FontFamily = new FontFamily("Menlo, Consolas, monospace"),
            FontSize = 13,
            TextWrapping = TextWrapping.NoWrap,
            MaxLength = ScriptParser.MaxSourceLength
        });
        ScrollViewer.SetHorizontalScrollBarVisibility(content, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(content, ScrollBarVisibility.Auto);
        DialogKit.Note(panel, ScriptParser.Help);
        var status = DialogKit.Note(panel, "");
        var buttons = DialogKit.Buttons(this, panel, "保存为脚本", "检查语法", "取消");
        buttons[1].Click += (_, _) =>
        {
            try
            {
                var steps = ScriptParser.Parse(content.Text);
                status.Foreground = DialogKit.MutedBrush;
                status.Text = $"语法正确，共 {steps.Count} 步。";
            }
            catch (ArgumentException error)
            {
                status.Foreground = DialogKit.ErrorBrush;
                status.Text = error.Message;
            }
        };
        buttons[0].Click += (_, _) =>
        {
            var count = int.TryParse(repeat.Text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : -1;
            try
            {
                Close(save(new ScriptDraft(title.Text?.Trim() ?? "", content.Text ?? "", count)));
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException)
            {
                status.Foreground = DialogKit.ErrorBrush;
                status.Text = error.Message;
            }
        };
        Opened += (_, _) =>
        {
            title.Focus();
            title.SelectAll();
        };
    }

    public static Task<FleetScript?> ReviewAsync(Window owner, string name, string source, int actions,
        Func<ScriptDraft, FleetScript> save) =>
        new RecordedScriptDialog(name, source, actions, save).ShowDialog<FleetScript?>(owner);
}
