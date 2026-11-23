using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.OpenApi;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class OpenApiChecks
{
    // Node.js fetch 的请求头、正文上限与同时接收的正文总量、请求行限时、每个地址的连接数、503 回复上限、不支持接口不解析正文。
    public static async Task Hardening()
    {
        Assert.Equal(1024 * 1024, new OpenApiOptions().MaxBodyBytes);
        await using (var api = await ApiHarness.StartAsync())
        {
            // Node 18+ 自带的 fetch（undici）的请求头：总带 sec-fetch-mode: cors，但没有 Origin 和 Sec-Fetch-Site。
            var node = await api.PostAsync("/openapi/deviceList", null,
                "accept: */*\r\naccept-language: *\r\nsec-fetch-mode: cors\r\nuser-agent: node\r\naccept-encoding: gzip, deflate\r\n");
            Assert.True(node.Status == 200 && node.Code == 0, node.Text);
            Assert.Equal(403, (await api.PostAsync("/openapi/deviceList", null, "sec-fetch-mode: cors\r\nsec-fetch-site: cross-site\r\n")).Status);
            var large = await RawAsync(api.Port, Encoding.ASCII.GetBytes(
                $"POST /openapi/deviceList HTTP/1.1\r\nHost: 127.0.0.1:{api.Port}\r\nContent-Type: application/json\r\nContent-Length: {1024 * 1024 + 1}\r\n\r\n{{"));
            Assert.Equal(413, large.Status);
            // 不支持的接口不看参数：正文不是 JSON 也只回 2001。
            Assert.Equal(OpenApiCodes.Unsupported, (await api.PostAsync("/openapi/bleClickPoint", "{not json")).Code);
        }

        // 连接后不发请求行：限时到了就回 408，不会占着连接直到请求头超时。
        await using (var api = await ApiHarness.StartAsync(options => options with
        {
            RequestLineTimeout = TimeSpan.FromMilliseconds(300),
            HeaderTimeout = TimeSpan.FromSeconds(20)
        }))
        {
            var timer = Stopwatch.StartNew();
            using var silent = new TcpClient();
            await silent.ConnectAsync(IPAddress.Loopback, api.Port);
            Assert.Equal(408, (await ReadResponseAsync(silent.GetStream(), TimeSpan.FromSeconds(10))).Status);
            using var dribble = new TcpClient();
            await dribble.ConnectAsync(IPAddress.Loopback, api.Port);
            await dribble.GetStream().WriteAsync("POST /openapi/devi"u8.ToArray());
            Assert.Equal(408, (await ReadResponseAsync(dribble.GetStream(), TimeSpan.FromSeconds(10))).Status);
            Assert.True(timer.ElapsedMilliseconds < 5000, $"request line deadline took {timer.ElapsedMilliseconds} ms");
        }

        // 同一地址最多 2 个连接（检查只能从回环地址连，打开回环地址的限制）；第 3 个回 503，旧连接释放后恢复。
        await using (var api = await ApiHarness.StartAsync(options => options with
        {
            LimitLoopbackPerAddress = true,
            MaxConnectionsPerAddress = 2,
            RequestLineTimeout = TimeSpan.FromSeconds(10)
        }))
        {
            var idle = new[] { new TcpClient(), new TcpClient() };
            foreach (var client in idle)
                await client.ConnectAsync(IPAddress.Loopback, api.Port);
            await Task.Delay(200);
            var third = await api.PostAsync("/openapi/deviceList");
            Assert.True(third.Status == 503 && third.Msg.Contains("同一地址"), third.Text);
            foreach (var client in idle)
                client.Dispose();
            await WaitAsync(() => api.PostAsync("/openapi/deviceList").Result.Code == 0);
        }

        // 连接数已满且 503 回复数也到上限：多出来的连接直接关闭，不再回复，也不读对方的数据。
        await using (var api = await ApiHarness.StartAsync(options => options with
        {
            MaxConnections = 1,
            MaxPendingRejects = 0,
            RequestLineTimeout = TimeSpan.FromSeconds(10)
        }))
        {
            using var holder = new TcpClient();
            await holder.ConnectAsync(IPAddress.Loopback, api.Port);
            await Task.Delay(200);
            Assert.Equal(0, await ReadAllAsync(api.Port, Request("POST", "/openapi/deviceList", api.Port, "{}")));
        }

        // 同时接收的正文总量上限 4 KB：一个连接正在发 4000 字节时，另一个请求回 503；前者发完后恢复。
        await using (var api = await ApiHarness.StartAsync(options => options with
        {
            MaxBodyBytes = 4096,
            MaxInFlightBodyBytes = 4096
        }))
        {
            using var slow = new TcpClient();
            await slow.ConnectAsync(IPAddress.Loopback, api.Port);
            var stream = slow.GetStream();
            var body = "{\"deviceId\":\"" + new string('0', 4000 - 15) + "\"}";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(
                $"POST /openapi/deviceList HTTP/1.1\r\nHost: 127.0.0.1:{api.Port}\r\nContent-Type: application/json\r\nContent-Length: {body.Length}\r\n\r\n{body[..10]}"));
            await Task.Delay(300);
            var other = "{\"deviceId\":\"" + new string('0', 200) + "\"}";
            var busy = await api.PostAsync("/openapi/deviceList", other);
            Assert.True(busy.Status == 503 && busy.Msg.Contains("接收的请求过多"), busy.Text);
            await stream.WriteAsync(Encoding.ASCII.GetBytes(body[10..]));
            Assert.Equal(0, (await ReadResponseAsync(stream, TimeSpan.FromSeconds(10))).Code);
            Assert.Equal(0, (await api.PostAsync("/openapi/deviceList", other)).Code);
        }
    }

    // 处理函数里的系统异常：带本机路径的 IO 与权限错误只回通用说明（路径只写进中控日志），数据损坏和超时为 1003，不再是 500。
    public static async Task ErrorMapping()
    {
        var router = new OpenApiRouter()
            .Post("/t/io", _ => throw new IOException(@"The process cannot access the file 'C:\Users\secret\a.png'."))
            .Post("/t/denied", _ => throw new UnauthorizedAccessException(@"Access to the path 'C:\Users\secret' is denied."))
            .Post("/t/missing", _ => throw new FileNotFoundException(@"Could not find file 'C:\Users\secret\b.png'."))
            .Post("/t/data", _ => throw new InvalidDataException("截图无法解析：数据不完整。"))
            .Post("/t/timeout", _ => throw new TimeoutException("等待超时。"));
        await using var api = await ApiHarness.StartAsync(router: router);
        var logged = new List<string>();
        api.Server.Log += message =>
        {
            lock (logged)
                logged.Add(message);
        };
        foreach (var path in new[] { "/t/io", "/t/denied", "/t/missing" })
        {
            var reply = await api.PostAsync(path);
            Assert.True(reply.Status == 200 && reply.Code == OpenApiCodes.DeviceFailed && !reply.Text.Contains("secret")
                && reply.Msg.StartsWith("电脑上的文件", StringComparison.Ordinal), path + ": " + reply.Text);
        }
        lock (logged)
            Assert.True(logged.Count == 3 && logged.All(line => line.Contains("secret")), "local log keeps the details");
        foreach (var (path, text) in new[] { ("/t/data", "截图无法解析"), ("/t/timeout", "等待超时") })
        {
            var reply = await api.PostAsync(path);
            Assert.True(reply.Status == 200 && reply.Code == OpenApiCodes.DeviceFailed && reply.Msg.Contains(text), path + ": " + reply.Text);
        }
    }

    // installApp 的路径先做纯字符串判断：UNC（\\电脑\共享）、设备路径、批准目录外的路径不访问文件系统就拒绝，
    // 目录外的路径存在与否回复相同；操作做了一半失败时，回复里说明已经发生的副作用。
    public static async Task PathsAndSideEffects()
    {
        await using var api = await ApiHarness.StartAsync();
        Dictionary<string, object?> Install(string path) => J(("deviceId", Udid), ("bundleId", "a.b"), ("path", path));
        var unc = @"\\lianxin-check-unreachable.invalid\share\a.ipa";
        var timer = Stopwatch.StartNew();
        var remote = await api.PostAsync("/openapi/installApp", Install(unc));
        Assert.True(remote.Code == OpenApiCodes.InvalidArgument && remote.Msg.Contains("只能安装批准目录内的应用")
            && timer.ElapsedMilliseconds < 1000, $"{remote.Text} in {timer.ElapsedMilliseconds} ms");
        timer.Restart();
        Assert.Throws<UnauthorizedAccessException>(() => Lianxin.Fleet.Core.Validation.FleetValidation.AppPath(unc, api.Apps));
        Assert.Throws<UnauthorizedAccessException>(() => Lianxin.Fleet.Core.Validation.FleetValidation.AppPath("//lianxin-check-unreachable.invalid/share/a.ipa", api.Apps));
        Assert.True(timer.ElapsedMilliseconds < 1000, $"UNC rejected lexically in {timer.ElapsedMilliseconds} ms");
        var inside = Path.Combine(api.Apps, "x.ipa");
        File.WriteAllText(inside, "x");
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/installApp", Install(@"\\?\" + inside))).Code);
        var existing = Path.Combine(api.Root, "outside.ipa");
        File.WriteAllText(existing, "x");
        var outsideExisting = await api.PostAsync("/openapi/installApp", Install(existing));
        var outsideMissing = await api.PostAsync("/openapi/installApp", Install(Path.Combine(api.Root, "missing", "nope.ipa")));
        Assert.True(outsideExisting.Code == OpenApiCodes.InvalidArgument && outsideExisting.Msg == outsideMissing.Msg, "no existence oracle");
        Assert.Equal(0, api.Bridge.Calls.Count);

        // homeScreen：解锁成功、回主屏失败时说明手机已解锁；回主屏结果不确定时仍是 1004。
        api.Bridge.Execute = (action, _) => action == Lianxin.Fleet.Core.Domain.FleetAction.Home
            ? throw new Lianxin.Fleet.Core.Devices.DeviceOperationException("WDA 拒绝执行：测试") : Task.CompletedTask;
        var home = await api.PostAsync("/openapi/homeScreen", J(("deviceId", Udid)));
        Assert.True(home.Code == OpenApiCodes.DeviceFailed && home.Msg.StartsWith("手机已解锁", StringComparison.Ordinal), home.Text);
        api.Bridge.Execute = (action, _) => action == Lianxin.Fleet.Core.Domain.FleetAction.Home
            ? throw new Lianxin.Fleet.Core.Devices.DeviceOperationException("无法确认 WDA 响应", uncertain: true) : Task.CompletedTask;
        Assert.Equal(OpenApiCodes.Uncertain, (await api.PostAsync("/openapi/homeScreen", J(("deviceId", Udid)))).Code);
        api.Bridge.Execute = null;
    }

    // 读到对方关闭连接为止，返回收到的字节数（连接被直接关闭时为 0）。
    private static async Task<int> ReadAllAsync(int port, byte[] request)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();
        try
        {
            await stream.WriteAsync(request);
            using var output = new MemoryStream();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await stream.CopyToAsync(output, deadline.Token);
            return (int)output.Length;
        }
        catch (Exception error) when (error is IOException or SocketException)
        {
            return 0;
        }
    }
}
