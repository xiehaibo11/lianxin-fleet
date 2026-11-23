using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Lianxin.Fleet.Core.Imaging;

// 不依赖第三方库的 PNG 解码：灰度、RGB、调色板、灰度+透明、RGBA，位深 1–16，不支持隔行扫描。
// 解码失败统一抛出 InvalidDataException，消息可直接显示给用户。
// 与多数轻量解码器一样不校验数据块 CRC（有些工具写出的 CRC 本身就是错的）；图像数据由 zlib 的 Adler-32 校验。
public static partial class PngDecoder
{
    public const int MaxDimension = 16_384;
    public const long MaxPixels = 25_000_000;
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    private sealed record Header(int Width, int Height, int BitDepth, int ColorType)
    {
        public int Channels => ColorType switch
        {
            0 or 3 => 1,
            2 => 3,
            4 => 2,
            _ => 4
        };
        public int Stride => (int)(((long)Width * Channels * BitDepth + 7) / 8);
        public int BytesPerPixel => Math.Max(1, Channels * BitDepth / 8);
    }

    public static bool IsPng(ReadOnlySpan<byte> data) => data.StartsWith(Signature);

    // maxPixels：调用方能接受的最大像素数（例如模板图片），文件头超出时在分配任何像素内存之前就拒绝。
    public static PixelImage Decode(byte[] data, long maxPixels = MaxPixels)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (!IsPng(data))
            throw new InvalidDataException("不是 PNG 图片。");
        Header? header = null;
        byte[]? palette = null;
        byte[]? transparency = null;
        using var compressed = new MemoryStream();
        var position = Signature.Length;
        var ended = false;
        while (!ended)
        {
            if (position + 12 > data.Length)
                throw new InvalidDataException("PNG 数据不完整。");
            var length = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(position));
            if (length > int.MaxValue || position + 12L + length > data.Length)
                throw new InvalidDataException("PNG 数据块长度无效。");
            var type = Encoding.ASCII.GetString(data, position + 4, 4);
            var body = data.AsSpan(position + 8, (int)length);
            if (header is null && type != "IHDR")
                throw new InvalidDataException(type == "CgBI" ? "不支持苹果 CgBI 优化格式的 PNG，请用普通截图或另存为标准 PNG。"
                    : "PNG 缺少文件头。");
            switch (type)
            {
                case "IHDR" when header is null:
                    header = ReadHeader(body, Math.Min(maxPixels, MaxPixels));
                    break;
                case "PLTE":
                    if (length % 3 != 0 || length == 0 || length > 768)
                        throw new InvalidDataException("PNG 调色板无效。");
                    palette = body.ToArray();
                    break;
                case "tRNS":
                    transparency = body.ToArray();
                    break;
                case "IDAT":
                    compressed.Write(body);
                    break;
                case "IEND":
                    ended = true;
                    break;
                default:
                    // 首字母大写的数据块是解码必需的；不认识就不能保证结果正确。
                    if (char.IsUpper(type[0]))
                        throw new InvalidDataException($"不支持的 PNG 数据块 {type}。");
                    break;
            }
            position += 12 + (int)length;
        }
        if (header!.ColorType == 3 && palette is null)
            throw new InvalidDataException("调色板 PNG 缺少调色板。");
        var raw = Inflate(compressed, header);
        Unfilter(raw, header);
        return new PixelImage(header.Width, header.Height, ToRgba(raw, header, palette, transparency));
    }

    private static Header ReadHeader(ReadOnlySpan<byte> body, long maxPixels)
    {
        if (body.Length != 13)
            throw new InvalidDataException("PNG 文件头无效。");
        var width = BinaryPrimitives.ReadUInt32BigEndian(body);
        var height = BinaryPrimitives.ReadUInt32BigEndian(body[4..]);
        if (width is < 1 or > MaxDimension || height is < 1 or > MaxDimension || (long)width * height > maxPixels)
            throw new InvalidDataException($"PNG 尺寸 {width}×{height} 超出支持范围。");
        int bitDepth = body[8], colorType = body[9];
        var valid = colorType switch
        {
            0 => bitDepth is 1 or 2 or 4 or 8 or 16,
            3 => bitDepth is 1 or 2 or 4 or 8,
            2 or 4 or 6 => bitDepth is 8 or 16,
            _ => false
        };
        if (!valid)
            throw new InvalidDataException($"不支持的 PNG 颜色类型 {colorType}（位深 {bitDepth}）。");
        if (body[10] != 0 || body[11] != 0)
            throw new InvalidDataException("不支持的 PNG 压缩或过滤方式。");
        if (body[12] != 0)
            throw new InvalidDataException("不支持隔行扫描的 PNG，请另存为非隔行格式。");
        return new Header((int)width, (int)height, bitDepth, colorType);
    }

    private static byte[] Inflate(MemoryStream compressed, Header header)
    {
        var size = (long)header.Height * (header.Stride + 1);
        if (size > 512L * 1024 * 1024)
            throw new InvalidDataException("PNG 图片过大。");
        // deflate 最多约 1032:1：文件头声称的尺寸远大于压缩数据能解出的量时，不分配内存直接拒绝。
        if (size > compressed.Length * 1032L + 4096)
            throw new InvalidDataException("PNG 图像数据不完整或已损坏。");
        // 缓冲区随解出的数据增长，到达声明的大小为止；数据不够时不会先分配整幅图的内存。
        var raw = new byte[(int)Math.Min(size, 4L * 1024 * 1024)];
        compressed.Position = 0;
        try
        {
            using var inflater = new ZLibStream(compressed, CompressionMode.Decompress, leaveOpen: true);
            var filled = 0;
            while (filled < size)
            {
                if (filled == raw.Length)
                    Array.Resize(ref raw, (int)Math.Min(size, raw.Length * 2L));
                var count = inflater.Read(raw, filled, raw.Length - filled);
                if (count == 0)
                    throw new EndOfStreamException();
                filled += count;
            }
            // 读到压缩流末尾才会核对 Adler-32；多余的尾部数据最多读 64 KB 后忽略。
            var tail = new byte[4096];
            for (var extra = 0; extra < 16; extra++)
                if (inflater.Read(tail) == 0)
                    break;
        }
        catch (Exception error) when (error is EndOfStreamException or InvalidDataException or IOException)
        {
            throw new InvalidDataException("PNG 图像数据不完整或已损坏。", error);
        }
        return raw;
    }

    // 每行第一个字节是过滤类型，还原后原地覆盖；过滤字节保留不用。
    private static void Unfilter(byte[] raw, Header header)
    {
        var stride = header.Stride;
        var bpp = header.BytesPerPixel;
        for (var row = 0; row < header.Height; row++)
        {
            var start = row * (stride + 1);
            var line = raw.AsSpan(start + 1, stride);
            var previous = row == 0 ? Span<byte>.Empty : raw.AsSpan(start - stride, stride);
            switch (raw[start])
            {
                case 0:
                    break;
                case 1:
                    for (var i = bpp; i < stride; i++)
                        line[i] += line[i - bpp];
                    break;
                case 2:
                    if (row > 0)
                        for (var i = 0; i < stride; i++)
                            line[i] += previous[i];
                    break;
                case 3:
                    for (var i = 0; i < stride; i++)
                    {
                        var left = i >= bpp ? line[i - bpp] : 0;
                        var up = row > 0 ? previous[i] : 0;
                        line[i] += (byte)((left + up) >> 1);
                    }
                    break;
                case 4:
                    for (var i = 0; i < stride; i++)
                    {
                        var left = i >= bpp ? line[i - bpp] : 0;
                        var up = row > 0 ? previous[i] : 0;
                        var corner = i >= bpp && row > 0 ? previous[i - bpp] : 0;
                        line[i] += Paeth(left, up, corner);
                    }
                    break;
                default:
                    throw new InvalidDataException($"PNG 第 {row + 1} 行的过滤类型 {raw[start]} 无效。");
            }
        }
    }

    private static byte Paeth(int left, int up, int corner)
    {
        var estimate = left + up - corner;
        var toLeft = Math.Abs(estimate - left);
        var toUp = Math.Abs(estimate - up);
        var toCorner = Math.Abs(estimate - corner);
        return (byte)(toLeft <= toUp && toLeft <= toCorner ? left : toUp <= toCorner ? up : corner);
    }
}
