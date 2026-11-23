using System.Net;
using System.Net.Sockets;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.OpenApi;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class OpenApiChecks
{
    // 截图、不支持与待接入的接口、启停自动化、USB 服务重启和开发者模式。
    public static async Task Endpoints()
    {
        var restarts = 0;
        Exception? restartError = null;
        await using var api = await ApiHarness.StartAsync(backend: backend => new OpenApiBackend(backend.Store, backend.Bridge,
            backend.Service, backend.Scripts, backend.Host, backend.Setup, backend.AppDirectory)
        {
            AutomationReadyTimeout = TimeSpan.FromSeconds(1),
            JpegEncoder = backend.JpegEncoder,
            Manager = backend.Manager,
            TransferDirectory = backend.TransferDirectory,
            RestartUsbService = _ =>
            {
                restarts++;
                return restartError is null ? Task.CompletedTask : Task.FromException(restartError);
            }
        });
        var body = new
        {
            deviceId = Udid
        };
        var png = await api.PostAsync("/openapi/captureFullScreenPng", body);
        Assert.True(png.Status == 200 && png.Headers["Content-Type"] == "image/png" && png.Body.SequenceEqual(api.Bridge.Screenshot), "png");
        var usb = await api.PostAsync("/openapi/captureFullScreenNoAutoByte", body);
        Assert.True(usb.Headers["Content-Type"] == "image/png" && usb.Body.SequenceEqual(api.Bridge.UsbScreenshot), "usb png");
        api.Bridge.UsbScreenshot = [.. "II*\0"u8, 1, 2, 3];
        Assert.Equal("image/tiff", (await api.PostAsync("/openapi/captureFullScreenNoAutoByte", body)).Headers["Content-Type"]);
        var jpeg = await api.PostAsync("/openapi/captureFullScreenJpg", body);
        Assert.True(jpeg.Headers["Content-Type"] == "image/jpeg" && jpeg.Body[^1] == 80, "jpg default quality");
        Assert.Equal(50, (await api.PostAsync("/openapi/captureFullScreenByteEx", new
        {
            deviceId = Udid,
            type = "1",
            quality = "50"
        })).Body[^1]);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/captureFullScreenByteEx", new
        {
            deviceId = Udid,
            type = "3"
        })).Code);
        Assert.Equal(OpenApiCodes.DeviceNotFound, (await api.PostAsync("/openapi/captureFullScreenPng", new
        {
            deviceId = "nobody-0000"
        })).Code);

        // 文档中的接口全部登记：没有能力的返回 2001，都不是成功；待接入（2002）的接口已全部实现。
        var routes = OpenApiRouter.CreateDefault().Routes;
        Assert.Equal(142, routes.Count);
        Assert.True(routes.Count(route => route.Method == "GET") == 3, "GET routes");
        Assert.Equal(51, routes.Count(route => route.Kind == OpenApiRouteKind.Implemented));
        Assert.Equal(91, routes.Count(route => route.Kind == OpenApiRouteKind.Unsupported));
        Assert.Equal(0, routes.Count(route => route.Kind == OpenApiRouteKind.Pending));
        foreach (var route in routes.Where(route => route.Kind != OpenApiRouteKind.Implemented))
        {
            var reply = route.Method == "GET"
                ? await RawAsync(api.Port, Request("GET", route.Path, api.Port, null))
                : await api.PostAsync(route.Path, body);
            var expected = route.Kind == OpenApiRouteKind.Pending ? OpenApiCodes.Pending : OpenApiCodes.Unsupported;
            Assert.True(reply.Status == 200 && reply.Code == expected && reply.Msg.StartsWith("联信 Fleet 暂不支持", StringComparison.Ordinal),
                $"{route.Path}: {reply.Text}");
        }
        foreach (var path in new[] { "pauseScript", "continueScript", "isScriptPause", "getBattery", "isCharging", "unmountDevImage",
            "checkDevImage", "uninstallApp", "reboot", "dumpXml", "fsyncFileOpr", "fsyncPullPush", "isLocked", "setOrientation" })
            Assert.True(routes.Single(route => route.Path == "/openapi/" + path).Kind == OpenApiRouteKind.Implemented, path);
        foreach (var path in new[] { "multiTouch", "getOrientationNoAuto", "erasePhone", "setAssistiveTouch" })
            Assert.True(routes.Single(route => route.Path == "/openapi/" + path).Kind == OpenApiRouteKind.Unsupported, path);
        var router = new OpenApiRouter().Pending("/openapi/getBattery", "读取电量")
            .Post("/openapi/getBattery", _ => Task.FromResult(OpenApiReply.Ok(87)))
            .Pending("/openapi/getBattery", "读取电量");
        Assert.Equal(OpenApiRouteKind.Implemented, router.Find("/openapi/getBattery")!.Kind);
        Assert.Throws<InvalidOperationException>(() => router.Post("/openapi/getBattery", _ => Task.FromResult(OpenApiReply.Ok())));

        // 启动自动化：WDA 已在线时直接成功；离线且 Runner 起不来时返回失败，不会假装成功。
        Assert.True((await api.PostAsync("/openapi/startAutoEnv", body)).Msg.Contains("已在运行"), "already running");
        api.Bridge.Status = new(false);
        var start = await api.PostAsync("/openapi/startAutoEnv", body);
        Assert.True(start.Code == OpenApiCodes.DeviceFailed && start.Msg.Contains("WDA 进程未能启动"), start.Text);
        Assert.True((await api.PostAsync("/openapi/closeEnv", body)).Msg.Contains("没有由本中控启动"), "close without channel");
        Assert.True((await api.PostAsync("/openapi/resetUsbConn", body)).Msg.Contains("无需重置"), "reset without channel");
        var info = await api.PostAsync("/openapi/getDeviceInfo", body);
        Assert.Equal(OpenApiCodes.DeviceFailed, info.Code);
        api.Bridge.Status = new(true, 390, 844);

        Assert.True((await api.PostAsync("/openapi/reconnectUsb", body)).Code == 0 && restarts == 1, "usb restart");
        restartError = new OperationCanceledException("已取消管理员授权，USB 服务未重启。");
        var cancelled = await api.PostAsync("/openapi/reconnectUsb", body);
        Assert.True(cancelled.Code == OpenApiCodes.DeviceFailed && cancelled.Msg.Contains("已取消管理员授权")
            && cancelled.Msg.StartsWith("已停止全部脚本和自动化通道", StringComparison.Ordinal), cancelled.Text);

        // 开发者模式与镜像走一键配置同一套命令（iOS 17：先查状态，关闭时开启）。
        api.Store.UpdateDeviceInfo(new(Udid, "Phone A", OsVersion: "17.5"));
        Assert.Equal(0, (await api.PostAsync("/openapi/enableDeveloperMode", body)).Code);
        Assert.True(api.Runner.Calls.Select(call => call.Arguments[3] + " " + call.Arguments[4])
            .SequenceEqual(["amfi developer-mode-status", "amfi enable-developer-mode"]), "developer mode commands");
        api.Runner.Calls.Clear();
        Assert.Equal(0, (await api.PostAsync("/openapi/mountDevImage", body)).Code);
        Assert.True(api.Runner.Calls.Single().Arguments.SequenceEqual(["-m", "pymobiledevice3", "--no-color", "mounter", "auto-mount", "--udid", Udid]),
            "mount command");
    }

    // 启停、端口占用、连接数上限，以及停止时取消正在执行的设备操作。
    public static async Task Lifecycle()
    {
        using (var occupied = new TcpListener(IPAddress.Loopback, 0))
        {
            occupied.Start();
            var busyPort = ((IPEndPoint)occupied.LocalEndpoint).Port;
            var busy = await Assert.ThrowsAsync<OpenApiPortBusyException>(async () =>
            {
                await using var api = await ApiHarness.StartAsync(options => options with { Port = busyPort });
            });
            Assert.True(busy.Port == busyPort && busy.Message.Contains(busyPort.ToString()) && busy.Message.Contains("8019"), busy.Message);
        }

        var port = FreePort();
        await using (var api = await ApiHarness.StartAsync(options => options with { Port = port, MaxConnections = 1, HeaderTimeout = TimeSpan.FromSeconds(3) }))
        {
            Assert.True(api.Server.IsRunning && api.Port == port && api.Server.Address == "127.0.0.1:" + port, api.Server.Address);
            using var idle = new TcpClient();
            await idle.ConnectAsync(IPAddress.Loopback, port);
            await Task.Delay(100);
            var rejected = await api.PostAsync("/openapi/deviceList");
            Assert.True(rejected.Status == 503 && rejected.Code == 503, rejected.Text);
            idle.Close();
            await Task.Delay(200);
            Assert.Equal(0, (await api.PostAsync("/openapi/deviceList")).Code);

            // 停止服务：执行中的设备操作被取消并回复 503，之后不再接受连接；重复停止无害。
            var entered = new TaskCompletionSource();
            api.Bridge.Execute = async (_, token) =>
            {
                entered.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
            };
            var pending = api.PostAsync("/openapi/home", new
            {
                deviceId = Udid
            });
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await api.Server.StopAsync();
            var stopped = await pending;
            Assert.True(stopped.Status == 503 && stopped.Msg.Contains("正在关闭"), stopped.Text);
            Assert.True(!api.Server.IsRunning, "stopped");
            await api.Server.StopAsync();
            await Assert.ThrowsAsync<SocketException>(async () =>
            {
                using var late = new TcpClient();
                await late.ConnectAsync(IPAddress.Loopback, port);
            });
        }
        // 端口释放后可以立即在同一端口重新启动。
        await using var again = await ApiHarness.StartAsync(options => options with { Port = port });
        Assert.Equal(0, (await again.PostAsync("/openapi/deviceList")).Code);
    }
}
