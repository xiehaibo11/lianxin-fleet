using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Lianxin.Fleet.Core.Infrastructure;
using Lianxin.Fleet.Desktop.ViewModels;
using Lianxin.Fleet.Desktop.Views.Center;

namespace Lianxin.Fleet.Desktop.Views.Dialogs;

// 无线中心：查看连接方式，按设备开关「允许本机通过 Wi-Fi 连接」。开关需要手机先用 USB 连接并信任电脑。
public sealed class WirelessDialog : Window
{
    private readonly CenterWindow center;
    private readonly DataGrid grid;
    private readonly TextBlock status, bonjour;
    private bool working;

    public WirelessDialog(CenterWindow center)
    {
        this.center = center;
        var panel = DialogKit.Prepare(this, "无线中心", 780);
        DialogKit.Note(panel, "开启 Wi-Fi 连接后，手机拔掉数据线、和电脑在同一局域网时，中控仍能识别它（「连接」列显示 Wi-Fi），"
            + "自动化与投屏改走无线。要求：先用 USB 信任这台电脑；Windows 需要安装 Bonjour（Apple 设备服务靠它发现无线手机）。"
            + "\n目前支持 iOS 16 及以下的无线控制；iOS 17 及以上需要额外的隧道服务，暂不支持。");
        bonjour = DialogKit.Note(panel, "正在检查 Bonjour 服务…");
        grid = new DataGrid { Height = 300, IsReadOnly = true, SelectionMode = DataGridSelectionMode.Extended };
        grid.Columns.Add(new DataGridTextColumn { Header = "设备", Binding = new Binding(nameof(WifiItem.Name)), Width = 150 });
        grid.Columns.Add(new DataGridTextColumn { Header = "系统", Binding = new Binding(nameof(WifiItem.Os)), Width = 70 });
        grid.Columns.Add(new DataGridTextColumn { Header = "当前连接", Binding = new Binding(nameof(WifiItem.Connection)), Width = 80 });
        grid.Columns.Add(new DataGridTextColumn { Header = "Wi-Fi 连接", Binding = new Binding(nameof(WifiItem.Wifi)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        grid.ItemsSource = center.Rows.Select(row => new WifiItem(row)).ToList();
        DialogKit.Field(panel, "设备（可多选）", grid);
        status = DialogKit.Note(panel, "");
        var buttons = DialogKit.Buttons(panel, "开启 Wi-Fi 连接", "关闭 Wi-Fi 连接", "查询状态", "关闭");
        buttons[0].Click += async (_, _) => await ApplyAsync(true);
        buttons[1].Click += async (_, _) => await ApplyAsync(false);
        buttons[2].Click += async (_, _) => await ApplyAsync(null);
        Loaded += async (_, _) => await CheckBonjourAsync();
    }

    private async Task CheckBonjourAsync()
    {
        var installed = await UsbServiceControl.IsServiceInstalledAsync(UsbServiceControl.BonjourServiceName, cancellationToken: center.Lifetime);
        bonjour.Text = installed switch
        {
            true => "✓ 已安装 Bonjour 服务，可以发现同一局域网的无线手机。",
            false => "✗ 没有安装 Bonjour 服务：Windows 上的 Apple 设备服务找不到无线手机，只能用 USB。请先安装 Apple 的 Bonjour 或 iTunes。",
            null => "无法确认 Bonjour 服务状态。"
        };
        bonjour.Foreground = installed == false ? DialogKit.ErrorBrush : DialogKit.MutedBrush;
    }

    // enable 为 null 时只查询状态。
    private async Task ApplyAsync(bool? enable)
    {
        if (working)
            return;
        var targets = grid.SelectedItems.OfType<WifiItem>().ToArray();
        if (targets.Length == 0)
        {
            status.Text = "请先在列表里选中设备。";
            return;
        }
        working = true;
        status.Foreground = DialogKit.MutedBrush;
        status.Text = "正在处理…";
        var failed = 0;
        foreach (var item in targets)
        {
            if (item.Row.Connection != "USB")
            {
                item.Show("需要 USB 连接");
                failed++;
                continue;
            }
            try
            {
                if (enable is { } value)
                    await center.Runtime.Setup.SetWifiConnectionsAsync(item.Row.Device, value, center.Lifetime);
                item.Show(await center.Runtime.Setup.GetWifiConnectionsAsync(item.Row.Device, center.Lifetime) ? "已开启" : "已关闭");
            }
            catch (OperationCanceledException) when (center.Lifetime.IsCancellationRequested)
            {
                break;
            }
            catch (Exception error)
            {
                item.Show("失败：" + error.Message);
                failed++;
            }
        }
        status.Text = failed == 0 ? $"已处理 {targets.Length} 台设备。" : $"已处理 {targets.Length} 台，其中 {failed} 台失败。";
        working = false;
    }

    private sealed class WifiItem(DeviceRow row) : INotifyPropertyChanged
    {
        public DeviceRow Row { get; } = row;
        public string Name => (Row.Alias.Length > 0 ? Row.Alias : Row.Name) + " · " + Row.Udid[..Math.Min(8, Row.Udid.Length)];
        public string Os => Row.OsVersion;
        public string Connection => Row.Connection;
        public string Wifi { get; private set; } = "未查询";
        public event PropertyChangedEventHandler? PropertyChanged;

        public void Show(string text)
        {
            Wifi = text;
            PropertyChanged?.Invoke(this, new(nameof(Wifi)));
        }
    }
}
