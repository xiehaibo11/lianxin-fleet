using System.Text.Encodings.Web;
using System.Text.Json;
using Lianxin.Sms.Api.Persistence;
using Lianxin.Sms.Api.Protocol;
using Lianxin.Sms.Api.Security;
using Lianxin.Sms.Api.Validation;

namespace Lianxin.Sms.Api.Routing;

internal sealed partial class SmsRouter
{
    private string PortalHome => lamp is null ? "/" : "/sms";

    public IResult Portal(HttpContext context, string origin, IFormCollection? form)
    {
        var route = context.Request.Path.Value;
        var session = context.Request.Cookies["lianxin"];
        var user = AccountFor(context, session);
        var secure = origin.StartsWith("https:", StringComparison.Ordinal);
        var formCookie = secure ? "__Host-lx-form" : "lx-form";
        var csrf = context.Request.Cookies[formCookie];
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'; base-uri 'none'; frame-ancestors 'none'";
        try
        {
            if (form is null)
            {
                if (lamp is not null && route == "/")
                    return Results.Redirect(PortalHome);
                InputValidation.Require(route == PortalHome, 404, "页面不存在。");
                if (csrf?.Length != 43)
                {
                    csrf = Credentials.Token();
                    context.Response.Cookies.Append(formCookie, csrf, new CookieOptions
                    {
                        Path = "/",
                        HttpOnly = true,
                        SameSite = SameSiteMode.Strict,
                        Secure = secure
                    });
                }
                if (user is null && lamp is not null)
                    return Page("请通过灯灯登录", """
                        <section><p>请在灯灯管理台登录，然后打开短信工作台。</p><a href="/" target="_top">进入灯灯管理台</a></section>
                        """, 401);
                return user is null ? LoginPage(csrf) : user["tenantId"] is null ? ManagementPage(user, csrf)
                    : WorkspacePage(user, csrf, context.Request.Query["queued"] == "1");
            }
            InputValidation.Require(csrf?.Length == 43 && form["csrf"] == csrf, 403, "表单已失效，请回到工作台刷新后重试。");
            string Field(string name) => form[name].ToString();
            long? Optional(string name) => string.IsNullOrWhiteSpace(Field(name)) ? null : FormNumber(Field(name), name);
            long Number(string name) => FormNumber(Field(name), name);
            JsonElement Data(object value) => JsonSerializer.SerializeToElement(value);
            if (route == "/portal/login")
            {
                Login(context, Data(new
                {
                    email = Field("email"),
                    password = Field("password")
                }), origin, session);
                return Results.Redirect(PortalHome);
            }
            InputValidation.Require(user is not null, 401, "请先登录。");
            if (route == "/portal/logout")
            {
                Logout(context, origin, session!);
                return Results.Redirect(lamp is null ? PortalHome : "/");
            }
            if (route == "/portal/send")
            {
                InputValidation.Require(user["tenantId"] is not null, 403, "请使用租户账号发送短信。");
                store.Send(user.Long("id"), Data(new
                {
                    recipients = new[] { Field("recipient") },
                    body = Field("body"),
                    deviceId = Number("deviceId"),
                    key = Field("key")
                }));
                return Results.Redirect(PortalHome + "?queued=1");
            }
            InputValidation.Require(user["tenantId"] is null, 403, "此操作仅限平台管理员。");
            if (route == "/portal/accounts")
                Admin(context.Request, "/api/admin/accounts", Data(new
                {
                    tenantId = Optional("tenantId"),
                    tenant = Field("tenant"),
                    name = Field("name"),
                    email = Field("email"),
                    password = Field("password"),
                    quota = Number("quota"),
                    lampUserId = Field("lampUserId")
                }), user);
            else if (route == "/portal/lamp-link" && lamp is not null)
                LinkLampUser(LampSessions.ReadId(Data(new
                {
                    lampUserId = Field("lampUserId")
                }), "lampUserId"), Number("id"));
            else if (route == "/portal/assign")
                store.AssignDevice(Number("deviceId"), Number("tenantId"));
            else if (route == "/portal/devices")
            {
                var (_, token) = RegisterPhone(Data(new
                {
                    name = Field("name"),
                    line = Field("line"),
                    tenantId = Number("tenantId")
                }));
                return Page("手机已登记", $"""
                    <section><h2>设备凭据</h2><p>在联信设备 APP 中填入服务的 HTTPS 地址和下方凭据。凭据仅在这次登记时显示。</p>
                    <label>设备凭据<input readonly value="{E(token)}"></label><p><a href="{PortalHome}">返回管理台</a></p></section>
                    """);
            }
            else
                throw new HttpFailure(404, "页面不存在。");
            return Results.Redirect(PortalHome);
        }
        catch (HttpFailure error)
        {
            return Page("操作未完成", $"<section><p>{E(error.Message)}</p><a href=\"{PortalHome}\">返回工作台</a></section>", error.Status);
        }
    }

    private static long FormNumber(string value, string field)
    {
        InputValidation.Require(long.TryParse(value, out var number), 400, field + " 须为数字。");
        return number;
    }

    private static string E(string? value) => HtmlEncoder.Default.Encode(value ?? "");
    private static string Csrf(string token) => $"<input type=\"hidden\" name=\"csrf\" value=\"{E(token)}\">";
    private static string LogoutForm(string token) =>
        $"<form method=\"post\" action=\"/portal/logout\">{Csrf(token)}<button class=\"secondary\">退出登录</button></form>";
    private static IResult LoginPage(string csrf) => Page("联信短信工作台", $"""
        <section class="login"><h2>登录</h2><form method="post" action="/portal/login" class="fields">
        {Csrf(csrf)}<label>邮箱<input type="email" name="email" autocomplete="username" maxlength="120" required></label>
        <label>密码<input type="password" name="password" autocomplete="current-password" maxlength="128" required></label>
        <button>登录</button></form></section>
        """);
    private static IResult Page(string title, string content, int status = 200) => Results.Content($$"""
        <!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
        <title>{{E(title)}}</title><style>
        *{box-sizing:border-box}body{margin:0;background:#f1f5f7;color:#17313b;font:16px system-ui,sans-serif}main{max-width:1040px;margin:auto;padding:32px 20px}h1{margin:0 0 24px;font-size:28px}h2{font-size:18px;margin:0 0 16px}
        section{background:white;border:1px solid #d4dfe3;border-radius:10px;padding:22px;margin:0 0 16px}.login{max-width:420px}.toolbar{display:flex;align-items:center;justify-content:space-between;gap:16px;margin-bottom:16px}.fields{display:grid;gap:12px}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(310px,100%),1fr));gap:16px}
        label{display:grid;gap:6px;font-weight:600}input,select,textarea,button{font:inherit;padding:10px 12px;border:1px solid #a8bcc5;border-radius:6px;min-width:0;color:inherit;background:white;width:100%}textarea{resize:vertical}button{background:#13634f;color:white;border-color:transparent;cursor:pointer}.secondary{background:#e4ecef;color:#17313b}.table{overflow:auto}table{width:100%;border-collapse:collapse;text-align:left}th,td{padding:10px;border-bottom:1px solid #e4ecef;overflow-wrap:anywhere}small{color:#47636c}a{color:#13634f}.notice{background:#e6f5ed;padding:10px;border-radius:6px}
        </style></head><body><main><h1>{{E(title)}}</h1>{{content}}</main></body></html>
        """, "text/html; charset=utf-8", statusCode: status);
}
