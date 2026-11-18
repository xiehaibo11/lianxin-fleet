# Windows 11 原生 iPhone 群控接入

Windows 11 通过 USB 直接连接 iPhone，运行 C# WPF 主控；Mac 只负责 Xcode 构建和签名。运行时无需常驻 Mac 节点。

## Windows 准备

1. 解压原生 Windows 发布包，启动 `Lianxin.Fleet.exe`。程序只连接真实 iPhone，没有演示模式。
2. 安装 Apple 官方设备驱动，确认 Apple Mobile Device Service 正常运行。
3. 安装 Python 3.9 以上版本，建议独立 Python 3.12 环境：

```powershell
python -m pip install -r requirements-windows.txt
python -m pymobiledevice3 --no-color version
python -m pymobiledevice3 --no-color usbmux list --usb
```

本项目固定 `pymobiledevice3==11.57.1`，已在本机实际核对相关 CLI 参数；Windows USB 运行仍需实机验收。若安装在独立环境，设置 `PYTHON_PATH` 为其绝对 `python.exe` 路径。[上游平台要求](https://github.com/doronz88/pymobiledevice3)

手机需解锁、信任 Windows 主机，并启用开发者模式及 UI 自动化。USB 集线器须有足够供电与稳定的数据链路。

## 签名方式

| 方式 | 账号和描述文件 |
| --- | --- |
| `debugging` / `development` | 普通开发者账号、开发证书、包含设备的开发描述文件；WDA 使用此方式 |
| `release-testing` / `ad-hoc` | 普通开发者账号、分发证书、包含设备的 Ad Hoc 描述文件 |
| `enterprise` | 企业开发者账号和企业内部发布描述文件 |

普通开发者账号不能生成企业内部发布描述文件。Apple 当前按每会员年度、每产品类型最多登记 100 台测试设备，停用不会立即恢复名额。[设备登记说明](https://developer.apple.com/help/account/devices/devices-overview)、[企业计划](https://developer.apple.com/programs/enterprise/)

## Mac 导出联信样例 App

证书及私钥保留在 Mac 钥匙串，Windows 只接收已签名安装包。

```sh
dotnet run --project src/Lianxin.Fleet.Cli -- sign-ios --check-only
dotnet run --project src/Lianxin.Fleet.Cli -- sign-ios --check-only \
  --team YOURTEAMID \
  --profile /absolute/path/Development.mobileprovision \
  --bundle-id com.yourcompany.lianxin.device \
  --method debugging \
  --udids YOUR_DEVICE_UDID
```

完整预检通过后去掉 `--check-only` 执行归档导出。可用 `--output` 指定不存在的新目录，工具拒绝覆盖已有产物。真实执行时会安装指定描述文件到当前用户 Xcode 配置目录，不上传 App Store，不自动创建证书或设备。

该命令针对 `ios/LianxinDevice.xcodeproj` 的单 App target。联信 Swift 样例用于人工确认短信，不代替 WDA。

## Mac 构建 WDA

官方 WDA 已迁移至 [appium/appium-ios 的 packages/webdriveragent](https://github.com/appium/appium-ios/tree/main/packages/webdriveragent)。本机准备的固定源码是提交 `582beec62211b41800a9dd32f8e91cced0228158`，版本 17.0.11，位于 `data/vendor/WebDriverAgent`，来源记录为 `data/vendor/WebDriverAgent.source.json`。

本机 Xcode 26.3 / iPhoneOS SDK 26.2 的无用户签名真机目标构建已成功，产物位于 `data/wda-build/Build/Products/Debug-iphoneos/WebDriverAgentRunner-Runner.app`。**该准备产物没有匹配的描述文件，不能直接安装。**

在 Xcode 中为 WDA runner 配置自己的 Team、Bundle ID 和开发描述文件，包含全部目标 UDID。然后重新构建：

```sh
xcodebuild -project /absolute/path/WebDriverAgent.xcodeproj \
  -scheme WebDriverAgentRunner -configuration Debug \
  -destination 'generic/platform=iOS' \
  -derivedDataPath /absolute/new/WDA-signed \
  build-for-testing DEVELOPMENT_TEAM=YOURTEAMID
```

确认 `WebDriverAgentRunner-Runner.app` 内完整签名和描述文件正确，记录实际 `CFBundleIdentifier`。可将完整签名 runner 放入新目录 `Payload/` 并打包 IPA：

```sh
mkdir -p /absolute/new/WDA-package/Payload
ditto /absolute/path/WebDriverAgentRunner-Runner.app \
  /absolute/new/WDA-package/Payload/WebDriverAgentRunner-Runner.app
ditto --norsrc --noextattr --noqtn -c -k --keepParent /absolute/new/WDA-package/Payload \
  /absolute/new/WDA-package/WebDriverAgent.ipa
```

不要删改 runner 内测试 bundle 或框架。WDA/iOS 组合须先通过一台真机验证。[Appium 真机准备](https://github.com/appium/appium-xcuitest-driver/blob/master/docs/getting-started/device-setup.md)

## 安装并批量启动

在 C# 桌面中点击“扫描 USB”，新设备会自动登记并分配本机端口。点击“打开应用目录”，将已签名 WDA IPA 放入批准目录，选择目标设备后批量安装。首次也可手动安装：

```powershell
python -m pymobiledevice3 --no-color apps install --udid YOUR_DEVICE_UDID C:\Lianxin\WebDriverAgent.ipa
```

桌面端默认使用联信签名包的 WDA Runner Bundle ID `com.lianxin.fleet.webdriveragent.xctrunner`。使用其他签名包时，输入其实际 Bundle ID。点击“启动通道”后，程序分批启动 WDA，并在进程内把每台手机的本机端口经 Apple USB 设备服务（Windows 为 127.0.0.1:27015，macOS 为 `/var/run/usbmuxd`）转发到手机 8100。实时投屏直接连接手机 9100 画面流，不占用本机端口。停止按钮结束本次创建的子进程和转发。

单机排障命令（不经主控时手动转发）：

```powershell
python -m pymobiledevice3 --no-color developer dvt xcuitest --udid YOUR_DEVICE_UDID --env USE_PORT=8100 com.yourcompany.WebDriverAgentRunner.xctrunner
python -m pymobiledevice3 --no-color usbmux forward --udid YOUR_DEVICE_UDID --host 127.0.0.1 8101 8100
```

两条排障命令分别长期运行，不要与主控同时占用同一设备或端口；主控启动通道时，若本机端口已被排障命令占用，会在通道日志中提示。每台手机使用独立本机端口，手机内部端口均可保持 8100。WDA 在线后在主控详情核对“设备实际名”；WDA 的 vendor UUID 不能代替硬件 UDID 核验。

## 操作边界

- 投屏为 WDA 画面流，约 5 帧/秒、30% 尺寸，不承诺视频帧率；USB 集线器带宽不足时会掉帧。停止投屏后回到每 3 秒快照，最多预览 12 台。
- 同步操作最多 32 台并行，同一台手机仍依次执行；锁屏/解锁、音量、多任务和剪贴板依赖 WDA 与 iOS 版本，需先在一台真机确认。
- 相对坐标群控要求目标页面和方向一致；不同布局、弹窗与加载状态可能产生不同结果。
- 安装只允许批准目录内的 `.ipa` 或 `.app`，仍受 Apple 签名与设备授权限制。
- 任务“已提交”表示排队，结果按各设备回报；“结果待确认”可能已经执行，核对后再操作。
- 数据库占用锁阻止多实例同时恢复和执行同一队列；旧版演示数据库会被拒绝打开。
- 当前已完成编译和自动检查，Windows 图形界面、驱动及物理 iPhone 尚需目标机验收。
