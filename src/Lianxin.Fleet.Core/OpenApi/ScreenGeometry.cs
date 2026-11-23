using System.Buffers.Binary;
using System.Collections.Concurrent;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.OpenApi;

// 当前方向下的屏幕：点尺寸来自 WDA window/size，Ratio 是截图像素与点的比例（普通机型为 2 或 3，开启「放大显示」时不是整数）。
public readonly record struct DeviceGeometry(int PointWidth, int PointHeight, double Ratio)
{
    public int PixelWidth => (int)Math.Round(PointWidth * Ratio);
    public int PixelHeight => (int)Math.Round(PointHeight * Ratio);
    // EasyClick 约定：1 竖屏，2 横屏（向右旋转 90 度）。
    public int Orientation => PointWidth > PointHeight ? 2 : 1;

    // EasyClick 坐标是截图像素；联信设备动作用 0–1 相对位置，桥接按 round(f × (点尺寸 − 1)) 还原成点。
    // 因此先除以 Ratio 得到点，再除以 (点尺寸 − 1)，桥接还原后正好落在该点上。
    public double X(double pixel) => Fraction(pixel, PointWidth, PixelWidth, "x");
    public double Y(double pixel) => Fraction(pixel, PointHeight, PixelHeight, "y");

    private double Fraction(double pixel, int points, int pixels, string label)
    {
        if (!double.IsFinite(pixel) || pixel < 0 || pixel > pixels)
            throw new OpenApiException(OpenApiCodes.InvalidArgument, $"{label} 坐标须在 0 到 {pixels - 1} 之间（截图像素）。");
        return points <= 1 ? 0 : Math.Clamp(pixel / Ratio / (points - 1), 0, 1);
    }
}

// 按设备缓存像素比例。比例只取决于机型和显示模式，与横竖屏无关；点尺寸长边变化（切换放大显示）时重新测量。
internal sealed class ScreenGeometry(IDeviceBridge bridge)
{
    private readonly ConcurrentDictionary<long, (int LongSide, double Ratio)> ratios = new();

    public async Task<DeviceGeometry> MeasureAsync(FleetDevice device, CancellationToken cancellationToken)
    {
        // 只取屏幕尺寸：三路状态探测各只等 2 秒，手机弹键盘或动画时会把正常的点按误报为失败。
        (int Width, int Height) status;
        try
        {
            status = await bridge.GetWindowSizeAsync(device, cancellationToken).ConfigureAwait(false);
        }
        catch (DeviceOperationException error)
        {
            throw new OpenApiException(OpenApiCodes.DeviceFailed, "自动化未就绪或没有响应，请先启动自动化（startAutoEnv）：" + error.Message);
        }
        var longSide = Math.Max(status.Width, status.Height);
        if (!ratios.TryGetValue(device.Id, out var known) || known.LongSide != longSide)
        {
            var screenshot = await bridge.GetScreenshotAsync(device, cancellationToken).ConfigureAwait(false);
            if (Learn(device.Id, screenshot, status.Width, status.Height) is null)
                throw new OpenApiException(OpenApiCodes.DeviceFailed, "无法从截图确定屏幕像素比例。");
            known = ratios[device.Id];
        }
        return new(status.Width, status.Height, known.Ratio);
    }

    // 记下截图长边与点尺寸长边之比；截图接口调用时顺带更新。
    public double? Learn(long deviceId, ReadOnlySpan<byte> png, int pointWidth, int pointHeight)
    {
        if (PngSize(png) is not { } size)
            return null;
        var points = Math.Max(pointWidth, pointHeight);
        var ratio = (double)Math.Max(size.Width, size.Height) / points;
        if (points < 1 || !double.IsFinite(ratio) || ratio is < 1 or > 5)
            return null;
        ratios[deviceId] = (points, ratio);
        return ratio;
    }

    // PNG 宽高位于文件头后的 IHDR 块。
    public static (int Width, int Height)? PngSize(ReadOnlySpan<byte> png)
    {
        if (png.Length < 24 || !png.StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a })
            || !png.Slice(12, 4).SequenceEqual("IHDR"u8))
            return null;
        var width = BinaryPrimitives.ReadInt32BigEndian(png.Slice(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(png.Slice(20, 4));
        return width is > 0 and <= 20000 && height is > 0 and <= 20000 ? (width, height) : null;
    }
}
