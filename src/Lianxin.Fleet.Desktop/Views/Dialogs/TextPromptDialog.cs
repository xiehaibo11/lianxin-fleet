using System.Windows;
using System.Windows.Controls;

namespace Lianxin.Fleet.Desktop.Views.Dialogs;

// 单行文本输入，例如别名、分组名称。取消时返回 null。
public sealed class TextPromptDialog : Window
{
    private readonly TextBox input;

    private TextPromptDialog(string title, string label, string initial, bool allowEmpty)
    {
        var panel = DialogKit.Prepare(this, title, 400);
        input = DialogKit.Field(panel, label, new TextBox { Text = initial, MaxLength = 60 });
        var error = DialogKit.Note(panel, "", DialogKit.ErrorBrush);
        var buttons = DialogKit.Buttons(panel, "确定", "取消");
        buttons[0].Click += (_, _) =>
        {
            if (!allowEmpty && input.Text.Trim().Length == 0)
            {
                error.Text = "内容不能为空。";
                return;
            }
            DialogResult = true;
        };
        Loaded += (_, _) =>
        {
            input.Focus();
            input.SelectAll();
        };
    }

    public static string? Ask(Window? owner, string title, string label, string initial, bool allowEmpty = false)
    {
        var dialog = new TextPromptDialog(title, label, initial, allowEmpty) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.input.Text.Trim() : null;
    }
}
