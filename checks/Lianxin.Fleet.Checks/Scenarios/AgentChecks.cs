using System.Buffers.Binary;
using System.Net;
using System.Text;
using System.Text.Json;
using Anthropic;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Agents;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Checks.Scenarios;

// 用假 HTTP 处理器驱动真实 SDK：不调用模型，只核对请求格式和看屏操作循环。
internal static class AgentChecks
{
    private static readonly FleetDevice Phone = new(1, "6s", "00008030-0012345678900001", 8100);

    public static Task ToolMapping()
    {
        static IReadOnlyDictionary<string, JsonElement> Input(object value) =>
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(value))!;
        var tap = AgentTools.Parse("tap", Input(new
        {
            x = 749,
            y = 0
        }), 750, 1334);
        Assert.True(tap.Action == FleetAction.Tap && tap.Parameters.X == 1 && tap.Parameters.Y == 0, "pixel to relative");
        var back = AgentTools.Parse("press_button", Input(new
        {
            button = "back"
        }), 750, 1334);
        Assert.Equal(FleetAction.Swipe, back.Action);
        var finish = AgentTools.Parse("finish", Input(new
        {
            success = true,
            summary = "好了"
        }), 750, 1334);
        Assert.True(finish is { Finished: true, Success: true, Summary: "好了" }, "finish");
        Assert.Throws<ArgumentException>(() => AgentTools.Parse("tap", Input(new { x = 750, y = 10 }), 750, 1334));
        Assert.Throws<ArgumentException>(() => AgentTools.Parse("press_button", Input(new { button = "power" }), 750, 1334));
        Assert.Throws<ArgumentException>(() => AgentTools.Parse("wait", Input(new { seconds = 60 }), 750, 1334));
        Assert.Throws<ArgumentException>(() => AgentTools.Parse("launch_app", Input(new { bundle_id = "bad id" }), 750, 1334));
        Assert.Throws<ArgumentException>(() => AgentTools.Parse("install", Input(new { }), 750, 1334));
        Assert.Equal(10, AgentTools.Definitions.Count);
        Assert.True(!AgentScreen.FromPng(Png(750, 1334)).NeedsResize && AgentScreen.FromPng(Png(1170, 2532)).NeedsResize, "resize limits");
        return Task.CompletedTask;
    }

    public static async Task Loop()
    {
        var requests = new List<(JsonElement Body, string Beta)>();
        var replies = new Queue<string>([
            Reply("""[{"type":"thinking","thinking":"设置图标在中间","signature":"sig-1"},{"type":"text","text":"打开设置"},{"type":"tool_use","id":"toolu_1","name":"tap","input":{"x":374,"y":666}},{"type":"tool_use","id":"toolu_2","name":"tap","input":{"x":9999,"y":1}}]""", "tool_use"),
            Reply("""[{"type":"tool_use","id":"toolu_3","name":"finish","input":{"success":true,"summary":"已打开设置"}}]""", "tool_use"),
        ]);
        var handler = new FakeHandler(async request =>
        {
            var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync()).RootElement.Clone();
            requests.Add((body, string.Join(",", request.Headers.TryGetValues("anthropic-beta", out var beta) ? beta : [])));
            return replies.Dequeue();
        });
        var executed = new List<(FleetAction Action, ActionParameters Parameters)>();
        var bridge = new FakeBridge((device, action, parameters, token) =>
        {
            executed.Add((action, parameters));
            return Task.CompletedTask;
        });
        var client = new AnthropicClient { ApiKey = "test-key", HttpClient = new HttpClient(handler), MaxRetries = 0 };
        var steps = new List<AgentStep>();
        var agent = new PhoneAgent(client, bridge, new AgentOptions(SettleSeconds: 0),
            (device, token) => Task.FromResult(AgentScreen.FromPng(Png(750, 1334))));
        var outcome = await agent.RunAsync(Phone, "打开设置", new Progress(steps));

        Assert.True(outcome is { Success: true, Summary: "已打开设置", Actions: 1, InputTokens: 200, OutputTokens: 20 }, outcome.ToString());
        Assert.True(executed.Single() is { Action: FleetAction.Tap } tap && Math.Abs(tap.Parameters.X!.Value - 374.0 / 749) < 1e-9
            && Math.Abs(tap.Parameters.Y!.Value - 666.0 / 1333) < 1e-9, "tap mapped to relative coordinates");
        Assert.True(steps.Any(step => step is { Kind: "思考", Text: "设置图标在中间" }) && steps.Any(step => step.Kind == "提示"), "progress");

        var first = requests[0].Body;
        Assert.Equal(PhoneAgent.DefaultModel, first.GetProperty("model").GetString());
        Assert.Equal("default", first.GetProperty("fallbacks").GetString());
        Assert.True(requests[0].Beta.Contains("server-side-fallback-2026-07-01"), requests[0].Beta);
        Assert.Equal(10, first.GetProperty("tools").GetArrayLength());
        Assert.Equal("image", first.GetProperty("messages")[0].GetProperty("content")[0].GetProperty("type").GetString());

        // 第二次请求：原样回传思考块签名；两个工具结果，越界的标为错误，新截图附在最后一个结果里。
        var history = requests[1].Body.GetProperty("messages");
        Assert.Equal(3, history.GetArrayLength());
        var echo = history[1].GetProperty("content");
        Assert.True(echo[0].GetProperty("type").GetString() == "thinking" && echo[0].GetProperty("signature").GetString() == "sig-1", "thinking echoed");
        var results = history[2].GetProperty("content");
        Assert.True(results[0].GetProperty("tool_use_id").GetString() == "toolu_1" && !results[0].GetProperty("is_error").GetBoolean(), "ok result");
        Assert.True(results[1].GetProperty("is_error").GetBoolean(), "out-of-range result flagged");
        Assert.Equal("image", results[1].GetProperty("content")[1].GetProperty("type").GetString());

        // 拒答：直接结束并给出原因，不执行任何操作。
        replies.Enqueue(Reply("[]", "refusal"));
        executed.Clear();
        var refused = await agent.RunAsync(Phone, "做点什么", new Progress(steps));
        Assert.True(!refused.Success && refused.Summary.Contains("拒绝") && executed.Count == 0, refused.Summary);
    }

    private static string Reply(string content, string stopReason) => $$$"""
        {"id":"msg_x","type":"message","role":"assistant","model":"claude-opus-5-5","content":{{{content}}},
         "stop_reason":"{{{stopReason}}}","stop_sequence":null,"usage":{"input_tokens":100,"output_tokens":10}}
        """;

    private static byte[] Png(int width, int height)
    {
        var png = new byte[33];
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 }.CopyTo(png, 0);
        BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(16), width);
        BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(20), height);
        return png;
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, Task<string>> reply) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(await reply(request), Encoding.UTF8, "application/json")
            };
    }

    private sealed class Progress(List<AgentStep> steps) : IProgress<AgentStep>
    {
        public void Report(AgentStep value) => steps.Add(value);
    }
}
