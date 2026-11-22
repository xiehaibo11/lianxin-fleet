using Avalonia;
using Avalonia.Media.Imaging;
using Lianxin.Fleet.Core.Agents;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Mac.Interaction;

// AI 助手取图：大屏手机的截图先等比缩小到模型的看图上限，保证模型给出的坐标就是这张图上的坐标。
public static class ScreenShrinker
{
    public static async Task<AgentScreen> CaptureAsync(IDeviceBridge bridge, FleetDevice device, CancellationToken cancellationToken)
    {
        var screen = AgentScreen.FromPng(await bridge.GetScreenshotAsync(device, cancellationToken));
        return screen.NeedsResize ? await Task.Run(() => Shrink(screen), cancellationToken) : screen;
    }

    private static AgentScreen Shrink(AgentScreen screen)
    {
        var scale = Math.Min((double)AgentScreen.MaxLongEdge / Math.Max(screen.Width, screen.Height),
            Math.Sqrt((double)AgentScreen.MaxPixels / ((long)screen.Width * screen.Height)));
        // 向下取整，缩放后不会超过上限。
        var size = new PixelSize(Math.Max(1, (int)(screen.Width * scale)), Math.Max(1, (int)(screen.Height * scale)));
        using var input = new MemoryStream(screen.Data);
        using var source = new Bitmap(input);
        using var resized = source.CreateScaledBitmap(size, BitmapInterpolationMode.HighQuality);
        using var output = new MemoryStream();
        resized.Save(output, new JpegBitmapEncoderOptions { Quality = 85 });
        return new(output.ToArray(), "image/jpeg", size.Width, size.Height);
    }
}
