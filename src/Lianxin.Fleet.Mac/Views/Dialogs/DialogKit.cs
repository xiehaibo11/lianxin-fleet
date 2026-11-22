using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Lianxin.Fleet.Mac.Views.Dialogs;

// 代码构建的对话框共用的深色外观、表单布局，以及确认框和提示框。
internal static class DialogKit
{
    public static readonly IBrush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x8A, 0x8A));
    public static readonly IBrush MutedBrush = new SolidColorBrush(Color.FromRgb(0x9D, 0xA4, 0xBE));

    public static StackPanel Prepare(Window window, string title, double width)
    {
        window.Title = title;
        window.Width = width;
        window.SizeToContent = SizeToContent.Height;
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.CanResize = false;
        window.ShowInTaskbar = false;
        window.Background = new SolidColorBrush(Color.FromRgb(0x15, 0x19, 0x24));
        var panel = new StackPanel { Margin = new Thickness(22, 18, 22, 18), Spacing = 2 };
        window.Content = panel;
        return panel;
    }

    public static T Field<T>(Panel panel, string title, T input) where T : Control
    {
        panel.Children.Add(new Label { Content = title, Padding = new Thickness(0, 8, 0, 2), Target = input });
        panel.Children.Add(input);
        AutomationProperties.SetName(input, title);
        return input;
    }

    public static TextBlock Note(Panel panel, string text, IBrush? brush = null)
    {
        var note = new TextBlock
        {
            Text = text,
            Foreground = brush ?? MutedBrush,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 6)
        };
        panel.Children.Add(note);
        return note;
    }

    // 右下角按钮行；第一个按钮为默认按钮（回车），最后一个为取消按钮（Esc），点它关闭窗口。
    public static Button[] Buttons(Window window, Panel panel, params string[] labels)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(0, 12, 0, 0)
        };
        var buttons = labels.Select((text, index) => new Button
        {
            Content = text,
            IsDefault = index == 0,
            IsCancel = index == labels.Length - 1,
            MinWidth = 76,
            HorizontalContentAlignment = HorizontalAlignment.Center
        }).ToArray();
        foreach (var button in buttons)
            row.Children.Add(button);
        buttons[^1].Click += (_, _) => window.Close();
        panel.Children.Add(row);
        return buttons;
    }

    // 替代 WPF 的 MessageBox：确认返回 true，取消或关闭窗口返回 false。
    public static Task<bool> ConfirmAsync(Window owner, string title, string message, string accept = "确定")
    {
        var window = new Window();
        var panel = Prepare(window, title, 460);
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4) });
        var buttons = Buttons(window, panel, accept, "取消");
        buttons[0].Click += (_, _) => window.Close(true);
        return window.ShowDialog<bool>(owner);
    }

    public static Task AlertAsync(Window owner, string title, string message)
    {
        var window = new Window();
        var panel = Prepare(window, title, 460);
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4) });
        Buttons(window, panel, "好");
        return window.ShowDialog(owner);
    }
}
