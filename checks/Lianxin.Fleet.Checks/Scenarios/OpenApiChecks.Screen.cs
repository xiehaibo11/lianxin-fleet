using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.OpenApi;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class OpenApiChecks
{
    private const string Tree = """
        <?xml version="1.0" encoding="UTF-8"?>
        <XCUIElementTypeApplication type="XCUIElementTypeApplication" name="设置" label="设置" enabled="true" visible="true" x="0" y="0" width="390" height="844">
          <XCUIElementTypeWindow type="XCUIElementTypeWindow" enabled="true" visible="true" x="0" y="0" width="390" height="844">
            <XCUIElementTypeOther type="XCUIElementTypeOther" enabled="true" visible="true" x="0" y="0" width="390" height="844">
              <XCUIElementTypeButton type="XCUIElementTypeButton" name="login" label="登录" enabled="true" visible="true" x="20.5" y="500" width="335" height="44"/>
              <XCUIElementTypeStaticText type="XCUIElementTypeStaticText" label="隐藏" enabled="true" visible="false" x="0" y="0" width="10" height="10"/>
            </XCUIElementTypeOther>
            <XCUIElementTypeStaticText type="XCUIElementTypeStaticText" label="标题" enabled="true" visible="true" x="0" y="44" width="390" height="20"/>
          </XCUIElementTypeWindow>
        </XCUIElementTypeApplication>
        """;

    // 锁屏状态、设置方向后坐标系跟随新方向（比例沿用缓存）、方向锁定时失败，以及抓取节点的过滤与像素换算。
    public static async Task ScreenAndNodes()
    {
        await using var api = await ApiHarness.StartAsync();
        var bridge = api.Bridge;
        var one = J(("deviceId", Udid));
        bridge.Locked = true;
        Assert.True((await api.OkAsync("/openapi/isLocked", one)).GetBoolean(), "locked");
        bridge.Locked = false;
        Assert.True(!(await api.OkAsync("/openapi/isLocked", one)).GetBoolean(), "unlocked");

        // 竖屏 390×844 点、截图 3 倍。转到横屏后点击坐标按 2532×1170 像素换算，不重新截图测比例。
        Assert.True((await api.PostAsync("/openapi/setOrientation", J(("deviceId", Udid), ("orientation", 2)))).Msg.Contains("横屏"), "landscape");
        Assert.True(bridge.Rotations.SequenceEqual([ScreenOrientation.Landscape]) && bridge.Status is { Width: 844, Height: 390 }, "rotated");
        Assert.Equal(2, (await api.OkAsync("/openapi/getOrientation", one)).GetInt32());
        await api.OkAsync("/openapi/clickPoint", J(("deviceId", Udid), ("x", 2531), ("y", 1169)));
        Assert.True(bridge.Last().Parameters is { X: 1.0, Y: 1.0 }, "landscape corner");
        await api.OkAsync("/openapi/clickPoint", J(("deviceId", Udid), ("x", 1266), ("y", 585)));
        Assert.True(bridge.Last().Parameters.X == 422.0 / 843 && bridge.Last().Parameters.Y == 195.0 / 389, "landscape centre");
        var info = await api.OkAsync("/openapi/getDeviceInfo", one);
        Assert.True(info.GetProperty("screenWidth").GetInt32() == 2532 && info.GetProperty("screenHeight").GetInt32() == 1170
            && info.GetProperty("orientation").GetInt32() == 2, info.ToString());
        Assert.Equal(1, bridge.ScreenshotCount);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/clickPoint", J(("deviceId", Udid), ("x", 10), ("y", 1171)))).Code);
        await api.OkAsync("/openapi/setOrientation", J(("deviceId", Udid), ("orientation", "1")));
        Assert.True(bridge.Status is { Width: 390, Height: 844 }, "back to portrait");
        await api.OkAsync("/openapi/clickPoint", J(("deviceId", Udid), ("x", 585), ("y", 1266)));
        Assert.True(bridge.Last().Parameters.X == 195.0 / 389 && bridge.Last().Parameters.Y == 422.0 / 843, "portrait centre");
        foreach (var bad in new object?[] { 0, 3, "横", null })
            Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/setOrientation", J(("deviceId", Udid), ("orientation", bad)))).Code);
        Assert.Equal(2, bridge.Rotations.Count);
        // 界面不跟随旋转（应用只支持竖屏或开启了方向锁定）：等待后明确失败，坐标系保持竖屏。
        bridge.RotationLocked = true;
        var locked = await api.PostAsync("/openapi/setOrientation", J(("deviceId", Udid), ("orientation", 2)));
        Assert.True(locked.Code == OpenApiCodes.DeviceFailed && locked.Msg.Contains("方向锁定"), locked.Text);
        Assert.Equal(1, (await api.OkAsync("/openapi/getOrientation", one)).GetInt32());
        bridge.RotationLocked = false;
        bridge.Status = new(false, Error: "WDA 尚未就绪。");
        Assert.Equal(OpenApiCodes.DeviceFailed, (await api.PostAsync("/openapi/setOrientation", J(("deviceId", Udid), ("orientation", 2)))).Code);
        Assert.Equal(3, bridge.Rotations.Count);
        bridge.Status = new(true, 390, 844);
        bridge.Unsupported = true;
        var unsupported = await api.PostAsync("/openapi/isLocked", one);
        Assert.True(unsupported.Code == OpenApiCodes.Unsupported && unsupported.Msg.StartsWith("联信 Fleet 暂不支持", StringComparison.Ordinal), unsupported.Text);
        bridge.Unsupported = false;

        // 抓取节点：成功时响应体是 XML；坐标换算成截图像素，过滤后保留满足条件的子节点。
        bridge.PageSource = Tree;
        var full = await api.PostAsync("/openapi/dumpXml", one);
        Assert.True(full.Status == 200 && full.Headers["Content-Type"] == "application/xml; charset=utf-8"
            && full.Text.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", StringComparison.Ordinal), full.Text);
        var all = XDocument.Parse(full.Text).Root!;
        Assert.True(all.Attribute("width")!.Value == "1170" && all.Attribute("height")!.Value == "2532", all.ToString());
        Assert.Equal(6, all.DescendantsAndSelf().Count());
        var filtered = XDocument.Parse((await api.PostAsync("/openapi/dumpXml", J(("deviceId", Udid), ("visibleFilter", "2"),
            ("labelFilter", "2"), ("maxDepth", "50"), ("excludedAttributes", "visible, Enabled")))).Text).Root!;
        Assert.True(filtered.Elements().Select(item => (string?)item.Attribute("label")).SequenceEqual(["登录", "标题"]), filtered.ToString());
        var button = filtered.Elements().First();
        Assert.True((string?)button.Attribute("x") == "62" && (string?)button.Attribute("y") == "1500" && (string?)button.Attribute("width") == "1005"
            && button.Attribute("visible") is null && button.Attribute("enabled") is null && (string?)button.Attribute("name") == "login", button.ToString());
        var shallow = XDocument.Parse((await api.PostAsync("/openapi/dumpXml", J(("deviceId", Udid), ("maxDepth", 3)))).Text).Root!;
        Assert.True(shallow.DescendantsAndSelf().Count() == 4 && !shallow.Descendants("XCUIElementTypeButton").Any(), shallow.ToString());
        foreach (var (name, value) in new[] { ("visibleFilter", "3"), ("labelFilter", "yes"), ("maxDepth", "-1"), ("maxDepth", "abc"), ("maxDepth", "5000") })
            Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/dumpXml", J(("deviceId", Udid), (name, value)))).Code);
        bridge.PageSource = "<XCUIElementTypeApplication";
        Assert.Equal(OpenApiCodes.DeviceFailed, (await api.PostAsync("/openapi/dumpXml", one)).Code);
        bridge.PageSource = "<!DOCTYPE x [<!ENTITY e SYSTEM \"file:///c:/windows/win.ini\">]><x>&e;</x>";
        Assert.Equal(OpenApiCodes.DeviceFailed, (await api.PostAsync("/openapi/dumpXml", one)).Code);
    }

    // DeviceBridge：锁屏查询为不带会话的 GET /wda/locked；旋转为会话内 POST orientation，方向值为 PORTRAIT / LANDSCAPE。
    public static async Task BridgeScreen()
    {
        var device = new FleetDevice(1, "Phone", "00008110-001234567890001E", 18100);
        var requests = new ConcurrentQueue<(string Method, string Path, string Body)>();
        object? locked = true;
        var reject = false;
        using var client = new HttpClient(new WdaHandler(async (request, token) =>
        {
            var uri = request.RequestUri!;
            Assert.True(uri.Host == "127.0.0.1" && uri.Port == 18100, "only the forwarded loopback port");
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token);
            requests.Enqueue((request.Method.Method, uri.AbsolutePath, body));
            var post = request.Method == HttpMethod.Post;
            object? value = uri.AbsolutePath switch
            {
                "/wda/locked" => locked,
                "/session" => new { sessionId = "s-1" },
                "/session/s-1/orientation" when !post => "PORTRAIT",
                "/session/s-1/orientation" when reject && body.Contains("LANDSCAPE") => new { error = "unknown error", message = "Unable To Rotate Device" },
                _ => null
            };
            return new HttpResponseMessage(reject && post && body.Contains("LANDSCAPE") && uri.AbsolutePath.EndsWith("/orientation", StringComparison.Ordinal)
                ? HttpStatusCode.InternalServerError : HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { value }), Encoding.UTF8, "application/json")
            };
        }));
        using var bridge = new DeviceBridge("C:\\Python\\python.exe", client, "win32", new RecordingRunner(_ => RecordingRunner.Ok()));
        Assert.True(await bridge.IsLockedAsync(device), "locked");
        Assert.Equal(("GET", "/wda/locked", ""), requests.Last());
        locked = false;
        Assert.True(!await bridge.IsLockedAsync(device), "unlocked");
        locked = "yes";
        await Assert.ThrowsAsync<DeviceOperationException>(() => bridge.IsLockedAsync(device));
        Assert.True(requests.All(item => item.Path == "/wda/locked"), "no session needed to query the lock");

        await bridge.SetOrientationAsync(device, ScreenOrientation.Landscape);
        var rotate = requests.Last();
        Assert.True(rotate.Method == "POST" && rotate.Path == "/session/s-1/orientation", rotate.ToString());
        using (var landscape = JsonDocument.Parse(rotate.Body))
            Assert.Equal("LANDSCAPE", landscape.RootElement.GetProperty("orientation").GetString());
        await bridge.SetOrientationAsync(device, ScreenOrientation.Portrait);
        using (var portrait = JsonDocument.Parse(requests.Last().Body))
            Assert.Equal("PORTRAIT", portrait.RootElement.GetProperty("orientation").GetString());
        var before = requests.Count;
        await Assert.ThrowsAsync<ArgumentException>(() => bridge.SetOrientationAsync(device, (ScreenOrientation)7));
        Assert.Equal(before, requests.Count);
        reject = true;
        var refused = await Assert.ThrowsAsync<DeviceOperationException>(() => bridge.SetOrientationAsync(device, ScreenOrientation.Landscape));
        Assert.True(refused.Message.Contains("Unable To Rotate Device") && !refused.Uncertain, refused.Message);
        // 被拒绝的横屏请求要撤销：按旋转前读到的方向再设一次，否则下一个应用会以横屏打开。
        var undo = requests.Last();
        Assert.True(undo.Method == "POST" && undo.Path == "/session/s-1/orientation" && undo.Body.Contains("PORTRAIT"), undo.ToString());

        // 没有实现这两项的旧通道得到明确的不支持错误。
        IDeviceBridge legacy = new FakeBridge();
        await Assert.ThrowsAsync<NotSupportedException>(() => legacy.IsLockedAsync(device));
        await Assert.ThrowsAsync<NotSupportedException>(() => legacy.SetOrientationAsync(device, ScreenOrientation.Landscape));
    }

    private sealed class WdaHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
