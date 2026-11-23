using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Imaging;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class ImagingChecks
{
    // 精确截取的模板：不论目标落在什么对齐位置、贴着哪条边，都找到原位置，不会返回别处；细长模板也很快；并发找图有上限。
    public static async Task Alignment()
    {
        var screens = new (string Kind, PixelImage Image)[] { ("界面", Screen(1170, 2532, 9)), ("笔画", Strokes(1170, 2532, 4)), ("噪点", Noise(1170, 2532, 3)) };
        foreach (var (kind, screen) in screens)
            foreach (var size in new[] { 96, 120 })
                foreach (var (dx, dy) in new[] { (0, 0), (3, 5), (5, 2), (7, 7) })
                {
                    int x = 400 + dx, y = 1200 + dy;
                    var found = TemplateMatcher.Find(screen, ImageTemplate.From(screen.Crop(x, y, size, size / 2)), 0.9);
                    Assert.True(found is { } match && match.X == x && match.Y == y, $"{kind} {size}×{size / 2} at +{dx},+{dy}: {found}");
                }
        // 评审复现的两例：贴底边的 96×96 原来找不到；未对齐的 96×48 原来返回了 (334,2273)。
        var bottom = Screen(1170, 2532, 21);
        Assert.True(TemplateMatcher.Find(bottom, ImageTemplate.From(bottom.Crop(0, 2436, 96, 96)), 0.9) is { X: 0, Y: 2436 }, "bottom edge");
        Assert.True(TemplateMatcher.Find(screens[0].Image, ImageTemplate.From(screens[0].Image.Crop(402, 1202, 96, 48)), 0.9) is { X: 402, Y: 1202 },
            "unaligned 96×48");
        foreach (var (width, height) in new[] { (96, 96), (61, 61), (100, 37) })
            foreach (var (x, y) in new[] { (0, 0), (1170 - width, 2532 - height), (1170 - width, 0), (0, 2532 - height) })
                Assert.True(TemplateMatcher.Find(bottom, ImageTemplate.From(bottom.Crop(x, y, width, height)), 0.9) is { } edge
                    && edge.X == x && edge.Y == y, $"corner {width}×{height} at {x},{y}");

        // 细长模板按两个方向分别缩小，不再退化成逐点全图比对（原来 400×20 约 2 秒、23×600 约 12 秒）。
        var large = Screen(1170, 2532, 5);
        foreach (var (width, height) in new[] { (400, 20), (600, 23), (23, 600), (300, 30) })
        {
            var template = ImageTemplate.From(large.Crop(100, 1000, width, height));
            var timer = Stopwatch.StartNew();
            var found = TemplateMatcher.Find(large, template, 0.9);
            Assert.True(found is { X: 100, Y: 1000 } && timer.ElapsedMilliseconds < 1000, $"{width}×{height}: {found} in {timer.ElapsedMilliseconds} ms");
        }

        // 32 台设备同时找图：经受限的工作线程计算，结果不受影响。
        var crop = ImageTemplate.From(large.Crop(700, 1800, 96, 96));
        var results = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => TemplateMatcher.FindAsync(large, crop, 0.9)));
        Assert.True(results.All(result => result is { X: 700, Y: 1800 }), "concurrent FindAsync");
    }

    // 文件头声称巨大尺寸、实际只有几十字节数据的 PNG：按数据量在分配像素内存前拒绝；调用方可以再收紧像素上限。
    public static Task PngLimits()
    {
        var bogus = Png(16384, 1525, 16, 6, new byte[64]);
        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<InvalidDataException>(() => PngDecoder.Decode(bogus));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(bogus.Length < 200 && allocated < 4 * 1024 * 1024, $"{bogus.Length} bytes allocated {allocated / 1024} KB");
        var big = EncodePng(1200, 1200, 6, 8, (x, y) => [x % 256, y % 256, 0, 255], _ => 0);
        Assert.True(PngDecoder.Decode(big) is { Width: 1200, Height: 1200 }, "buffer grows to the real size");
        try
        {
            PngDecoder.Decode(big, 1000 * 1000);
            throw new InvalidOperationException("caller's pixel limit ignored");
        }
        catch (InvalidDataException error)
        {
            Assert.True(error.Message.Contains("1200×1200"), error.Message);
        }
        return Task.CompletedTask;
    }

    // 只有文件头和一段 zlib 数据（raw 未压缩前的字节）的 PNG。
    private static byte[] Png(int width, int height, int bitDepth, int colorType, byte[] raw)
    {
        using var output = new MemoryStream();
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        void Chunk(string type, byte[] body)
        {
            var length = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(length, (uint)body.Length);
            output.Write(length);
            output.Write(Encoding.ASCII.GetBytes(type));
            output.Write(body);
            output.Write(new byte[4]);
        }
        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), (uint)height);
        header[8] = (byte)bitDepth;
        header[9] = (byte)colorType;
        Chunk("IHDR", header);
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
            zlib.Write(raw);
        Chunk("IDAT", compressed.ToArray());
        Chunk("IEND", []);
        return output.ToArray();
    }

    // 随机噪点（与评审复现用的生成器相同）。
    private static PixelImage Noise(int width, int height, int seed)
    {
        var random = new Random(seed);
        var rgba = new byte[width * height * 4];
        random.NextBytes(rgba);
        for (var i = 3; i < rgba.Length; i += 4)
            rgba[i] = 255;
        return new PixelImage(width, height, rgba);
    }

    // 浅色底上的 2–3 像素深色短笔画，类似 3 倍屏上的文字。
    private static PixelImage Strokes(int width, int height, int seed)
    {
        var random = new Random(seed);
        var rgba = new byte[width * height * 4];
        Array.Fill(rgba, (byte)245);
        for (var k = 0; k < width * height / 60; k++)
        {
            int x = random.Next(width - 12), y = random.Next(height - 12), length = random.Next(4, 12), thickness = random.Next(2, 4);
            var horizontal = random.Next(2) == 0;
            for (var i = 0; i < length; i++)
                for (var t = 0; t < thickness; t++)
                {
                    var offset = ((horizontal ? y + t : y + i) * width + (horizontal ? x + i : x + t)) * 4;
                    rgba[offset] = rgba[offset + 1] = rgba[offset + 2] = 30;
                }
        }
        for (var i = 3; i < rgba.Length; i += 4)
            rgba[i] = 255;
        return new PixelImage(width, height, rgba);
    }
}
