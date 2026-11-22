using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lianxin.Fleet.Core.Agents;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Desktop.Interaction;

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
        var source = new BitmapImage();
        using (var input = new MemoryStream(screen.Data))
        {
            source.BeginInit();
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.StreamSource = input;
            source.EndInit();
        }
        var resized = new TransformedBitmap(source, new ScaleTransform(scale, scale));
        var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
        encoder.Frames.Add(BitmapFrame.Create(resized));
        using var output = new MemoryStream();
        encoder.Save(output);
        return new(output.ToArray(), "image/jpeg", resized.PixelWidth, resized.PixelHeight);
    }
}
