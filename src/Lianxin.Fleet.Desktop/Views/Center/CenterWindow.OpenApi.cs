using System.IO;
using System.Net.Sockets;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lianxin.Fleet.Core.OpenApi;
using Lianxin.Fleet.Desktop.Interaction;

namespace Lianxin.Fleet.Desktop.Views.Center;

// 开放接口的启停：按设置启动本机 HTTP 服务，设置变化时重启，关闭中控时先于运行时停止。
public partial class CenterWindow
{
    private readonly SemaphoreSlim openApiGate = new(1, 1);
    private OpenApiServer? openApi;
    private string openApiStatus = "开放接口未开启。";

    public event Action? OpenApiChanged;

    public string OpenApiStatus => openApi is { IsRunning: true } server
        ? $"{openApiStatus} · 已处理 {server.RequestCount} 个请求" : openApiStatus;

    // 令牌与 AI 密钥一样按当前 Windows 用户 DPAPI 加密保存；换用户或损坏时返回 null。
    public string? LoadOpenApiToken() => SecretProtector.Unprotect(Settings.OpenApiTokenProtected);

    public static string ProtectOpenApiToken(string token) => SecretProtector.Protect(token);

    public async Task ApplyOpenApiAsync()
    {
        await openApiGate.WaitAsync();
        try
        {
            if (openApi is not null)
            {
                await openApi.StopAsync();
                openApi = null;
            }
            if (closing || !Settings.OpenApiEnabled)
            {
                ShowOpenApi("接口 关闭", null, "开放接口未开启。");
                return;
            }
            var token = LoadOpenApiToken();
            if (Settings.OpenApiAllowLan && !OpenApiOptions.ValidToken(token))
            {
                ShowOpenApi("接口 无令牌", false, "局域网模式缺少有效的访问令牌，请在设置里点「重新生成」。");
                return;
            }
            var backend = OpenApiBackend.FromRuntime(Runtime, LoadRunnerBundleId, EncodeJpeg);
            var options = new OpenApiOptions { Port = Settings.OpenApiPort, AllowLan = Settings.OpenApiAllowLan, Token = token };
            try
            {
                var server = await OpenApiServer.StartAsync(backend, options, cancellationToken: Lifetime);
                server.Log += message => Dispatcher.BeginInvoke(() =>
                {
                    if (!closing)
                        Report(message);
                });
                openApi = server;
                ShowOpenApi($"接口 :{server.Port}", true, server.AllowLan
                    ? $"运行中：局域网 http://本机IP:{server.Port}，请求须带访问令牌"
                    : $"运行中：http://127.0.0.1:{server.Port}（仅本机）");
            }
            catch (Exception error) when (error is OpenApiPortBusyException or ArgumentException or SocketException)
            {
                ShowOpenApi(error is OpenApiPortBusyException ? "接口 端口占用" : "接口 异常", false, error.Message);
                Report("开放接口未启动：" + error.Message);
            }
        }
        catch (OperationCanceledException) when (closing) { }
        finally { openApiGate.Release(); }
    }

    // 关闭中控时在释放运行时之前调用：正在执行的接口请求先收到取消并回复 503。
    private async Task StopOpenApiAsync()
    {
        await openApiGate.WaitAsync();
        try
        {
            if (openApi is not null)
                await openApi.StopAsync();
            openApi = null;
        }
        finally { openApiGate.Release(); }
    }

    private void ShowOpenApi(string label, bool? healthy, string status)
    {
        openApiStatus = status;
        ApiText.Text = label;
        ApiChip.ToolTip = status;
        ApiChip.Background = new SolidColorBrush(healthy switch
        {
            true => Color.FromRgb(0x23, 0x30, 0x24),
            false => Color.FromRgb(0x3A, 0x1F, 0x24),
            null => Color.FromRgb(0x1F, 0x24, 0x33)
        });
        ApiChip.BorderBrush = new SolidColorBrush(healthy switch
        {
            true => Color.FromRgb(0x3E, 0x6B, 0x47),
            false => Color.FromRgb(0x7A, 0x3A, 0x44),
            null => Color.FromRgb(0x3B, 0x42, 0x5A)
        });
        OpenApiChanged?.Invoke();
    }

    // JPG 截图接口用 WPF 自带的 JPEG 编码器，不引入额外图片库。在接口线程上调用。
    internal static byte[] EncodeJpeg(byte[] png, int quality)
    {
        using var input = new MemoryStream(png);
        var frame = BitmapFrame.Create(input, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var encoder = new JpegBitmapEncoder { QualityLevel = Math.Clamp(quality, 1, 100) };
        encoder.Frames.Add(BitmapFrame.Create(new FormatConvertedBitmap(frame, PixelFormats.Bgr24, null, 0)));
        using var output = new MemoryStream();
        encoder.Save(output);
        return output.ToArray();
    }
}
