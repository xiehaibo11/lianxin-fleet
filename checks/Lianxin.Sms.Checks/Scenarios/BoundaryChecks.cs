using Lianxin.Sms.Checks.Infrastructure;

namespace Lianxin.Sms.Checks.Scenarios;

internal static class BoundaryChecks
{
    public static async Task Run()
    {
        await using var fixture = await HttpFixture.Create(true);
        var client = fixture.Client();
        await client.Raw("/login", HttpMethod.Delete, "{}", "application/json", 405);
        await client.Raw("/login", HttpMethod.Post, "{}", "text/plain", 415);
        await client.Raw("/login", HttpMethod.Post, "not-json", "application/json", 400);
        await client.Raw("/login", HttpMethod.Post, "[]", "application/json", 400);
        await client.Raw("/login", HttpMethod.Post, "{\"padding\":\"" + new string('x', 40000) + "\"}", "application/json", 413);
        var member = await fixture.Login("lin@lianxin.local", true);
        await member.Call("/messages", new
        {
            key = "unicode-phone",
            recipients = new[] { "+1١٢٣٤٥٦٧" },
            body = "拒绝非 ASCII 数字"
        }, 400);
    }
}
