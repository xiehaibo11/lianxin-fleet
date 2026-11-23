using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Lianxin.Fleet.Core.OpenApi;

// 请求进入路由前的检查：Host 白名单（防 DNS 重绑定）、拒绝浏览器请求（防 CSRF）、局域网令牌和 JSON 内容类型。
internal static class OpenApiSecurity
{
    private static readonly string[] LoopbackNames = ["localhost", "127.0.0.1", "[::1]"];

    // 通过返回 null，否则返回拒绝的响应。
    public static OpenApiReply? CheckOrigin(OpenApiRequest request, OpenApiOptions options, int port, IReadOnlyCollection<string> machineNames)
    {
        if (!HostAllowed(request.Header("Host"), options.AllowLan, port, machineNames))
            return Reject(403, "Host 请求头不在允许范围内。请用 127.0.0.1、localhost 或本机 IP 访问开放接口。");
        // 浏览器发出的跨站请求总会带 Origin 或 Sec-Fetch-Site；开放接口只供程序调用，一律拒绝。
        // 只看这两个头：Node.js 18+ 自带的 fetch（undici）总会发送 sec-fetch-mode: cors，但不带 Origin 和 Sec-Fetch-Site。
        if (request.Header("Origin") is not null || request.Header("Sec-Fetch-Site") is not null)
            return Reject(403, "开放接口不接受浏览器发出的请求（带 Origin 或 Sec-Fetch-Site 请求头）。");
        if (options.AllowLan && !TokenMatches(request, options.Token!))
            return Reject(401, "缺少或错误的访问令牌：请在 X-Fleet-Token 或 Authorization: Bearer 请求头中提供中控设置里的令牌。")
                .WithHeader("WWW-Authenticate", "Bearer");
        return null;
    }

    public static OpenApiReply? CheckContentType(OpenApiRequest request)
    {
        if (request.Method != "POST")
            return null;
        var type = request.Header("Content-Type")?.Split(';')[0].Trim();
        return string.Equals(type, "application/json", StringComparison.OrdinalIgnoreCase)
            ? null : Reject(415, "POST 请求的 Content-Type 必须是 application/json。");
    }

    internal static bool HostAllowed(string? host, bool allowLan, int port, IReadOnlyCollection<string> machineNames)
    {
        if (string.IsNullOrEmpty(host))
            return false;
        host = host.ToLowerInvariant();
        string name;
        var close = host.LastIndexOf(']');
        var colon = host.LastIndexOf(':');
        if (colon > close && (host.StartsWith('[') || host.IndexOf(':') == colon))
        {
            var suffix = host[(colon + 1)..];
            if (!int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var given) || given != port)
                return false;
            name = host[..colon];
        }
        else
            name = host;
        if (LoopbackNames.Contains(name))
            return true;
        if (!allowLan)
            return false;
        // 局域网模式还接受 IP 字面量和本机名；攻击者控制的域名不在其中，DNS 重绑定无法得逞。
        if (name.StartsWith('[') && name.EndsWith(']'))
            return IPAddress.TryParse(name[1..^1], out var v6) && v6.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
        if (Regex.IsMatch(name, @"^\d{1,3}(\.\d{1,3}){3}$"))
            return IPAddress.TryParse(name, out _);
        return machineNames.Contains(name);
    }

    internal static bool TokenMatches(OpenApiRequest request, string expected)
    {
        var supplied = request.Header("X-Fleet-Token");
        if (supplied is null && request.Header("Authorization") is { } authorization
            && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            supplied = authorization[7..].Trim();
        if (string.IsNullOrEmpty(supplied))
            return false;
        // 先取摘要再比较，比较时间与令牌内容和长度都无关。
        return CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(supplied)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
    }

    public static IReadOnlyCollection<string> MachineNames()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            var host = Dns.GetHostName().ToLowerInvariant();
            if (host.Length > 0)
            {
                names.Add(host);
                names.Add(host.EndsWith(".local", StringComparison.Ordinal) ? host[..^6] : host + ".local");
            }
        }
        catch (System.Net.Sockets.SocketException) { }
        return names;
    }

    internal static OpenApiReply Reject(int status, string message) => OpenApiReply.Envelope(status, message, false, status);
}
