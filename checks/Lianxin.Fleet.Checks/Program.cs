using Lianxin.Fleet.Checks.Scenarios;

namespace Lianxin.Fleet.Checks;

internal static class Program
{
    private static async Task<int> Main()
    {
        var tests = new (string Name, Func<Task> Run)[]
        {
            ("数据库隔离、幂等及重启恢复", StoreChecks.Persistence),
            ("桌面与 CLI 跨实例数据库占用锁", StoreChecks.ExclusiveLease),
            ("设备任务串行、批量并发及未知结果", StoreChecks.Queue),
            ("大批量排队、并发上限与同设备任务顺序", QueueScaleChecks.Run),
            ("应用目录和符号链接边界", StoreChecks.ApplicationBoundary),
            ("原生主控屏黑边和横竖坐标", ScreenChecks.Coordinates),
            ("WDA 协议、并发及 Windows 命令", NativeBridgeChecks.RunAsync),
            ("Mac 旧版 iOS USB 发现、安装参数与兼容回退", HostDeviceChecks.RunAsync),
            ("实时画面流解析与进程内端口转发", ControlChecks.MjpegAndForwarding),
            ("usbmuxd 协议包与设备编号格式", ControlChecks.UsbmuxProtocol),
            ("扩展指令路由、参数校验与开流降载", ControlChecks.ActionRoutes),
            ("签名描述文件和分发方式校验", SigningChecks.Profiles)
        };
        var failures = 0;
        foreach (var test in tests)
        {
            try
            {
                await test.Run();
                Console.WriteLine("PASS " + test.Name);
            }
            catch (Exception error) { failures++; Console.Error.WriteLine($"FAIL {test.Name}: {error}"); }
        }
        Console.WriteLine($"{tests.Length - failures}/{tests.Length} checks passed");
        return failures == 0 ? 0 : 1;
    }
}
