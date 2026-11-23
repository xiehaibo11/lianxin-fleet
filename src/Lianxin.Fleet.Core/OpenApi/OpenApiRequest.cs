using System.Buffers;
using System.Text;

namespace Lianxin.Fleet.Core.OpenApi;

// 已解析的 HTTP/1.1 请求。正文在安全检查通过后才读取。
public sealed class OpenApiRequest
{
    private readonly Dictionary<string, string> headers;

    internal OpenApiRequest(string method, string path, string query, Dictionary<string, string> headers, long contentLength)
    {
        Method = method;
        Path = path;
        Query = query;
        this.headers = headers;
        ContentLength = contentLength;
    }

    public string Method
    {
        get;
    }
    public string Path
    {
        get;
    }
    public string Query
    {
        get;
    }
    // -1 表示请求没有 Content-Length。
    public long ContentLength
    {
        get;
    }
    public ReadOnlyMemory<byte> Body
    {
        get; internal set;
    }
    public string? Header(string name) => headers.TryGetValue(name, out var value) ? value : null;
}

// 请求格式不符合本服务支持的 HTTP 子集，按 Status 回复后关闭连接。
internal sealed class OpenApiProtocolException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

internal static class OpenApiRequestReader
{
    private static readonly byte[] HeaderEnd = "\r\n\r\n"u8.ToArray();

    // 读到空行为止。返回 null 表示对方没有发送任何数据就关闭了连接；Leftover 是已读到的正文开头。
    public static async Task<(OpenApiRequest Request, byte[] Leftover)?> ReadHeadAsync(Stream stream, OpenApiOptions options,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[options.MaxHeaderBytes + HeaderEnd.Length];
        var filled = 0;
        // 请求行（第一个 CRLF 之前）必须在 RequestLineTimeout 内到达，之后才按整体的请求头超时等待。
        using var firstLine = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        firstLine.CancelAfter(options.RequestLineTimeout);
        var lineSeen = false;
        while (true)
        {
            var count = await stream.ReadAsync(buffer.AsMemory(filled), lineSeen ? cancellationToken : firstLine.Token)
                .ConfigureAwait(false);
            if (!lineSeen && buffer.AsSpan(0, filled + count).IndexOf("\r\n"u8) >= 0)
                lineSeen = true;
            if (count == 0)
            {
                if (filled == 0)
                    return null;
                throw new OpenApiProtocolException(400, "请求头不完整，连接已关闭。");
            }
            var searchFrom = Math.Max(0, filled - HeaderEnd.Length + 1);
            filled += count;
            var end = buffer.AsSpan(searchFrom, filled - searchFrom).IndexOf(HeaderEnd);
            if (end >= 0)
            {
                end += searchFrom;
                if (end > options.MaxHeaderBytes)
                    throw new OpenApiProtocolException(431, "请求头过大。");
                var request = Parse(Encoding.Latin1.GetString(buffer, 0, end), options);
                return (request, buffer[(end + HeaderEnd.Length)..filled]);
            }
            if (filled >= buffer.Length)
                throw new OpenApiProtocolException(431, "请求头过大。");
        }
    }

    internal static OpenApiRequest Parse(string head, OpenApiOptions options)
    {
        var lines = head.Split("\r\n");
        var parts = lines[0].Split(' ');
        if (parts.Length != 3 || parts[0].Length is 0 or > 16 || !parts[0].All(char.IsAsciiLetterUpper))
            throw new OpenApiProtocolException(400, "请求行格式无效。");
        if (parts[2] is not ("HTTP/1.1" or "HTTP/1.0"))
            throw new OpenApiProtocolException(400, "只支持 HTTP/1.1。");
        var target = parts[1];
        if (!target.StartsWith('/') || target.Length > 2048 || target.Any(c => c <= ' ' || c >= '\u007f'))
            throw new OpenApiProtocolException(400, "请求路径无效。");
        var query = target.IndexOf('?');
        var path = query < 0 ? target : target[..query];
        if (lines.Length - 1 > options.MaxHeaderCount)
            throw new OpenApiProtocolException(431, "请求头数量过多。");
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines.Skip(1))
        {
            var colon = line.IndexOf(':');
            // 不接受旧式折行（以空白开头的续行）和没有冒号的行。
            if (colon <= 0 || line[0] is ' ' or '\t')
                throw new OpenApiProtocolException(400, "请求头格式无效。");
            var name = line[..colon];
            var value = line[(colon + 1)..].Trim(' ', '\t');
            if (!name.All(IsTokenChar) || value.Any(c => (c < ' ' && c != '\t') || c == '\u007f'))
                throw new OpenApiProtocolException(400, "请求头包含非法字符。");
            if (!headers.TryAdd(name, value))
            {
                if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) && headers[name] == value)
                    continue;
                if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) || name.Equals("Host", StringComparison.OrdinalIgnoreCase))
                    throw new OpenApiProtocolException(400, $"重复的 {name} 请求头。");
                headers[name] += ", " + value;
            }
        }
        if (parts[2] == "HTTP/1.1" && !headers.ContainsKey("Host"))
            throw new OpenApiProtocolException(400, "缺少 Host 请求头。");
        if (headers.ContainsKey("Transfer-Encoding"))
            throw new OpenApiProtocolException(411, "不支持分块传输，请发送带 Content-Length 的请求。");
        long length = -1;
        if (headers.TryGetValue("Content-Length", out var declared))
        {
            if (declared.Length is 0 or > 18 || !declared.All(char.IsAsciiDigit))
                throw new OpenApiProtocolException(400, "Content-Length 无效。");
            length = long.Parse(declared, System.Globalization.CultureInfo.InvariantCulture);
            if (length > options.MaxBodyBytes)
                throw new OpenApiProtocolException(413, $"请求体超过 {options.MaxBodyBytes / 1024 / 1024} MB 上限。");
        }
        if (parts[0] == "POST" && length < 0)
            throw new OpenApiProtocolException(411, "POST 请求必须带 Content-Length。");
        return new OpenApiRequest(parts[0], path, query < 0 ? "" : target[(query + 1)..], headers, length);
    }

    // 按 Content-Length 读取正文；内存随实际收到的数据增长，不按声明长度预先分配。
    public static async Task<ReadOnlyMemory<byte>> ReadBodyAsync(Stream stream, byte[] leftover, long length,
        CancellationToken cancellationToken)
    {
        if (length <= 0)
            return ReadOnlyMemory<byte>.Empty;
        var body = new MemoryStream((int)Math.Min(length, 1024 * 1024));
        body.Write(leftover, 0, (int)Math.Min(leftover.Length, length));
        var chunk = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            while (body.Length < length)
            {
                var wanted = (int)Math.Min(chunk.Length, length - body.Length);
                var count = await stream.ReadAsync(chunk.AsMemory(0, wanted), cancellationToken).ConfigureAwait(false);
                if (count == 0)
                    throw new OpenApiProtocolException(400, "请求体不完整，连接已关闭。");
                body.Write(chunk, 0, count);
            }
        }
        finally { ArrayPool<byte>.Shared.Return(chunk); }
        return body.GetBuffer().AsMemory(0, (int)length);
    }

    private static bool IsTokenChar(char c) => char.IsAsciiLetterOrDigit(c) || "!#$%&'*+-.^_`|~".Contains(c);
}
