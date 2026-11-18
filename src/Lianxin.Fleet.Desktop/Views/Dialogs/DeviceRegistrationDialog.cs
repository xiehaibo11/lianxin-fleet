using System.Windows.Controls;
using System.Windows;

namespace Lianxin.Fleet.Desktop.Views.Dialogs;

public sealed class DeviceRegistrationDialog : Window
{
    private readonly TextBox _name = new() { MaxLength = 80 };
    private readonly TextBox _udid = new() { MaxLength = 80 };
    private readonly TextBox _port = new() { Text = "8100", MaxLength = 5 };
    private readonly TextBox _group = new() { Text = "未分组", MaxLength = 50 };
    public string DeviceName => _name.Text.Trim();
    public string Udid => _udid.Text.Trim();
    public int WdaPort => int.Parse(_port.Text);
    public string GroupName => _group.Text.Trim();

    public DeviceRegistrationDialog()
    {
        Title = "登记 iPhone";
        Width = 430;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        var panel = new StackPanel { Margin = new Thickness(22) };
        Content = panel;
        Add(panel, "设备名称", _name);
        Add(panel, "设备 UDID", _udid);
        Add(panel, "本机 WDA 转发端口", _port);
        Add(panel, "分组名称", _group);
        panel.Children.Add(new TextBlock { Text = "同一台设备使用固定本机端口。请核对端口映射对应的手机。", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 12) });
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "取消", IsCancel = true, Padding = new Thickness(14, 7, 14, 7) };
        var submit = new Button { Content = "登记", IsDefault = true, Padding = new Thickness(14, 7, 14, 7), Margin = new Thickness(8, 0, 0, 0) };
        submit.Click += (_, _) =>
        {
            if (DeviceName.Length == 0 || Udid.Length == 0 || GroupName.Length == 0 || !int.TryParse(_port.Text, out var port) || port < 1024 || port > 65535)
            {
                MessageBox.Show(this, "请填写名称、UDID、分组，以及 1024–65535 的端口。", "检查设备信息");
                return;
            }
            DialogResult = true;
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(submit);
        panel.Children.Add(buttons);
    }
    private static void Add(Panel panel, string title, TextBox input)
    {
        panel.Children.Add(new Label { Content = title, Target = input, Padding = new Thickness(0, 8, 0, 3) });
        input.Padding = new Thickness(7);
        panel.Children.Add(input);
        System.Windows.Automation.AutomationProperties.SetName(input, title);
    }
}
