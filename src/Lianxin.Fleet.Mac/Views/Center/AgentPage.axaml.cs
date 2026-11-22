using System.Collections.ObjectModel;
using Anthropic;
using Anthropic.Exceptions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Lianxin.Fleet.Core.Agents;
using Lianxin.Fleet.Mac.Interaction;
using Lianxin.Fleet.Mac.ViewModels;

namespace Lianxin.Fleet.Mac.Views.Center;

public sealed partial class AgentPage : UserControl
{
    private readonly ObservableCollection<AgentLogItem> log = [];
    private CenterWindow center = null!;
    private CancellationTokenSource? running;

    private sealed record DeviceChoice(DeviceRow Row)
    {
        public string Label => $"{(Row.Alias.Length > 0 ? Row.Alias : Row.Name)} · {Row.Model} · iOS {Row.OsVersion}";
        public override string ToString() => Label;
    }

    public AgentPage()
    {
        InitializeComponent();
        LogList.ItemsSource = log;
    }

    public void Attach(CenterWindow host)
    {
        center = host;
        center.Refreshed += UpdateDevices;
        UpdateModelLabel();
    }

    // 切换到本页时刷新模型和密钥说明（设置页可能刚改过）。
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty && IsVisible && center is not null && !center.IsClosing)
            UpdateModelLabel();
    }

    private void UpdateModelLabel() => ModelLabel.Text =
        $"模型 {center.Settings.AgentModel} · 最多 {center.Settings.AgentMaxSteps} 轮 · 密钥：{(ResolveKey() is null ? "未在设置中填写（将尝试系统登录凭据）" : "已配置")}";

    private void UpdateDevices()
    {
        if (running is not null)
            return;
        var ready = center.Rows.Where(row => row.AutomationReady).ToArray();
        var selected = (DeviceBox.SelectedItem as DeviceChoice)?.Row.Id;
        if (ready.Select(row => row.Id).SequenceEqual(DeviceBox.Items.OfType<DeviceChoice>().Select(choice => choice.Row.Id)))
            return;
        var choices = ready.Select(row => new DeviceChoice(row)).ToArray();
        DeviceBox.ItemsSource = choices;
        DeviceBox.SelectedItem = choices.FirstOrDefault(choice => choice.Row.Id == selected) ?? choices.FirstOrDefault();
    }

    // 优先用设置里保存的密钥，其次 ANTHROPIC_API_KEY；都没有时交给 SDK 自己找登录凭据。
    private string? ResolveKey() => AgentKeyStore.Load(center.Runtime.DataDirectory)
        ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

    private void Add(string kind, string text) =>
        log.Add(new AgentLogItem(DateTime.Now, kind, text.Length > 2000 ? text[..2000] + "…" : text));

    private async void StartClicked(object? sender, RoutedEventArgs e)
    {
        if (running is not null)
            return;
        if (DeviceBox.SelectedItem is not DeviceChoice choice)
        {
            ResultLabel.Text = "没有可用设备：请先在设备页为手机启动自动化。";
            return;
        }
        var instruction = InstructionInput.Text?.Trim() ?? "";
        if (instruction.Length == 0)
        {
            ResultLabel.Text = "请填写任务说明。";
            return;
        }
        using var source = CancellationTokenSource.CreateLinkedTokenSource(center.Lifetime);
        running = source;
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
        DeviceBox.IsEnabled = false;
        ResultLabel.Text = "正在执行…";
        Add("开始", $"{choice.Label}：{instruction}");
        try
        {
            var key = ResolveKey();
            var client = key is null ? new AnthropicClient() : new AnthropicClient { ApiKey = key };
            var agent = new PhoneAgent(client, center.Runtime.Bridge,
                new AgentOptions(center.Settings.AgentModel, center.Settings.AgentMaxSteps),
                (device, token) => ScreenShrinker.CaptureAsync(center.Runtime.Bridge, device, token));
            var progress = new Progress<AgentStep>(step => Add(step.Kind, step.Text));
            var outcome = await agent.RunAsync(choice.Row.Device, instruction, progress, source.Token);
            ResultLabel.Text = $"{(outcome.Success ? "✓ 完成" : "✗ 未完成")}：{outcome.Summary}\n执行 {outcome.Actions} 个操作，"
                + $"输入 {outcome.InputTokens:N0} / 输出 {outcome.OutputTokens:N0} tokens。";
            Add(outcome.Success ? "完成" : "结束", outcome.Summary);
        }
        catch (OperationCanceledException) when (source.IsCancellationRequested)
        {
            ResultLabel.Text = "已停止。";
            Add("停止", "已手动停止，手机停在当前画面。");
        }
        catch (AnthropicUnauthorizedException)
        {
            ResultLabel.Text = "模型服务拒绝了 API 密钥，请在「设置」里重新填写。";
        }
        catch (AnthropicRateLimitException)
        {
            ResultLabel.Text = "模型服务限流，请稍后再试。";
        }
        catch (Exception error)
        {
            ResultLabel.Text = "执行失败：" + error.Message;
            Add("错误", error.Message);
        }
        finally
        {
            running = null;
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            DeviceBox.IsEnabled = true;
            if (log.LastOrDefault() is { } last)
                LogList.ScrollIntoView(last);
        }
    }

    public void Stop() => running?.Cancel();

    private void StopClicked(object? sender, RoutedEventArgs e) => Stop();

    private void ClearClicked(object? sender, RoutedEventArgs e) => log.Clear();
}
