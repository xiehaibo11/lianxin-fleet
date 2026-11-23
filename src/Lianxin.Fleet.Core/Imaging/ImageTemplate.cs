namespace Lianxin.Fleet.Core.Imaging;

// 灰度浮点图，供模板匹配使用。
internal sealed class GrayImage(int width, int height, float[] pixels)
{
    public int Width { get; } = width;
    public int Height { get; } = height;
    public float[] Pixels { get; } = pixels;

    public static GrayImage From(PixelImage image)
    {
        var rgba = image.Rgba;
        var pixels = new float[image.Width * image.Height];
        for (int i = 0, offset = 0; i < pixels.Length; i++, offset += 4)
            pixels[i] = rgba[offset] * 0.299f + rgba[offset + 1] * 0.587f + rgba[offset + 2] * 0.114f;
        return new GrayImage(image.Width, image.Height, pixels);
    }

    // 按 factorX×factorY 的不重叠方块取平均缩小，多余的边缘像素丢弃（模板的粗搜层）。
    public GrayImage Downscale(int factorX, int factorY)
    {
        if (factorX == 1 && factorY == 1)
            return this;
        int width = Width / factorX, height = Height / factorY;
        var output = new float[width * height];
        var scale = 1f / (factorX * factorY);
        for (var y = 0; y < height; y++)
            for (var row = 0; row < factorY; row++)
            {
                var source = (y * factorY + row) * Width;
                for (var x = 0; x < width; x++)
                {
                    var sum = 0f;
                    for (var column = 0; column < factorX; column++)
                        sum += Pixels[source + x * factorX + column];
                    output[y * width + x] += sum * scale;
                }
            }
        return new GrayImage(width, height, output);
    }

    // 以每个像素为左上角的 factorX×factorY 方块平均值（滑动窗口，逐像素都有）。
    // 截图在任意位置按这张图取样，都等于从那个位置开始划分方块后的平均值，所以粗搜与目标的对齐方式无关。
    public GrayImage BoxMean(int factorX, int factorY)
    {
        if (factorX == 1 && factorY == 1)
            return this;
        int width = Width - factorX + 1, height = Height - factorY + 1;
        var rows = new float[width * Height];
        for (var y = 0; y < Height; y++)
        {
            var line = Pixels.AsSpan(y * Width, Width);
            double sum = 0;
            for (var x = 0; x < factorX; x++)
                sum += line[x];
            rows[y * width] = (float)sum;
            for (var x = 1; x < width; x++)
            {
                sum += line[x + factorX - 1] - line[x - 1];
                rows[y * width + x] = (float)sum;
            }
        }
        var output = new float[width * height];
        var column = new double[width];
        for (var y = 0; y < factorY; y++)
            for (var x = 0; x < width; x++)
                column[x] += rows[y * width + x];
        var scale = 1.0 / (factorX * factorY);
        for (var y = 0; y < height; y++)
        {
            if (y > 0)
                for (var x = 0; x < width; x++)
                    column[x] += rows[(y + factorY - 1) * width + x] - rows[(y - 1) * width + x];
            for (var x = 0; x < width; x++)
                output[y * width + x] = (float)(column[x] * scale);
        }
        return new GrayImage(width, height, output);
    }
}

// 模板的一层：去掉均值后的灰度和它的能量（平方和的平方根）。
internal sealed class TemplateLevel
{
    public TemplateLevel(GrayImage image)
    {
        Width = image.Width;
        Height = image.Height;
        var mean = image.Pixels.Average();
        Centered = image.Pixels.Select(value => value - mean).ToArray();
        Norm = Math.Sqrt(Centered.Sum(value => (double)value * value));
    }

    public int Width
    {
        get;
    }
    public int Height
    {
        get;
    }
    public float[] Centered
    {
        get;
    }
    public double Norm
    {
        get;
    }
    public double Deviation => Norm / Math.Sqrt(Width * Height);
}

// 预处理过的模板图片：完整分辨率一层，加上横纵各缩小 FactorX、FactorY 倍用于粗搜的一层。透明度不参与匹配。
public sealed class ImageTemplate
{
    public const int MinSide = 4;
    public const int MaxSide = 2000;
    // 粗搜层每边目标约 8 像素（8–15）：细长模板（例如 400×20）两个方向分别缩小，不会退化成全分辨率逐点比对。
    private const int CoarseSide = 8;
    private const double MinDeviation = 2;

    private ImageTemplate(TemplateLevel full, TemplateLevel coarse, int factorX, int factorY)
    {
        Full = full;
        Coarse = coarse;
        FactorX = factorX;
        FactorY = factorY;
    }

    internal TemplateLevel Full
    {
        get;
    }
    internal TemplateLevel Coarse
    {
        get;
    }
    public int FactorX
    {
        get;
    }
    public int FactorY
    {
        get;
    }
    public int Factor => Math.Max(FactorX, FactorY);
    public int Width => Full.Width;
    public int Height => Full.Height;

    public static ImageTemplate From(PixelImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (Math.Min(image.Width, image.Height) < MinSide || Math.Max(image.Width, image.Height) > MaxSide)
            throw new ArgumentException($"模板图片边长须为 {MinSide} 至 {MaxSide} 像素。");
        var gray = GrayImage.From(image);
        var full = new TemplateLevel(gray);
        if (full.Deviation < MinDeviation)
            throw new ArgumentException("模板图片几乎是纯色，无法可靠找图，请改用「点击颜色」。");
        int factorX = Math.Max(1, image.Width / CoarseSide), factorY = Math.Max(1, image.Height / CoarseSide);
        // 缩小后细节被抹平（几乎纯色）就退回更小的倍数。
        while (factorX > 1 || factorY > 1)
        {
            var coarse = new TemplateLevel(gray.Downscale(factorX, factorY));
            if (coarse.Deviation >= MinDeviation)
                return new ImageTemplate(full, coarse, factorX, factorY);
            factorX = Math.Max(1, factorX / 2);
            factorY = Math.Max(1, factorY / 2);
        }
        return new ImageTemplate(full, full, 1, 1);
    }
}
