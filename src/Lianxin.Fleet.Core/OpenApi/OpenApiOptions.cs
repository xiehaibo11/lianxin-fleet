using System.Security.Cryptography;

namespace Lianxin.Fleet.Core.OpenApi;

// 开放接口的监听方式与请求上限。默认只监听 127.0.0.1；AllowLan 时监听全部网卡并强制校验令牌。
public sealed record OpenApiOptions
{
    public const int DefaultPort = 8019;

    // 0 表示由系统分配空闲端口（只用于检查）。
    public int Port { get; init; } = DefaultPort;
    public bool AllowLan
    {
        get; init;
    }
    public string? Token
    {
        get; init;
    }
    public int MaxHeaderBytes { get; init; } = 16 * 1024;
    public int MaxHeaderCount { get; init; } = 100;
    // 已实现的接口都只收 JSON 参数（文件经中转目录交换，不在正文里上传），1 MB 绰绰有余。
    public int MaxBodyBytes { get; init; } = 1024 * 1024;
    // 同时正在接收的正文总量上限，超出时返回 503，避免很多连接同时发大正文占满内存。
    public long MaxInFlightBodyBytes { get; init; } = 16L * 1024 * 1024;
    public int MaxConnections { get; init; } = 32;
    // 局域网模式下同一个远程地址最多占用的连接数（本机回环地址不受此限制），一台机器无法占满全部连接。
    public int MaxConnectionsPerAddress { get; init; } = 8;
    // 连接数已满时同时进行的 503 回复上限，再多的连接直接关闭。
    public int MaxPendingRejects { get; init; } = 16;
    // 连接建立后必须在这段时间内发来请求行，防止慢速连接长时间占位。
    public TimeSpan RequestLineTimeout { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan HeaderTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan BodyTimeout { get; init; } = TimeSpan.FromSeconds(120);
    // 安装应用、开启开发者模式、挂载镜像都可能持续数分钟。
    public TimeSpan HandlerTimeout { get; init; } = TimeSpan.FromMinutes(10);
    public TimeSpan WriteTimeout { get; init; } = TimeSpan.FromSeconds(60);
    // 只供检查使用：本机回环连接也按每个地址的连接数上限计算（检查只能从回环地址连接）。
    internal bool LimitLoopbackPerAddress
    {
        get; init;
    }
    // 只供检查使用：局域网模式也只监听回环地址，避免在测试机上打开全部网卡（和防火墙提示）。
    internal System.Net.IPAddress? ListenAddress
    {
        get; init;
    }

    public static bool ValidPort(int port) => port is >= 1024 and <= 65535;

    // 32 字节随机数，URL 安全的 base64（43 个字符），可直接放进请求头。
    public static string NewToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static bool ValidToken(string? token) => token is { Length: >= 16 and <= 128 }
        && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or '~');

    internal void Validate()
    {
        if (Port != 0 && !ValidPort(Port))
            throw new ArgumentException("开放接口端口须为 1024 至 65535。");
        if (AllowLan && !ValidToken(Token))
            throw new ArgumentException("局域网模式必须设置访问令牌（16–128 位字母、数字或 -_.~）。");
        if (MaxHeaderBytes < 1024 || MaxHeaderCount < 8 || MaxBodyBytes < 1024 || MaxConnections < 1
            || MaxInFlightBodyBytes < MaxBodyBytes || MaxConnectionsPerAddress < 1 || MaxPendingRejects < 0
            || RequestLineTimeout <= TimeSpan.Zero)
            throw new ArgumentException("开放接口请求上限无效。");
    }
}
