using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Imaging;

namespace Lianxin.Fleet.Checks.Scenarios;

// PNG 解码（每种颜色类型、位深和过滤方式）、错误拒绝，以及比色与区域找色。
internal static partial class ImagingChecks
{
    public static Task Decoding()
    {
        const int width = 9, height = 6;
        static (int R, int G, int B, int A) Pixel(int x, int y) => (x * 28, y * 50, (x * y * 37 + 11) % 256, (x + y) * 23 % 256);
        var cases = new List<(string Name, int Type, int Depth, Func<int, int, int[]> Samples, Func<int, int, (int, int, int, int)> Expected, byte[]? Palette, byte[]? Trns)>
        {
            ("RGBA8", 6, 8, (x, y) => [Pixel(x, y).R, Pixel(x, y).G, Pixel(x, y).B, Pixel(x, y).A], Pixel, null, null),
            ("RGB8", 2, 8, (x, y) => [Pixel(x, y).R, Pixel(x, y).G, Pixel(x, y).B], (x, y) => (Pixel(x, y).R, Pixel(x, y).G, Pixel(x, y).B, 255), null, null),
            ("Gray8", 0, 8, (x, y) => [Pixel(x, y).R], (x, y) => (Pixel(x, y).R, Pixel(x, y).R, Pixel(x, y).R, x == 2 ? 0 : 255), null, [0, 56]),
            ("GrayAlpha8", 4, 8, (x, y) => [Pixel(x, y).G, Pixel(x, y).A], (x, y) => (Pixel(x, y).G, Pixel(x, y).G, Pixel(x, y).G, Pixel(x, y).A), null, null),
            ("RGBA16", 6, 16, (x, y) => [Pixel(x, y).R << 8 | 0x5A, Pixel(x, y).G << 8 | 1, Pixel(x, y).B << 8 | 0xFF, Pixel(x, y).A << 8], Pixel, null, null),
            ("RGB16", 2, 16, (x, y) => [Pixel(x, y).R << 8 | 7, Pixel(x, y).G << 8, Pixel(x, y).B << 8 | 9],
                (x, y) => (Pixel(x, y).R, Pixel(x, y).G, Pixel(x, y).B, 255), null, null),
            ("Gray16", 0, 16, (x, y) => [Pixel(x, y).B << 8 | 3], (x, y) => (Pixel(x, y).B, Pixel(x, y).B, Pixel(x, y).B, 255), null, null),
            ("GrayAlpha16", 4, 16, (x, y) => [Pixel(x, y).R << 8, Pixel(x, y).A << 8 | 4], (x, y) => (Pixel(x, y).R, Pixel(x, y).R, Pixel(x, y).R, Pixel(x, y).A), null, null)
        };
        foreach (var depth in new[] { 1, 2, 4 })
        {
            var levels = (1 << depth) - 1;
            cases.Add(($"Gray{depth}", 0, depth, (x, y) => [(x + y) % (levels + 1)], (x, y) =>
            {
                var level = (x + y) % (levels + 1) * 255 / levels;
                return (level, level, level, 255);
            }, null, null));
        }
        foreach (var depth in new[] { 1, 2, 4, 8 })
        {
            var entries = Math.Min(1 << depth, 256);
            var palette = Enumerable.Range(0, entries).SelectMany(i => new[] { (byte)(i * 7), (byte)(255 - i), (byte)(i * 13 % 256) }).ToArray();
            byte[] alpha = [0, 128];
            cases.Add(($"Palette{depth}", 3, depth, (x, y) => [(x * 3 + y) % entries], (x, y) =>
            {
                var index = (x * 3 + y) % entries;
                return (index * 7 % 256, 255 - index, index * 13 % 256, index < alpha.Length ? alpha[index] : 255);
            }, palette, alpha));
        }
        foreach (var (name, type, depth, samples, expected, palette, trns) in cases)
            foreach (var filter in new[] { 0, 1, 2, 3, 4, -1 })
            {
                var png = EncodePng(width, height, type, depth, samples, row => filter < 0 ? row % 5 : filter, palette, trns);
                PixelImage image;
                try
                {
                    image = PngDecoder.Decode(png);
                }
                catch (InvalidDataException error)
                {
                    throw new InvalidOperationException($"{name} filter {filter}: {error.Message}", error);
                }
                Assert.True(image.Width == width && image.Height == height, name);
                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                    {
                        var offset = (y * width + x) * 4;
                        var actual = (image.Rgba[offset], image.Rgba[offset + 1], image.Rgba[offset + 2], image.Rgba[offset + 3]);
                        var (r, g, b, a) = expected(x, y);
                        Assert.True(actual == ((byte)r, (byte)g, (byte)b, (byte)a), $"{name} filter {filter} pixel {x},{y}: {actual} != {(r, g, b, a)}");
                    }
            }

        // 其他编码器生成的 PNG（1×1 灰度+透明）。
        var external = PngDecoder.Decode(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jz5kAAAAASUVORK5CYII="));
        Assert.True(external.Width == 1 && external.Height == 1, "external sample");

        int[] Rgb(int x, int y) => [x, y, 0];
        Reject(EncodePng(4, 4, 2, 8, Rgb, _ => 0, interlace: 1), "隔行");
        Reject(EncodePng(4, 4, 2, 4, (x, y) => [1, 1, 1], _ => 0), "不支持的 PNG 颜色类型");
        Reject(EncodePng(4, 4, 3, 8, (x, y) => [x], _ => 0, palette: [1, 2, 3]), "调色板索引越界");
        Reject(EncodePng(4, 4, 3, 8, (x, y) => [0], _ => 0), "缺少调色板");
        Reject(EncodePng(4, 4, 2, 8, Rgb, _ => 0, tamper: chunks => chunks.Insert(0, ("CgBI", [0, 0, 0, 0]))), "CgBI");
        Reject(EncodePng(4, 4, 2, 8, Rgb, _ => 0, tamper: chunks => chunks.Insert(1, ("zZZz", [1]))), "", accept: true);
        Reject(EncodePng(4, 4, 2, 8, Rgb, _ => 0, tamper: chunks => chunks.Insert(1, ("ABCD", [1]))), "不支持的 PNG 数据块");
        Reject(EncodePng(4, 4, 2, 8, Rgb, _ => 0, tamper: chunks => chunks.RemoveAt(chunks.FindLastIndex(chunk => chunk.Type == "IDAT"))),
            "不完整");
        var good = EncodePng(4, 4, 2, 8, Rgb, _ => 0);
        var corrupted = (byte[])good.Clone();
        corrupted[^20] ^= 0x40;
        Reject(corrupted, "已损坏");
        var checksum = (byte[])good.Clone();
        checksum[^17] ^= 0x01;
        Reject(checksum, "已损坏");
        // 数据块 CRC 错误（常见于某些工具）不影响解码。
        var badCrc = (byte[])good.Clone();
        badCrc[30] ^= 0xFF;
        Assert.Equal(0x030000, PngDecoder.Decode(badCrc).Rgb(3, 0));
        Reject(good[..(good.Length - 15)], "PNG 数据");
        Reject([1, 2, 3, 4, 5, 6, 7, 8, 9], "不是 PNG");
        Reject(EncodePng(4, 4, 2, 8, Rgb, row => row == 2 ? 4 : 0, tamper: chunks =>
        {
            var index = chunks.FindIndex(chunk => chunk.Type == "IHDR");
            var header = (byte[])chunks[index].Body.Clone();
            header[0] = 0x7F;
            chunks[index] = ("IHDR", header);
        }), "超出支持范围");
        return Task.CompletedTask;
    }

    private static void Reject(byte[] png, string fragment, bool accept = false)
    {
        try
        {
            PngDecoder.Decode(png);
        }
        catch (InvalidDataException error)
        {
            Assert.True(!accept && error.Message.Contains(fragment, StringComparison.Ordinal), $"Unexpected message: {error.Message}");
            return;
        }
        Assert.True(accept, "PNG should have been rejected: " + fragment);
    }

    public static Task Colors()
    {
        var pixels = new byte[50 * 40 * 4];
        for (var i = 0; i < pixels.Length; i += 4)
            (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]) = ((byte)10, (byte)20, (byte)30, (byte)255);
        var image = new PixelImage(50, 40, pixels);
        void Paint(int x, int y, byte r, byte g, byte b) => (pixels[(y * 50 + x) * 4], pixels[(y * 50 + x) * 4 + 1], pixels[(y * 50 + x) * 4 + 2]) = (r, g, b);
        Paint(33, 17, 250, 100, 0);
        Paint(5, 35, 250, 100, 0);
        Assert.Equal(0xFA6400, image.Rgb(33, 17));
        Assert.Equal((33, 17), ColorMatcher.Find(image, 0xFA6400, 0, 0, 0, 49, 39));
        Assert.Equal((5, 35), ColorMatcher.Find(image, 0xFA6400, 0, 0, 20, 49, 39));
        Assert.Equal((33, 17), ColorMatcher.Find(image, 0xF06400, 10, 0, 0, 49, 39));
        Assert.Equal(null, ColorMatcher.Find(image, 0xF06400, 9, 0, 0, 49, 39));
        Assert.Equal(null, ColorMatcher.Find(image, 0xFA6400, 0, 34, 0, 49, 30));
        Assert.True(ColorMatcher.Matches(image, 33, 17, 0xFF6A05, 6) && !ColorMatcher.Matches(image, 33, 17, 0xFF6A05, 5), "tolerance per channel");
        Assert.Equal((0, 0, 49, 39), ColorMatcher.ToPixels(image, 0, 0, 1, 1));
        Assert.Equal((25, 20, 49, 39), ColorMatcher.ToPixels(image, 0.5, 0.5, 1, 1));
        var (rx, ry) = image.ToRelative(33, 17);
        Assert.Equal((33, 17), image.ToPixel(rx, ry));
        Assert.Equal("#0A141E", ColorMatcher.Format(image.Rgb(0, 0)));
        var crop = image.Crop(30, 15, 5, 4);
        Assert.True(crop.Width == 5 && crop.Height == 4 && crop.Rgb(3, 2) == 0xFA6400, "crop");
        Assert.Throws<ArgumentOutOfRangeException>(() => image.Crop(48, 0, 5, 5));
        return Task.CompletedTask;
    }
}
