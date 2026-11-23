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
            ("投屏录制：全部动作、等待取整、数字格式、上限与解析器读回", RecorderChecks.Run),
            ("WDA 协议、并发及 Windows 命令", NativeBridgeChecks.RunAsync),
            ("真机控制：Home 键、返回手势、多任务、无焦点输入与旧系统打开链接", DeviceInputChecks.Run),
            ("Mac 旧版 iOS USB 发现、安装参数与兼容回退", HostDeviceChecks.RunAsync),
            ("WDA 转发端口被占用时不会启动 Runner", HostDeviceChecks.OccupiedWdaPortAsync),
            ("实时画面流解析与进程内端口转发", ControlChecks.MjpegAndForwarding),
            ("usbmuxd 协议包与设备编号格式", ControlChecks.UsbmuxProtocol),
            ("扩展指令路由、参数校验与开流降载", ControlChecks.ActionRoutes),
            ("签名描述文件和分发方式校验", SigningChecks.Profiles),
            ("脚本语法、行号报错与步数上限", ScriptChecks.Parser),
            ("旧数据库增量迁移、别名、ECID 与分组", ScriptChecks.Migration),
            ("脚本同机串行、多轮、停止、失败与中断恢复", ScriptChecks.Runner),
            ("脚本块结构、嵌套上限与未闭合块行号报错", ScriptChecks.Blocks),
            ("脚本元素、图色、日志、截图等新命令解析与校验", ScriptChecks.Commands),
            ("脚本循环、条件、跳出、停止、失败与「当」循环上限", ScriptChecks.Flow),
            ("脚本暂停、继续、暂停中停止与关闭", ScriptChecks.PauseResume),
            ("元素查找协议、谓词转义与元素编号校验", ElementChecks.Protocol),
            ("抓取节点：界面树整理与选择器建议", ElementChecks.PageSource),
            ("脚本元素命令：等待出现、消失、点击、输入与超时", ElementChecks.Commands),
            ("PNG 解码：颜色类型、位深、过滤方式与损坏拒绝", ImagingChecks.Decoding),
            ("比色、区域找色与坐标换算", ImagingChecks.Colors),
            ("模板找图：位置、亮度偏移、缺失与耗时", ImagingChecks.Templates),
            ("脚本图色命令、模板目录与截图保存", ImagingChecks.Commands),
            ("工作站设置读写与损坏回退", ScriptChecks.Settings),
            ("定时任务到点、宽限、星期与间隔规则", ScheduleChecks.DueRules),
            ("定时任务分组目标、只触发一次与删除约束", ScheduleChecks.Firing),
            ("定时任务同时到点时忙碌设备自动补跑", ScheduleChecks.Collision),
            ("一键配置命令、版本跳过与批准目录", SetupChecks.OneClickSetup),
            ("发现补充 ECID、Wi-Fi 设备与失败回退", SetupChecks.DiscoveryDetails),
            ("macOS 重置 USB 提权命令与取消授权", SetupChecks.MacUsbReset),
            ("AI 助手工具换算、参数校验与截图尺寸", AgentChecks.ToolMapping),
            ("AI 助手请求格式、回传思考块、错误结果与拒答", AgentChecks.Loop),
            ("开放接口请求解析上限：411、413、431、400、404、405、408 与 100-continue", OpenApiChecks.Protocol),
            ("开放接口 Host 白名单、拒绝浏览器请求、JSON 类型与局域网令牌", OpenApiChecks.Security),
            ("开放接口信封、设备列表、别名、脚本启停与刷新信息", OpenApiChecks.Center),
            ("开放接口截图像素到 WDA 点坐标换算、动作参数与桥接错误码", OpenApiChecks.Actions),
            ("开放接口截图、接口登记与不支持接口、自动化启停、USB 重启与开发者模式", OpenApiChecks.Endpoints),
            ("开放接口启停、端口占用、连接上限与停止时取消设备操作", OpenApiChecks.Lifecycle),
            ("开放接口暂停、继续、查询暂停状态与暂停中停止", OpenApiChecks.ScriptPause),
            ("开放接口电量与充电、开发者镜像查询与卸载、卸载应用与重启", OpenApiChecks.DeviceManagement),
            ("开放接口文件列出删除新建、推送拉取与电脑端中转目录边界", OpenApiChecks.Files),
            ("开放接口锁屏状态、旋转后的坐标换算与抓取节点过滤", OpenApiChecks.ScreenAndNodes),
            ("WDA 锁屏查询与屏幕旋转协议", OpenApiChecks.BridgeScreen),
            ("开放接口加固：Node fetch 请求头、正文上限与在途总量、请求行限时、每个地址的连接数与 503 上限", OpenApiChecks.Hardening),
            ("开放接口的系统异常不泄露本机路径，数据损坏与超时返回 1003", OpenApiChecks.ErrorMapping),
            ("安装路径先做字符串判断（UNC、设备路径、目录外），半途失败时说明副作用", OpenApiChecks.PathsAndSideEffects),
            ("设备详情解析、锁屏服务参数与只用已有信任", DeviceManagementChecks.InfoAndBattery),
            ("电量列单进程批量读取、缓存间隔与 WDA 回退", DeviceManagementChecks.BatteryCache),
            ("自动化在线时电量取 WDA、USB 失败只用 WDA", DeviceManagementChecks.WdaInfo),
            ("应用列表与卸载、开发者镜像状态与卸载、重启关机", DeviceManagementChecks.AppsImagesAndPower),
            ("媒体区文件路径校验、上传下载删除参数与本机边界", DeviceManagementChecks.Files),
            ("设备批量操作并发上限、顺序与待确认结果", DeviceManagementChecks.Batch),
            ("pymobiledevice3 真实错误输出：没送达手机为确定失败，改动操作中途断开为待确认", DeviceManagementChecks.ToolErrors),
            ("脚本连写上万个「非」不递归、不栈溢出", ScriptChecks.NegationChain),
            ("暂停中的脚本让出并发名额，排队的设备先运行", ScriptChecks.PausedSlot),
            ("等待元素遇到临时查询失败继续查到超时，并报告最后的错误", ScriptChecks.WaitRetries),
            ("抓取节点选择器：长文字和换行改用包含片段，不唯一时标明", ScriptChecks.Suggestions),
            ("模板找图：任意对齐与贴边的精确截取、细长模板耗时、并发上限", ImagingChecks.Alignment),
            ("PNG 解码先按数据量和调用方的像素上限拒绝，再分配内存", ImagingChecks.PngLimits),
            ("投屏录制等主控执行成功才写入，按操作时刻排序", RecorderChecks.LateResults)
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
