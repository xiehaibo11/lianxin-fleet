using Anthropic;
using Anthropic.Models.Beta;
using Anthropic.Models.Beta.Messages;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Agents;

public sealed record AgentOptions(string Model = PhoneAgent.DefaultModel, int MaxSteps = 25, double SettleSeconds = 1);
public sealed record AgentStep(string Kind, string Text);
public sealed record AgentOutcome(bool Success, string Summary, int Actions, long InputTokens, long OutputTokens);

// 看屏操作循环：截图 → 模型选择工具 → 在手机上执行 → 再截图，直到模型调用 finish 或达到步数上限。
public sealed class PhoneAgent(AnthropicClient client, IDeviceBridge bridge, AgentOptions? options = null,
    Func<FleetDevice, CancellationToken, Task<AgentScreen>>? capture = null)
{
    public const string DefaultModel = "claude-opus-5-5";

    // 这些模型支持服务端拒答回退（server-side fallbacks）。
    private static readonly HashSet<string> FallbackModels =
        ["claude-fable-5-2", "claude-fable-5-1", "claude-opus-5-5", "claude-opus-5", "claude-sonnet-5-5"];

    private const string SystemPrompt = """
        You operate an iPhone for the user through the provided tools. Every tool result ends with a fresh screenshot.
        Coordinates are pixels in the most recent screenshot, origin at the top-left corner.
        Work in small steps: look at the screen, take one or two actions, then check the new screenshot before continuing.
        When you know an app's bundle identifier or a URL, prefer launch_app or open_url over searching for icons.
        Do not enter passwords, pay, send messages, change security settings or delete data unless the instruction explicitly asks for it.
        If the screen needs something the instruction does not cover (a login, a permission prompt with consequences, a captcha), stop.
        When the task is complete or cannot be completed, call finish with a short summary in the language of the user's instruction.
        """;

    private readonly AgentOptions settings = options ?? new();

    public async Task<AgentOutcome> RunAsync(FleetDevice device, string instruction, IProgress<AgentStep> progress,
        CancellationToken cancellationToken = default)
    {
        instruction = FleetValidation.Text(instruction, 2000, "任务说明");
        if (settings.MaxSteps is < 1 or > 200)
            throw new ArgumentException("最大步数须为 1 至 200。");
        var screen = await CaptureAsync(device, cancellationToken).ConfigureAwait(false);
        List<BetaMessageParam> messages =
        [
            new BetaUserMessageParam
            {
                Content = new List<BetaContentBlockParam>
                {
                    Image(screen),
                    new BetaTextBlockParam { Text = $"Task: {instruction}\nThe screenshot above is {screen.Width}x{screen.Height} pixels." },
                },
            },
        ];
        long input = 0, output = 0;
        var actions = 0;
        for (var turn = 1; turn <= settings.MaxSteps; turn++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await client.Beta.Messages.Create(Request(messages), cancellationToken).ConfigureAwait(false);
            input += response.Usage.InputTokens;
            output += response.Usage.OutputTokens;
            if (response.StopReason == BetaStopReason.Refusal)
                return new(false, "模型拒绝了这个任务" + (response.StopDetails?.Explanation is { Length: > 0 } why ? "：" + why : "。"),
                    actions, input, output);
            var reply = AgentTranscript.Echo(response, progress);
            messages.Add(new BetaAssistantMessageParam { Content = reply.Blocks });
            if (reply.ToolUses.Count == 0)
                return new(false, reply.Text.Length > 0 ? reply.Text : "AI 没有调用工具就结束了。", actions, input, output);
            if (response.StopReason == BetaStopReason.MaxTokens)
                return new(false, "模型输出被截断，请缩短任务说明后重试。", actions, input, output);

            var results = new List<ToolOutcome>();
            AgentOutcome? finished = null;
            foreach (var call in reply.ToolUses)
            {
                AgentAction action;
                try
                {
                    action = AgentTools.Parse(call.Name, call.Input, screen.Width, screen.Height);
                }
                catch (ArgumentException error)
                {
                    progress.Report(new("提示", "AI 的参数无效：" + error.Message));
                    results.Add(new(call.ID, error.Message, true));
                    continue;
                }
                if (action.Finished)
                {
                    finished = new(action.Success, action.Summary ?? "", actions, input, output);
                    results.Add(new(call.ID, "Finished.", false));
                    continue;
                }
                progress.Report(new("操作", action.Description));
                try
                {
                    if (action.Action is { } command)
                    {
                        await bridge.ExecuteAsync(device, command, action.Parameters, cancellationToken).ConfigureAwait(false);
                        actions++;
                    }
                    if (action.WaitSeconds > 0)
                        await Task.Delay(TimeSpan.FromSeconds(action.WaitSeconds), cancellationToken).ConfigureAwait(false);
                    results.Add(new(call.ID, "Done.", false));
                }
                catch (DeviceOperationException error) when (!cancellationToken.IsCancellationRequested)
                {
                    progress.Report(new("错误", error.Message));
                    results.Add(new(call.ID, "The phone rejected the action: " + error.Message, true));
                }
            }
            if (finished is not null)
                return finished;
            await Task.Delay(TimeSpan.FromSeconds(settings.SettleSeconds), cancellationToken).ConfigureAwait(false);
            screen = await CaptureAsync(device, cancellationToken).ConfigureAwait(false);
            messages.Add(new BetaUserMessageParam { Content = AgentTranscript.Results(results, Image(screen), screen) });
        }
        return new(false, $"已达到最多 {settings.MaxSteps} 轮，任务没有完成。", actions, input, output);
    }

    private MessageCreateParams Request(List<BetaMessageParam> messages)
    {
        var request = new MessageCreateParams
        {
            Model = settings.Model,
            MaxTokens = 16000,
            System = SystemPrompt,
            Tools = [.. AgentTools.Definitions.Select(tool => (BetaToolUnion)tool)],
            Messages = [.. messages],
            OutputConfig = new BetaOutputConfig { Effort = Effort.Medium },
            Thinking = new BetaThinkingConfigAdaptive { Display = Display.Summarized },
            // 截图逐轮累加，自动缓存已发送的前缀。
            CacheControl = new BetaCacheControlEphemeral(),
        };
        return FallbackModels.Contains(settings.Model)
            ? request with
            {
                Betas = [AnthropicBeta.ServerSideFallback2026_07_01],
                Fallbacks = new Default()
            }
            : request;
    }

    private async Task<AgentScreen> CaptureAsync(FleetDevice device, CancellationToken cancellationToken) =>
        capture is null ? await AgentScreen.CaptureAsync(bridge, device, cancellationToken).ConfigureAwait(false)
            : await capture(device, cancellationToken).ConfigureAwait(false);

    private static BetaImageBlockParam Image(AgentScreen screen) => new()
    {
        Source = new BetaBase64ImageSource
        {
            Data = Convert.ToBase64String(screen.Data),
            MediaType = screen.MediaType == "image/jpeg" ? MediaType.ImageJpeg : MediaType.ImagePng,
        },
    };
}
