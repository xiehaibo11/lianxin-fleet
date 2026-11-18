using System.Runtime.CompilerServices;
using System.Text;

namespace Lianxin.Fleet.Core.Devices;

// 解析 WDA 画面流：HTTP/1.0 响应头后，每帧为 "--BoundaryString" + Content-Length 头 + JPEG + "\r\n\r\n"。
public static class MjpegReader
{
    private const int MaxHeader = 8 * 1024;
    private const int MaxFrame = 8 * 1024 * 1024;

    public static async IAsyncEnumerable<byte[]> ReadFramesAsync(Stream source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // WDA 收到客户端任意数据后才开始推流。
        await source.WriteAsync("GET / HTTP/1.1\r\nHost: localhost\r\n\r\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
        var input = new BufferedStream(source, 64 * 1024);
        var status = await ReadHeaderAsync(input, cancellationToken).ConfigureAwait(false);
        if (status is null || !status.StartsWith("HTTP/1.", StringComparison.Ordinal) || !status.Contains(" 200 ", StringComparison.Ordinal))
            throw new InvalidDataException("WDA 画面流未就绪。");
        while (await ReadHeaderAsync(input, cancellationToken).ConfigureAwait(false) is { } headers)
        {
            var length = headers.Split("\r\n").Select(line => line.Split(':', 2))
                .Where(parts => parts.Length == 2 && parts[0].Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                .Select(parts => int.TryParse(parts[1].Trim(), out var value) ? value : -1).FirstOrDefault(-1);
            if (length is < 2 or > MaxFrame)
                throw new InvalidDataException("WDA 画面帧长度无效。");
            var frame = new byte[length];
            await input.ReadExactlyAsync(frame, cancellationToken).ConfigureAwait(false);
            if (frame[0] != 0xFF || frame[1] != 0xD8)
                throw new InvalidDataException("WDA 画面帧不是 JPEG。");
            yield return frame;
        }
    }

    // 跳过前导空行，读到空行为止；流结束且没有数据时返回 null。
    private static async Task<string?> ReadHeaderAsync(Stream input, CancellationToken cancellationToken)
    {
        var bytes = new List<byte>(256);
        var one = new byte[1];
        while (true)
        {
            if (await input.ReadAsync(one, cancellationToken).ConfigureAwait(false) == 0)
                return bytes.Count == 0 ? null : throw new EndOfStreamException("WDA 画面流中断。");
            if (bytes.Count == 0 && one[0] is (byte)'\r' or (byte)'\n')
                continue;
            bytes.Add(one[0]);
            if (bytes.Count > MaxHeader)
                throw new InvalidDataException("WDA 画面流头部过大。");
            var count = bytes.Count;
            if (count >= 4 && bytes[count - 4] == '\r' && bytes[count - 3] == '\n' && bytes[count - 2] == '\r' && bytes[count - 1] == '\n')
                return Encoding.ASCII.GetString(bytes.ToArray(), 0, count - 4);
        }
    }
}
