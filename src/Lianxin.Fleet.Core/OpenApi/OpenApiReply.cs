using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Lianxin.Fleet.Core.OpenApi;

// 一次响应：JSON 信封 {"code","msg","data"}，或截图类接口的原始图片字节。
public sealed class OpenApiReply
{
    private static readonly JsonSerializerOptions Json = new()
    {
        // 中文原样输出，HTML 敏感字符仍转义。
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    private OpenApiReply(int status, int code, string contentType, ReadOnlyMemory<byte> body)
    {
        Status = status;
        Code = code;
        ContentType = contentType;
        Body = body;
    }

    public int Status
    {
        get;
    }
    // 图片响应没有信封，Code 为 0。
    public int Code
    {
        get;
    }
    public string ContentType
    {
        get;
    }
    public ReadOnlyMemory<byte> Body
    {
        get;
    }
    public IReadOnlyList<KeyValuePair<string, string>> Headers { get; private init; } = [];

    // 附加响应头；名称和值来自本服务代码，仍拒绝换行以免拼出额外的响应头。
    public OpenApiReply WithHeader(string name, string value)
    {
        if ((name + value).Any(c => c is '\r' or '\n'))
            throw new ArgumentException("响应头不能包含换行。");
        return new(Status, Code, ContentType, Body)
        {
            Headers = [.. Headers, new(name, value)]
        };
    }

    public static OpenApiReply Ok(object? data = null, string msg = "成功") => Envelope(OpenApiCodes.Ok, msg, data ?? true);

    public static OpenApiReply Fail(int code, string msg, object? data = null, int status = 200) =>
        Envelope(code, msg, data ?? false, status);

    public static OpenApiReply Image(byte[] bytes) => new(200, OpenApiCodes.Ok, ImageType(bytes), bytes);

    public static OpenApiReply Envelope(int code, string msg, object? data, int status = 200)
    {
        using var output = new MemoryStream();
        using (var writer = new Utf8JsonWriter(output, new JsonWriterOptions { Encoder = Json.Encoder }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("code", code);
            writer.WriteString("msg", msg);
            writer.WritePropertyName("data");
            JsonSerializer.Serialize(writer, data, data?.GetType() ?? typeof(object), Json);
            writer.WriteEndObject();
        }
        return new(status, code, "application/json; charset=utf-8", output.ToArray());
    }

    // 按文件头判断图片格式；USB 截图服务在部分旧系统上返回 TIFF。
    public static string ImageType(ReadOnlySpan<byte> image) =>
        image.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47 }) ? "image/png"
        : image.StartsWith(new byte[] { 0xff, 0xd8, 0xff }) ? "image/jpeg"
        : image.StartsWith("II*\0"u8) || image.StartsWith("MM\0*"u8) ? "image/tiff"
        : "application/octet-stream";

    internal async Task WriteAsync(Stream stream, CancellationToken cancellationToken)
    {
        var head = new StringBuilder()
            .Append("HTTP/1.1 ").Append(Status).Append(' ').Append(Reason(Status)).Append("\r\n")
            .Append("Content-Type: ").Append(ContentType).Append("\r\n")
            .Append("Content-Length: ").Append(Body.Length).Append("\r\n")
            .Append("Connection: close\r\n")
            .Append("Cache-Control: no-store\r\n")
            .Append("X-Content-Type-Options: nosniff\r\n");
        foreach (var (name, value) in Headers)
            head.Append(name).Append(": ").Append(value).Append("\r\n");
        head.Append("\r\n");
        await stream.WriteAsync(Encoding.ASCII.GetBytes(head.ToString()), cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(Body, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static string Reason(int status) => status switch
    {
        100 => "Continue",
        200 => "OK",
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        405 => "Method Not Allowed",
        408 => "Request Timeout",
        411 => "Length Required",
        413 => "Content Too Large",
        415 => "Unsupported Media Type",
        431 => "Request Header Fields Too Large",
        500 => "Internal Server Error",
        503 => "Service Unavailable",
        _ => "Status"
    };

    // dumpXml 成功时的响应：界面树 XML 本身，没有信封。
    public static OpenApiReply Xml(string xml) =>
        new(200, OpenApiCodes.Ok, "application/xml; charset=utf-8", Encoding.UTF8.GetBytes(xml));
}
