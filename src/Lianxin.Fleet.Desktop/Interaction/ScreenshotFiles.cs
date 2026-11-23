using System.Globalization;
using System.IO;
using System.Windows.Media.Imaging;

namespace Lianxin.Fleet.Desktop.Interaction;

// 集控投屏的截图文件：设备名加时间命名、不覆盖已有文件；PNG 原样写出，其他格式和界面画面帧转成 PNG。
public static class ScreenshotFiles
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static byte[] ToPng(byte[] image)
    {
        if (image.AsSpan().StartsWith(PngSignature))
            return image;
        using var input = new MemoryStream(image);
        var frame = BitmapDecoder.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        return ToPng(frame);
    }

    public static byte[] ToPng(BitmapSource image)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var output = new MemoryStream();
        encoder.Save(output);
        return output.ToArray();
    }

    // suffix 附在时间之后，例如 _preview 表示取自低分辨率预览画面的图。
    public static async Task<string> WriteAsync(string folder, string deviceName, DateTime at, byte[] png, CancellationToken cancellationToken,
        string suffix = "")
    {
        var invalid = Path.GetInvalidFileNameChars();
        var name = new string(deviceName.Select(c => invalid.Contains(c) || char.IsControl(c) ? '_' : c).ToArray()).Trim().TrimEnd('.');
        name = name.Length == 0 ? "iPhone" : name[..Math.Min(name.Length, 60)];
        var stem = name + "_" + at.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + suffix;
        for (var index = 1; ; index++)
        {
            var path = Path.Combine(folder, index == 1 ? stem + ".png" : $"{stem}_{index}.png");
            FileStream stream;
            try
            {
                stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            }
            catch (IOException) when (File.Exists(path) && index < 1000)
            {
                continue;
            }
            await using (stream)
                await stream.WriteAsync(png, cancellationToken);
            return path;
        }
    }
}
