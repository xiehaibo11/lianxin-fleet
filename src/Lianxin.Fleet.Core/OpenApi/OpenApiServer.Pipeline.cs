using System.Text.Json;
using Lianxin.Fleet.Core.Devices;

namespace Lianxin.Fleet.Core.OpenApi;

// 单个请求的处理顺序：请求头 → Host/Origin/令牌 → 路由与方法 → Content-Type → 正文 → JSON → 处理函数。
// 安全检查都在读取正文之前，未授权的请求不会让服务器接收大正文。
public sealed partial class OpenApiServer
{
    private static readonly byte[] Continue = "HTTP/1.1 100 Continue\r\n\r\n"u8.ToArray();

    private async Task<OpenApiReply?> ProcessAsync(Stream stream)
    {
        (OpenApiRequest Request, byte[] Leftover)? head;
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token))
        {
            timeout.CancelAfter(options.HeaderTimeout);
            try
            {
                head = await OpenApiRequestReader.ReadHeadAsync(stream, options, timeout.Token).ConfigureAwait(false);
            }
            catch (OpenApiProtocolException error) { return OpenApiSecurity.Reject(error.Status, error.Message); }
            catch (OperationCanceledException) when (!lifetime.IsCancellationRequested)
            {
                return OpenApiSecurity.Reject(408, "读取请求头超时。");
            }
        }
        if (head is not { } received)
            return null;
        Interlocked.Increment(ref requests);
        var request = received.Request;
        if (OpenApiSecurity.CheckOrigin(request, options, Port, machineNames) is { } denied)
            return denied;
        var route = router.Find(request.Path);
        if (route is null)
            return OpenApiSecurity.Reject(404, "接口不存在：" + OpenApiBackend.Short(request.Path));
        if (route.Method != request.Method)
            return OpenApiSecurity.Reject(405, $"该接口只接受 {route.Method} 请求。").WithHeader("Allow", route.Method);
        if (OpenApiSecurity.CheckContentType(request) is { } badType)
            return badType;
        // 所有连接同时接收的正文总量有上限；超出时直接 503，不读这份正文。
        if (!TryReserveBody(request.ContentLength))
            return OpenApiSecurity.Reject(503, "开放接口正在接收的请求过多，请稍后重试。");
        try
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token))
            {
                timeout.CancelAfter(options.BodyTimeout);
                try
                {
                    if (request.ContentLength > received.Leftover.Length
                        && string.Equals(request.Header("Expect"), "100-continue", StringComparison.OrdinalIgnoreCase))
                        await stream.WriteAsync(Continue, timeout.Token).ConfigureAwait(false);
                    request.Body = await OpenApiRequestReader.ReadBodyAsync(stream, received.Leftover, request.ContentLength,
                        timeout.Token).ConfigureAwait(false);
                }
                catch (OpenApiProtocolException error) { return OpenApiSecurity.Reject(error.Status, error.Message); }
                catch (OperationCanceledException) when (!lifetime.IsCancellationRequested)
                {
                    return OpenApiSecurity.Reject(408, "读取请求体超时。");
                }
            }
            return await DispatchAsync(route, request).ConfigureAwait(false);
        }
        finally { ReleaseBody(request.ContentLength); }
    }

    private async Task<OpenApiReply> DispatchAsync(OpenApiRoute route, OpenApiRequest request)
    {
        JsonDocument? document = null;
        try
        {
            // 不支持的接口不看参数，正文也不解析。
            if (request.Method == "POST" && route.Kind == OpenApiRouteKind.Implemented)
            {
                var body = request.Body;
                if (body.Span.StartsWith("﻿"u8))
                    body = body[3..];
                try
                {
                    document = JsonDocument.Parse(body.IsEmpty ? "{}"u8.ToArray() : body, new JsonDocumentOptions { MaxDepth = 32 });
                }
                catch (JsonException)
                {
                    return OpenApiSecurity.Reject(400, "请求体不是有效的 JSON。");
                }
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    return OpenApiSecurity.Reject(400, "请求体必须是 JSON 对象。");
            }
            return await InvokeAsync(route, request, document?.RootElement ?? default).ConfigureAwait(false);
        }
        finally { document?.Dispose(); }
    }

    private async Task<OpenApiReply> InvokeAsync(OpenApiRoute route, OpenApiRequest request, JsonElement body)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        deadline.CancelAfter(options.HandlerTimeout);
        try
        {
            return await route.Handler(new OpenApiCall(backend, request, body, deadline.Token)).ConfigureAwait(false);
        }
        catch (OpenApiException error) { return OpenApiReply.Fail(error.Code, error.Message, status: error.Status); }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
            return OpenApiSecurity.Reject(503, "开放接口正在关闭，操作已取消。");
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            return OpenApiReply.Fail(OpenApiCodes.Uncertain, "操作超时，结果待确认，请检查手机状态。");
        }
        catch (OperationCanceledException error)
        {
            // 例如用户在管理员授权框点了取消。
            return OpenApiReply.Fail(OpenApiCodes.DeviceFailed, error.Message);
        }
        catch (DeviceOperationException error)
        {
            return OpenApiReply.Fail(error.Uncertain ? OpenApiCodes.Uncertain : OpenApiCodes.DeviceFailed, error.Message);
        }
        // 设备通道没有实现某项能力（IDeviceBridge 的默认实现）。
        catch (NotSupportedException error) when (error is not PlatformNotSupportedException)
        {
            return OpenApiReply.Fail(OpenApiCodes.Unsupported, "联信 Fleet 暂不支持：" + error.Message);
        }
        // 本机文件系统的原始错误信息可能带电脑上的路径：不转给调用方（局域网模式下是别的电脑），只写进中控日志。
        // 需要告诉调用方的参数问题由各接口自己换成 OpenApiException。
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Report($"开放接口 {route.Path}：{error.Message}");
            return OpenApiReply.Fail(OpenApiCodes.DeviceFailed, error is UnauthorizedAccessException
                ? "电脑上的文件或文件夹没有访问权限，详情见中控状态栏。" : "电脑上的文件操作失败，详情见中控状态栏。");
        }
        catch (Exception error) when (error is ArgumentException or KeyNotFoundException or FormatException)
        {
            return OpenApiReply.Fail(OpenApiCodes.InvalidArgument, error.Message);
        }
        catch (Exception error) when (error is InvalidOperationException or InvalidDataException or TimeoutException
            or PlatformNotSupportedException or System.ComponentModel.Win32Exception)
        {
            return OpenApiReply.Fail(OpenApiCodes.DeviceFailed, error.Message);
        }
        catch (Exception error)
        {
            Report($"开放接口 {route.Path} 内部错误：{error}");
            return OpenApiSecurity.Reject(500, "开放接口内部错误：" + error.GetType().Name);
        }
    }
}
