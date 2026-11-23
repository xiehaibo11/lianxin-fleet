namespace Lianxin.Fleet.Core.Imaging;

// 比色与找色。颜色为 0xRRGGBB；容差是 R、G、B 各通道允许的最大差值（0–255），透明度不参与比较。
public static class ColorMatcher
{
    public static bool Matches(int actual, int expected, int tolerance) =>
        Math.Abs((actual >> 16 & 0xFF) - (expected >> 16 & 0xFF)) <= tolerance
        && Math.Abs((actual >> 8 & 0xFF) - (expected >> 8 & 0xFF)) <= tolerance
        && Math.Abs((actual & 0xFF) - (expected & 0xFF)) <= tolerance;

    public static bool Matches(PixelImage image, int x, int y, int rgb, int tolerance)
    {
        ArgumentNullException.ThrowIfNull(image);
        return Matches(image.Rgb(x, y), rgb, tolerance);
    }

    // 在像素区域 [left, right] × [top, bottom]（含边界）内从上到下、从左到右找第一个符合的点。
    public static (int X, int Y)? Find(PixelImage image, int rgb, int tolerance, int left, int top, int right, int bottom)
    {
        ArgumentNullException.ThrowIfNull(image);
        left = Math.Clamp(left, 0, image.Width - 1);
        right = Math.Clamp(right, 0, image.Width - 1);
        top = Math.Clamp(top, 0, image.Height - 1);
        bottom = Math.Clamp(bottom, 0, image.Height - 1);
        int red = rgb >> 16 & 0xFF, green = rgb >> 8 & 0xFF, blue = rgb & 0xFF;
        var pixels = image.Rgba;
        for (var y = top; y <= bottom; y++)
        {
            var offset = (y * image.Width + left) * 4;
            for (var x = left; x <= right; x++, offset += 4)
                if (Math.Abs(pixels[offset] - red) <= tolerance && Math.Abs(pixels[offset + 1] - green) <= tolerance
                    && Math.Abs(pixels[offset + 2] - blue) <= tolerance)
                    return (x, y);
        }
        return null;
    }

    // 相对区域（0–1）换算成像素区域，左上角与右下角都包含在内。
    public static (int Left, int Top, int Right, int Bottom) ToPixels(PixelImage image, double left, double top,
        double right, double bottom)
    {
        ArgumentNullException.ThrowIfNull(image);
        var (x1, y1) = image.ToPixel(left, top);
        var (x2, y2) = image.ToPixel(right, bottom);
        return (Math.Min(x1, x2), Math.Min(y1, y2), Math.Max(x1, x2), Math.Max(y1, y2));
    }

    public static string Format(int rgb) => "#" + (rgb & 0xFFFFFF).ToString("X6", System.Globalization.CultureInfo.InvariantCulture);
}
