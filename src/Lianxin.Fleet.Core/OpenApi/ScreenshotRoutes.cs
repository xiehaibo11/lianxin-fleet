using System.Globalization;

namespace Lianxin.Fleet.Core.OpenApi;

// 截图接口（advance/openapi/screenshot）。成功时响应体是图片本身（Content-Type 标明格式），失败时是 JSON 信封。
internal static class ScreenshotRoutes
{
    private const string StreamReason = "图片流接口（联信的实时画面是约 30% 尺寸、5 帧/秒的预览流，不能作为抓图来源），请改用 captureFullScreenPng 或 captureFullScreenJpg。";

    public static void Register(OpenApiRouter router) => router
        .Post("/openapi/captureFullScreenPng", Png)
        .Post("/openapi/captureFullScreenNoAutoByte", UsbCapture)
        .Post("/openapi/captureFullScreenJpg", call => Jpeg(call, 80))
        .Post("/openapi/captureFullScreenByteEx", ExtendedJpeg)
        .Unsupported("/openapi/resetCaptureScreenNoAutoEnv", "重置 EasyClick 无权限截图环境；无自动化截图直接调用 captureFullScreenNoAutoByte 即可。")
        .Unsupported("/openapi/startPreCapScreen", StreamReason)
        .Unsupported("/openapi/startScreenStream", StreamReason)
        .Unsupported("/openapi/captureScreenStreamByte", StreamReason)
        .Unsupported("/openapi/stopScreenStream", StreamReason)
        .Unsupported("/openapi/isScreenStreamOk", StreamReason);

    // WDA 截图，PNG 原图，像素与点击坐标一致。
    private static async Task<OpenApiReply> Png(OpenApiCall call) => OpenApiReply.Image(await CaptureAsync(call).ConfigureAwait(false));

    // 不需要启动自动化：走 USB 截图服务（需要已挂载开发者镜像）。多数系统为 PNG，个别旧系统为 TIFF。
    private static async Task<OpenApiReply> UsbCapture(OpenApiCall call)
    {
        var image = await call.Backend.Bridge.GetUsbScreenshotAsync(call.Device(), call.Cancellation).ConfigureAwait(false);
        return OpenApiReply.Image(image);
    }

    // type=1 时质量取 1、50、100 三档，type=2 时 1–100；联信两种模式都按给定质量编码。
    private static Task<OpenApiReply> ExtendedJpeg(OpenApiCall call)
    {
        var type = call.Text("type")?.Trim() ?? "1";
        if (type is not ("1" or "2"))
            throw OpenApiCall.Invalid("type 只能是 1 或 2。");
        var quality = call.Number("quality") ?? 50;
        if (quality is < 1 or > 100)
            throw OpenApiCall.Invalid("quality 须为 1 至 100。");
        return Jpeg(call, (int)Math.Round(quality));
    }

    private static async Task<OpenApiReply> Jpeg(OpenApiCall call, int quality)
    {
        var encoder = call.Backend.JpegEncoder;
        if (encoder is null)
            return OpenApiReply.Fail(OpenApiCodes.Unsupported, "联信 Fleet 暂不支持 JPG 截图（当前环境没有图片编码器），请改用 captureFullScreenPng。");
        var png = await CaptureAsync(call).ConfigureAwait(false);
        byte[] jpeg;
        try
        {
            jpeg = encoder(png, quality);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            throw new OpenApiException(OpenApiCodes.DeviceFailed, "截图转换为 JPG 失败：" + error.Message);
        }
        return OpenApiReply.Image(jpeg).WithHeader("X-Fleet-Jpeg-Quality", quality.ToString(CultureInfo.InvariantCulture));
    }

    private static Task<byte[]> CaptureAsync(OpenApiCall call) =>
        call.Backend.Bridge.GetScreenshotAsync(call.Device(), call.Cancellation);
}
