using Lianxin.Sms.Checks.Scenarios;

namespace Lianxin.Sms.Checks;

internal static class Program
{
    public static async Task<int> Main()
    {
        var checks = new (string Name, Func<Task> Run)[]
        {
            ("同租户/跨租户权限、千号码批量与设备同步", DemoChecks.Run),
            ("设备任务领取、回信归属、原设备续发和失败幂等退款", GatewayChecks.Run),
            ("旧版 scrypt 密码、SHA256 设备凭据和会话兼容", CompatibilityChecks.Run),
            ("请求来源、JSON 媒体类型和请求大小边界", BoundaryChecks.Run),
            ("反向代理转发地址与登录限流按真实客户端计数", ProxyChecks.Run)
        };
        var failed = 0;
        foreach (var check in checks)
        {
            try
            {
                await check.Run();
                Console.WriteLine("PASS " + check.Name);
            }
            catch (Exception error) { failed++; Console.Error.WriteLine($"FAIL {check.Name}: {error}"); }
        }
        Console.WriteLine($"{checks.Length - failed}/{checks.Length} SMS HTTP checks passed");
        return failed == 0 ? 0 : 1;
    }
}
