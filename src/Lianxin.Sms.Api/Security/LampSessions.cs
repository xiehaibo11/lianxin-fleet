using System.Globalization;
using System.Text.Json;
using Lianxin.Sms.Api.Hosting;
using Lianxin.Sms.Api.Protocol;
using Lianxin.Sms.Api.Validation;
using Microsoft.Extensions.Caching.Memory;

namespace Lianxin.Sms.Api.Security;

internal sealed class LampSessions : IDisposable
{
    private const string Cookie = "lianxin";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private static readonly object IdentityKey = new();
    private readonly HttpClient client;
    private readonly MemoryCache sessions = new(new MemoryCacheOptions { SizeLimit = 4096 });

    public LampSessions(SmsOptions options)
    {
        ValidateOptions(options);
        client = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false })
        {
            BaseAddress = new Uri(options.LampInternalOrigin!),
            Timeout = TimeSpan.FromSeconds(4),
            MaxResponseContentBufferSize = 64 * 1024
        };
    }

    public static void ValidateOptions(SmsOptions options)
    {
        if (options.LampInternalOrigin is null)
        {
            if (options.LampAdminUserId is not null)
                throw new ArgumentException("LAMP_ADMIN_USER_ID requires LAMP_INTERNAL_ORIGIN.");
            return;
        }
        if (!Uri.TryCreate(options.LampInternalOrigin, UriKind.Absolute, out var origin)
            || origin.Scheme != "http" || origin.Host != "127.0.0.1" || origin.UserInfo.Length != 0
            || origin.GetLeftPart(UriPartial.Authority) != options.LampInternalOrigin
            || options.LampAdminUserId is not > 0 || options.Demo || options.PublicOrigin is null)
            throw new ArgumentException("Lamp requires a loopback HTTP origin, a positive LAMP_ADMIN_USER_ID, and PUBLIC_ORIGIN in real-device mode.");
    }

    public static bool TryId(string? value, out long id) =>
        long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0;

    public static long ReadId(JsonElement body, string key)
    {
        InputValidation.Require(body.TryGetProperty(key, out var value) && TryId(JsonId(value), out _), 400, "灯灯用户 ID 无效。");
        return long.Parse(JsonId(value)!, CultureInfo.InvariantCulture);
    }

    public static long? UserId(HttpContext context) =>
        context.Items.TryGetValue(IdentityKey, out var id) && id is long value ? value : null;

    public async Task<long> ExchangeIdentity(HttpContext context, string origin)
    {
        InputValidation.Require(context.Request.Headers.Origin == origin, 403, "请求来源无效。");
        var token = context.Request.Headers["Token"].ToString();
        var id = await Verify(token, context.RequestAborted);
        InputValidation.Require(id is not null, 401, "灯灯会话未登录或已失效。");
        return id.Value;
    }

    public void Issue(HttpContext context)
    {
        Forget(context);
        var secret = Credentials.Token();
        var key = Credentials.Hash(secret);
        sessions.Set(key, context.Request.Headers["Token"].ToString(),
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Lifetime, Size = 1 });
        InputValidation.Require(sessions.TryGetValue(key, out _), 503, "登录繁忙，请稍后重试。");
        context.Response.Cookies.Append(Cookie, secret, CookieOptions(Lifetime));
    }

    public async Task LoadIdentity(HttpContext context)
    {
        var secret = context.Request.Cookies[Cookie];
        if (secret?.Length == 43 && sessions.TryGetValue(Credentials.Hash(secret), out string? token))
            context.Items[IdentityKey] = await Verify(token!, context.RequestAborted);
    }

    public void Logout(HttpContext context)
    {
        Forget(context);
        context.Response.Cookies.Append(Cookie, "", CookieOptions(TimeSpan.Zero));
    }

    private void Forget(HttpContext context)
    {
        var secret = context.Request.Cookies[Cookie];
        if (secret?.Length == 43)
            sessions.Remove(Credentials.Hash(secret));
    }

    private async Task<long?> Verify(string token, CancellationToken cancellationToken)
    {
        // Sa-Token emits UUIDs. Keep the header opaque and reject delimiters and whitespace.
        if (token.Length is < 1 or > 512 || token.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')))
            return null;
        using var request = new HttpRequestMessage(HttpMethod.Get, "anyone/getUserInfoById");
        request.Headers.Add("Token", token);
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if ((int)response.StatusCode >= 500)
                throw new HttpFailure(503, "暂时无法核验灯灯会话。");
            if (!response.IsSuccessStatusCode)
                return null;
            using var json = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(cancellationToken));
            var envelope = json.RootElement;
            if (envelope.ValueKind != JsonValueKind.Object || !envelope.TryGetProperty("code", out var code)
                || code.ValueKind != JsonValueKind.Number || !code.TryGetInt32(out var status) || status != 0
                || !envelope.TryGetProperty("data", out var user) || user.ValueKind != JsonValueKind.Object
                || !user.TryGetProperty("state", out var state) || state.ValueKind != JsonValueKind.True
                || !user.TryGetProperty("id", out var id) || !TryId(JsonId(id), out var parsed))
                return null;
            return parsed;
        }
        catch (Exception error) when (error is HttpRequestException or JsonException
            || error is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new HttpFailure(503, "暂时无法核验灯灯会话。");
        }
    }

    private static string? JsonId(JsonElement id) => id.ValueKind switch
    {
        JsonValueKind.String => id.GetString(),
        JsonValueKind.Number => id.GetRawText(),
        _ => null
    };

    private static CookieOptions CookieOptions(TimeSpan age) => new()
    {
        Path = "/",
        Secure = true,
        HttpOnly = true,
        SameSite = SameSiteMode.Strict,
        MaxAge = age
    };

    public void Dispose()
    {
        client.Dispose();
        sessions.Dispose();
    }
}
