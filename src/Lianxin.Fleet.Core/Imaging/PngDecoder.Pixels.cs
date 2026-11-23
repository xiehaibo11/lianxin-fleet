namespace Lianxin.Fleet.Core.Imaging;

// 把还原过滤后的扫描行统一转换成 8 位 RGBA。16 位样本取高字节，低位深灰度按比例放大到 0–255。
public static partial class PngDecoder
{
    private static byte[] ToRgba(byte[] raw, Header header, byte[]? palette, byte[]? transparency)
    {
        var width = header.Width;
        var stride = header.Stride;
        var output = new byte[(long)width * header.Height * 4];
        var depth = header.BitDepth;
        var paletteAlpha = header.ColorType == 3 ? transparency : null;
        var key = header.ColorType is 0 or 2 && transparency is not null ? TransparentKey(transparency, header) : null;
        for (var row = 0; row < header.Height; row++)
        {
            var start = row * (stride + 1) + 1;
            var target = row * width * 4;
            if (depth == 8 && header.ColorType == 6)
            {
                Buffer.BlockCopy(raw, start, output, target, width * 4);
                continue;
            }
            for (var x = 0; x < width; x++, target += 4)
            {
                switch (header.ColorType)
                {
                    case 0:
                        {
                            var gray = Sample(raw, start, x, depth);
                            var level = Scale(gray, depth);
                            output[target] = output[target + 1] = output[target + 2] = level;
                            output[target + 3] = key is not null && key[0] == gray ? (byte)0 : (byte)255;
                            break;
                        }
                    case 2:
                        {
                            int r = Sample(raw, start, x * 3, depth), g = Sample(raw, start, x * 3 + 1, depth),
                                b = Sample(raw, start, x * 3 + 2, depth);
                            output[target] = Scale(r, depth);
                            output[target + 1] = Scale(g, depth);
                            output[target + 2] = Scale(b, depth);
                            output[target + 3] = key is not null && key[0] == r && key[1] == g && key[2] == b ? (byte)0 : (byte)255;
                            break;
                        }
                    case 3:
                        {
                            var index = Sample(raw, start, x, depth);
                            if (index * 3 + 2 >= palette!.Length)
                                throw new InvalidDataException("PNG 调色板索引越界。");
                            output[target] = palette[index * 3];
                            output[target + 1] = palette[index * 3 + 1];
                            output[target + 2] = palette[index * 3 + 2];
                            output[target + 3] = paletteAlpha is not null && index < paletteAlpha.Length ? paletteAlpha[index] : (byte)255;
                            break;
                        }
                    case 4:
                        {
                            var gray = Scale(Sample(raw, start, x * 2, depth), depth);
                            output[target] = output[target + 1] = output[target + 2] = gray;
                            output[target + 3] = Scale(Sample(raw, start, x * 2 + 1, depth), depth);
                            break;
                        }
                    default:
                        for (var channel = 0; channel < 4; channel++)
                            output[target + channel] = Scale(Sample(raw, start, x * 4 + channel, depth), depth);
                        break;
                }
            }
        }
        return output;
    }

    // 第 index 个样本的原始值（未缩放）。
    private static int Sample(byte[] raw, int rowStart, int index, int depth)
    {
        switch (depth)
        {
            case 8:
                return raw[rowStart + index];
            case 16:
                return raw[rowStart + index * 2] << 8 | raw[rowStart + index * 2 + 1];
            default:
                var bit = index * depth;
                var shift = 8 - depth - (bit & 7);
                return (raw[rowStart + (bit >> 3)] >> shift) & ((1 << depth) - 1);
        }
    }

    private static byte Scale(int sample, int depth) => depth switch
    {
        8 => (byte)sample,
        16 => (byte)(sample >> 8),
        _ => (byte)(sample * 255 / ((1 << depth) - 1))
    };

    // 灰度或 RGB 图的 tRNS：一个（或三个）16 位样本值，等于它的像素完全透明。
    private static int[]? TransparentKey(byte[] transparency, Header header)
    {
        var count = header.ColorType == 0 ? 1 : 3;
        if (transparency.Length < count * 2)
            return null;
        var key = new int[count];
        for (var i = 0; i < count; i++)
            key[i] = transparency[i * 2] << 8 | transparency[i * 2 + 1];
        return key;
    }
}
