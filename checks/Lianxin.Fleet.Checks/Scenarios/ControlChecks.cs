using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static class ControlChecks
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 0xFF, 0xD9];

    public static async Task MjpegAndForwarding()
    {
        var feed = await FakeMjpegServerAsync();
        await using (var stream = await ConnectAsync(feed))
        {
            var frames = new List<byte[]>();
            await foreach (var frame in MjpegReader.ReadFramesAsync(stream))
                frames.Add(frame);
            Assert.Equal(2, frames.Count);
            Assert.True(frames.All(frame => frame.SequenceEqual(Jpeg)), "MJPEG frames survive parsing");
        }
        var device = new TcpListener(IPAddress.Loopback, 0);
        device.Start();
        _ = Task.Run(async () =>
        {
            using var phone = await device.AcceptTcpClientAsync();
            var buffer = new byte[4];
            await phone.GetStream().ReadExactlyAsync(buffer);
            await phone.GetStream().WriteAsync(Encoding.ASCII.GetBytes(Encoding.ASCII.GetString(buffer).ToUpperInvariant()));
        });
        var forwarder = new UsbPortForwarder(0, _ => ConnectAsync(((IPEndPoint)device.LocalEndpoint).Port));
        await using (var client = await ConnectAsync(forwarder.LocalPort))
        {
            await client.WriteAsync("ping"u8.ToArray());
            var reply = new byte[4];
            await client.ReadExactlyAsync(reply);
            Assert.Equal("PING", Encoding.ASCII.GetString(reply));
        }
        await forwarder.DisposeAsync();
        device.Stop();
        await Assert.ThrowsAsync<SocketException>(() => ConnectAsync(forwarder.LocalPort));
    }

    public static Task UsbmuxProtocol()
    {
        var packet = Usbmux.Packet("Connect", [("DeviceID", 7), ("PortNumber", (int)(ushort)IPAddress.HostToNetworkOrder((short)8100))]);
        Assert.Equal(packet.Length, BitConverter.ToInt32(packet, 0));
        Assert.True(BitConverter.ToInt32(packet, 4) == 1 && BitConverter.ToInt32(packet, 8) == 8, "Plist protocol header");
        Assert.True(Encoding.UTF8.GetString(packet, 16, packet.Length - 16).Contains("<key>PortNumber</key><integer>42015</integer>"),
            "Device port is sent in network byte order");
        var reply = Usbmux.Parse(Encoding.UTF8.GetBytes("""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0"><dict><key>MessageType</key><string>Result</string><key>Number</key><integer>0</integer></dict></plist>
            """));
        Assert.Equal("0", reply["Number"].Value);
        Assert.Equal(Usbmux.Normalize("00008030-001234567890002E"), Usbmux.Normalize("00008030001234567890002e"));
        return Task.CompletedTask;
    }

    public static async Task ActionRoutes()
    {
        var requests = new ConcurrentQueue<(string Path, string Body)>();
        using var client = new HttpClient(new Handler(async (request, token) =>
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token);
            requests.Enqueue((request.RequestUri!.AbsolutePath, body));
            object? value = request.RequestUri.AbsolutePath switch
            {
                "/session" => new { sessionId = "s1" },
                "/window/size" => new { width = 390, height = 844 },
                _ => null
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { value })) };
        }));
        var feed = await FakeMjpegServerAsync();
        using var bridge = new DeviceBridge(httpClient: client, streamConnector: (_, _) => ConnectAsync(feed));
        var phone = new FleetDevice(1, "Phone", "00008110-001234567890001E", 18100);
        async Task<(string Path, string Body)> Run(FleetAction action, ActionParameters parameters)
        {
            await bridge.ExecuteAsync(phone, action, parameters);
            return requests.Last();
        }
        Assert.Equal("/wda/lock", (await Run(FleetAction.Lock, new())).Path);
        var volume = await Run(FleetAction.VolumeUp, new());
        Assert.True(volume.Path == "/session/s1/wda/pressButton" && volume.Body.Contains("volumeUp"), "Volume uses the session button route");
        Assert.Equal("/session/s1/url", (await Run(FleetAction.OpenUrl, new(Text: "weixin://dl/scan"))).Path);
        var paste = await Run(FleetAction.Clipboard, new(Text: "你好"));
        Assert.True(paste.Body.Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes("你好"))), "Clipboard text is UTF-8 base64");
        var hold = await Run(FleetAction.LongPress, new(X: .5, Y: .5, Duration: 1));
        Assert.True(hold.Path == "/session/s1/wda/touchAndHold" && hold.Body.Contains("\"duration\":1"), "Long press keeps its duration");
        var swipe = await Run(FleetAction.Swipe, new(FromX: .75, FromY: .8, ToX: .2, ToY: .1, Duration: .48));
        Assert.Equal("/session/s1/actions", swipe.Path);
        using (var body = JsonDocument.Parse(swipe.Body))
        {
            var pointer = body.RootElement.GetProperty("actions").EnumerateArray().Single();
            Assert.Equal("pointer", pointer.GetProperty("type").GetString());
            Assert.Equal("touch", pointer.GetProperty("parameters").GetProperty("pointerType").GetString());
            var steps = pointer.GetProperty("actions").EnumerateArray().ToArray();
            Assert.True(steps.Select(step => step.GetProperty("type").GetString())
                .SequenceEqual(["pointerMove", "pointerDown", "pointerMove", "pointerUp"]),
                "A swipe moves immediately after touch down without a long press");
            Assert.Equal((292, 674, 0), ReadMove(steps[0]));
            Assert.Equal((78, 84, 480), ReadMove(steps[2]));
        }
        Assert.Equal("/session/s1/wda/apps/terminate", (await Run(FleetAction.Terminate, new(BundleId: "com.example.app"))).Path);
        var before = requests.Count;
        await Assert.ThrowsAsync<ArgumentException>(() => bridge.ExecuteAsync(phone, FleetAction.OpenUrl, new(Text: "https://a b")));
        Assert.Equal(before, requests.Count);
        var frames = 0;
        await foreach (var _ in bridge.StreamAsync(phone))
            frames++;
        Assert.Equal(2, frames);
        Assert.True(requests.Last().Body.Contains("\"mjpegScalingFactor\":30"), "Stream start lowers WDA frame size");

        static (int X, int Y, int Duration) ReadMove(JsonElement step)
        {
            Assert.Equal("viewport", step.GetProperty("origin").GetString());
            return (step.GetProperty("x").GetInt32(), step.GetProperty("y").GetInt32(), step.GetProperty("duration").GetInt32());
        }
    }

    private static async Task<int> FakeMjpegServerAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        _ = Task.Run(async () =>
        {
            using var phone = await listener.AcceptTcpClientAsync();
            listener.Stop();
            var stream = phone.GetStream();
            // 与 WDA 一致：收到客户端请求后才推流。
            if (await stream.ReadAsync(new byte[256]) == 0)
                return;
            var output = new MemoryStream();
            output.Write("HTTP/1.0 200 OK\r\nContent-Type: multipart/x-mixed-replace; boundary=--BoundaryString\r\n\r\n"u8);
            for (var i = 0; i < 2; i++)
            {
                output.Write(Encoding.ASCII.GetBytes($"--BoundaryString\r\nContent-type: image/jpeg\r\nContent-Length: {Jpeg.Length}\r\n\r\n"));
                output.Write(Jpeg);
                output.Write("\r\n\r\n"u8);
            }
            await stream.WriteAsync(output.ToArray());
        });
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static async Task<Stream> ConnectAsync(int port)
    {
        var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        return client.GetStream();
    }

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
