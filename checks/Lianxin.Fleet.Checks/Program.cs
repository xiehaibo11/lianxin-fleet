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
            ("USB 扫描连接清单与离线登记保留", StoreChecks.ConnectedDiscovery),
            ("仅探测指定设备并更新设备状态", StoreChecks.TargetedStatusRefresh),
            ("USB 在 WDA 掉线时可读画面，独立报告拔线与探测故障", StoreChecks.UsbPresenceAndCapture),
            ("设备任务串行、批量并发及未知结果", StoreChecks.Queue),
            ("大批量排队、并发上限与同设备任务顺序", QueueScaleChecks.Run),
            ("应用目录和符号链接边界", StoreChecks.ApplicationBoundary),
            ("原生主控屏黑边和横竖坐标", ScreenChecks.Coordinates),
            ("WDA 协议、并发及 Windows 命令", NativeBridgeChecks.RunAsync),
            ("Mac 旧版 iOS USB 发现、安装参数与兼容回退", HostDeviceChecks.RunAsync),
            ("WDA 转发端口被占用时不会启动 Runner", HostDeviceChecks.OccupiedWdaPortAsync),
            ("实时画面流解析与进程内端口转发", ControlChecks.MjpegAndForwarding),
            ("usbmuxd 协议包与设备编号格式", ControlChecks.UsbmuxProtocol),
            ("扩展指令路由、参数校验与开流降载", ControlChecks.ActionRoutes),
            ("签名描述文件和分发方式校验", SigningChecks.Profiles),
            ("脚本语法、行号报错与步数上限", ScriptChecks.Parser),
            ("旧数据库增量迁移、别名、ECID 与分组", ScriptChecks.Migration),
            ("脚本同机串行、多轮、停止、失败与中断恢复", ScriptChecks.Runner),
            ("工作站设置读写与损坏回退", ScriptChecks.Settings),
            ("定时任务到点、宽限、星期与间隔规则", ScheduleChecks.DueRules),
            ("定时任务分组目标、只触发一次与删除约束", ScheduleChecks.Firing),
            ("定时任务同时到点时忙碌设备自动补跑", ScheduleChecks.Collision),
            ("一键配置命令、版本跳过与批准目录", SetupChecks.OneClickSetup),
            ("发现补充 ECID、Wi-Fi 设备与失败回退", SetupChecks.DiscoveryDetails),
            ("macOS 重置 USB 提权命令与取消授权", SetupChecks.MacUsbReset),
            ("AI 助手工具换算、参数校验与截图尺寸", AgentChecks.ToolMapping),
            ("AI 助手请求格式、回传思考块、错误结果与拒答", AgentChecks.Loop)
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
