# 联信 Fleet · Windows 11 原生群控

## 代码规范：单文件以 200–300 行为目标，300 行为上限

按职责分类管理代码，目录与 C# 命名空间保持一致。职责完整的小文件保留，不填充行数；超过 300 行必须按职责拆分。构建会自动检查上限、类别和命名空间，完整规则见 [代码治理规范](docs/CODE_GOVERNANCE.md)。

主控使用 **C# / .NET 10 + WPF**，数据库、任务调度、设备通讯和 WDA 主机管理均使用 C#。iPhone 样例客户端保留 Swift。当前运行入口不使用 JavaScript、Node.js、WebView 或 Electron。

## 运行 Windows 程序

解压 `LianxinFleet-Native-Windows11-x64.zip`，运行：

- `Lianxin.Fleet.exe` 或 `StartFleet.cmd`：连接真实 iPhone 使用。程序只支持真机与联机调试，不提供演示模式。

Windows x64 发布包包含 .NET 10 运行时，无需安装 Node.js 或 .NET SDK。真实设备仍需 Apple Mobile Device Service、Python 及固定版本的 USB 协议工具：

```powershell
python -m pip install -r requirements-windows.txt
```

如使用独立 Python 环境，启动前设置 `PYTHON_PATH` 为该环境的 `python.exe`。USB 工具只是底层协议适配器，界面与业务逻辑均在 C# 内。

## 已有功能

- USB 扫描自动登记、手动登记、分组、搜索、筛选、多选与连接配置导出。
- Windows 设备墙分别显示 USB、画面和 WDA 控制状态。已接入的设备可以同时预览。实时画面中断或 WDA 掉线时，支持独立 USB 截图服务的手机会切到定时快照。
- 一键连接会先登记 USB 设备并加载预览，再启动已安装的签名 WDA Runner。Windows 工作站打开即用，无需登录 Fleet。
- 主控或选中设备支持单击、双击、长按、滑动、返回、多任务、Home、锁屏/解锁、音量、文本输入、写入剪贴板、打开链接及启动和关闭应用。同步时自动跳过 WDA 控制未就绪的设备。每张设备卡片可单独发送文字和返回主屏。
- 批量安装批准目录内的签名 `.ipa` / `.app`。
- 批量启动/停止已安装的 WDA；本机端口转发由程序直接经 Apple USB 设备服务（usbmuxd）完成，每台手机只保留一个 Python 进程。
- SQLite 持久任务、幂等请求、同设备串行、全局最多 32 台并发。
- 中断恢复时运行任务标为“结果待确认”，未执行队列取消，不自动重放可能已执行的动作。
- 同一数据库的桌面、macOS 工作站与 CLI 使用独占文件锁，避免多实例重复操机。
- 中控主窗口（Windows 与 macOS 相同）：设备表格（别名、型号、系统、电量、ECID、连接方式、脚本与自动化状态）、分组与列设置、脚本管理与批量运行、暂停 / 继续脚本、定时任务、一键配置（开发者模式、开发者镜像、安装 WDA）、重置 USB、无线中心和 AI 助手。设备墙改为从中控打开的「集控投屏」窗口。详见 [中控使用说明](docs/CENTER_GUIDE.md)。
- 脚本：流程控制（重复、如果、当、跳出）、界面元素（点击元素、等待元素、抓取节点）、比色找色与模板找图、日志与截图，附 EasyClick 函数对照表，见 [脚本说明](docs/SCRIPT_GUIDE.md)。集控投屏可以录制主控上的操作生成脚本，并保存原始截图。
- 设备管理：设备详情（序列号、容量、电池）、电量列、用户应用列表与卸载、手机媒体区文件浏览与上传下载、开发者镜像查询 / 挂载 / 卸载、重启与关机，以及勾选设备的批量镜像与电源操作。
- 开放接口：兼容 EasyClick 企业版开放接口的本机 HTTP 服务（51 个接口，含脚本暂停、电量、镜像、卸载应用、重启、锁屏状态、旋转、抓取节点与手机文件），默认只监听本机，局域网模式必须带令牌；文件推送拉取只使用数据目录下的 `transfer` 文件夹。见 [开放接口](docs/OPEN_API.md)。

安装包必须已经满足 Apple 的签名及设备授权要求。群控不会赋予普通 App 任意读取系统短信的权限。

## 工程结构

