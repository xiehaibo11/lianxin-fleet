using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.OpenApi;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class OpenApiChecks
{
    // 暂停后不再执行新的步骤，继续后接着执行；重复暂停、没有暂停时继续、暂停中停止，以及 refreshMsg 的暂停状态。
    public static async Task ScriptPause()
    {
        await using var api = await ApiHarness.StartAsync();
        var ids = J(("deviceIds", new[] { Udid }));
        var one = J(("deviceId", Udid));
        var loop = api.Store.SaveScript(null, "暂停测试", "主屏\n等待 0.05", 0);
        await api.OkAsync("/openapi/startScript", J(("deviceIds", new[] { Udid }), ("scriptId", loop.Id)));
        await WaitAsync(() => api.Bridge.Calls.Count >= 2);
        Assert.True(!(await api.OkAsync("/openapi/isScriptPause", one)).GetBoolean(), "running");

        var paused = await api.PostAsync("/openapi/pauseScript", ids);
        Assert.True(paused.Code == 0 && paused.Json.GetProperty("data").GetBoolean() && paused.Msg.Contains("已暂停 1 台"), paused.Text);
        Assert.True((await api.OkAsync("/openapi/isScriptPause", J(("deviceId", Udid), ("deviceAlias", "phone1")))).GetBoolean(), "paused");
        Assert.True((await api.PostAsync("/openapi/pauseScript", ids)).Msg.Contains("已经是暂停状态"), "pause twice");
        var message = (await api.OkAsync("/openapi/refreshMsg", ids)).GetProperty(Udid);
        Assert.True(message.GetProperty("paused").GetBoolean() && message.GetProperty("running").GetBoolean()
            && message.GetProperty("state").GetString() == "paused", message.ToString());
        // 正在执行的这一步可以做完，之后不再发出新的动作。
        await Task.Delay(200);
        var frozen = api.Bridge.Calls.Count;
        await Task.Delay(400);
        Assert.Equal(frozen, api.Bridge.Calls.Count);
        Assert.Equal(1, api.Scripts.RunningCount);

        var resumed = await api.PostAsync("/openapi/continueScript", ids);
        Assert.True(resumed.Code == 0 && resumed.Msg.Contains("已继续 1 台"), resumed.Text);
        await WaitAsync(() => api.Bridge.Calls.Count > frozen);
        Assert.True(!(await api.OkAsync("/openapi/isScriptPause", one)).GetBoolean(), "resumed");
        Assert.Equal("running", (await api.OkAsync("/openapi/refreshMsg", ids)).GetProperty(Udid).GetProperty("state").GetString());
        Assert.True((await api.PostAsync("/openapi/continueScript", ids)).Msg.Contains("没有暂停中的脚本"), "nothing to resume");

        // 暂停中可以停止；停止后查询为未暂停，刷新信息为已停止。
        await api.OkAsync("/openapi/pauseScript", ids);
        Assert.True((await api.PostAsync("/openapi/stopScript", ids)).Msg.Contains("1 台"), "stop while paused");
        await WaitAsync(() => api.Scripts.RunningCount == 0);
        Assert.True(!(await api.OkAsync("/openapi/isScriptPause", one)).GetBoolean(), "stopped");
        Assert.Equal("stopped", (await api.OkAsync("/openapi/refreshMsg", ids)).GetProperty(Udid).GetProperty("state").GetString());
        Assert.True((await api.PostAsync("/openapi/pauseScript", ids)).Msg.Contains("没有正在运行的脚本"), "idle");
        Assert.Equal(OpenApiCodes.DeviceNotFound, (await api.PostAsync("/openapi/pauseScript", J(("deviceIds", new[] { Udid, "404" })))).Code);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/continueScript", J())).Code);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/isScriptPause", J())).Code);
    }
}
