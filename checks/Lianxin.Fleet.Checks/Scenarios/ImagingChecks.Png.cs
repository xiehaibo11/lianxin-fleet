using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Lianxin.Fleet.Core.Imaging;

namespace Lianxin.Fleet.Checks.Scenarios;

// 检查用的 PNG 编码器：按指定颜色类型、位深和过滤方式生成标准 PNG，用来验证解码器。
internal static partial class ImagingChecks
{
    // samples(x, y) 返回该像素各通道的原始样本值；filter(row) 选择每行的过滤类型（0–4）。
    internal static byte[] EncodePng(int width, int height, int colorType, int bitDepth, Func<int, int, int[]> samples,
        Func<int, int> filter, byte[]? palette = null, byte[]? transparency = null, int interlace = 0,
        Action<List<(string Type, byte[] Body)>>? tamper = null)
    {
        var channels = colorType switch
        {
            0 or 3 => 1,
            2 => 3,
            4 => 2,
            _ => 4
        };
        var stride = (width * channels * bitDepth + 7) / 8;
        var bpp = Math.Max(1, channels * bitDepth / 8);
        using var raw = new MemoryStream();
        var previous = new byte[stride];
        for (var y = 0; y < height; y++)
        {
            var line = new byte[stride];
            var bit = 0;
            for (var x = 0; x < width; x++)
                foreach (var sample in samples(x, y))
                {
                    if (bitDepth == 16)
                        BinaryPrimitives.WriteUInt16BigEndian(line.AsSpan(bit / 8), (ushort)sample);
                    else if (bitDepth == 8)
                        line[bit / 8] = (byte)sample;
                    else
                        line[bit / 8] |= (byte)(sample << (8 - bitDepth - bit % 8));
                    bit += bitDepth;
                }
            var type = filter(y);
            raw.WriteByte((byte)type);
            for (var i = 0; i < stride; i++)
            {
                int left = i >= bpp ? line[i - bpp] : 0, up = previous[i], corner = i >= bpp ? previous[i - bpp] : 0;
                var predictor = type switch
                {
                    0 => 0,
                    1 => left,
                    2 => up,
                    3 => (left + up) / 2,
                    _ => Paeth(left, up, corner)
                };
                raw.WriteByte((byte)(line[i] - predictor));
            }
            previous = line;
        }
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(raw.ToArray());
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = (byte)bitDepth;
        header[9] = (byte)colorType;
        header[12] = (byte)interlace;
        var chunks = new List<(string Type, byte[] Body)> { ("IHDR", header) };
        if (palette is not null)
            chunks.Add(("PLTE", palette));
        if (transparency is not null)
            chunks.Add(("tRNS", transparency));
        // 拆成两个 IDAT，验证多块拼接。
        var data = compressed.ToArray();
        chunks.Add(("tEXt", Encoding.ASCII.GetBytes("Comment\0check")));
        chunks.Add(("IDAT", data[..(data.Length / 2)]));
        chunks.Add(("IDAT", data[(data.Length / 2)..]));
        chunks.Add(("IEND", []));
        tamper?.Invoke(chunks);
        using var output = new MemoryStream();
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        foreach (var (type, body) in chunks)
        {
            var typeBytes = Encoding.ASCII.GetBytes(type);
            var length = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, body.Length);
            output.Write(length);
            output.Write(typeBytes);
            output.Write(body);
            var crc = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(crc, Crc([.. typeBytes, .. body]));
            output.Write(crc);
        }
        return output.ToArray();
    }

    // 8 位 RGB 或 RGBA 的便捷编码。
    internal static byte[] EncodePng(PixelImage image, bool alpha = true) =>
        EncodePng(image.Width, image.Height, alpha ? 6 : 2, 8, (x, y) =>
        {
            var offset = (y * image.Width + x) * 4;
            return alpha
                ? [image.Rgba[offset], image.Rgba[offset + 1], image.Rgba[offset + 2], image.Rgba[offset + 3]]
                : [image.Rgba[offset], image.Rgba[offset + 1], image.Rgba[offset + 2]];
        }, row => row % 5);

    private static int Paeth(int left, int up, int corner)
    {
        var estimate = left + up - corner;
        int toLeft = Math.Abs(estimate - left), toUp = Math.Abs(estimate - up), toCorner = Math.Abs(estimate - corner);
        return toLeft <= toUp && toLeft <= toCorner ? left : toUp <= toCorner ? up : corner;
    }

    private static uint Crc(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc ^= value;
            for (var k = 0; k < 8; k++)
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }
        return crc ^ 0xFFFFFFFFu;
    }
}
