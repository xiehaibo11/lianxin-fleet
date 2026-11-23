using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Checks.Scenarios;

// 界面元素：WDA 协议（假 HTTP，不连真机）、谓词转义、界面树整理，以及脚本里的元素命令。
internal static class ElementChecks
{
    public static async Task Protocol()
    {
        var device = new FleetDevice(1, "Phone", "00008110-001234567890001E", 18100);
        var requests = new ConcurrentQueue<(string Path, string Query, string Body)>();
        using var client = new HttpClient(new Handler(async (request, token) =>
        {
            var uri = request.RequestUri!;
            Assert.True(uri.Host == "127.0.0.1" && uri.Port == 18100, "only the forwarded loopback port");
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token);
            requests.Enqueue((uri.AbsolutePath, uri.Query, body));
            object? value = uri.AbsolutePath switch
            {
                "/session" => new { sessionId = "s-1" },
                "/session/s-1/elements" => new object[]
                {
                    new { ELEMENT = "0B000000-0000-0000-0001-000000000000" },
                    new Dictionary<string, string> { ["element-6066-11e4-a52e-4f735466cecf"] = "0B000000-0000-0000-0002-000000000000" },
                    new { ELEMENT = "../../wda/shutdown" },
                    new { other = "x" }
                },
                "/session/s-1/element/0B000000-0000-0000-0001-000000000000/rect" => new { x = 10, y = 20.5, width = 100, height = 44 },
                "/session/s-1/source" => "<XCUIElementTypeApplication type=\"XCUIElementTypeApplication\"/>",
                _ => null
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { value })) };
        }));
        using var bridge = new DeviceBridge("C:\\Python\\python.exe", client, "win32", new RecordingRunner(_ => RecordingRunner.Ok()));

        var tricky = new ElementSelector(ElementSelectorKind.Text, "it's \"ok\" \\ 100% %@ $x");
        var ids = await bridge.FindElementsAsync(device, tricky);
        Assert.True(ids.SequenceEqual(["0B000000-0000-0000-0001-000000000000", "0B000000-0000-0000-0002-000000000000"]), "invalid element ids are dropped");
        using (var find = JsonDocument.Parse(requests.Last().Body))
        {
            Assert.Equal("predicate string", find.RootElement.GetProperty("using").GetString());
            const string literal = "'it\\'s \"ok\" \\\\ 100% %@ $x'";
            Assert.Equal($"label == {literal} OR name == {literal} OR value == {literal}", find.RootElement.GetProperty("value").GetString());
        }
        Assert.Equal(("accessibility id", "login"), DeviceBridge.Locator(new ElementSelector(ElementSelectorKind.Id, "login")));
        Assert.Equal(("class name", "XCUIElementTypeButton"), DeviceBridge.Locator(new ElementSelector(ElementSelectorKind.Type, "button")));
        Assert.Equal(("xpath", "//*[@name='a']"), DeviceBridge.Locator(new ElementSelector(ElementSelectorKind.XPath, "//*[@name='a']")));
        Assert.Equal(("predicate string", "label CONTAINS '登' OR name CONTAINS '登' OR value CONTAINS '登'"),
            DeviceBridge.Locator(new ElementSelector(ElementSelectorKind.Contains, "登")));
        Assert.Throws<ArgumentException>(() => DeviceBridge.Locator(new ElementSelector(ElementSelectorKind.Text, "a\nb")));

        var bounds = await bridge.GetElementBoundsAsync(device, ids[0]);
        Assert.Equal(new ElementBounds(10, 20.5, 100, 44), bounds);
        await bridge.ClickElementAsync(device, ids[0]);
        Assert.Equal($"/session/s-1/element/{ids[0]}/click", requests.Last().Path);
        await bridge.SetElementValueAsync(device, ids[1], "你好 world");
        using (var value = JsonDocument.Parse(requests.Last().Body))
            Assert.Equal("你好 world", value.RootElement.GetProperty("value")[0].GetString());
        Assert.True((await bridge.GetPageSourceAsync(device)).StartsWith("<XCUIElementTypeApplication", StringComparison.Ordinal), "page source");
        Assert.Equal("?format=xml", requests.Last().Query);
        var before = requests.Count;
        await Assert.ThrowsAsync<ArgumentException>(() => bridge.ClickElementAsync(device, "../../wda/shutdown"));
        await Assert.ThrowsAsync<ArgumentException>(() => bridge.SetElementValueAsync(device, ids[0], ""));
        Assert.Equal(before, requests.Count);
        Assert.Equal(1, requests.Count(r => r.Path == "/session"));

        // 没有实现元素接口的设备通道（旧的假设备）得到明确的不支持错误。
        IDeviceBridge legacy = new FakeBridge();
        await Assert.ThrowsAsync<NotSupportedException>(() => legacy.FindElementsAsync(device, tricky));
        await Assert.ThrowsAsync<NotSupportedException>(() => legacy.GetPageSourceAsync(device));
    }

    public static Task PageSource()
    {
        var elements = ScriptParser.DescribeElements("""
            <?xml version="1.0" encoding="UTF-8"?>
            <XCUIElementTypeApplication type="XCUIElementTypeApplication" name="设置" label="设置" visible="true" x="0" y="0" width="375" height="667">
              <XCUIElementTypeWindow type="XCUIElementTypeWindow" visible="true" x="0" y="0" width="375" height="667">
                <XCUIElementTypeButton type="XCUIElementTypeButton" name="loginButton" label="立即 登录" visible="true" x="20" y="500" width="335" height="44"/>
                <XCUIElementTypeStaticText type="XCUIElementTypeStaticText" name="欢迎" label="欢迎" visible="true" x="20" y="100" width="100" height="20"/>
                <XCUIElementTypeTextField type="XCUIElementTypeTextField" value="请输入手机号" visible="true" x="20" y="200" width="335" height="40"/>
                <XCUIElementTypeSecureTextField type="XCUIElementTypeSecureTextField" visible="true" x="20" y="260" width="335" height="40"/>
                <XCUIElementTypeStaticText type="XCUIElementTypeStaticText" label="隐藏" visible="false" x="0" y="0" width="10" height="10"/>
                <XCUIElementTypeOther type="XCUIElementTypeOther" visible="true" x="0" y="0" width="375" height="667"/>
              </XCUIElementTypeWindow>
            </XCUIElementTypeApplication>
            """);
        Assert.Equal(4, elements.Count);
        Assert.True(elements[0].Selector == "id=loginButton" && elements[0].Label == "立即 登录" && elements[0].Y == 500, elements[0].Selector);
        Assert.Equal("文字=欢迎", elements[1].Selector);
        Assert.Equal("文字=请输入手机号", elements[2].Selector);
        Assert.Equal("类型=SecureTextField", elements[3].Selector);
        foreach (var element in elements)
            ScriptParser.Parse("点击元素 " + element.Selector);
        Assert.Equal(2, ScriptParser.DescribeElements(string.Concat(Enumerable.Repeat(
            "<a type=\"XCUIElementTypeButton\" label=\"x\" visible=\"true\" width=\"1\" height=\"1\"/>", 5)).Insert(0, "<r>") + "</r>", 2).Count);
        Assert.Throws<ArgumentException>(() => ScriptParser.DescribeElements("<!DOCTYPE x [<!ENTITY e \"boom\">]><x>&e;</x>"));
        Assert.Equal(0, ScriptParser.DescribeElements("").Count);
        return Task.CompletedTask;
    }

    public static async Task Commands()
    {
        using var store = new FleetStore(":memory:");
        var device = store.RegisterDevice("Phone", "00008030-0012345678900001", 8101);
        var bridge = new ScriptBridge();
        await using var runner = new ScriptRunner(store, bridge);
        int polls = 0, loading = 0;
        bridge.Find = selector => selector.Value switch
        {
            "登录" => ++polls >= 3 ? ["el-login"] : [],
            "加载中" => ++loading <= 2 ? ["el-loading"] : [],
            "user" => ["el-user"],
            _ => []
        };
        var script = store.SaveScript(null, "元素", """
            等待元素 文字=登录 5
            点击元素 文字=登录
            输入到 id=user 张三 1
            等待消失 文字=加载中 5
            如果 存在 文字=没有
                主屏
            否则
                锁屏
            结束
            """, 1);
        runner.Start(script.Id, [device.Id], "手动");
        await ScriptChecks.WaitFor(() => runner.RunningCount == 0);
        var run = store.GetLatestScriptRuns()[device.Id];
        Assert.True(run.State == ScriptRunState.Succeeded, run.Message ?? "");
        var calls = bridge.Calls.ToArray();
        Assert.True(calls.Count(call => call == "find 文字=登录") == 4 && calls.Count(call => call == "find 文字=加载中") == 3,
            string.Join(" | ", calls));
        Assert.True(calls.Contains("click el-login") && calls.Contains("value el-user 张三 1") && calls.Contains("lock") && !calls.Contains("home"),
            string.Join(" | ", calls));

        bridge.Calls.Clear();
        script = store.SaveScript(null, "超时", "等待元素 文字=永远没有 1", 1);
        var started = DateTimeOffset.UtcNow;
        runner.Start(script.Id, [device.Id], "手动");
        await ScriptChecks.WaitFor(() => runner.RunningCount == 0);
        run = store.GetLatestScriptRuns()[device.Id];
        Assert.True(run.State == ScriptRunState.Failed && run.Message!.Contains("等待 1 秒后仍未出现")
            && run.Message.Contains("第 1/1 步"), run.Message ?? "");
        Assert.True(DateTimeOffset.UtcNow - started >= TimeSpan.FromSeconds(1) && bridge.Count("find") is >= 2 and <= 4, "polls about twice a second");

        script = store.SaveScript(null, "输入不存在", "输入到 文字=没有 hi", 1);
        runner.Start(script.Id, [device.Id], "手动");
        await ScriptChecks.WaitFor(() => runner.RunningCount == 0);
        Assert.True(store.GetLatestScriptRuns()[device.Id].Message!.Contains("没有找到该元素"), "input target missing");
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
