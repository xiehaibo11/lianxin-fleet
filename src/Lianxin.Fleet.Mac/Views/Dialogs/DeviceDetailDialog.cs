using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Mac.ViewModels;
using Lianxin.Fleet.Mac.Views.Center;

namespace Lianxin.Fleet.Mac.Views.Dialogs;

// 设备详情：信息、应用、文件、开发者镜像、电源五个标签。查询走 USB，不需要自动化；
// 同一时间只进行一项操作，进度和错误显示在底部状态行，长操作不阻塞界面。
public sealed partial class DeviceDetailDialog : Window
{
    private static readonly string[] Titles = ["信息", "应用", "文件", "开发者镜像", "电源"];
    private readonly CenterWindow center;
    private readonly DeviceRow row;
    private readonly TextBlock header, status;
    private readonly TabStrip strip = new();
    private readonly Grid pages = new() { Height = 440 };
    private readonly HashSet<string> loaded = [];
    private readonly Grid infoGrid = new() { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,*") };
    // 关闭窗口时取消这里发起的操作（与中控的生命周期联动）。
    private readonly CancellationTokenSource lifetime;
    private bool working, workingChanges, closeConfirmed;
    private string workingText = "";

    public DeviceDetailDialog(CenterWindow center, DeviceRow row, string tab = "信息")
    {
        this.center = center;
        this.row = row;
        lifetime = CancellationTokenSource.CreateLinkedTokenSource(center.Lifetime);
        var panel = DialogKit.Prepare(this, "设备详情 · " + (row.Alias.Length > 0 ? row.Alias : row.Name), 900);
        header = DialogKit.Note(panel, "");
        header.FontSize = 13;
        strip.Classes.Add("tabs");
        strip.Margin = new Thickness(0, 4, 0, 10);
        AutomationProperties.SetName(strip, "设备详情页面");
        foreach (var title in Titles)
            strip.Items.Add(new TabStripItem { Content = title });
        panel.Children.Add(strip);
        panel.Children.Add(pages);
        foreach (var page in new[] { BuildInfo(), BuildApps(), BuildFiles(), BuildImage(), BuildPower() })
        {
            page.IsVisible = false;
            pages.Children.Add(page);
        }
        status = DialogKit.Note(panel, "");
        DialogKit.Buttons(this, panel, "关闭");
        UpdateHeader();
        center.Refreshed += UpdateHeader;
        Closing += ConfirmClose;
        Closed += (_, _) =>
        {
            center.Refreshed -= UpdateHeader;
            lifetime.Dispose();
        };
        strip.SelectionChanged += async (_, _) => await SelectAsync(strip.SelectedIndex);
        // TabStrip 加入项目时已自动选中第一项，选中项不变时不会触发 SelectionChanged，这里直接加载。
        Opened += async (_, _) =>
        {
            var index = Math.Max(0, Array.IndexOf(Titles, tab));
            if (strip.SelectedIndex == index)
                await SelectAsync(index);
            else
                strip.SelectedIndex = index;
        };
    }

    // 没有读到型号、系统版本的设备（例如手动登记）省略这两项，不留空的分隔符。
    private void UpdateHeader() => header.Text = string.Join(" · ", new[] { row.Alias, row.Name, row.Model,
        row.OsVersion.Length > 0 ? "iOS " + row.OsVersion : "" }.Where(part => part.Length > 0))
        + $" · 连接 {row.Connection} · 自动化 {row.Automation} · 电量 {row.Battery}\n设备 ID {row.Udid}";

    // 第一次切换到某个标签时自动读取。
    private async Task SelectAsync(int index)
    {
        if (index < 0)
            return;
        for (var page = 0; page < pages.Children.Count; page++)
            pages.Children[page].IsVisible = page == index;
        if (!loaded.Add(Titles[index]))
            return;
        await (Titles[index] switch
        {
            "信息" => LoadInfoAsync(),
            "应用" => LoadAppsAsync(),
            "文件" => LoadFilesAsync("/"),
            "开发者镜像" => LoadImageAsync(),
            _ => Task.CompletedTask
        });
    }

    private Control BuildInfo()
    {
        var page = new DockPanel();
        var top = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        var refresh = ActionButton("刷新信息", async () => await LoadInfoAsync());
        DockPanel.SetDock(refresh, Dock.Right);
        top.Children.Add(refresh);
        top.Children.Add(Hint("序列号、容量等来自 USB；自动化已就绪时电量和屏幕取自动化的实时值。"));
        DockPanel.SetDock(top, Dock.Top);
        page.Children.Add(top);
        page.Children.Add(new ScrollViewer { Content = infoGrid });
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
                infoGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var label = new TextBlock { Text = pairs[index].Label, Classes = { "muted" }, Margin = new Thickness(0, 6, 14, 6) };
            var value = new SelectableTextBlock { Text = pairs[index].Value, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 24, 6) };
            AutomationProperties.SetName(value, pairs[index].Label);
            Place(label, index / 2, index % 2 * 2);
            Place(value, index / 2, index % 2 * 2 + 1);
        }
        Show($"已读取设备信息（{DateTime.Now:HH:mm:ss}）。", false);
    }, readOnly: true);

    private void Place(Control element, int gridRow, int column)
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
        finally { working = false; }
    }

    // 关闭时中断本窗口发起的操作；会改动手机的操作进行中时先确认（Avalonia 的关闭事件不能等待，先取消再按确认结果重新关闭）。
    private async void ConfirmClose(object? sender, WindowClosingEventArgs e)
    {
        if (closeConfirmed || !working || !workingChanges)
        {
            lifetime.Cancel();
            return;
        }
        e.Cancel = true;
        if (!await ConfirmAsync("关闭设备详情", $"「{workingText.TrimEnd('…')}」还在进行。关闭窗口会中断它，手机上的结果可能只完成了一部分。仍要关闭吗？", "关闭"))
            return;
        closeConfirmed = true;
        Close();
    }

    private void Show(string text, bool error)
    {
        status.Text = text;
        status.Foreground = error ? DialogKit.ErrorBrush : DialogKit.MutedBrush;
    }

    private Task<bool> ConfirmAsync(string title, string message, string accept) =>
        DialogKit.ConfirmAsync(this, title, message, accept);

    private static Button ActionButton(string text, Action click)
    {
        var button = new Button { Content = text };
        button.Click += (_, _) => click();
        return button;
    }

    private static WrapPanel Toolbar(params Control[] items)
    {
        var bar = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
        bar.Classes.Add("toolbar");
        foreach (var item in items)
            bar.Children.Add(item);
        return bar;
    }

    private static TextBlock Hint(string text) => new()
    {
        Text = text,
        Foreground = DialogKit.MutedBrush,
        FontSize = 12,
        TextWrapping = TextWrapping.Wrap,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 4, 0, 6)
    };
}
