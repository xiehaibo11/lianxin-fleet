using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Lianxin.Fleet.Core.Devices;

namespace Lianxin.Fleet.Mac.Views;

internal sealed record DeviceRegistration(string Name, string Udid, int Port, string Group);

internal sealed partial class RegisterDeviceWindow : Window
{
    public RegisterDeviceWindow(int port)
    {
        InitializeComponent();
        PortInput.Text = port.ToString(CultureInfo.InvariantCulture);
        Opened += (_, _) => DeviceNameInput.Focus();
    }

    private void RegisterClicked(object? sender, RoutedEventArgs args)
    {
        var name = DeviceNameInput.Text?.Trim() ?? "";
        var udid = UdidInput.Text?.Trim() ?? "";
        var group = DeviceGroupInput.Text?.Trim();
        if (name.Length == 0 || !DeviceBridge.ValidUdid(udid) || !int.TryParse(PortInput.Text, out var port) || port is < 1024 or > 65535)
        {
            ErrorText.Text = "请输入设备名称、有效的 UDID 和 1024–65535 之间的端口。";
            return;
        }
        Close(new DeviceRegistration(name, udid, port, string.IsNullOrEmpty(group) ? "未分组" : group));
    }

    private void CancelClicked(object? sender, RoutedEventArgs args) => Close(null);
}
