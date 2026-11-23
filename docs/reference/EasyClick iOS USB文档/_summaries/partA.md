# Part A：开始使用、AI 智能体、投屏教程与高级功能

本部分共 39 页，按侧栏顺序分为四块：概览和「开始使用」（下载工具、签名安装代理 IPA、安装中控、安装 IDEA 插件、创建第一个工程）；「AI 智能体」15 页（环境与授权、快速开始、AI 对话、工作流编辑器、.spk/.spl 打包与授权、图色找图、步骤参考、配置、外部集成、定时任务、任务监控、排障）；投屏 4 页（11.6.0 新手安装、电脑配置案例、新/经典界面操作）；「高级功能」16 页（AI 辅助编程、脱机激活器、蓝牙 BLE、OTG HID、桥接配置、网页工具、远程装 IPA、部署、插件占位页、键盘码表、代码混淆、NPM/TypeScript、远程调试）。
多数页面是图文操作步骤，配有授权类型（USB 设备授权 / USB 投屏授权）和系统版本条件（iOS 15+、iOS 17+、IDEA 2026.2+ 等）。代码集中在 AI 辅助编程、系统配置、网页工具、键盘码表、混淆和 NPM/TS 几页。
远程安装 IPA、系统部署两页在原文中标注为“已失效”。插件开发、Java-JS 混合开发、远程调试三页只放了指向安卓文档的链接。
注意：原站 15 个 AI 智能体页面的页头都有一条「产品升级提示（10.6.0+）」，说明从 10.6.0 起 AI 智能体已移出 iOS USB 中控，改为独立产品 iEasyRun（https://ieasyrun.com/）。这条提示不在正文区，当前 Markdown 里缺失。

