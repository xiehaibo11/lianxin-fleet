using System.Numerics;

namespace Lianxin.Fleet.Core.Imaging;

// 位置是模板左上角在截图中的像素坐标；Score 为归一化相关系数（与 OpenCV TM_CCOEFF_NORMED 同义），1 表示完全一致。
public readonly record struct TemplateMatch(int X, int Y, int Width, int Height, double Score)
{
    public double CenterX => X + (Width - 1) / 2.0;
    public double CenterY => Y + (Height - 1) / 2.0;
}

// 灰度模板匹配，分两步：
// 1. 粗搜：模板缩小成约 8×8 的方块均值，与截图在【每一个】像素位置开始的同样大小的方块均值比较（滑动方块均值 + 间隔取样），
//    所以目标不论落在什么对齐位置，粗搜得分都和对齐时一样——精确截取的模板在真实位置的粗搜得分就是 1；
// 2. 精比：取粗搜得分的局部最高点（按得分从高到低，最多 MaxCandidates 个），在原图上逐点爬坡求最高的完整相关系数。
// 粗搜的计算量约为 截图像素数 × 粗搜层像素数（≤ 225），与模板大小、形状无关，细长模板也不会退化成逐点全图比对。
public static class TemplateMatcher
{
    private const int MaxCandidates = 48;
    // 窗口灰度标准差低于 0.5 视为纯色区域，相关系数没有意义，记为 0。
    private const double FlatVariance = 0.25;
    // 同时进行的找图不超过一半的处理器核数：几十台手机的脚本同时找图时不会占满线程池、拖慢其他操作。
    private static readonly SemaphoreSlim Workers = new(Math.Max(1, Environment.ProcessorCount / 2));

