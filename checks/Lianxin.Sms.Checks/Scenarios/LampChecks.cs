using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Lianxin.Sms.Api.Hosting;
using Lianxin.Sms.Checks.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Lianxin.Sms.Checks.Scenarios;

internal static class LampChecks
{
    private const string PublicOrigin = "https://bocail.test";
    private const long Admin = 9223372036854775700;
    private const long Existing = 9223372036854775701;
    private const long NewMember = 9223372036854775702;
    private const string AdminToken = "lamp-admin";
    private const string ExistingToken = "lamp-existing";
    private const string NewMemberToken = "lamp-new";

    public static async Task Run()
    {
        var directory = Directory.CreateTempSubdirectory("lianxin-lamp-");
        try
        {
            var database = Path.Combine(directory.FullName, "sms.sqlite");
            var initial = new SmsOptions(database, AdminPassword: HttpFixture.Password);
            string oldCookie;
            long existingUser;
            string phoneToken;
            await using (var legacy = await Start(SmsApplication.Build([], initial)))
            {
                using var client = Client(legacy);
                using var login = await Call(client, "/api/login", 200, new
                {
                    email = "admin@lianxin.local",
                    password = HttpFixture.Password
                });
                oldCookie = Cookie(login);
                using var existing = await Call(client, "/api/admin/accounts", 201,
                    new
                    {
                        tenant = "现有租户",
                        name = "成员甲",
                        email = "old@example.test",
                        password = HttpFixture.Password,
                        quota = 5
                    }, oldCookie);
                existingUser = (await Body(existing)).GetProperty("id").GetInt64();
                using var phone = await Call(client, "/api/admin/devices", 201, new
                {
                    name = "测试手机",
                    line = "+12025550101",
                    tenantId = 1
                }, oldCookie);
                phoneToken = (await Body(phone)).GetProperty("token").GetString()!;
            }

            var revoked = false;
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            await using var upstream = builder.Build();
            upstream.MapGet("/anyone/getUserInfoById", (HttpRequest request) =>
            {
                if (request.QueryString.HasValue)
                    return Results.BadRequest();
                return request.Headers["Token"].ToString() switch
                {
                    AdminToken => Results.Json(new { code = 0, data = new { id = Admin.ToString(), state = true } }),
                    ExistingToken => Results.Json(new { code = 0, data = new { id = Existing, state = !Volatile.Read(ref revoked), roles = new[] { "*" } } }),
                    NewMemberToken => Results.Json(new { code = 0, data = new { id = NewMember.ToString(), state = true } }),
                    "lamp-disabled" => Results.Json(new { code = 0, data = new { id = Admin, state = false } }),
                    "fake-success" => Results.Json(new { code = -1, data = new { id = Admin, state = true } }),
                    _ => Results.Json(new { code = 11012, data = (object?)null })
                };
            });
            await Start(upstream);
            var lamp = initial with
            {
                PublicOrigin = PublicOrigin,
                LampInternalOrigin = upstream.Urls.Single(),
                LampAdminUserId = Admin
            };
            await using var app = await Start(SmsApplication.Build([], lamp));
            using var browser = Client(app);

            using var oldLogin = await Call(browser, "/api/login", 401, new
            {
                email = "admin@lianxin.local",
                password = HttpFixture.Password
            });
            using var oldPortal = await Call(browser, "/portal/login", 401, new
            {
                email = "admin@lianxin.local",
                password = HttpFixture.Password
            });
            using var oldSession = await Call(browser, "/api/me", 401, cookie: oldCookie);
            using var missingOrigin = await Call(browser, "/portal/lamp-session", 403, new
            {
            }, lampToken: AdminToken);
            using var badOrigin = await Call(browser, "/portal/lamp-session", 403, new
            {
            }, lampToken: AdminToken, origin: "https://other.test");
            using var badEnvelope = await Call(browser, "/portal/lamp-session", 401, new
            {
            }, lampToken: "fake-success", origin: PublicOrigin);
            using var disabled = await Call(browser, "/portal/lamp-session", 401, new
            {
            }, lampToken: "lamp-disabled", origin: PublicOrigin);
            using var unbound = await Call(browser, "/portal/lamp-session", 403, new
            {
            }, lampToken: ExistingToken, origin: PublicOrigin);

            using var adminSession = await Call(browser, "/portal/lamp-session", 204, new
            {
            }, lampToken: AdminToken, origin: PublicOrigin);
            var sameSiteCookie = adminSession.Headers.Single(p => p.Key == "Set-Cookie").Value.Single();
            foreach (var attribute in new[] { "secure", "httponly", "samesite=strict", "max-age=300" })
                Assert.True(sameSiteCookie.Contains(attribute, StringComparison.OrdinalIgnoreCase), "Missing " + attribute);
            var adminCookie = Cookie(adminSession);
            using var adminPage = await Call(browser, "/sms", 200, cookie: adminCookie);
            Assert.True((await adminPage.Content.ReadAsStringAsync()).Contains("/portal/lamp-link", StringComparison.Ordinal));
            using var linked = await Call(browser, "/api/admin/lamp-link", 200, new
            {
                id = existingUser,
                lampUserId = Existing
            }, adminCookie);
            using var tenant = await Call(browser, "/api/admin/accounts", 201,
                new
                {
                    tenant = "新租户",
                    name = "成员乙",
                    email = "new@example.test",
                    lampUserId = NewMember.ToString(),
                    quota = 5
                }, adminCookie);

            using var memberSession = await Call(browser, "/portal/lamp-session", 204, new
            {
            }, lampToken: ExistingToken, origin: PublicOrigin);
            var memberCookie = Cookie(memberSession);
            using var member = await Call(browser, "/api/me", 200, cookie: memberCookie);
            Assert.Equal(existingUser, (await Body(member)).GetProperty("id").GetInt64());
            using var notAdmin = await Call(browser, "/api/admin/overview", 403, cookie: memberCookie);
            using var poll = await Call(browser, "/api/device/poll", 200, new
            {
                canSend = true
            }, bearer: phoneToken);
            using var sent = await Call(browser, "/api/messages", 201,
                new
                {
                    deviceId = 1,
                    key = "lamp-sms",
                    body = "测试",
                    recipients = new[] { "+12025550102" }
                }, memberCookie);
            using var sentPoll = await Call(browser, "/api/device/poll", 200, new
            {
                canSend = true
            }, bearer: phoneToken);
            Assert.Equal("测试", (await Body(sentPoll)).GetProperty("job").GetProperty("body").GetString());
            using var otherSession = await Call(browser, "/portal/lamp-session", 204, new
            {
            }, lampToken: NewMemberToken, origin: PublicOrigin);
            using var isolated = await Call(browser, "/api/messages", 404,
                new
                {
                    deviceId = 1,
                    key = "wrong-tenant",
                    body = "测试",
                    recipients = new[] { "+12025550103" }
                }, Cookie(otherSession));

            using var logout = await Call(browser, "/api/logout", 200, new
            {
            }, adminCookie);
            using var deadAdmin = await Call(browser, "/api/me", 401, cookie: adminCookie);
            Volatile.Write(ref revoked, true);
            using var deadMember = await Call(browser, "/api/me", 401, cookie: memberCookie);
        }
        finally
        {
            directory.Delete(true);
        }
    }

    private static async Task<WebApplication> Start(WebApplication app)
    {
        app.Urls.Clear();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        return app;
    }

    private static HttpClient Client(WebApplication app) => new(new HttpClientHandler
    {
        UseCookies = false,
        AllowAutoRedirect = false
    })
    {
        BaseAddress = new Uri(app.Urls.Single())
    };

    private static async Task<HttpResponseMessage> Call(HttpClient client, string route, int status, object? body = null,
        string? cookie = null, string? lampToken = null, string? origin = null, string? bearer = null)
    {
        using var request = new HttpRequestMessage(body is null ? HttpMethod.Get : HttpMethod.Post, route);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        if (cookie is not null)
            request.Headers.Add("Cookie", cookie);
        if (lampToken is not null)
            request.Headers.Add("Token", lampToken);
        if (origin is not null)
            request.Headers.Add("Origin", origin);
        if (bearer is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        var response = await client.SendAsync(request);
        Assert.True((int)response.StatusCode == status,
            $"{route}: expected {status}, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return response;
    }

    private static string Cookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("lianxin=", StringComparison.Ordinal)).Split(';')[0];

    private static async Task<JsonElement> Body(HttpResponseMessage response)
    {
        using var data = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return data.RootElement.Clone();
    }
}
