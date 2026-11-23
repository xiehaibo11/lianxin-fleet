using System.Net;
using System.Net.Sockets;
using System.Text;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.OpenApi;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class OpenApiChecks
{
    // 请求解析上限：411（分块/缺长度）、413、431、400、404、405、408 与 100-continue。
    public static async Task Protocol()
    {
        await using var api = await ApiHarness.StartAsync(options => options with
        {
            MaxBodyBytes = 4096,
            HeaderTimeout = TimeSpan.FromMilliseconds(500)
        });
        var port = api.Port;
        async Task<RawResponse> Send(string text) => await RawAsync(port, Encoding.ASCII.GetBytes(text));

        var chunked = await Send($"POST /openapi/deviceList HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\nContent-Type: application/json\r\nTransfer-Encoding: chunked\r\n\r\n2\r\n{{}}\r\n0\r\n\r\n");
        Assert.Equal(411, chunked.Status);
        Assert.Equal(411, chunked.Code);
        Assert.Equal(411, (await Send($"POST /openapi/deviceList HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\nContent-Type: application/json\r\n\r\n")).Status);
        // 声明长度超限时不读正文，直接 413，并且不让连接被重置。
        var large = await Send($"POST /openapi/deviceList HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\nContent-Type: application/json\r\nContent-Length: 4097\r\n\r\n{{");
        Assert.Equal(413, large.Status);
        var exact = "{\"deviceId\":\"" + new string('0', 4096 - 15) + "\"}";
        Assert.Equal(4096, exact.Length);
        Assert.Equal(200, (await api.PostAsync("/openapi/deviceList", exact)).Status);
        var bigHead = await Send($"POST /openapi/deviceList HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\nX-Pad: {new string('a', 17 * 1024)}\r\nContent-Length: 2\r\n\r\n{{}}");
        Assert.Equal(431, bigHead.Status);
        Assert.Equal(400, (await Send("POST /openapi/deviceList\r\nHost: x\r\n\r\n")).Status);
        Assert.Equal(400, (await Send($"GET http://evil/ HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\n\r\n")).Status);
        Assert.Equal(400, (await Send($"POST /openapi/deviceList HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\nContent-Length: 2\r\nContent-Length: 3\r\n\r\n{{}}")).Status);
        Assert.Equal(400, (await Send($"POST /openapi/deviceList HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\nContent-Length: -1\r\n\r\n")).Status);
        Assert.Equal(400, (await Send($"POST /openapi/deviceList HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\n folded: x\r\nContent-Length: 0\r\n\r\n")).Status);
        Assert.Equal(400, (await Send($"POST /openapi/deviceList HTTP/1.1\r\nContent-Length: 0\r\n\r\n")).Status);
        var badJson = await api.PostAsync("/openapi/deviceList", "{\"deviceId\":");
        Assert.True(badJson.Status == 400 && badJson.Msg.Contains("JSON"), badJson.Text);
        Assert.Equal(400, (await api.PostAsync("/openapi/deviceList", "[1,2]")).Status);
        // 空正文按空对象处理；带 BOM 的 JSON 也接受。
        Assert.Equal(0, (await api.PostAsync("/openapi/deviceList", "")).Code);
        Assert.Equal(0, (await api.PostAsync("/openapi/deviceList", "﻿{}")).Code);
        var missing = await api.PostAsync("/openapi/nope");
        Assert.True(missing.Status == 404 && missing.Code == 404, missing.Text);
        var method = await RawAsync(port, Request("GET", "/openapi/deviceList", port, null));
        Assert.True(method.Status == 405 && method.Headers["Allow"] == "POST", method.Text);
        Assert.Equal(405, (await api.PostAsync("/cmd")).Status);
        Assert.Equal(2001, (await RawAsync(port, Request("GET", "/cmd", port, null))).Code);
        Assert.Equal("application/json; charset=utf-8", missing.Headers["Content-Type"]);
        Assert.Equal("nosniff", missing.Headers["X-Content-Type-Options"]);

        // 请求头没发完：超时返回 408。
        using (var slow = new TcpClient())
        {
            await slow.ConnectAsync(IPAddress.Loopback, port);
            var stream = slow.GetStream();
            await stream.WriteAsync(Encoding.ASCII.GetBytes("POST /openapi/deviceList HTTP/1.1\r\nHost: 127.0.0.1"));
            Assert.Equal(408, (await ReadResponseAsync(stream, TimeSpan.FromSeconds(10))).Status);
        }
        // Expect: 100-continue：先收到 100，再发正文。
        using (var expect = new TcpClient())
        {
            await expect.ConnectAsync(IPAddress.Loopback, port);
            var stream = expect.GetStream();
            await stream.WriteAsync(Encoding.ASCII.GetBytes($"POST /openapi/deviceList HTTP/1.1\r\nHost: 127.0.0.1:{port}\r\nContent-Type: application/json\r\nContent-Length: 2\r\nExpect: 100-continue\r\n\r\n"));
            var interim = new byte[25];
            await stream.ReadExactlyAsync(interim);
            Assert.Equal("HTTP/1.1 100 Continue\r\n\r\n", Encoding.ASCII.GetString(interim));
            await stream.WriteAsync("{}"u8.ToArray());
            Assert.Equal(0, (await ReadResponseAsync(stream, TimeSpan.FromSeconds(10))).Code);
        }
        await Assert.ThrowsAsync<ArgumentException>(() => OpenApiServer.StartAsync(null!, new OpenApiOptions { Port = 80 }));
    }

    // Host 白名单、拒绝浏览器请求、Content-Type 与局域网令牌。
    public static async Task Security()
    {
        await using (var local = await ApiHarness.StartAsync())
        {
            var port = local.Port;
            async Task<int> WithHost(string host) => (await RawAsync(port, Encoding.ASCII.GetBytes(
                $"POST /openapi/deviceList HTTP/1.1\r\nHost: {host}\r\nContent-Type: application/json\r\nContent-Length: 2\r\n\r\n{{}}"))).Status;
            Assert.Equal(200, await WithHost($"localhost:{port}"));
            Assert.Equal(200, await WithHost($"[::1]:{port}"));
            Assert.Equal(200, await WithHost("127.0.0.1"));
            Assert.Equal(403, await WithHost($"evil.example:{port}"));
            Assert.Equal(403, await WithHost($"127.0.0.1:{port + 1}"));
            Assert.Equal(403, await WithHost($"127.0.0.1.nip.io:{port}"));
            // 回环模式不接受局域网 IP 作 Host。
            Assert.Equal(403, await WithHost($"192.168.1.10:{port}"));
            var origin = await local.PostAsync("/openapi/deviceList", null, "Origin: http://evil.example\r\n");
            Assert.True(origin.Status == 403 && origin.Code == 403, origin.Text);
            Assert.Equal(403, (await local.PostAsync("/openapi/deviceList", null, "Origin: null\r\n")).Status);
            Assert.Equal(403, (await local.PostAsync("/openapi/deviceList", null, "Sec-Fetch-Site: cross-site\r\n")).Status);
            var plain = await RawAsync(port, Request("POST", "/openapi/deviceList", port, "{}", contentType: "text/plain"));
            Assert.True(plain.Status == 415 && plain.Code == 415, plain.Text);
            Assert.Equal(415, (await RawAsync(port, Request("POST", "/openapi/deviceList", port, "{}", contentType: "application/x-www-form-urlencoded"))).Status);
            Assert.Equal(200, (await RawAsync(port, Request("POST", "/openapi/deviceList", port, "{}", contentType: "Application/JSON; charset=utf-8"))).Status);
            // 回环模式不需要令牌。
            Assert.Equal(0, (await local.PostAsync("/openapi/deviceList")).Code);
        }

        var token = OpenApiOptions.NewToken();
        Assert.True(OpenApiOptions.ValidToken(token) && token.Length == 43, token);
        Assert.True(!OpenApiOptions.ValidToken("short") && !OpenApiOptions.ValidToken(new string('a', 20) + " x"), "token rules");
        await Assert.ThrowsAsync<ArgumentException>(() => OpenApiServer.StartAsync(null!, new OpenApiOptions { Port = 0, AllowLan = true }));
        await using var lan = await ApiHarness.StartAsync(options => options with
        {
            AllowLan = true,
            Token = token,
            ListenAddress = IPAddress.Loopback
        });
        var none = await lan.PostAsync("/openapi/deviceList");
        Assert.True(none.Status == 401 && none.Code == 401 && none.Headers["WWW-Authenticate"] == "Bearer", none.Text);
        Assert.Equal(401, (await lan.PostAsync("/openapi/deviceList", null, "X-Fleet-Token: " + OpenApiOptions.NewToken() + "\r\n")).Status);
        Assert.Equal(401, (await lan.PostAsync("/openapi/deviceList", null, "Authorization: Basic " + token + "\r\n")).Status);
        Assert.Equal(0, (await lan.PostAsync("/openapi/deviceList", null, "X-Fleet-Token: " + token + "\r\n")).Code);
        Assert.Equal(0, (await lan.PostAsync("/openapi/deviceList", null, "Authorization: Bearer " + token + "\r\n")).Code);
        // 令牌检查在路由之前：未授权时连接口是否存在都不透露。
        Assert.Equal(401, (await lan.PostAsync("/openapi/nope")).Status);
        // 局域网模式接受 IP 字面量和本机名，仍拒绝其他域名。
        async Task<int> LanHost(string host) => (await RawAsync(lan.Port, Encoding.ASCII.GetBytes(
            $"POST /openapi/deviceList HTTP/1.1\r\nHost: {host}\r\nX-Fleet-Token: {token}\r\nContent-Type: application/json\r\nContent-Length: 2\r\n\r\n{{}}"))).Status;
        Assert.Equal(200, await LanHost($"192.168.1.10:{lan.Port}"));
        Assert.Equal(200, await LanHost($"[fe80::1]:{lan.Port}"));
        Assert.Equal(200, await LanHost($"{Dns.GetHostName()}:{lan.Port}"));
        Assert.Equal(403, await LanHost($"attacker.example:{lan.Port}"));
        Assert.Equal(403, await LanHost($"0x7f.1:{lan.Port}"));
        Assert.True(lan.Server.AllowLan && lan.Server.Address.StartsWith("0.0.0.0:", StringComparison.Ordinal), lan.Server.Address);
    }
}
