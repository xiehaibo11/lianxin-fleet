namespace Lianxin.Fleet.Core.Imaging;

// 解码后的图片：每像素 4 字节 RGBA，逐行从左上角开始。
public sealed class PixelImage
{
    public PixelImage(int width, int height, byte[] rgba)
    {
        ArgumentNullException.ThrowIfNull(rgba);
        if (width < 1 || height < 1 || (long)width * height * 4 != rgba.Length)
            throw new ArgumentException("图片尺寸与像素数据不一致。");
        Width = width;
        Height = height;
        Rgba = rgba;
    }

    public int Width
    {
        get;
    }
    public int Height
    {
        get;
    }
    public byte[] Rgba
    {
        get;
    }

    // 0xRRGGBB，忽略透明度。
    public int Rgb(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            throw new ArgumentOutOfRangeException(nameof(x), "像素坐标超出图片范围。");
        var offset = (y * Width + x) * 4;
        return Rgba[offset] << 16 | Rgba[offset + 1] << 8 | Rgba[offset + 2];
    }

    // 相对位置（0–1）与像素的换算，和点按路径一致：0 对应第一个像素，1 对应最后一个像素。
    public (int X, int Y) ToPixel(double relativeX, double relativeY) =>
        ((int)Math.Round(Math.Clamp(relativeX, 0, 1) * (Width - 1), MidpointRounding.AwayFromZero),
            (int)Math.Round(Math.Clamp(relativeY, 0, 1) * (Height - 1), MidpointRounding.AwayFromZero));

    public (double X, double Y) ToRelative(double pixelX, double pixelY) =>
        (Width == 1 ? 0 : Math.Clamp(pixelX / (Width - 1), 0, 1), Height == 1 ? 0 : Math.Clamp(pixelY / (Height - 1), 0, 1));

    // 截取一块区域（像素坐标，含左上角，不含右下角）。
    public PixelImage Crop(int left, int top, int width, int height)
    {
        if (left < 0 || top < 0 || width < 1 || height < 1 || left + width > Width || top + height > Height)
            throw new ArgumentOutOfRangeException(nameof(left), "截取区域超出图片范围。");
        var pixels = new byte[width * height * 4];
        for (var row = 0; row < height; row++)
            Buffer.BlockCopy(Rgba, ((top + row) * Width + left) * 4, pixels, row * width * 4, width * 4);
        return new PixelImage(width, height, pixels);
    }
}
