namespace Lianxin.Fleet.Core.OpenApi;

// 返回信封里的 code：0 成功；1xxx 业务失败、2xxx 不支持（HTTP 状态均为 200）；
// 与 HTTP 状态码相同的值（400、401、403…）表示请求本身被服务器拒绝，HTTP 状态也是这个值。
public static class OpenApiCodes
{
    public const int Ok = 0;
    public const int InvalidArgument = 1001;
    public const int DeviceNotFound = 1002;
    public const int DeviceFailed = 1003;
    public const int Uncertain = 1004;
    public const int ScriptNotFound = 1005;
    public const int Busy = 1006;
    public const int Unsupported = 2001;
    public const int Pending = 2002;
}

// 处理函数抛出它即返回指定的 code 和中文消息。
public sealed class OpenApiException(int code, string message, int status = 200) : Exception(message)
{
    public int Code { get; } = code;
    public int Status { get; } = status;
}
