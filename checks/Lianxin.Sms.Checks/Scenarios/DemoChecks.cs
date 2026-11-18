using Lianxin.Sms.Checks.Infrastructure;

namespace Lianxin.Sms.Checks.Scenarios;

internal static class DemoChecks
{
    public static async Task Run()
    {
        await using var fixture = await HttpFixture.Create(true);
        var lin = await fixture.Login("lin@lianxin.local", true);
        var chen = await fixture.Login("chen@lianxin.local", true);
        var zhou = await fixture.Login("zhou@lianxin.local", true);
        var admin = await fixture.Login("admin@lianxin.local", true);
        await fixture.Client().Call("/inbox", expected: 401);
        var old = await lin.Call("/inbox");
        var threadId = old.GetProperty("items")[0].GetProperty("id").GetInt64();
        await chen.Call("/conversations/" + threadId, expected: 404);
        await zhou.Call($"/conversations/{threadId}/simulate", new
        {
            body = "隔离检查"
        }, 404);
        await chen.Call("/messages", new
        {
            key = "wrong-owner",
            conversation = threadId,
            body = "隔离检查"
        }, 404);
        await chen.Call("/admin/overview", expected: 403);
        await admin.Call("/conversations/" + threadId, expected: 403);
        await fixture.Client(origin: "https://unrelated.example").Call("/login",
            new
            {
                email = "lin@lianxin.local",
                password = HttpFixture.DemoPassword
            }, 403);
        var recipients = Enumerable.Range(1000, 1000).Select(i => "+1202555" + i.ToString("0000")).ToArray();
        var batch = new
        {
            key = "demo-batch",
            recipients,
            body = "测试批量任务"
        };
        var sent = await lin.Call("/messages", batch, 201);
        Assert.Equal(1000, sent.GetProperty("count").GetInt32());
        var duplicate = await lin.Call("/messages", batch, 201);
        Assert.Equal(sent.GetProperty("conversation").GetInt64(), duplicate.GetProperty("conversation").GetInt64());
        await lin.Call("/messages", new
        {
            key = "demo-batch",
            recipients,
            body = "其他内容"
        }, 409);
        var inbox = await lin.Call("/inbox");
        Assert.Equal(old.GetProperty("quota").GetInt32() - 1000, inbox.GetProperty("quota").GetInt32());
        Assert.True(inbox.GetProperty("more").GetBoolean());
        var deviceNames = new HashSet<string>();
        foreach (var item in inbox.GetProperty("items").EnumerateArray().Take(10))
        {
            var detail = await lin.Call("/conversations/" + item.GetProperty("id").GetInt64());
            deviceNames.Add(detail.GetProperty("device").GetString()!);
        }
        Assert.Equal(10, deviceNames.Count);
        var previousSubmitted = old.GetProperty("stats").GetProperty("submitted").GetInt32();
        await Assert.WaitFor(async () => (await lin.Call("/inbox")).GetProperty("stats").GetProperty("submitted").GetInt32() == previousSubmitted + 1000);
        var overview = await admin.Call("/admin/overview?offset=950");
        Assert.Equal(1000, overview.GetProperty("stats").GetProperty("total").GetInt32());
        Assert.Equal(986, overview.GetProperty("stats").GetProperty("online").GetInt32());
        Assert.Equal(50, overview.GetProperty("devices").GetArrayLength());
        var sync = await admin.Call("/admin/sync", new
        {
        });
        Assert.Equal(986, sync.GetProperty("total").GetInt32());
        await Assert.WaitFor(async () => (await admin.Call("/admin/overview")).GetProperty("lastSync").GetProperty("confirmed").GetInt32() == 986);
        await lin.Call("/logout", new
        {
        });
        await lin.Call("/me", expected: 401);
    }
}
