using Avalonia.Controls;

namespace Lianxin.Fleet.Mac.Views.Dialogs;

// 单行文本输入，例如别名、分组名称。取消时返回 null。
public sealed class TextPromptDialog : Window
{
    private TextPromptDialog(string title, string label, string initial, bool allowEmpty)
    {
        var panel = DialogKit.Prepare(this, title, 400);
        var input = DialogKit.Field(panel, label, new TextBox { Text = initial, MaxLength = 60 });
        var error = DialogKit.Note(panel, "", DialogKit.ErrorBrush);
        var buttons = DialogKit.Buttons(this, panel, "确定", "取消");
        buttons[0].Click += (_, _) =>
        {
            var text = input.Text?.Trim() ?? "";
            if (!allowEmpty && text.Length == 0)
            {
                error.Text = "内容不能为空。";
                return;
            }
            Close(text);
        };
        Opened += (_, _) =>
        {
            input.Focus();
            input.SelectAll();
        };
    }

    public static Task<string?> AskAsync(Window owner, string title, string label, string initial, bool allowEmpty = false) =>
        new TextPromptDialog(title, label, initial, allowEmpty).ShowDialog<string?>(owner);
}
