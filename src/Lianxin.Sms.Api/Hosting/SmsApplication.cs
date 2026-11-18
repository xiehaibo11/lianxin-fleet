using System.Text.Json;
using Lianxin.Sms.Api.Persistence;
using Lianxin.Sms.Api.Protocol;
using Lianxin.Sms.Api.Routing;
using Lianxin.Sms.Api.Security;
using Lianxin.Sms.Api.Validation;
using Microsoft.AspNetCore.HttpOverrides;

namespace Lianxin.Sms.Api.Hosting;

public static class SmsApplication
{
    public static WebApplication Build(string[] args, SmsOptions? options = null)
    {
        var demo = args.Contains("--demo");
        var suppliedPath = Value(args, "--db-path") ?? Environment.GetEnvironmentVariable("DB_PATH");
        options ??= new(suppliedPath ?? (demo ? "data/sms-demo-native.sqlite" : "data/sms-native.sqlite"), demo,
            Environment.GetEnvironmentVariable("ADMIN_PASSWORD"), Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@lianxin.local",
            Environment.GetEnvironmentVariable("PUBLIC_ORIGIN"), Environment.GetEnvironmentVariable("LAMP_INTERNAL_ORIGIN"),
            LampSessions.TryId(Environment.GetEnvironmentVariable("LAMP_ADMIN_USER_ID"), out var lampAdmin) ? lampAdmin : null);
        if (options.PublicOrigin is { } origin && (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || uri.Scheme != "https" || uri.GetLeftPart(UriPartial.Authority) != origin))
            throw new ArgumentException("PUBLIC_ORIGIN must be an HTTPS origin without a trailing slash.");
        LampSessions.ValidateOptions(options);
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [] });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(server => server.Limits.MaxRequestBodySize = 40_000);
        var port = Value(args, "--port") ?? Environment.GetEnvironmentVariable("PORT") ?? "4173";
        if (!int.TryParse(port, out var number) || number is < 0 or > 65535)
            throw new ArgumentException("PORT 无效。");
        builder.WebHost.UseUrls("http://127.0.0.1:" + number);
        // 仅信任回环反向代理的 X-Forwarded-For（最右一跳）；不转发 Host/Proto，Origin 校验仍用原始 Host。
        builder.Services.Configure<ForwardedHeadersOptions>(forwarded => forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<SmsStore>();
        if (options.LampInternalOrigin is not null)
            builder.Services.AddSingleton<LampSessions>();
        builder.Services.AddHostedService<SmsTicker>();
        var app = builder.Build();
        var store = app.Services.GetRequiredService<SmsStore>();
        var lamp = app.Services.GetService<LampSessions>();
        var router = new SmsRouter(store, options, lamp);
        app.UseForwardedHeaders();
        app.Run(async context =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers.CacheControl = "no-store";
            try
            {
                var route = context.Request.Path.Value ?? "/";
                if (route == "/health" && HttpMethods.IsGet(context.Request.Method))
                {
                    await context.Response.WriteAsJsonAsync(new
                    {
                        service = "Lianxin SMS API",
                        demo = options.Demo
                    });
                    return;
                }
                InputValidation.Require(HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsPost(context.Request.Method), 405, "请求方法无效。");
                var expectedOrigin = options.PublicOrigin ?? "http://" + context.Request.Host;
                if (HttpMethods.IsPost(context.Request.Method))
                    InputValidation.Require(!context.Request.Headers.ContainsKey("Origin")
                    || context.Request.Headers.Origin == expectedOrigin, 403, "请求来源无效。");
                if (lamp is not null)
                {
                    if (route == "/portal/lamp-session")
                    {
                        InputValidation.Require(HttpMethods.IsPost(context.Request.Method), 405, "请求方法无效。");
                        var id = await lamp.ExchangeIdentity(context, expectedOrigin);
                        InputValidation.Require(store.Use(() => router.LampAccountFor(id)) is not null, 403, "此灯灯账号尚未获分配短信权限。");
                        lamp.Issue(context);
                        context.Response.StatusCode = StatusCodes.Status204NoContent;
                        return;
                    }
                    if (route is "/api/login" or "/portal/login")
                        throw new HttpFailure(401, "请通过灯灯管理系统登录。");
                    if (!route.StartsWith("/api/device/", StringComparison.Ordinal) && route != "/api/config")
                        await lamp.LoadIdentity(context);
                }
                if (route is "/" or "/sms" || route.StartsWith("/portal/", StringComparison.Ordinal))
                {
                    context.Response.Headers["Referrer-Policy"] = "same-origin";
                    IFormCollection? form = null;
                    if (HttpMethods.IsPost(context.Request.Method))
                    {
                        InputValidation.Require(context.Request.ContentType?.Split(';')[0] == "application/x-www-form-urlencoded", 415, "请从工作台提交表单。");
                        form = await context.Request.ReadFormAsync(context.RequestAborted);
                    }
                    var page = store.Use(() => router.Portal(context, expectedOrigin, form));
                    await page.ExecuteAsync(context);
                    return;
                }
                InputValidation.Require(route.StartsWith("/api/", StringComparison.Ordinal), 404, "接口不存在。");
                var data = HttpMethods.IsPost(context.Request.Method) ? await ReadJson(context.Request) : default;
                var result = store.Use(() => router.Handle(context, data, expectedOrigin));
                await result.ExecuteAsync(context);
            }
            catch (Exception error)
            {
                if (context.Response.HasStarted)
                    return;
                context.Response.StatusCode = error is HttpFailure failure ? failure.Status : error is BadHttpRequestException bad ? bad.StatusCode : 500;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = error is HttpFailure ? error.Message : context.Response.StatusCode == 413 ? "请求过大。" : "服务暂时不可用，请稍后再试。"
                });
            }
        });
        return app;
    }
    private static string? Value(string[] args, string key)
    {
        var index = Array.IndexOf(args, key);
        if (index < 0)
            return null;
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException(key + " 缺少值。");
        return args[index + 1];
    }
    private static async Task<JsonElement> ReadJson(HttpRequest request)
    {
        InputValidation.Require(request.ContentType?.Split(';')[0].Equals("application/json", StringComparison.OrdinalIgnoreCase) == true, 415, "请求必须为 JSON。");
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        int count;
        while ((count = await request.Body.ReadAsync(chunk)) > 0)
        {
            InputValidation.Require(buffer.Length + count <= 40_000, 413, "请求过大。");
            await buffer.WriteAsync(chunk.AsMemory(0, count));
        }
        try
        {
            using var document = JsonDocument.Parse(buffer.ToArray());
            InputValidation.Require(document.RootElement.ValueKind == JsonValueKind.Object, 400, "请求格式无效。");
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new HttpFailure(400, "JSON 格式无效。");
        }
    }
}
