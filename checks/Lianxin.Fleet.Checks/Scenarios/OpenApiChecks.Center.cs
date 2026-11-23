using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.OpenApi;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class OpenApiChecks
{
    // 中控接口：信封格式、设备列表筛选、别名、脚本启停、刷新信息与设备编号解析。
    public static async Task Center()
    {
        await using var api = await ApiHarness.StartAsync();
        var other = api.Store.RegisterDevice("Phone B", "00008030-001234567890B002", FreePort(), "二组");
        var list = await api.PostAsync("/openapi/deviceList", J(("deviceId", ""), ("groupId", "")));
        Assert.True(list.Json.EnumerateObject().Select(item => item.Name).SequenceEqual(["code", "msg", "data"]), list.Text);
        var data = list.Json.GetProperty("data");
        Assert.Equal(2, data.GetProperty("total").GetInt32());
        Assert.Equal(1, data.GetProperty("page").GetInt32());
        var first = data.GetProperty("list")[0];
        foreach (var field in new[] { "bridgeUrl", "deviceAlias", "groupId", "deviceNo", "deviceId", "deviceName", "serialNo", "groupName", "productVersion" })
            Assert.True(first.TryGetProperty(field, out _), "deviceList field " + field);
        Assert.Equal(Udid, first.GetProperty("deviceId").GetString());
        // deviceId 模糊匹配（大小写、连字符不敏感），groupId 是分组名称。
        Assert.Equal(1, (await api.OkAsync("/openapi/deviceList", J(("deviceId", "890a001")))).GetProperty("total").GetInt32());
        var grouped = await api.OkAsync("/openapi/deviceList", J(("groupId", "二组")));
        Assert.Equal(other.Udid, grouped.GetProperty("list")[0].GetProperty("deviceId").GetString());

        Assert.True((await api.OkAsync("/openapi/setDeviceAlias", J(("deviceId", Udid), ("deviceAlias", "phone1")))).GetBoolean(), "alias");
        Assert.Equal("phone1", api.Store.GetDevice(api.Device.Id).Alias);
        // 设备编号同样可以作为 deviceId，数字也可以不加引号。
        await api.OkAsync("/openapi/setDeviceAlias", J(("deviceId", other.Id), ("deviceAlias", "二号")));
        Assert.Equal("二号", api.Store.GetDevice(other.Id).Alias);
        var unknown = await api.PostAsync("/openapi/setDeviceAlias", J(("deviceId", "00008030-FFFFFFFFFFFFFFFF"), ("deviceAlias", "x")));
        Assert.True(unknown.Status == 200 && unknown.Code == OpenApiCodes.DeviceNotFound && !unknown.Json.GetProperty("data").GetBoolean(), unknown.Text);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/setDeviceAlias", J(("deviceAlias", "x")))).Code);

        var script = api.Store.SaveScript(null, "太连指中", "主屏\n等待 0.05\n锁屏", 1);
        var missing = await api.PostAsync("/openapi/startScript", J(("deviceIds", new[] { Udid }), ("name", "不存在"), ("iecPath", "c:/x/release.iec")));
        Assert.Equal(OpenApiCodes.ScriptNotFound, missing.Code);
        // 任一设备不存在时整体拒绝，不启动任何脚本。
        var partial = await api.PostAsync("/openapi/startScript", J(("deviceIds", new[] { Udid, "99" }), ("name", script.Name)));
        Assert.True(partial.Code == OpenApiCodes.DeviceNotFound && partial.Msg.Contains("99"), partial.Text);
        // 与中控界面一样，自动化未就绪的设备不启动脚本。
        api.Bridge.Status = new(false);
        var notReady = await api.PostAsync("/openapi/startScript", J(("deviceIds", new[] { Udid }), ("name", script.Name)));
        Assert.True(notReady.Code == OpenApiCodes.DeviceFailed && notReady.Msg.Contains("startAutoEnv"), notReady.Text);
        api.Bridge.Status = new(true, 390, 844);
        Assert.Equal(0, api.Scripts.RunningCount);
        var started = await api.PostAsync("/openapi/startScript", J(("deviceIds", new[] { Udid }), ("name", script.Name), ("iecPath", "c:/x/release.iec")));
        Assert.True(started.Code == 0 && started.Json.GetProperty("data").GetBoolean() && started.Msg.Contains("1 台"), started.Text);
        await WaitAsync(() => api.Scripts.RunningCount == 0);
        Assert.True(api.Bridge.Calls.Select(call => call.Action).SequenceEqual([FleetAction.Home, FleetAction.Lock]), "script steps");
        var message = (await api.OkAsync("/openapi/refreshMsg", J(("deviceIds", new[] { Udid })))).GetProperty(Udid);
        Assert.Equal("太连指中", message.GetProperty("scriptName").GetString());
        Assert.Equal(Udid, message.GetProperty("deviceNo").GetString());
        Assert.True(message.GetProperty("msg").GetString()!.Contains("完成") && message.GetProperty("reportTime").GetString()!.Length == 19, message.ToString());
        Assert.True(message.GetProperty("serviceOk").GetString() is "0" or "1", "serviceOk");
        Assert.Equal(2, (await api.OkAsync("/openapi/refreshMsg", J())).EnumerateObject().Count());

        // 无限循环的脚本：通过 scriptId 启动，再用 stopScript 停止。
        var loop = api.Store.SaveScript(null, "循环", "等待 5", 0);
        await api.OkAsync("/openapi/startScript", J(("deviceIds", new[] { Udid }), ("scriptId", loop.Id)));
        Assert.Equal(OpenApiCodes.Busy, (await api.PostAsync("/openapi/startScript", J(("deviceIds", new[] { Udid }), ("name", "循环")))).Code);
        Assert.True((await api.PostAsync("/openapi/stopScript", J(("deviceIds", new[] { Udid })))).Msg.Contains("1 台"), "stop");
        await WaitAsync(() => api.Scripts.RunningCount == 0);
        var idle = await api.PostAsync("/openapi/pauseScript", J(("deviceIds", new[] { Udid })));
        Assert.True(idle.Code == 0 && idle.Msg.Contains("没有正在运行的脚本"), idle.Text);
    }

    internal static async Task WaitAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 200 && !condition(); attempt++)
            await Task.Delay(25);
        Assert.True(condition(), "Timed out waiting for condition");
    }
}