    public static async Task<TemplateMatch?> FindAsync(PixelImage screen, ImageTemplate template, double threshold,
        CancellationToken cancellationToken = default)
    {
        await Workers.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() => Find(screen, template, threshold), cancellationToken).ConfigureAwait(false);
        }
        finally { Workers.Release(); }
    }

    public static TemplateMatch? Find(PixelImage screen, ImageTemplate template, double threshold)
    {
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(template);
        if (!double.IsFinite(threshold) || threshold is < 0 or > 1)
            throw new ArgumentException("相似度须在 0 到 1 之间。");
        if (template.Width > screen.Width || template.Height > screen.Height)
            throw new ArgumentException("模板图片比屏幕截图还大，请从同一分辨率的截图中截取模板。");
        var full = GrayImage.From(screen);
        int outWidth = full.Width - template.Width + 1, outHeight = full.Height - template.Height + 1;
        var scores = CoarseScores(full.BoxMean(template.FactorX, template.FactorY), template, outWidth, outHeight);
        var candidates = Peaks(scores, outWidth, outHeight, Math.Max(0.1, threshold - 0.4), template);
        TemplateMatch? best = null;
        var cache = new Dictionary<long, double>();
        foreach (var (x, y) in candidates)
        {
            var match = Climb(full, template, x, y, outWidth, outHeight, cache);
            if (best is null || match.Score > best.Value.Score)
                best = match;
            if (best.Value.Score >= 0.9999)
                break;
        }
        return best is { } found && found.Score >= threshold ? found : null;
    }

    // 每个可能的左上角位置的粗搜得分（归一化相关系数），按行存放，宽 outWidth、高 outHeight。
    private static float[] CoarseScores(GrayImage box, ImageTemplate template, int outWidth, int outHeight)
    {
        var level = template.Coarse;
        int fx = template.FactorX, fy = template.FactorY, columns = level.Width, rows = level.Height, n = columns * rows;
        var scores = new float[outWidth * outHeight];
        // 分母：同一取样相位（X mod fx、Y mod fy）的位置共用一张积分图，求窗口内取样值的和与平方和。
        var stride = box.Width / fx + 2;
        var sums = new double[stride * (box.Height / fy + 2)];
        var squares = new double[sums.Length];
        for (var py = 0; py < Math.Min(fy, outHeight); py++)
            for (var px = 0; px < Math.Min(fx, outWidth); px++)
            {
                int gridWidth = (box.Width - 1 - px) / fx + 1, gridHeight = (box.Height - 1 - py) / fy + 1;
                for (var v = 0; v < gridHeight; v++)
                {
                    double row = 0, rowSquares = 0;
                    for (var u = 0; u < gridWidth; u++)
                    {
                        double value = box.Pixels[(py + v * fy) * box.Width + px + u * fx];
                        row += value;
                        rowSquares += value * value;
                        sums[(v + 1) * stride + u + 1] = sums[v * stride + u + 1] + row;
                        squares[(v + 1) * stride + u + 1] = squares[v * stride + u + 1] + rowSquares;
                    }
                }
                double Window(double[] table, int u, int v) => table[(v + rows) * stride + u + columns]
                    - table[v * stride + u + columns] - table[(v + rows) * stride + u] + table[v * stride + u];
                for (int v = 0, y = py; y < outHeight; v++, y += fy)
                    for (int u = 0, x = px; x < outWidth; u++, x += fx)
                    {
                        var sum = Window(sums, u, v);
                        var variance = Window(squares, u, v) - sum * sum / n;
                        scores[y * outWidth + x] = variance <= FlatVariance * n ? 0f : (float)(1 / (Math.Sqrt(variance) * level.Norm));
                    }
            }
        // 分子：模板每个方块的权重乘以截图方块均值图对应的一整行（连续内存，向量化），逐行累加。
        var accumulator = new float[outWidth];
        for (var y = 0; y < outHeight; y++)
        {
            Array.Clear(accumulator);
            for (var j = 0; j < rows; j++)
            {
                var line = box.Pixels.AsSpan((y + j * fy) * box.Width, box.Width);
                for (var i = 0; i < columns; i++)
                    MultiplyAdd(accumulator, line.Slice(i * fx, outWidth), level.Centered[j * columns + i]);
            }
            var row = scores.AsSpan(y * outWidth, outWidth);
            for (var x = 0; x < outWidth; x++)
                row[x] *= accumulator[x];
        }
        return scores;
    }

    private static void MultiplyAdd(Span<float> target, ReadOnlySpan<float> source, float weight)
    {
        var i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            var factor = new Vector<float>(weight);
            for (; i <= target.Length - Vector<float>.Count; i += Vector<float>.Count)
                (new Vector<float>(target[i..]) + new Vector<float>(source[i..]) * factor).CopyTo(target[i..]);
        }
        for (; i < target.Length; i++)
            target[i] += source[i] * weight;
    }

    // 粗搜得分的局部最高点（不低于周围 8 个位置且不低于 cut），按得分从高到低，相距不到半个方块的只留最高的。
    private static List<(int X, int Y)> Peaks(float[] scores, int width, int height, double cut, ImageTemplate template)
    {
        var found = new List<(float Score, int X, int Y)>();
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var score = scores[y * width + x];
                if (score < cut || !Maximum(scores, width, height, x, y, score))
                    continue;
                found.Add((score, x, y));
            }
        found.Sort((a, b) => b.Score.CompareTo(a.Score));
        int spreadX = Math.Max(1, template.FactorX / 2), spreadY = Math.Max(1, template.FactorY / 2);
        var picked = new List<(int X, int Y)>();
        foreach (var (_, x, y) in found)
        {
            if (picked.Count >= MaxCandidates)
                break;
            if (!picked.Any(p => Math.Abs(p.X - x) <= spreadX && Math.Abs(p.Y - y) <= spreadY))
                picked.Add((x, y));
        }
        return picked;
    }

    private static bool Maximum(float[] scores, int width, int height, int x, int y, float score)
    {
        for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if ((dx != 0 || dy != 0) && nx >= 0 && ny >= 0 && nx < width && ny < height && scores[ny * width + nx] > score)
                    return false;
            }
        return true;
    }

    // 从候选位置出发，在原图上向 8 个方向中得分最高的一步移动，直到四周都不更高。算过的位置记在 cache 里，多个候选共用。
    private static TemplateMatch Climb(GrayImage full, ImageTemplate template, int x, int y, int width, int height,
        Dictionary<long, double> cache)
    {
        double Score(int px, int py)
        {
            var key = (long)py * width + px;
            if (!cache.TryGetValue(key, out var value))
                cache[key] = value = DirectScore(full, template.Full, px, py);
            return value;
        }
        var score = Score(x, y);
        for (var step = 0; step < template.FactorX + template.FactorY + 8; step++)
        {
            int bestX = x, bestY = y;
            var bestScore = score;
            for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;
                    var candidate = Score(nx, ny);
                    if (candidate > bestScore)
                        (bestX, bestY, bestScore) = (nx, ny, candidate);
                }
            if (bestX == x && bestY == y)
                break;
            (x, y, score) = (bestX, bestY, bestScore);
        }
        return new TemplateMatch(x, y, template.Width, template.Height, score);
    }

    private static double DirectScore(GrayImage image, TemplateLevel level, int x, int y)
    {
        double sum = 0, squares = 0;
        for (var row = 0; row < level.Height; row++)
        {
            var line = image.Pixels.AsSpan((y + row) * image.Width + x, level.Width);
            foreach (var value in line)
            {
                sum += value;
                squares += (double)value * value;
            }
        }
        var n = level.Width * level.Height;
        var variance = squares - sum * sum / n;
        return variance <= FlatVariance * n ? 0 : Dot(image, level, x, y) / (Math.Sqrt(variance) * level.Norm);
    }

    // 模板去均值后与窗口逐行点积，等于两者协方差之和。
    private static double Dot(GrayImage image, TemplateLevel level, int x, int y)
    {
        var total = 0.0;
        var width = level.Width;
        for (var row = 0; row < level.Height; row++)
        {
            var a = level.Centered.AsSpan(row * width, width);
            var b = image.Pixels.AsSpan((y + row) * image.Width + x, width);
            var i = 0;
            var accumulator = Vector<float>.Zero;
            if (Vector.IsHardwareAccelerated)
                for (; i <= width - Vector<float>.Count; i += Vector<float>.Count)
                    accumulator += new Vector<float>(a[i..]) * new Vector<float>(b[i..]);
            var sum = Vector.Sum(accumulator);
            for (; i < width; i++)
                sum += a[i] * b[i];
            total += sum;
        }
        return total;
    }
}
