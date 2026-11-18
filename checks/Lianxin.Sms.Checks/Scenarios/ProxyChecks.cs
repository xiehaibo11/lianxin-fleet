using Lianxin.Sms.Checks.Infrastructure;

namespace Lianxin.Sms.Checks.Scenarios;

internal static class ProxyChecks
{
    public static async Task Run()
    {
        await using var fixture = await HttpFixture.Create(false);
        var attacker = fixture.Client(forwardedFor: "203.0.113.1");
        for (var i = 0; i < 30; i++)
            await attacker.Call("/login", new
            {
                email = "admin@lianxin.local",
                password = "wrong-password"
            }, 401);
        await attacker.Call("/login", new
        {
            email = "admin@lianxin.local",
            password = HttpFixture.Password
        }, 429);
        await fixture.Client(forwardedFor: "203.0.113.2").Call("/login", new
        {
            email = "admin@lianxin.local",
            password = HttpFixture.Password
        });
        await fixture.Client(forwardedFor: "198.51.100.9, 203.0.113.1").Call("/login", new
        {
            email = "admin@lianxin.local",
            password = HttpFixture.Password
        }, 429);
    }
}
