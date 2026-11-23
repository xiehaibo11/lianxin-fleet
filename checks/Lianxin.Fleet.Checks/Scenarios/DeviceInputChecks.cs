using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Checks.Scenarios;

// 真机（iPhone 6s、iOS 15.8.8）上发现的控制问题：返回手势、多任务、无焦点输入、旧系统打开链接。
internal static class DeviceInputChecks
{
    private sealed class FakeWda
    {
        public ConcurrentQueue<(string Method, string Path, string Body)> Requests { get; } = new();
        public (int Width, int Height) Size { get; set; } = (375, 667);
        public bool Focused
        {
            get; set;
        }
        public bool Keyboard
        {
            get; set;
        }
        public bool OldIos
        {
            get; set;
        }
        public int AppState { get; set; } = 4;
        public bool SizeFails
        {
            get; set;
        }

        public HttpClient Client() => new(new Handler(async (request, token) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token);
            Requests.Enqueue((request.Method.Method, path, body));
            if (path.EndsWith("/wda/keys", StringComparison.Ordinal))
                Focused = Keyboard = false;
            if (path.EndsWith("/element/bar/click", StringComparison.Ordinal))
                Focused = Keyboard = true;
            object? value = path switch
            {
                "/session" => new { sessionId = "s-1" },
                "/status" => new { ready = true },
                "/wda/device/info" => new { name = "Phone" },
                "/window/size" when SizeFails => Error("unknown error", "snapshot timed out"),
                "/window/size" => new { width = Size.Width, height = Size.Height },
                "/session/s-1/url" when OldIos => Error("unknown error", "Error Domain=com.apple.dt.xctest.ui-testing.error Code=10401 \"Device does not have the capability to open the default app from a url\""),
                "/session/s-1/element/active" => Focused ? (object)new Dictionary<string, string> { ["ELEMENT"] = "field" } : Error("no such element", "unable to find an element"),
                "/session/s-1/elements" when body.Contains("XCUIElementTypeKeyboard") => Keyboard ? new[] { new Dictionary<string, string> { ["ELEMENT"] = "kb" } } : Array.Empty<object>(),
                "/session/s-1/elements" when body.Contains("TabBarItemTitle") => new[] { new Dictionary<string, string> { ["ELEMENT"] = "bar" } },
                "/session/s-1/wda/apps/state" => AppState,
                _ => null
            };
            var failed = value is Dictionary<string, object> map && map.ContainsKey("error");
            return new HttpResponseMessage(failed ? HttpStatusCode.NotFound : HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { value }), Encoding.UTF8, "application/json")
            };
        }));

        private static Dictionary<string, object> Error(string error, string message) => new() { ["error"] = error, ["message"] = message };

        public int Count(string suffix) => Requests.Count(item => item.Path.EndsWith(suffix, StringComparison.Ordinal));
        public (string Method, string Path, string Body) Last(string suffix) => Requests.Last(item => item.Path.EndsWith(suffix, StringComparison.Ordinal));
    }

    public static async Task Run()
    {
        var device = new FleetDevice(1, "Phone", "00008110-001234567890001E", 18100);
        var wda = new FakeWda();
        using var client = wda.Client();
        using var bridge = new DeviceBridge("C:\\Python\\python.exe", client, "win32", new RecordingRunner(_ => RecordingRunner.Ok()));

        // 主屏：直接按 Home 键（/wda/homescreen 在真机上会让 XCTest 卡住约 60 秒）。
        var clock = System.Diagnostics.Stopwatch.StartNew();
        await bridge.ExecuteAsync(device, FleetAction.Home, new ActionParameters());
        Assert.True(wda.Count("/wda/homescreen") == 0 && wda.Last("/wda/pressButton").Body.Contains("\"home\""), "Home presses the hardware button");
        // 回主屏动画结束前紧接着启动应用会被吞掉，Home 在设备锁内等动画结束再返回。
        Assert.True(clock.ElapsedMilliseconds >= 550, $"Home settled after {clock.ElapsedMilliseconds} ms");

        // 返回：从左边缘开始的滑动走 XCUICoordinate 拖动；普通滑动仍用 W3C 动作。
        await bridge.ExecuteAsync(device, FleetAction.Swipe, new ActionParameters(FromX: 0, FromY: .5, ToX: .7, ToY: .5, Duration: .05));
        using (var drag = JsonDocument.Parse(wda.Last("/wda/pressAndDragWithVelocity").Body))
        {
            var root = drag.RootElement;
            Assert.True(root.GetProperty("fromX").GetInt32() == 0 && root.GetProperty("toX").GetInt32() == 262, drag.RootElement.ToString());
            var velocity = root.GetProperty("velocity").GetDouble();
            Assert.True(velocity is >= 400 and <= 2500, $"velocity {velocity}");
        }
        var actions = wda.Count("/actions");
        await bridge.ExecuteAsync(device, FleetAction.Swipe, new ActionParameters(FromX: .5, FromY: .8, ToX: .5, ToY: .2, Duration: .3));
        Assert.True(wda.Count("/actions") == actions + 1, "Ordinary swipes keep W3C actions");
        await bridge.ExecuteAsync(device, FleetAction.Swipe, new ActionParameters(FromX: 1, FromY: .5, ToX: .3, ToY: .5, Duration: .3));
        Assert.True(wda.Count("/wda/pressAndDragWithVelocity") == 2, "Right-edge swipes are edge drags too");

        // 多任务：有 Home 键的机型明确拒绝，不发送会打开控制中心的上滑。
        var drags = wda.Count("/wda/pressAndDragWithVelocity");
        var refused = await Assert.ThrowsAsync<DeviceOperationException>(() =>
            bridge.ExecuteAsync(device, FleetAction.AppSwitcher, new ActionParameters()));
        Assert.True(refused.Message.Contains("Home 键") && !refused.Uncertain && wda.Count("/wda/pressAndDragWithVelocity") == drags, refused.Message);
        wda.Size = (390, 844);
        await bridge.ExecuteAsync(device, FleetAction.AppSwitcher, new ActionParameters());
        Assert.True(wda.Count("/wda/pressAndDragWithVelocity") == drags + 1, "Full-screen iPhones keep the swipe-up gesture");
        Assert.True(DeviceBridge.HasHomeButton((375, 667)) && DeviceBridge.HasHomeButton((414, 736)) && DeviceBridge.HasHomeButton((320, 568))
            && !DeviceBridge.HasHomeButton((375, 812)) && !DeviceBridge.HasHomeButton((844, 390)) && !DeviceBridge.HasHomeButton((1024, 768)), "home button sizes");
        wda.Size = (375, 667);

        // 输入：没有焦点也没有键盘时立即失败，不发送会让 WDA 等一分钟的 /wda/keys。
        var noFocus = await Assert.ThrowsAsync<DeviceOperationException>(() =>
            bridge.ExecuteAsync(device, FleetAction.Text, new ActionParameters(Text: "你好")));
        Assert.True(noFocus.Message.Contains("输入框") && wda.Count("/wda/keys") == 0, noFocus.Message);
        wda.Focused = true;
        await bridge.ExecuteAsync(device, FleetAction.Text, new ActionParameters(Text: "你好"));
        Assert.True(wda.Count("/wda/keys") == 1, "Focused field receives text");
        wda.Keyboard = true;
        await bridge.ExecuteAsync(device, FleetAction.Text, new ActionParameters(Text: "第三方键盘"));
        Assert.True(wda.Count("/wda/keys") == 2, "A visible keyboard without a reported focus is enough");

        // 关闭应用：没运行就不发送；后台挂起的先切到前台再结束（WDA 刚启动时直接结束会让 XCTest 卡 60 秒）。
        var settings = new ActionParameters(BundleId: "com.apple.Preferences");
        wda.AppState = 1;
        await bridge.ExecuteAsync(device, FleetAction.Terminate, settings);
        Assert.True(wda.Count("/wda/apps/terminate") == 0 && wda.Count("/wda/apps/launch") == 0, "Not-running apps need no request");
        wda.AppState = 2;
        await bridge.ExecuteAsync(device, FleetAction.Terminate, settings);
        var order = wda.Requests.Select(item => item.Path).Where(path => path.Contains("/wda/apps/", StringComparison.Ordinal)).ToArray();
        Assert.True(order.TakeLast(3).SequenceEqual(["/session/s-1/wda/apps/state", "/session/s-1/wda/apps/launch", "/session/s-1/wda/apps/terminate"]),
            string.Join(", ", order));
        wda.AppState = 4;
        await bridge.ExecuteAsync(device, FleetAction.Terminate, settings);
        Assert.True(wda.Count("/wda/apps/launch") == 1 && wda.Count("/wda/apps/terminate") == 2, "Foreground apps are terminated directly");

        // 打开链接：iOS 16.4 以上直接 /url；更早的系统经 Safari 地址栏输入并回车。
        await bridge.ExecuteAsync(device, FleetAction.OpenUrl, new ActionParameters(Text: "https://example.com"));
        Assert.True(wda.Count("/session/s-1/url") == 1 && wda.Count("/wda/apps/launch") == 1, "New iOS opens URLs directly");
        wda.OldIos = true;
        await bridge.ExecuteAsync(device, FleetAction.OpenUrl, new ActionParameters(Text: "https://example.com/a?b=1"));
        Assert.True(wda.Last("/wda/apps/launch").Body.Contains("com.apple.mobilesafari"), "Old iOS launches Safari");
        Assert.True(wda.Count("/element/bar/click") == 1, "Address bar is tapped");
        using (var typed = JsonDocument.Parse(wda.Last("/wda/keys").Body))
            Assert.Equal("https://example.com/a?b=1\n", typed.RootElement.GetProperty("value")[0].GetString());
        Assert.True(wda.Count("/alert/accept") == 0, "http links never touch alerts");

        // 旧系统上的自定义协议：Safari 的确认框 WDA 点不到，启动 Safari 之前就明确拒绝，也不碰任何弹窗。
        var launches = wda.Count("/wda/apps/launch");
        var scheme = await Assert.ThrowsAsync<DeviceOperationException>(() =>
            bridge.ExecuteAsync(device, FleetAction.OpenUrl, new ActionParameters(Text: "weixin://dl/scan")));
        Assert.True(scheme.Message.Contains("启动 BundleID") && !scheme.Uncertain && wda.Count("/wda/apps/launch") == launches
            && wda.Count("/alert/accept") == 0, scheme.Message);
        wda.OldIos = false;
        await bridge.ExecuteAsync(device, FleetAction.OpenUrl, new ActionParameters(Text: "weixin://dl/scan"));
        Assert.True(wda.Count("/wda/apps/launch") == launches, "iOS 16.4+ opens custom schemes directly");

        // 状态探测：/status 正常而手机忙得来不及回答尺寸时，沿用上次的尺寸，不把能控制的手机报成离线；
        // 从没读到过尺寸的手机仍报错。
        wda.SizeFails = true;
        await Assert.ThrowsAsync<DeviceOperationException>(() => bridge.GetStatusAsync(device with { Id = 2, WdaPort = 18102 }));
        wda.SizeFails = false;
        var first = await bridge.GetStatusAsync(device);
        wda.SizeFails = true;
        var busy = await bridge.GetStatusAsync(device);
        Assert.True(first.Online && busy.Online && busy.Width == 375 && busy.Height == 667 && busy.DeviceName == "Phone", busy.ToString());
        wda.SizeFails = false;
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