- [概览](index.md)：产品总览。iOS USB 版用于免越狱真机自动化，支持 iOS 12–27+，有两种用法：新中控的 AI 智能体（对话 / 工作流）和 EC 脚本开发。页面给出两种用法的入口和各章节阅读路线。标签：表格 / 注意事项 / 版本要求
- [下载开发工具](tools/download_resources.md)：开发插件、IPA 包和签名工具的网盘地址（123 网盘、百度网盘、官方下载站），IDEA 2026.2+ 的下载地址和 Classic UI 插件提示，以及老冷视频教程链接。标签：注意事项 / 版本要求
- [安装代理ipa](tools/signagent.md)：代理 IPA 的签名途径（巨魔免费签、688 元/年个人开发者、个签/企业签）和用爱思助手安装；iOS 15+ 可以改用脱机主程序。后半部分讲在 macOS 上用 Xcode 编译 Agent 源码、设置 Team 签名，以及 bundle id 冲突、证书过期等常见 Xcode 报错的处理。标签：截图 / 注意事项 / 版本要求
- [安装中控程序](tools/installcenter.md)：下载解压 Windows/macOS 中控（以 9.19.0 为例，附 10.2.0 新界面说明），iOS 17 以下要替换开发者镜像；然后登录验证、填写 bundleID、开启/测试自动化，说明两种 USB 授权，以及单台/批量执行 iec 脚本和群控入口。标签：截图 / 注意事项 / 版本要求
- [安装开发插件](tools/installdevtools.md)：从网盘下载 EasyClick 开发插件，在 IDEA（强烈推荐 2026.2+）里选「从磁盘安装」，然后重启 IDEA。标签：截图 / 版本要求
- [创建第一个工程](firstproject.md)：在 IDEA 中新建「EasyClick IOS USB版-脚本项目」模块，再通过中控调试地址（默认 127.0.0.1:8019）连接中控调试，多台设备时要选中目标设备。标签：截图
- [环境与授权](advance/ai-agent/prerequisites.md)：AI 智能体的必读前提：先购买并绑定 USB 设备授权（不要买成投屏授权），再配好运行环境：方案 A 是自动化 IPA / 脱机主程序，方案 B 是蓝牙 BLE / OTG。末尾有跑工作流前的自检表。标签：表格 / 注意事项
- [常见问题速查](advance/ai-agent/faq.md)：三条高频问答：该买哪种授权；哪些功能需要大模型（只有 AI 对话和 AI 写流程需要）；跑工作流是否消耗 Token（运行/试跑不消耗，OCR 和找图在本地执行，VLM 单独计费）。标签：表格 / 注意事项
- [快速开始](advance/ai-agent/getting-started.md)：三步上手：打开新中控的 AI 智能体，配置大模型（DeepSeek / OpenAI / Ollama 模板），选择设备或分组。还介绍了能做的事和典型场景、判断“已就绪”的方法，以及数据目录 aibin/data/aiagent/ 和日志目录 aibin/log/。标签：表格 / 注意事项
- [界面预览](advance/ai-agent/screenshots.md)：AI 对话、工作流编辑器和任务监控三个界面的截图，外加一张 Mermaid 流程图，展示 30 秒上手路径。标签：截图 / 注意事项
- [AI 对话](advance/ai-agent/ai-chat.md)：对话页的布局和工具栏按钮、三步用法和示例说法、无自动化/自动化的截图·OCR·找图名称对照、左侧工作流库的操作，以及未配大模型时可用的 `list workflows` / `run` 固定指令。标签：表格 / 注意事项
- [工作流编辑器](advance/ai-agent/workflow-editor.md)：编辑器界面和工作流库（根流程/子流程），新建、保存、校验、排版，可视化与文本两种模式，流程图元素，流程配置和变量池，试跑与回放，AI 辅助写流程，各类步骤的预览，以及子工作流。标签：表格 / 注意事项
- [工作流 .spk](advance/ai-agent/workflow-spk.md)：实施方把根工作流导出为加密的只读 .spk 包（导出闭包预览、密码、可选 zip），文件和密码要分渠道发送；执行方导入（并列安装或覆盖），导入后能做什么、如何卸载，并附常见问答。标签：表格
- [工作流 .spl 授权](advance/ai-agent/workflow-spk-license.md)：实施方签发 .spl 商业授权，绑定工作流、手机 deviceId 和有效期（签发 Tab 和历史记录 Tab）；执行方在编辑器或对话工作流库里导入/更新授权、查看授权详情。运行前会校验授权，并且必须联网对时。标签：表格 / 注意事项
- [图色找图](advance/ai-agent/image-match.md)：界面没有可用节点时，用模板找图或偏色找图定位，套路是“截图 → 找图 → pick_match → 点击”。内容有模板来源和编辑器找图预览、match_template / find_image_by_color / pick_match 参数表、典型流程示例和未匹配、点偏等问题。标签：表格 / 注意事项
- [工作流步骤参考](advance/ai-agent/workflow-steps.md)：按类别列出全部步骤 op 和主要参数，类别有环境与 App、等待日志、手势、截图/OCR、VLM、USB 恢复、相册、IME、BLE、数据提取、图色识别、本机文件、HTTP/CLI/MCP 和流程控制。还说明了 {{var}} 模板语法、baseDir 路径和保存前的校验规则。标签：表格 / 注意事项
- [配置与数据目录](advance/ai-agent/config.md)：设置对话框（数据存储目录、单任务最大设备数、自动保存）、可保存多套的大模型配置、需要手动编辑的 vlm_config.json、OCR 引擎类型、找图模板目录、数据目录里各文件的用途，以及可选的环境变量。标签：表格 / 注意事项
- [外部集成](advance/ai-agent/external-integration.md)：在「CLI/MCP 集成」里登记命令行程序、HTTP 接口和 MCP 服务，供工作流步骤和 AI 对话调用；附安全提示，并说明它和“与 AI 结合辅助编程”里的 CLI 互不相关。标签：表格 / 注意事项
- [定时任务](advance/ai-agent/workflow-schedule.md)：按“每天 / 间隔分钟 / 间隔秒”规则自动运行工作流。内容有新建字段、设备忙策略、列表操作、任务来源标签，以及使用注意：错过的触发不补跑，AI 进程必须一直运行，执行次数上限。标签：表格 / 注意事项
- [任务与监控](advance/ai-agent/tasks.md)：任务的来源，任务面板列表和各状态的含义，每台设备的执行明细，继续、强制停止、回放、复制任务 ID 等操作，以及“等人操作”的暂停步骤。标签：表格
- [常见问题](advance/ai-agent/troubleshooting.md)：按现象排障：AI 窗口打不开或显示未连接、没配大模型、授权失败、自动化/BLE/截图/OCR/找图失败、校验失败、外部命令跑不起来。末尾有日志文件位置表和求助时要准备的材料。标签：表格
- [苹果群控投屏新手安装](advance/ios-usb-beginner-install.md)：11.6.0 USB/无线二合一的 12 步图文安装：用启动器一键安装中控，一键配置开发者模式、镜像和 IPA，信任证书，启动自动化，选择投屏方式，绑定 USB 投屏授权。附 17 条常见卡点、USB HID 模式（iOS 17+）和报错排查。标签：表格 / 截图 / 注意事项 / 版本要求
- [电脑配置与客户案例](advance/ios-usb-pc-config.md)：群控电脑选购经验：先看主板 USB（B85/X79 大板、PCIe 扩展卡），避开部分 X99，规模大时考虑分布式桥接。内容还有投屏卡顿的软硬件排查和 7 个客户实际配置案例（约 94 台投屏，约 160 台脚本加投屏）。标签：表格 / 截图 / 注意事项
- [新界面操作指引](advance/ios-usb-screen-new.md)：全新投屏客户端的进入方式和界面区域、回中控启动自动化、四种场景模式的选择（USB+自动化服务 / 无自动化截图+USB_BLE / 无自动化截图+USB_HID / 主程序录屏+USB_HID，后三种需 iOS 17+），以及打字、脚本录制、传图片视频、剪贴板、打开网址等操作。标签：表格 / 注意事项 / 版本要求
- [经典界面操作指引](advance/ios-usb-screen-legacy.md)：经典投屏界面的进入方式、填 bundleID 前缀后启动自动化、代理 IPA 方式投屏，以及打字、录制、上传图片视频、剪贴板、打开 URL 等操作，按 iOS 15 以上/以下分别说明。标签：截图 / 注意事项 / 版本要求
- [与AI结合辅助编程](advance/ios-usb-ai.md)：用 Cursor + IDEA（插件 9.27.0+）辅助写 EC 脚本：安装 AI DOCS，配置 IDEA 插件提供的 MCP 服务（127.0.0.1:18562/mcp）；ec_work_config/ios/bin 下有 CLI 和 SKILL.md，可让 AI 编译、运行工程（USB 版不能预览）。页末附完整的 ec-ios-cli SKILL.md。该文件的一级标题是“说明”。标签：代码示例 / 截图 / 注意事项 / 版本要求
- [脱机激活器](advance/ios-usb-activator.md)：独立的桌面客户端，用来批量准备设备环境：连接设备、配置主程序/代理 BundleId、刷入镜像、批量初始化、开启 WiFi 调试、一键自激活（生成并推送配对文件和镜像）、单设备操作、授权状态，附常见问题。标签：表格 / 注意事项
- [蓝牙BLE教程](advance/ios-usb-ble.md)：ESP32-C3 蓝牙 HID 方案：下载和刷入固件（相对/绝对坐标两种），中控绑定和测试，串口/WiFi 通信，键盘快捷键；没有代理 IPA 时用快捷指令实现文字输入和相册导入。还列出手机需要开启的辅助功能选项、灯语和常见问题。标签：截图 / 版本要求
- [OTG HID教程](advance/ios-usb-otg.md)：ESP32-S3 OTG HID 方案（仅 iOS 17+）：刷固件、绑定、配网和 IP 扫描，借助爱思装的 Bonjour 开启无线调试，测试 OTG、键盘快捷键，以及没有代理时的输入/相册方案，附常见问题。标签：截图 / 版本要求
- [系统配置](advance/centerconfig.md)：桥接配置文件 bridgebin/config/config.toml 的各项说明：中控地址 site.remote、代理/投屏端口、发现设备模式、启动超时、iOS 15+ 按普通 App 运行、自动重置 USB、投屏质量。标签：代码示例
- [网页工具](advance/webtools.md)：桥接自带的网页工具（/tools?deviceId=…），在 iOS 浏览器里实现上传/获取内容、复制到剪贴板、打开 URL。内容有局域网和服务器两种部署、URL 参数，以及 /postApiData、/getPageData 两个接口的请求/返回示例。标签：代码示例 / 截图 / 注意事项
- [远程安装IPA](advance/xcoderemote.md)：原文标注已失效。做法是用中控的 Xcode 远程调试或 ios-forward 转发器，把设备端端口暴露到公网（花生壳/frp），让远程 Mac 上的 Xcode 安装代理 IPA。标签：截图 / 注意事项
- [系统部署](advance/deploy.md)：原文标注已失效。内容有单 PC 部署、在树莓派上运行 ios-bridge 的分布式部署（改 site.remote、后台启动、中控设为短链接模式并设置分布式地址）和集群部署。标签：截图 / 注意事项
- [插件开发](funcs/plugin/plugins.md)：占位页，只链接到安卓版插件开发文档。标签：仅导航
- [Java-JS混合开发](funcs/plugin/javajs.md)：占位页，只链接到安卓版 Java-JS 混合开发文档。标签：仅导航
- [键盘输入码表](advance/keyboard.md)：HID 键盘模拟用的 eventPageID / eventUsageID 常量表，分快捷键（Consumer Page 0x0C）和键盘输入（Keyboard Page 0x07）两部分，内容从源码复制。标签：代码示例
- [代码混淆](advance/jsobfuscator.md)：IDEA 插件 5.0.0+ 会生成 obfuscator.json。要开启编译期混淆，需安装 Node.js 和 javascript-obfuscator，并配置 nodeBinPath / obfuscatorBinPath。页面也提醒混淆会增大体积、耗资源，建议只在发布时开启。标签：代码示例 / 注意事项 / 版本要求
- [NPM和TypeScript支持](advance/npmandtypescript.md)：EC USB 8.0+ 工程可启用 NPM（生成 package.json，只能用纯 JS 类库）和 TypeScript（生成 tsconfig.json）。页面对比了在 js 文件夹和 ts 文件夹下写 ts 的区别（直接引用或 export + require），给出示例代码和混编建议。标签：代码示例 / 注意事项 / 版本要求
- [远程调试](remotedebug.md)：只有一句话，链接到安卓版远程调试软件的视频。标签：仅导航
