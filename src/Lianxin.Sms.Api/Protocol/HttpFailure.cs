namespace Lianxin.Sms.Api.Protocol;

internal sealed class HttpFailure(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
