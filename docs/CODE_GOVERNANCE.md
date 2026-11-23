# 代码治理规范

## 文件规模

单文件以 200–300 行为目标，300 行为硬上限。按物理行计数，包含注释和空行。职责完整的小文件不受最低行数约束；不能填充注释、复制逻辑或压缩多语句来凑行数。

超过上限时按稳定职责拆分。需要共享状态的同一类型可使用 partial，文件后缀表达职责，例如 `FleetStore.Tasks.cs`；不使用无意义的 Part1/Part2。同类碎片在完整文件仍不超过 300 行时合并。

检查范围包括项目 C#、XAML、SQL、Swift、PowerShell 和启动脚本；自动生成的 `bin/obj`、运行数据 `data`、发布产物 `dist` 和第三方源码不参加源代码治理。

## 类别与目录

| 项目 | 类别与职责 |
| --- | --- |
| Fleet.Core / Domain | 领域枚举和不可变记录 |
| Fleet.Core / Devices | 设备契约、WDA 通讯、画面流和控制通道 |
| Fleet.Core / Persistence | SQLite 连接、设备和任务持久化 |
| Fleet.Core / Services | 状态刷新与任务编排 |
| Fleet.Core / Validation | 输入、坐标和安装目录校验 |
| Fleet.Core / Infrastructure | USB 工具与受控子进程 |
| Fleet.Core / Serialization | 序列化约定 |
| Fleet.Core / Agents | AI 助手：模型请求、看屏与操作循环 |
| Fleet.Core / Imaging | PNG 解码、比色找色与模板找图 |
| Fleet.Core / OpenApi | 兼容 EasyClick 企业版开放接口的本机 HTTP 服务：请求解析、安全检查、接口路由与电脑端文件中转目录边界 |
| Fleet.Mac / Views | macOS Avalonia 窗口、标签页、对话框及其代码后置 |
| Fleet.Mac / ViewModels | 设备、脚本、定时任务和任务显示状态 |
| Fleet.Mac / Interaction | 命令、屏幕坐标换算、AI 截图缩放与本机密钥保存 |
| Fleet.Mac / Resources | Avalonia 样式与模板 |
| Fleet.Cli / Commands | 命令行参数 |
| Fleet.Cli / Signing | iOS 描述文件与签名导出 |
| Fleet.Cli / Properties | 程序集元数据 |
| Fleet.Desktop / Views | 原生窗口及其代码后置 |
| Fleet.Desktop / ViewModels | 设备和任务显示状态 |
| Fleet.Desktop / Interaction | 命令、屏幕坐标换算、AI 截图缩放与本机密钥保护 |
| Fleet.Desktop / Resources | WPF 样式与模板 |
| Sms.Api / Hosting | 启动、配置和定时任务 |
| Sms.Api / Routing | 认证、管理员、设备和用户协议路由 |
| Sms.Api / Persistence | 短信数据与 SQL schema |
| Sms.Api / Security | token、密码和凭据算法 |
| Sms.Api / Validation | HTTP 输入校验 |
| Sms.Api / Protocol | 错误与旧协议编码约定 |
| checks / Scenarios | 行为与兼容性检查 |
| checks / Infrastructure | 断言、临时目录、假设备和 HTTP fixture |
| ios / App、Models、Networking、Security、Views | 应用入口、模型、请求、钥匙串和界面 |

项目根目录仅保留程序入口，例如 `Program.cs`；WPF 的 `App.xaml`、Avalonia 的 `App.axaml` 与各自代码后置保留在根目录。`AssemblyInfo.cs` 放在 `Properties`，可不声明命名空间。新增项目或职责类别需要先更新治理规则。

## 依赖方向

- Domain 不依赖数据库、进程或 UI。
- Devices 通过设备契约向 Services 提供能力，调用 Infrastructure 的受控进程工具。
- Services 编排设备和持久化；Persistence 不反向调用 UI。
- Desktop 与 CLI 调用 Core；Core 不引用 Desktop 或 CLI。
- SMS Hosting 组装路由与存储，协议与凭据辅助不依赖 UI。
- checks 通过公开入口验证行为；生产项目不引用 checks。

不为分类创建只有转发作用的接口或包装层，不改变数据模型、任务状态、交易式扣额、签名和设备权限边界。

## 命名空间与格式

C# 命名空间与目录一致，例如 `src/Lianxin.Fleet.Core/Devices/DeviceBridge.cs` 对应 `Lianxin.Fleet.Core.Devices`。使用 file-scoped namespace。类型名称描述职责，避免笼统的 Misc/Helper 大杂烩。

`.editorconfig` 统一 UTF-8、缩进、换行和 C# 格式。保持标准展开布局，清理无用 using。Windows 启动与 PowerShell 脚本使用 CRLF。WPF 的 `x:Class`、资源 URI、测试链接及 Xcode 显式编译源必须随文件移动一起更新。

## 构建门禁

普通 `dotnet build` 自动执行 `Directory.Build.targets` 中的源代码治理任务：

- `GOV001`：超过 300 行。
- `GOV002`：未按项目职责类别归档。
- `GOV003`：命名空间与物理目录不一致。
- `GOV004`：项目链接指向不存在的源文件。

检查器实现位于 `build/SourceGovernance.cs`，只依赖 .NET SDK 自带 MSBuild，不引入运行时依赖。规则自检覆盖 300/301 行边界、错误类别、错误命名空间、根入口、生成文件排除及移动后引用完整性。

独立执行全仓库检查：

```sh
dotnet msbuild src/Lianxin.Fleet.Core/Lianxin.Fleet.Core.csproj -t:ValidateGovernanceRules,ValidateRepositoryGovernance
```

完整验证：

```sh
dotnet build Lianxin.Fleet.sln -c Release
dotnet format Lianxin.Fleet.sln whitespace --verify-no-changes --no-restore
dotnet format Lianxin.Fleet.sln style --diagnostics IDE0005 --severity info --verify-no-changes --no-restore
dotnet run --no-build -c Release --project checks/Lianxin.Fleet.Checks
dotnet run --no-build -c Release --project checks/Lianxin.Sms.Checks
```

Windows 可直接执行 `powershell -File scripts/verify.ps1`。发布脚本也会先执行治理门禁。iOS 调整后另执行 Xcode 模拟器无签名构建。
