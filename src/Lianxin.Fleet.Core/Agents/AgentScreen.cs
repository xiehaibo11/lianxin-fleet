using System.Buffers.Binary;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Agents;

// 发给模型的一张截图。Width/Height 是图片像素尺寸，模型返回的坐标按这张图计算。
public sealed record AgentScreen(byte[] Data, string MediaType, int Width, int Height)
{
    // 模型看图的推荐上限；超过时由界面层先缩小，保证模型看到的就是这张图。
    public const int MaxLongEdge = 1568;
    public const int MaxPixels = 1_150_000;

    public bool NeedsResize => Math.Max(Width, Height) > MaxLongEdge || (long)Width * Height > MaxPixels;

    public static AgentScreen FromPng(byte[] png)
    {
        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (png.Length < 24 || !png.AsSpan(0, 8).SequenceEqual(signature))
            throw new InvalidDataException("截图不是 PNG。");
        var width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16));
        var height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20));
        if (width is < 2 or > 20000 || height is < 2 or > 20000)
            throw new InvalidDataException("截图尺寸无效。");
        return new(png, "image/png", width, height);
    }

    // 默认取图：WDA 截图原样发送。需要缩小时由调用方传入自己的取图函数。
    public static async Task<AgentScreen> CaptureAsync(IDeviceBridge bridge, FleetDevice device, CancellationToken cancellationToken)
        => FromPng(await bridge.GetScreenshotAsync(device, cancellationToken).ConfigureAwait(false));
}
