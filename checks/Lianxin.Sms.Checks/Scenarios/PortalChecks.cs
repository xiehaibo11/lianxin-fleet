using System.Text.Json;
using System.Text.RegularExpressions;
using Lianxin.Sms.Checks.Infrastructure;

namespace Lianxin.Sms.Checks.Scenarios;

internal static class PortalChecks
{
    public static async Task Run()
    {
        await using var fixture = await HttpFixture.Create(false);
        var admin = await PortalLogin(fixture, "admin@lianxin.local");
        var adminPage = await admin.Portal("/");
        Assert.True(adminPage.Contains("action=\"/portal/devices\"", StringComparison.Ordinal));
        var adminCsrf = Input(adminPage, "csrf");
        await admin.Portal("/portal/accounts", 302, new()
        {
            ["csrf"] = adminCsrf, ["tenant"] = "甲组", ["name"] = "成员甲", ["email"] = "a@example.test",
            ["password"] = HttpFixture.Password, ["quota"] = "5"
        });
        await admin.Portal("/portal/accounts", 302, new()
        {
            ["csrf"] = adminCsrf, ["tenant"] = "乙组", ["name"] = "成员乙", ["email"] = "b@example.test",
            ["password"] = HttpFixture.Password, ["quota"] = "5"
        });
        var tenants = (await admin.Call("/admin/overview")).GetProperty("tenants");
        var tenantA = tenants[0].GetProperty("id").GetInt64();
        var tenantB = tenants[1].GetProperty("id").GetInt64();
        var first = await admin.Call("/admin/devices", new { name = "Phone-A", line = "+12025550101", tenantId = tenantA }, 201);
        var second = await admin.Call("/admin/devices", new { name = "Phone-B", line = "+12025550102", tenantId = tenantB }, 201);
        var phoneA = fixture.Client(first.GetProperty("token").GetString());
        var phoneB = fixture.Client(second.GetProperty("token").GetString());
        await phoneA.Call("/device/poll", new { canSend = true });
        await phoneB.Call("/device/poll", new { canSend = true });
        var member = await PortalLogin(fixture, "a@example.test");
        var page = await member.Portal("/");
        Assert.True(page.Contains("Phone-A", StringComparison.Ordinal) && !page.Contains("Phone-B", StringComparison.Ordinal));
        var send = new Dictionary<string, string>
        {
            ["key"] = Input(page, "key"), ["recipient"] = "+12025550103", ["body"] = "<script>test</script>",
            ["deviceId"] = second.GetProperty("id").GetRawText()
        };
        await member.Portal("/portal/send", 403, send);
        send["csrf"] = Input(page, "csrf");
        await member.Portal("/portal/send", 404, send);
        send["deviceId"] = first.GetProperty("id").GetRawText();
        await member.Portal("/portal/send", 302, send);
        await member.Portal("/portal/send", 302, send);
        Assert.Equal(4L, (await member.Call("/me")).GetProperty("quota").GetInt64());
        Assert.Equal(JsonValueKind.Null, (await phoneB.Call("/device/poll", new { canSend = true })).GetProperty("job").ValueKind);
        var job = (await phoneA.Call("/device/poll", new { canSend = true })).GetProperty("job");
        var id = job.GetProperty("id").GetInt64();
        Assert.Equal("+12025550103", job.GetProperty("recipient").GetString());
        await admin.Call("/admin/device-assignment", new { deviceId = first.GetProperty("id").GetInt64(), tenantId = tenantB }, 409);
        await phoneA.Call("/device/claim", new { id });
        await phoneA.Call("/device/report", new { id, status = "submitted" });
        var history = await member.Portal("/");
        Assert.True(history.Contains("已提交发送", StringComparison.Ordinal));
        Assert.True(history.Contains("&lt;script&gt;test&lt;/script&gt;", StringComparison.Ordinal));
        Assert.True(!history.Contains("<script>", StringComparison.Ordinal));
    }

    private static async Task<ApiClient> PortalLogin(HttpFixture fixture, string email)
    {
        var client = fixture.Client(origin: fixture.Origin.GetLeftPart(UriPartial.Authority));
        var login = await client.Portal("/");
        await client.Portal("/portal/login", 302, new()
        {
            ["csrf"] = Input(login, "csrf"), ["email"] = email, ["password"] = HttpFixture.Password
        });
        return client;
    }

    private static string Input(string html, string name)
    {
        var match = Regex.Match(html, $"name=\"{Regex.Escape(name)}\" value=\"([^\"]+)\"");
        Assert.True(match.Success, "Missing form field: " + name);
        return match.Groups[1].Value;
    }
}
