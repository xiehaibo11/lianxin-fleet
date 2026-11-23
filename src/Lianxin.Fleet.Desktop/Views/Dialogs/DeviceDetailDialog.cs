using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Desktop.ViewModels;
using Lianxin.Fleet.Desktop.Views.Center;

namespace Lianxin.Fleet.Desktop.Views.Dialogs;

// 设备详情：信息、应用、文件、开发者镜像、电源五个标签。查询走 USB，不需要自动化；
// 同一时间只进行一项操作，进度和错误显示在底部状态行，长操作不阻塞界面。
public sealed partial class DeviceDetailDialog : Window
{
    private readonly CenterWindow center;
    private readonly DeviceRow row;
    private readonly TextBlock header, status;
    private readonly Grid pages = new() { Height = 440 };
    private readonly Dictionary<string, (RadioButton Tab, FrameworkElement Page)> tabs = [];
    private readonly HashSet<string> loaded = [];
    private readonly Grid infoGrid = new();
    // 关闭窗口时取消这里发起的操作（与中控的生命周期联动）。
    private readonly CancellationTokenSource lifetime;
    private bool working, workingChanges;
    private string workingText = "";

    public DeviceDetailDialog(CenterWindow center, DeviceRow row, string tab = "信息")
    {
        this.center = center;
        this.row = row;
        lifetime = CancellationTokenSource.CreateLinkedTokenSource(center.Lifetime);
        var panel = DialogKit.Prepare(this, "设备详情 · " + (row.Alias.Length > 0 ? row.Alias : row.Name), 900);
        header = DialogKit.Note(panel, "");
        header.FontSize = 13;
        var strip = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 10) };
        panel.Children.Add(strip);
        panel.Children.Add(pages);
        AddTab(strip, "信息", BuildInfo());
        AddTab(strip, "应用", BuildApps());
        AddTab(strip, "文件", BuildFiles());
        AddTab(strip, "开发者镜像", BuildImage());
        AddTab(strip, "电源", BuildPower());
        status = DialogKit.Note(panel, "");
        DialogKit.Buttons(panel, "关闭");
        UpdateHeader();
        center.Refreshed += UpdateHeader;
        Closing += ConfirmClose;
        Closed += (_, _) =>
        {
            center.Refreshed -= UpdateHeader;
            lifetime.Dispose();
        };
        Loaded += (_, _) => Select(tabs.ContainsKey(tab) ? tab : "信息");
    }

    // 没有读到型号、系统版本的设备（例如手动登记）省略这两项，不留空的分隔符。
    private void UpdateHeader() => header.Text = string.Join(" · ", new[] { row.Alias, row.Name, row.Model,
        row.OsVersion.Length > 0 ? "iOS " + row.OsVersion : "" }.Where(part => part.Length > 0))
        + $" · 连接 {row.Connection} · 自动化 {row.Automation} · 电量 {row.Battery}\n设备 ID {row.Udid}";

    private void AddTab(Panel strip, string title, FrameworkElement page)
    {
        var tab = new RadioButton { Content = title, GroupName = "DeviceDetail" + GetHashCode(), Style = (Style)FindResource("TabButton") };
        tab.Checked += (_, _) => Select(title);
        strip.Children.Add(tab);
        page.Visibility = Visibility.Collapsed;
        pages.Children.Add(page);
        tabs[title] = (tab, page);
    }

    // 第一次切换到某个标签时自动读取。
    private async void Select(string title)
    {
        foreach (var (name, (tab, page)) in tabs)
        {
            page.Visibility = name == title ? Visibility.Visible : Visibility.Collapsed;
            tab.IsChecked = name == title;
        }
        if (!loaded.Add(title))
            return;
        await (title switch
        {
            "信息" => LoadInfoAsync(),
            "应用" => LoadAppsAsync(),
            "文件" => LoadFilesAsync("/"),
            "开发者镜像" => LoadImageAsync(),
            _ => Task.CompletedTask
        });
    }

    private FrameworkElement BuildInfo()
    {
        var page = new DockPanel();
        var refresh = ActionButton("刷新信息", async () => await LoadInfoAsync());
        var top = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(refresh, Dock.Right);
        top.Children.Add(refresh);
        top.Children.Add(new TextBlock
        {
            Text = "序列号、容量等来自 USB；自动化已就绪时电量和屏幕取自动化的实时值。",
            Foreground = DialogKit.MutedBrush,
            VerticalAlignment = VerticalAlignment.Center
        });
        DockPanel.SetDock(top, Dock.Top);
        page.Children.Add(top);
        foreach (var width in new[] { GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto, new GridLength(1, GridUnitType.Star) })
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
        page.Children.Add(new ScrollViewer { Content = infoGrid, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        return page;
    }

    private Task LoadInfoAsync() => RunAsync("正在读取设备信息…", async token =>
    {
        var info = await center.Runtime.DeviceManager.GetInfoAsync(row.Device, row.AutomationReady, token);
        row.SetBattery(info.Battery);
        infoGrid.Children.Clear();
        infoGrid.RowDefinitions.Clear();
        var pairs = DeviceText.Describe(info, row);
        for (var index = 0; index < pairs.Count; index++)
        {
            if (index % 2 == 0)
                infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var label = new TextBlock { Text = pairs[index].Label, Foreground = DialogKit.MutedBrush, Margin = new Thickness(0, 6, 14, 6) };
            var value = new TextBox
            {
                Text = pairs[index].Value,
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 6, 24, 6),
                TextWrapping = TextWrapping.Wrap
            };
            System.Windows.Automation.AutomationProperties.SetName(value, pairs[index].Label);
            Place(label, index / 2, index % 2 * 2);
            Place(value, index / 2, index % 2 * 2 + 1);
        }
        Show($"已读取设备信息（{DateTime.Now:HH:mm:ss}）。", false);
    }, readOnly: true);

    private void Place(UIElement element, int gridRow, int column)
    {
        Grid.SetRow(element, gridRow);
        Grid.SetColumn(element, column);
        infoGrid.Children.Add(element);
    }

    // 对话框内的操作统一入口：已有操作进行中时不重复启动；错误留在底部状态行，不弹窗。
    // readOnly 为 false 的操作会改动手机或本机文件，关闭窗口前要确认（关闭会中断它）。
    private async Task<bool> RunAsync(string progress, Func<CancellationToken, Task> action, bool readOnly = false)
    {
        if (working)
        {
            Show("上一项操作仍在进行，请稍候。", true);
            return false;
        }
        working = true;
        workingChanges = !readOnly;
        workingText = progress;
        Show(progress, false);
        Cursor = Cursors.AppStarting;
        var token = lifetime.Token;
        try
        {
            await action(token);
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception error)
        {
            Show((error is DeviceOperationException { Uncertain: true } ? "结果待确认：" : "失败：") + error.Message, true);
            return false;
        }
        finally
        {
            working = false;
            Cursor = null;
        }
    }

    // 关闭时中断本窗口发起的操作；会改动手机的操作进行中时先确认，避免误关后在别处重复操作。
    private void ConfirmClose(object? sender, CancelEventArgs e)
    {
        if (working && workingChanges && !Confirm("关闭设备详情",
            $"「{workingText.TrimEnd('…')}」还在进行。关闭窗口会中断它，手机上的结果可能只完成了一部分。仍要关闭吗？"))
        {
            e.Cancel = true;
            return;
        }
        lifetime.Cancel();
    }

    private void Show(string text, bool error)
    {
        status.Text = text;
        status.Foreground = error ? DialogKit.ErrorBrush : DialogKit.MutedBrush;
    }

    private bool Confirm(string title, string message) =>
        MessageBox.Show(this, message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    private static Button ActionButton(string text, Action click)
    {
        var button = new Button { Content = text };
        button.Click += (_, _) => click();
        return button;
    }

    private static WrapPanel Toolbar(params UIElement[] items)
    {
        var bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
        foreach (var item in items)
            bar.Children.Add(item);
        return bar;
    }

    private static TextBlock Hint(string text) => new() { Text = text, Foreground = DialogKit.MutedBrush, FontSize = 12, Margin = new Thickness(0, 4, 0, 6) };
}
