using System.Text.Json;
using System.Text.RegularExpressions;
using Lianxin.Sms.Api.Hosting;
using Lianxin.Sms.Api.Persistence;
using Lianxin.Sms.Api.Security;
using Lianxin.Sms.Api.Validation;

namespace Lianxin.Sms.Api.Routing;

internal sealed partial class SmsRouter(SmsStore store, SmsOptions options, LampSessions? lamp)
{
    private readonly Dictionary<string, (int Count, long Until)> attempts = [];
    private readonly string placeholder = Credentials.Password(Credentials.Token());
    private const long SessionLifetime = 12 * 60 * 60 * 1000;

    public IResult Handle(HttpContext context, JsonElement data, string origin)
    {
        var route = context.Request.Path.Value!;
        var get = HttpMethods.IsGet(context.Request.Method);
        var post = HttpMethods.IsPost(context.Request.Method);
        var session = context.Request.Cookies["lianxin"];
        if (get && route == "/api/config")
            return Results.Json(new
            {
                demo = options.Demo
            });
        if (post && route == "/api/login")
            return Login(context, data, origin, session);
        if (post && route.StartsWith("/api/device/", StringComparison.Ordinal))
            return Device(context, route, data);
        var user = AccountFor(context, session);
        InputValidation.Require(user is not null, 401, "请先登录。");
        if (get && route == "/api/me")
            return Results.Json(new
            {
                id = user!.Long("id"),
                name = user.Text("name"),
                email = user.Text("email"),
                quota = user.Long("quota"),
                tenantId = user["tenantId"],
                tenant = user["tenant"],
                admin = user["tenantId"] is null,
                demo = options.Demo
            });
        if (post && route == "/api/logout")
        {
            Logout(context, origin, session!);
            return Results.Json(new
            {
                ok = true
            });
        }
        if (route.StartsWith("/api/admin/", StringComparison.Ordinal))
        {
            InputValidation.Require(user!["tenantId"] is null, 403, "此操作仅限平台管理员。");
            return Admin(context.Request, route, data, user);
        }
        InputValidation.Require(user!["tenantId"] is not null, 403, "请使用租户账号进入短信工作台。");
        return User(context.Request, route, data, user);
    }
    private Dictionary<string, object?>? AccountFor(HttpContext context, string? session)
    {
        if (lamp is not null)
            return LampSessions.UserId(context) is long id ? LampAccountFor(id) : null;
        return session is not null && Regex.IsMatch(session, "^[\\w-]{43}$") ? store.One("""
            SELECT u.id,u.name,u.email,u.quota,u.tenant_id AS tenantId,t.name AS tenant FROM sessions s
            JOIN users u ON u.id=s.user_id LEFT JOIN tenants t ON t.id=u.tenant_id WHERE s.token=$p0 AND s.expires_at>$p1
            """, Credentials.Hash(session), SmsStore.Now) : null;
    }

    public Dictionary<string, object?>? LampAccountFor(long id) => id == options.LampAdminUserId
        ? store.One("""
            SELECT id,name,email,quota,tenant_id AS tenantId,NULL AS tenant FROM users
            WHERE email=$p0 AND tenant_id IS NULL
            """, InputValidation.Email(options.AdminEmail))
        : store.One("""
            SELECT u.id,u.name,u.email,u.quota,u.tenant_id AS tenantId,t.name AS tenant FROM lamp_user_links l
            JOIN users u ON u.id=l.sms_user_id JOIN tenants t ON t.id=u.tenant_id WHERE l.lamp_user_id=$p0
            """, id);

    private void Logout(HttpContext context, string origin, string session)
    {
        if (lamp is not null)
        {
            lamp.Logout(context);
            return;
        }
        store.Run("DELETE FROM sessions WHERE token=$p0", Credentials.Hash(session));
        SetCookie(context, "", origin, TimeSpan.Zero);
    }

    private IResult Login(HttpContext context, JsonElement data, string origin, string? session)
    {
        var email = InputValidation.Email(data.String("email"));
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        foreach (var entry in attempts.Where(entry => entry.Value.Until < SmsStore.Now).ToArray())
            attempts.Remove(entry.Key);
        var attempt = attempts.GetValueOrDefault(ip, (0, SmsStore.Now + 300_000));
        InputValidation.Require(attempt.Item1 < 30 && attempts.Count < 10000, 429, "登录尝试过多，请稍后再试。");
        var password = data.String("password");
        InputValidation.Require(password is not null && password.Length <= 128, 400, "密码格式无效。");
        attempts[ip] = (attempt.Item1 + 1, attempt.Item2);
        var account = store.One("SELECT id,password FROM users WHERE email=$p0", email);
        InputValidation.Require(Credentials.VerifyPassword(password!, account?.Text("password") ?? placeholder) && account is not null, 401, "邮箱或密码不正确。");
        attempts.Remove(ip);
        var secret = Credentials.Token();
        store.Run("DELETE FROM sessions WHERE expires_at<$p0 OR token=$p1", SmsStore.Now, session is null ? null : Credentials.Hash(session));
        store.Run("INSERT INTO sessions VALUES($p0,$p1,$p2)", Credentials.Hash(secret), account!.Long("id"), SmsStore.Now + SessionLifetime);
        SetCookie(context, secret, origin, TimeSpan.FromMilliseconds(SessionLifetime));
        return Results.Json(new
        {
            ok = true
        });
    }
    private static void SetCookie(HttpContext context, string value, string origin, TimeSpan lifetime) => context.Response.Cookies.Append("lianxin", value,
        new CookieOptions { Path = "/", HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = origin.StartsWith("https:", StringComparison.Ordinal), MaxAge = lifetime });
}
