using System.Net.Sockets;
using Avalonia.Controls;
using Avalonia.Threading;
using Lianxin.Fleet.Core.OpenApi;
using SkiaSharp;

namespace Lianxin.Fleet.Mac.Views.Center;

// 开放接口的启停：按设置启动本机 HTTP 服务，设置变化时重启，关闭中控时先于运行时停止。
public sealed partial class CenterWindow
{
    // 与 AI 密钥一样单独保存在数据目录，macOS 上权限为 600，不写进 settings.json。
    private const string TokenFile = "openapi-token";
    private readonly SemaphoreSlim openApiGate = new(1, 1);
    private OpenApiServer? openApi;
    private string openApiStatus = "开放接口未开启。";

    public event Action? OpenApiChanged;

    public string OpenApiStatus => openApi is { IsRunning: true } server
        ? $"{openApiStatus} · 已处理 {server.RequestCount} 个请求" : openApiStatus;

    public string? LoadOpenApiToken()
    {
        try
        {
            var token = File.ReadAllText(Path.Combine(Runtime.DataDirectory, TokenFile)).Trim();
            return OpenApiOptions.ValidToken(token) ? token : null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void SaveOpenApiToken(string token)
    {
        if (!OpenApiOptions.ValidToken(token))
            throw new ArgumentException("访问令牌格式无效。");
        var path = Path.Combine(Runtime.DataDirectory, TokenFile);
        var pending = path + ".tmp";
        File.Delete(pending);
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using (var writer = new StreamWriter(new FileStream(pending, options)))
            writer.Write(token);
        File.Move(pending, path, overwrite: true);
    }

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
            if (Settings.OpenApiAllowLan && token is null)
            {
                ShowOpenApi("接口 无令牌", false, "局域网模式缺少有效的访问令牌，请在设置里点「重新生成」。");
                return;
            }
            var backend = OpenApiBackend.FromRuntime(Runtime, LoadRunnerBundleId, EncodeJpeg);
            var options = new OpenApiOptions { Port = Settings.OpenApiPort, AllowLan = Settings.OpenApiAllowLan, Token = token };
            try
            {
                var server = await OpenApiServer.StartAsync(backend, options, cancellationToken: Lifetime);
                server.Log += message => Dispatcher.UIThread.Post(() =>
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
        ApiChip.Classes.Set("neutral", healthy is null);
        ApiChip.Classes.Set("bad", healthy == false);
        ToolTip.SetTip(ApiChip, status);
        OpenApiChanged?.Invoke();
    }

    // JPG 截图接口用 Avalonia 自带的 SkiaSharp 编码，不引入额外图片库。在接口线程上调用。
    internal static byte[] EncodeJpeg(byte[] png, int quality)
    {
        using var codec = SKCodec.Create(new MemoryStream(png)) ?? throw new InvalidDataException("截图不是有效的图片。");
        using var bitmap = SKBitmap.Decode(codec) ?? throw new InvalidDataException("截图不是有效的图片。");
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, Math.Clamp(quality, 1, 100))
            ?? throw new InvalidDataException("JPG 编码失败。");
        return data.ToArray();
    }
}