| 项目 | 职责 |
| --- | --- |
| `src/Lianxin.Fleet.Desktop` | WPF 原生桌面界面（Windows） |
| `src/Lianxin.Fleet.Mac` | Avalonia 12 macOS 中控与集控投屏，功能同 Windows 版，打包为 `LianxinFleet-Mac.app`（见 [docs/MAC_GUIDE.md](docs/MAC_GUIDE.md)） |
| `src/Lianxin.Fleet.Core` | SQLite、校验、任务、脚本执行、WDA 和 USB 进程、设备管理、图色识别与开放接口服务 |
| `src/Lianxin.Fleet.Cli` | 跨平台诊断、配置和 Mac iOS 签名工具 |
| `src/Lianxin.Sms.Api` | 原短信 HTTP API 的 ASP.NET Core 兼容服务 |
| `checks/Lianxin.Fleet.Checks` | 原生核心、桥接、坐标和签名自检 |
| `checks/Lianxin.Sms.Checks` | 真实 Kestrel HTTP 短信接口集成检查 |
| `ios/` | Swift iPhone 前台短信样例 |

Windows 桌面数据库位于 `%LOCALAPPDATA%\LianxinFleet\fleet.sqlite`。macOS 工作站与 CLI 共用 `~/Library/Application Support/LianxinFleet.Mac/fleet.sqlite`。旧版遗留的 `fleet-demo.sqlite` 已停用。CLI 可用 `--data` 覆盖数据目录，Windows 和 macOS 工作站可用 `FLEET_DATA_DIR` 指定数据目录，例如用来隔离测试。应用批准目录默认是数据目录中的 `apps`，可用 `FLEET_APP_DIR` 覆盖。

## 从源码构建

需要 .NET 10 SDK。WPF 主控运行需要 Windows 11；macOS 工作站（`src/Lianxin.Fleet.Mac`）运行在 macOS 上，Mac 也可构建和测试跨平台核心，并交叉编译 Windows 目标。

```sh
dotnet msbuild src/Lianxin.Fleet.Core/Lianxin.Fleet.Core.csproj -t:ValidateGovernanceRules,ValidateRepositoryGovernance
dotnet build Lianxin.Fleet.sln -c Release
dotnet format Lianxin.Fleet.sln whitespace --verify-no-changes --no-restore
dotnet run --no-build -c Release --project checks/Lianxin.Fleet.Checks
dotnet run --no-build -c Release --project checks/Lianxin.Sms.Checks
```

发布自包含 Windows x64 桌面程序：

```sh
dotnet publish src/Lianxin.Fleet.Desktop -c Release -r win-x64 --self-contained true -p:RuntimeFrameworkVersion=10.0.15 -o dist/native-win-x64
```

Windows 可使用 `scripts/verify.ps1` 执行完整治理与回归检查，使用 `scripts/publish-windows.ps1` 同时发布桌面和短信兼容服务。macOS 安装包在 Mac 上用 `scripts/publish-mac.sh` 打包，也可在 Windows 上用 `pwsh scripts/publish-mac.ps1` 交叉编译，产物为 `dist/LianxinFleet-Mac-<版本>-<arm64|x64>.zip`。跨平台 CLI 示例：

```sh
dotnet run --project src/Lianxin.Fleet.Cli -- status --data "$(mktemp -d)"
dotnet run --project src/Lianxin.Fleet.Cli -- sign-ios --help
```

## iOS 签名与短信兼容

Mac 除运行 macOS 群控工作站外，还负责 Xcode 构建和签名。普通开发者账号使用开发或 Ad Hoc 描述文件；企业分发必须使用企业账号和企业描述文件。签名工具显式校验 Team、Bundle ID、有效期、目标设备及钥匙串证书，不自动选择证书，不上传 App Store。

原短信接口由 C# 兼容服务提供，保留会话隔离、额度、设备领取回报、入站去重和同步协议。短信服务只提供 JSON 接口，不含网页工作台和 Lamp 登录；当前主控是原生桌面群控界面。详见 [短信兼容服务](docs/SMS_API.md)。

旧 JavaScript 版本已完整备份在本机 `data/migration/legacy-javascript-before-csharp-20261117.zip`，归档有逐文件 SHA-256 清单。原业务数据库没有删除或自动转换；原生群控使用独立数据库。

完整设备接入、WDA 签名和安装步骤见 [设备与签名指南](docs/DEVICE_SETUP.md)。Windows 版的手机控制已在 10 台 iPhone 6s（iOS 15.8.8，USB）上实测，发现的问题、修复和仍未解决的问题见 [真机测试记录](docs/DEVICE_TEST_REPORT.md)；全面屏机型、iOS 16.4 以上和 macOS 版仍需在目标工作站验收。
