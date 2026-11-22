using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Mac.Views.Dialogs;

public sealed record ScriptDraft(string Name, string Source, int Repeat);

// 脚本编辑器：保存时由调用方写库，出错时留在对话框里显示原因（含行号）。
public sealed class ScriptEditorDialog : Window
{
    private ScriptEditorDialog(FleetScript? existing, Func<ScriptDraft, FleetScript> save)
    {
        var panel = DialogKit.Prepare(this, existing is null ? "新建脚本" : "编辑脚本", 720);
        var name = DialogKit.Field(panel, "脚本名称", new TextBox { Text = existing?.Name ?? "", MaxLength = 60 });
        var repeat = DialogKit.Field(panel, "运行次数（0 表示一直循环，直到手动停止）", new TextBox
        {
            Text = (existing?.Repeat ?? 1).ToString(CultureInfo.InvariantCulture),
            MaxLength = 6,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left
        });
        var source = DialogKit.Field(panel, "脚本内容", new TextBox
        {
            Text = existing?.Source ?? "# 示例：回到主屏后打开设置\n主屏\n等待 1\n启动 com.apple.Preferences\n等待 2\n上滑\n",
            AcceptsReturn = true,
            AcceptsTab = true,
            Height = 300,
            FontFamily = new FontFamily("Menlo, Consolas, monospace"),
            FontSize = 13,
            TextWrapping = TextWrapping.NoWrap,
            MaxLength = ScriptParser.MaxSourceLength
        });
        ScrollViewer.SetHorizontalScrollBarVisibility(source, ScrollBarVisibility.Auto);
        ScrollViewer.SetVerticalScrollBarVisibility(source, ScrollBarVisibility.Auto);
        DialogKit.Note(panel, ScriptParser.Help);
        var status = DialogKit.Note(panel, "");
        var buttons = DialogKit.Buttons(this, panel, "保存", "检查语法", "取消");
        ScriptDraft Draft() => new(name.Text?.Trim() ?? "", source.Text ?? "",
            int.TryParse(repeat.Text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var count) ? count : -1);
        buttons[1].Click += (_, _) =>
        {
            try
            {
                var steps = ScriptParser.Parse(source.Text);
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
            try
            {
                Close(save(Draft()));
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                status.Foreground = DialogKit.ErrorBrush;
                status.Text = error.Message;
            }
        };
        Opened += (_, _) => (existing is null ? name : source).Focus();
    }

    public static Task<FleetScript?> EditAsync(Window owner, FleetScript? existing, Func<ScriptDraft, FleetScript> save) =>
        new ScriptEditorDialog(existing, save).ShowDialog<FleetScript?>(owner);
}
