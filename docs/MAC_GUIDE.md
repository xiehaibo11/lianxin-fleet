# macOS 版中控

macOS 版（`src/Lianxin.Fleet.Mac`，Avalonia 12）与 Windows 版功能相同：中控主窗口有设备、AI 助手、定时任务、设置四个标签，工具栏可打开集控投屏、一键配置和无线中心。各项功能的用法见 [CENTER_GUIDE.md](CENTER_GUIDE.md)，本文只说明 Mac 上的安装和差异。

## 安装

1. 从 GitHub Release 下载对应芯片的压缩包：Apple 芯片（M1 及以后）选 `LianxinFleet-Mac-<版本>-arm64.zip`，Intel 芯片选 `-x64.zip`。需要 macOS 14 或更高版本。
2. 双击解压，把 `LianxinFleet-Mac.app` 拖进「应用程序」。
3. 安装包没有经过 Apple 公证。首次打开前在「终端」执行一次，移除下载隔离属性：

   ```sh
   xattr -dr com.apple.quarantine /Applications/LianxinFleet-Mac.app
   ```

   否则系统会提示「已损坏」或「无法验证开发者」。也可以先双击一次，再到「系统设置 → 隐私与安全性」点「仍要打开」。
4. 安装设备工具（只需一次）。在「终端」建一个独立的 Python 环境并安装 pymobiledevice3：

   ```sh
   python3 -m venv ~/lianxin-python
   ~/lianxin-python/bin/python -m pip install pymobiledevice3
   echo ~/lianxin-python/bin/python
   ```

   没有 `python3` 时先安装 Xcode 命令行工具（`xcode-select --install`）或 Homebrew 的 Python。
5. 打开中控 →「设置」→「设备工具」，填写上一步最后一行打印出的完整路径（例如 `/Users/你的用户名/lianxin-python/bin/python`），保存后重启中控。从访达启动的程序读不到终端里的 PATH，所以这里必须填完整路径。

## 与 Windows 版的差异

| 项目 | macOS | Windows |
| --- | --- | --- |
| 数据目录 | `~/Library/Application Support/LianxinFleet.Mac/` | `%LOCALAPPDATA%\LianxinFleet\` |
| USB 设备服务 | 系统自带 usbmuxd，无需安装 iTunes | Apple Mobile Device Service |
| 重置 USB | 重启 usbmuxd，弹出管理员密码框 | 重启 Apple Mobile Device Service，弹出 UAC |
| 无线发现 | 系统自带 Bonjour | 需要另外安装 Bonjour |
| AI 密钥 | 数据目录下的 `agent-key` 文件，权限 600，只有当前用户可读 | 按当前 Windows 用户用 DPAPI 加密，存在 `settings.json` |

数据目录可用环境变量 `FLEET_DATA_DIR` 覆盖，应用批准目录（安装包必须放在这里）默认是数据目录下的 `apps`，可用 `FLEET_APP_DIR` 覆盖。macOS 工作站与 CLI 共用同一个数据库，同一时间只能由一个程序打开。

## 自行打包

- 在 Mac 上：`scripts/publish-mac.sh`，会做本机临时签名（ad-hoc）并用 `ditto` 压缩，产物在 `dist/`。
- 在 Windows 上交叉编译：`pwsh scripts/publish-mac.ps1`，需要 Python 写入带 Unix 权限的 zip。Windows 上无法签名整个 .app，只有主程序带临时签名，所以在 Mac 上仍需执行上面的 `xattr` 命令。

两种方式都只产生未公证的安装包。正式分发需要 Apple Developer ID 证书签名并提交公证。
