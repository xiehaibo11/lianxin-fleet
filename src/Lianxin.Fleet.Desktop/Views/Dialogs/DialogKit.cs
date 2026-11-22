using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Lianxin.Fleet.Desktop.Views.Dialogs;

// 代码构建的对话框共用的深色外观和表单布局。
internal static class DialogKit
{
    public static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x8A, 0x8A));
    public static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(0x9D, 0xA4, 0xBE));

    public static StackPanel Prepare(Window window, string title, double width)
    {
        window.Title = title;
        window.Width = width;
        window.SizeToContent = SizeToContent.Height;
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.ResizeMode = ResizeMode.NoResize;
        window.Background = new SolidColorBrush(Color.FromRgb(0x15, 0x19, 0x24));
        window.Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xE8, 0xF4));
        window.ShowInTaskbar = false;
        var panel = new StackPanel { Margin = new Thickness(22, 18, 22, 18) };
        window.Content = panel;
        return panel;
    }

    public static T Field<T>(Panel panel, string title, T input) where T : FrameworkElement
    {
        var label = new Label { Content = title, Padding = new Thickness(0, 8, 0, 2) };
        if (input is Control control)
            label.Target = control;
        panel.Children.Add(label);
        panel.Children.Add(input);
        System.Windows.Automation.AutomationProperties.SetName(input, title);
        return input;
    }

    public static TextBlock Note(Panel panel, string text, Brush? brush = null)
    {
        var note = new TextBlock { Text = text, Foreground = brush ?? MutedBrush, FontSize = 12, Margin = new Thickness(0, 6, 0, 6) };
        panel.Children.Add(note);
        return note;
    }

    // 右下角按钮行；第一个按钮为默认按钮，最后一个为取消按钮。
    public static Button[] Buttons(Panel panel, params string[] labels)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        var buttons = labels.Select((text, index) => new Button
        {
            Content = text,
            IsDefault = index == 0,
            IsCancel = index == labels.Length - 1,
            MinWidth = 76,
            Margin = new Thickness(8, 0, 0, 0)
        }).ToArray();
        foreach (var button in buttons)
            row.Children.Add(button);
        panel.Children.Add(row);
        return buttons;
    }
}
