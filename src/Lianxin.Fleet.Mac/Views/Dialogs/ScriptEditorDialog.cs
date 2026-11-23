using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;
using Lianxin.Fleet.Mac.Views.Center;

namespace Lianxin.Fleet.Mac.Views.Dialogs;

public sealed record ScriptDraft(string Name, string Source, int Repeat);

// 脚本编辑器：保存时由调用方写库，出错时留在对话框里显示原因（含行号）。
// 从中控打开时多一行工具：抓取已就绪手机的界面元素（双击插入「点击元素」），以及打开模板图片目录。
public sealed class ScriptEditorDialog : Window
{
    private readonly CancellationTokenSource closing = new();
    private sealed record DeviceChoice(FleetDevice Device)
    {
        public override string ToString() => Device.DisplayName;
    }

    private sealed record ElementChoice(ScreenElement Element)
    {
        public override string ToString() => ScriptParser.Summary(Element);
    }

    private ScriptEditorDialog(Window owner, FleetScript? existing, Func<ScriptDraft, FleetScript> save)
    {
        var panel = DialogKit.Prepare(this, existing is null ? "新建脚本" : "编辑脚本", 760);
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
            Text = existing?.Source ?? "# 示例：回到主屏后打开设置\n主屏\n等待 1\n启动 com.apple.Preferences\n等待元素 文字=通用 5\n点击元素 文字=通用\n",
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
        panel.Children.Add(new Expander
        {
            Header = "流程、元素与图色命令（完整说明见 docs/SCRIPT_GUIDE.md）",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = new TextBlock { Text = ScriptParser.FlowHelp, Foreground = DialogKit.MutedBrush, FontSize = 12, TextWrapping = TextWrapping.Wrap }
        });
        TextBlock? status = null;
        void Report(string text, bool error)
        {
            status!.Foreground = error ? DialogKit.ErrorBrush : DialogKit.MutedBrush;
            status.Text = text;
        }
        if (owner is CenterWindow center)
            AddTools(panel, center, source, Report, closing.Token);
        status = DialogKit.Note(panel, "");
        var buttons = DialogKit.Buttons(this, panel, "保存", "检查语法", "取消");
        ScriptDraft Draft() => new(name.Text?.Trim() ?? "", source.Text ?? "",
            int.TryParse(repeat.Text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var count) ? count : -1);
        buttons[1].Click += (_, _) =>
        {
            try
            {
                var steps = ScriptParser.Parse(source.Text);
                Report($"语法正确，共 {ScriptParser.CountSteps(steps)} 条命令。", false);
            }
            catch (ArgumentException error)
            {
                Report(error.Message, true);
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
                Report(error.Message, true);
            }
        };
        Opened += (_, _) => (existing is null ? name : source).Focus();
        Closed += (_, _) => closing.Cancel();
    }

    // dialogToken 在窗口关闭时取消：正在进行的抓取节点随之停止，不再占着这台设备的 WDA 请求。
    private static void AddTools(Panel panel, CenterWindow center, TextBox source, Action<string, bool> report,
        CancellationToken dialogToken)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4) };
        var choices = center.Rows.Where(item => item.AutomationReady).Select(item => new DeviceChoice(item.Device)).ToArray();
        var devices = new ComboBox { ItemsSource = choices, Width = 220, SelectedItem = choices.FirstOrDefault() };
        AutomationProperties.SetName(devices, "抓取节点的设备");
        var capture = new Button { Content = "抓取节点", MinWidth = 76 };
        var folder = new Button { Content = "打开模板目录", MinWidth = 96, IsEnabled = center.Runtime.Scripts.TemplateDirectory is not null };
        row.Children.Add(devices);
        row.Children.Add(capture);
        row.Children.Add(folder);
        panel.Children.Add(row);
        var list = new ListBox { Height = 150, IsVisible = false, FontFamily = new FontFamily("Menlo, Consolas, monospace"), FontSize = 12 };
        ToolTip.SetTip(list, "双击一行，在光标处插入「点击元素」命令");
        AutomationProperties.SetName(list, "当前界面元素");
        panel.Children.Add(list);
        capture.Click += async (_, _) =>
        {
            if (devices.SelectedItem is not DeviceChoice choice)
            {
                report("没有已启动自动化的设备，请先在设备页启动自动化。", true);
                return;
            }
            capture.IsEnabled = false;
            report($"正在读取「{choice}」的界面元素…", false);
            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(center.Lifetime, dialogToken);
                var xml = await center.Runtime.Bridge.GetPageSourceAsync(choice.Device, linked.Token);
                // 界面树可能有几十 MB，在后台线程整理，界面不卡。
                var elements = await Task.Run(() => ScriptParser.DescribeElements(xml), linked.Token);
                list.ItemsSource = elements.Select(element => new ElementChoice(element)).ToArray();
                list.IsVisible = true;
                report(elements.Count == 0 ? "当前界面没有可用的元素。" : $"共 {elements.Count} 个可见元素，双击一行插入「点击元素」命令。", false);
            }
            catch (Exception error) when (error is DeviceOperationException or ArgumentException or NotSupportedException
                or OperationCanceledException or ObjectDisposedException)
            {
                report("抓取节点失败：" + error.Message, true);
            }
            finally
            {
                capture.IsEnabled = true;
            }
        };
        list.DoubleTapped += (_, _) =>
        {
            if (list.SelectedItem is ElementChoice choice)
                InsertLine(source, "点击元素 " + choice.Element.Selector);
        };
        folder.Click += (_, _) =>
        {
            if (center.Runtime.Scripts.TemplateDirectory is not { } path)
                return;
            Directory.CreateDirectory(path);
            // 在访达中打开；在 Windows 上冒烟测试时交给资源管理器。
            var info = OperatingSystem.IsMacOS()
                ? new ProcessStartInfo { FileName = "/usr/bin/open", UseShellExecute = false }
                : new ProcessStartInfo(path) { UseShellExecute = true };
            if (OperatingSystem.IsMacOS())
                info.ArgumentList.Add(path);
            using var process = Process.Start(info);
            report("模板图片放在这个目录，脚本里写文件名（不带 .png）。", false);
        };
    }

    // 在光标所在位置另起一行插入命令。
    private static void InsertLine(TextBox source, string line)
    {
        var current = source.Text ?? "";
        var caret = Math.Clamp(source.CaretIndex, 0, current.Length);
        var text = (caret > 0 && current[caret - 1] != '\n' ? "\n" : "") + line + "\n";
        source.Text = current.Insert(caret, text);
        source.CaretIndex = caret + text.Length;
        source.Focus();
    }

    public static Task<FleetScript?> EditAsync(Window owner, FleetScript? existing, Func<ScriptDraft, FleetScript> save) =>
        new ScriptEditorDialog(owner, existing, save).ShowDialog<FleetScript?>(owner);
}
