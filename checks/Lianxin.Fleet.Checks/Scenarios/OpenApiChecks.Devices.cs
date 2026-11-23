using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Infrastructure;
using Lianxin.Fleet.Core.OpenApi;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class OpenApiChecks
{
    private static readonly string[] Cli = ["-m", "pymobiledevice3", "--no-color"];

    // 电量与充电（缓存与 refresh）、getDeviceInfo 的电量字段、开发者镜像查询与卸载、卸载应用、重启，以及没有设备管理模块时的 2001。
    public static async Task DeviceManagement()
    {
        await using var api = await ApiHarness.StartAsync();
        var level = 87;
        var charging = "true";
        var images = """[{"DiskImageType": "Developer", "IsMounted": true, "MountPath": "/Developer"}]""";
        var restart = RecordingRunner.Ok();
        api.ManagerReply = arguments => arguments switch
        {
            ["-c", _, "batteries", ..] => RecordingRunner.Ok($$"""[{"Identifier": "{{Udid}}", "BatteryCurrentCapacity": {{level}}, "BatteryIsCharging": {{charging}}, "ExternalConnected": true}]"""),
            [_, _, _, "mounter", "list", ..] => RecordingRunner.Ok(images),
            [_, _, _, "diagnostics", "restart", ..] => restart,
            _ => RecordingRunner.Ok()
        };
        var body = J(("deviceId", Udid));
        int Calls(string operation) => api.ManagerRunner.Calls.Count(call => call.Arguments.Contains(operation));

        // 电量经 USB 锁屏服务读取，不需要自动化；60 秒内与设备页电量列共用读数，refresh 强制重读。
        api.Bridge.Status = new(false, Error: "WDA 尚未就绪。");
        Assert.Equal(87, (await api.OkAsync("/openapi/getBattery", body)).GetInt32());
        Assert.True(api.ManagerRunner.Calls.Single().Arguments[2..].SequenceEqual(["batteries", Udid]), "single-device battery read");
        Assert.True((await api.OkAsync("/openapi/isCharging", body)).GetBoolean(), "charging");
        Assert.Equal(1, Calls("batteries"));
        level = 64;
        charging = "false";
        Assert.Equal(87, (await api.OkAsync("/openapi/getBattery", body)).GetInt32());
        Assert.Equal(64, (await api.OkAsync("/openapi/getBattery", J(("deviceId", Udid), ("refresh", true)))).GetInt32());
        Assert.True(!(await api.OkAsync("/openapi/isCharging", J(("deviceId", Udid), ("refresh", "1")))).GetBoolean(), "not charging");
        Assert.Equal(3, Calls("batteries"));
        // getDeviceInfo 需要自动化，电量与充电取同一份读数。
        api.Bridge.Status = new(true, 390, 844);
        var info = await api.OkAsync("/openapi/getDeviceInfo", body);
        Assert.True(info.GetProperty("battery").GetInt32() == 64 && !info.GetProperty("charging").GetBoolean(), info.ToString());
        Assert.Equal(3, Calls("batteries"));
        // 读不到（未信任、拔线）时返回失败，不返回假电量。
        api.ManagerReply = arguments => RecordingRunner.Ok(arguments.Contains("batteries")
            ? $$"""[{"Identifier": "{{Udid}}", "Error": "NotPairedError"}]""" : "{}");
        var unknown = await api.PostAsync("/openapi/getBattery", J(("deviceId", Udid), ("refresh", true)));
        Assert.True(unknown.Code == OpenApiCodes.DeviceFailed && unknown.Msg.Contains("信任"), unknown.Text);
        Assert.Equal(OpenApiCodes.DeviceFailed, (await api.PostAsync("/openapi/isCharging", body)).Code);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await api.OkAsync("/openapi/getDeviceInfo", body)).GetProperty("battery").ValueKind);
        api.ManagerReply = arguments => arguments switch
        {
            [_, _, _, "mounter", "list", ..] => RecordingRunner.Ok(images),
            [_, _, _, "diagnostics", "restart", ..] => restart,
            _ => RecordingRunner.Ok()
        };

        // 镜像：查询不挂载；卸载前先停止这台手机的脚本；没有挂载时也成功。
        var image = await api.OkAsync("/openapi/checkDevImage", body);
        Assert.True(image.GetProperty("mounted").GetBoolean() && image.GetProperty("result").GetBoolean()
            && image.GetProperty("path").GetString() == "/Developer" && image.GetProperty("imageType").GetString() == "Developer", image.ToString());
        var loop = api.Store.SaveScript(null, "镜像前循环", "等待 5", 0);
        api.Scripts.Start(loop.Id, [api.Device.Id], "检查");
        Assert.Equal(1, api.Scripts.RunningCount);
        var unmount = await api.PostAsync("/openapi/unmountDevImage", body);
        Assert.True(unmount.Code == 0 && unmount.Json.GetProperty("data").GetBoolean() && unmount.Msg.Contains("已卸载"), unmount.Text);
        Assert.True(api.ManagerRunner.Calls[^1].Arguments.SequenceEqual([.. Cli, "mounter", "umount-developer", "--udid", Udid]), "unmount args");
        await WaitAsync(() => api.Scripts.RunningCount == 0);
        images = """[{"DiskImageType": "Personalized", "IsMounted": false}]""";
        Assert.True((await api.PostAsync("/openapi/unmountDevImage", body)).Msg.Contains("本来就没有挂载"), "not mounted");
        var absent = await api.OkAsync("/openapi/checkDevImage", body);
        Assert.True(!absent.GetProperty("mounted").GetBoolean() && absent.GetProperty("path").GetString() == "", absent.ToString());

        // 卸载应用：Bundle ID 先校验，系统应用拒绝；通过的才启动子进程。
        Assert.Equal("ok", (await api.OkAsync("/openapi/uninstallApp", J(("deviceId", Udid), ("bundleId", "com.ownbook.notes")))).GetString());
        Assert.True(api.ManagerRunner.Calls[^1].Arguments.SequenceEqual([.. Cli, "apps", "uninstall", "--udid", Udid, "--", "com.ownbook.notes"]),
            string.Join(" ", api.ManagerRunner.Calls[^1].Arguments));
        var before = api.ManagerRunner.Calls.Count;
        foreach (var bad in new[] { "com.apple.mobilesafari", "bad bundle", "--help" })
            Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/uninstallApp", J(("deviceId", Udid), ("bundleId", bad)))).Code);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/uninstallApp", body)).Code);
        Assert.Equal(before, api.ManagerRunner.Calls.Count);

        // 重启：连接中途断开时结果待确认（1004）；找不到设备时确定没有执行（1003）。
        Assert.True((await api.OkAsync("/openapi/reboot", body)).GetBoolean(), "reboot");
        Assert.True(api.ManagerRunner.Calls[^1].Arguments.SequenceEqual([.. Cli, "diagnostics", "restart", "--udid", Udid]), "restart args");
        restart = new ProcessResult(1, "", "ConnectionAbortedError: reset");
        Assert.Equal(OpenApiCodes.Uncertain, (await api.PostAsync("/openapi/reboot", body)).Code);
        restart = new ProcessResult(1, "", ToolOutput.NotFound(Udid));
        var gone = await api.PostAsync("/openapi/reboot", body);
        Assert.True(gone.Code == OpenApiCodes.DeviceFailed && gone.Msg.Contains("没有找到这台手机") && gone.Msg.Contains("已停止这台手机的脚本和自动化"), gone.Text);
        Assert.Equal(OpenApiCodes.DeviceNotFound, (await api.PostAsync("/openapi/reboot", J(("deviceId", "00008030-FFFFFFFFFFFFFFFF")))).Code);

        // 没有设备管理模块的中控：这些接口明确返回不支持，getDeviceInfo 的电量为 null。
        await using var bare = await ApiHarness.StartAsync(backend: backend => new OpenApiBackend(backend.Store, backend.Bridge,
            backend.Service, backend.Scripts, backend.Host, backend.Setup, backend.AppDirectory));
        foreach (var path in new[] { "getBattery", "isCharging", "checkDevImage", "unmountDevImage", "reboot", "fsyncFileOpr" })
        {
            var reply = await bare.PostAsync("/openapi/" + path, J(("deviceId", Udid), ("bundleId", "com.a.b"), ("action", "list"), ("srcPath", "/")));
            Assert.True(reply.Code == OpenApiCodes.Unsupported && reply.Msg.StartsWith("联信 Fleet 暂不支持", StringComparison.Ordinal), path + ": " + reply.Text);
        }
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await bare.OkAsync("/openapi/getDeviceInfo", body)).GetProperty("battery").ValueKind);
        Assert.Equal(0, bare.ManagerRunner.Calls.Count);
    }
}
