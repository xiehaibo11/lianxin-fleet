# EasyClick iOS USB 文档（本地离线版）

来源：https://ieasyclick.com/iosdocs ，抓取日期 2026-11-22。共 98 页，代码块 1619 个，表格 430 个，图片 171 张（其中 23 张在官网本身已失效，保留原链接）。

- 每页一个 Markdown 文件，目录结构与网址一致（例如 `funcs/device-api.md` 对应 `/iosdocs/funcs/device-api/`），文件第一行注明原网址。
- 图片在 `_assets/`，原始网页在 `_html/`，页面顺序与侧栏分组见 `_toc.json`。
- 代码标签页（Python / Node.js / cURL / C#）用粗体标签分隔；提示框转成 `> **提示：…**` 引用块。
- 下面按侧栏顺序列出每页内容摘要（由子代理阅读全文后编写）。

## Part A：开始使用、AI 智能体、投屏教程与高级功能

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

## Part B：脚本函数 API 参考与未列入侧栏的 funcs 页面

本部分共 33 页：「参考 › 脚本函数」侧栏下的 25 个 API 页面，加上侧栏未列出的脚本函数概览页和 7 个 devtools（开发工具）页面。
API 按运行位置分为两类：一类是电脑侧中控函数（节点、图色、OCR、YOLO、网络、文件等），另一类是名称带“-手机内执行”的 Agent 版本（nodeAgent / imageAgent / ocrAgent / yolov8Agent）。另有 BLE、OTG、USB HID、Aux 四条外设或远程控机链路。
几乎每个函数都附 @param/@return 说明和 JavaScript 示例，并标注最低 EC iOS USB / 中控 / 脱机版本。
devtools 页面是 IDEA 开发插件的旧版操作说明：配图在站点上已经 404，部分文字沿用安卓版的说法（APK、免 root、无障碍）。概览页只有模块链接列表。

- [全局模块](funcs/global/index.md)：无需前缀、直接调用的全局函数，包括 CLI 参数 getCliArgs、version/ipaVersion、插件加载（loadDex/require/importClass）、脚本启停与暂停（exit/sleep/execScript/restartScript）、JSON、停止与异常回调、日志（logd/loge/setLogLevel）、IEC 包资源读取（readIECFileAsString/readResAutoImage 等）、自动化服务（startEnv/startTunnel/closeEnv）、时间、钉钉告警和授权查询。标签：代码示例 / 版本要求
- [全局快捷事件](funcs/global/global-shortcut.md)：全局模块里封装的快捷操作（无前缀）。内容有读取 UI 配置（readAllUIConfig/readAllUIConfig2）、点击/压力点击/长按/双击、多点触摸、滑动与拖动、文字输入（inputText/typingText/ioHIDEvent）、屏幕方向、系统按键与应用管理（home/lockScreen/openApp/appLaunch/installApp 等）、USB 重连与代理设置、相册上传，以及受沙盒限制的设备文件操作 fsyncFileOpr/fsyncFilePushPull。标签：代码示例 / 注意事项 / 版本要求
- [节点函数-手机内执行](funcs/node-agent-api.md)：`nodeAgent` 前缀，节点运算在手机内完成。内容有 setFetchNodeParam 抓取参数、getNodeInfo/getOneNodeInfo、parent/child/allChildren/siblings 等级联遍历、lockNode/releaseNode、dumpXml，并说明它与 auxEvent + lockNodeFromXml 方案的区别。标签：代码示例 / 注意事项 / 版本要求
- [节点函数](funcs/node-api.md)：无前缀的电脑侧节点函数。内容有 setFetchNodeParam（需与 IDEA「节点调试设置」一致）、lockNode/lockNodeFromXml（9.39.0+）/releaseNode 三种锁定方式的对比表、getNodeInfo/getOneNodeInfo、按 id/xpath/label/name/type/value/visible/index/depth/bounds 等属性做全匹配或正则匹配的选择器、clickCenter/clickRandom、多属性级联查询和父子兄弟节点导航。标签：代码示例 / 表格 / 注意事项 / 版本要求
- [代理事件](funcs/event-api.md)：`agentEvent` 前缀的代理模式专属函数，包括 setAgentCallParam 全局通信超时、touchDown/touchMove/touchUp 手势事件和 agentEvent.lockNodeFromXml（9.39.0+）。其余功能直接调用全局函数。标签：代码示例 / 版本要求
- [图色函数-手机内执行](funcs/image-agent-api.md)：`imageAgent` 前缀，截图和图色运算都在手机内完成，省去传输。内容有 useOpencvMat/setInitParam、captureFullScreen(Ex)、比色/找色/多点找色/找非色、findImage/matchTemplate 找图、灰度与二值化、旋转/读图/取像素、mat 格式转换与裁剪、pushImageToAgent/pullImageToCenter 图片交换，以及图片回收。标签：代码示例 / 注意事项 / 版本要求
- [图色函数](funcs/image-api.md)：`image` 前缀的电脑侧图色函数，覆盖设置（useOpencvMat/setFindColorImageMode）、流式截图（startScreenStream/captureScreenStream）、jpg/png 截图和无自动化截图 captureFullScreenNoAuto。另有比色/找色/找非色（含 J 后缀变体）、findImage/matchTemplate/initOpenCV 找图、灰度二值化、缩放旋转与像素读取、Bitmap/Base64/mat 格式转换和回收。标签：代码示例 / 版本要求
- [蓝牙BLE函数](funcs/ble-event-api.md)：`bleEvent` 前缀，用 ESP32-C3 蓝牙开发板模拟鼠标和键盘。内容有 openSerial/closeSerial、setScale 补偿率（绝对坐标固件必须设为 1）、屏幕尺寸/通信方式/WiFi 设置、鼠标移动与归零、触摸/点击/长按/双击/滑动/多点触摸、系统按键与字符按键、蓝牙名与 LED，并附用快捷指令输入文字、插入相册的完整示例。标签：代码示例 / 注意事项 / 版本要求
- [OTG HID函数](funcs/otg-event-api.md)：`otgEvent` 前缀，用 ESP32-S3 OTG 开发板操作手机（只有绝对坐标固件，需 iOS 17+）。内容有 isOtgConnect/scanOtgDevice/getOtgIp 连接、setScreenSize/restart、鼠标移动与归零、触摸/点击/长按/双击/滑动/多点触摸、系统键与字符键、软键盘、MAC 地址和鼠标键，并附快捷指令示例。标签：代码示例 / 注意事项 / 版本要求
- [USB HID函数](funcs/usb-hid-event-api.md)：`usbHidEvent` 前缀（EC iOS USB 10.7.0+，建议 iOS 17+），直接通过 USB 数据线模拟点击、滑动和输入，不需要开发板。内容有会话开启/关闭/重置、setScreenSize、触摸/点击/长按/双击/滑动、multiTouch 与真多指 multiTouch2、系统键、粘贴输入与打字、按键/组合键、音量与剪贴板。页面强调它与代理自动化服务互斥，而且可能重挂开发者镜像。标签：代码示例 / 注意事项 / 版本要求
- [Aux 远程辅助函数](funcs/aux-event-api.md)：`auxEvent` 前缀（EC iOS USB 9.39.0+），PC 中控脚本经 USB 或 WiFi 调用手机端易点云测 App 的服务。函数分八类：会话与桥接（scanWifiDevices/startAuxBridge/ensureSession）、Agent 自动化 agent*、系统录屏 screencap*、OTG 远程 otg*、BLE 远程 ble*、输入法 ime*、设备信息 device*、工具与相册 util*/auxInsert*。页面还有分类表、WiFi 设备 ID 注入与白名单扫描说明，以及返回值约定。标签：代码示例 / 表格 / 注意事项 / 版本要求
- [YOLO函数-手机内执行](funcs/yolo-agent-api.md)：`yolov8Agent` 前缀，利用手机算力做 YOLOv8 目标检测。内容有 releaseAll、newYolov8/newYolov8Onxx 创建实例，以及 Yolov8AgentUtil 的 getOnnxConfig/getDefaultConfig/initYoloModel/detectImage/release/getErrorMsg；训练教程指向安卓版文档。标签：代码示例 / 注意事项 / 版本要求
- [YOLO函数](funcs/yolo-api.md)：`yolov8Api` 前缀的电脑侧 YOLOv8 检测。内容有 newYolov8/newYolov8Onxx（多实例）、releaseAll，以及 Yolov8Util 的配置获取、initYoloModel、detectBitmap/detectImage、release/getErrorMsg；可用 num_thread 控制 CPU 占用。标签：代码示例 / 版本要求
- [OCR识别-手机内执行](funcs/ocr-agent-api.md)：`ocrAgent` 前缀，OCR 在手机内运行。内容有 initOcr/ocrImage/getErrorMsg/releaseAll(Ex)，以及 9.0.0+ 的多实例 newOcr（appleVision、ocrlite、paddle ONNX，9.8.0+ 新增 PaddleNcnnOcrV5）和参数示例。标签：代码示例 / 注意事项 / 版本要求
- [OCR识别](funcs/ocr-api.md)：`ocr` 前缀的电脑侧 OCR。内容有 newOcr、releaseAll，initOcr 各引擎的参数说明（ocrLite、Tesseract、paddleOcr、PaddleNcnnOcrV5、paddleOcrOnnxV4/V5/V6），实例方法 ocrBitmap/ocrImage/getErrorMsg/releaseAll，以及各引擎示例。标签：代码示例 / 版本要求
- [输入法函数](funcs/ime-api.md)：`imeApi` 前缀（USB 9.20.0+，脱机版 3.15.0+），需要在手机上启用脱机主程序自带的易点云测键盘并允许完全访问。内容有 isOk、input/paste、pressDel/pressEnter、dismiss、剪贴板读写与复制、切换键盘、清空输入框、openUrl、getText，以及 forwardImeServer/closeForwardImeServer 输入法服务转发。标签：代码示例 / 注意事项 / 版本要求
- [辅助模块](funcs/apphelper-api.md)：`appHelper` 前缀，需要安装文件传输助手 IPA（即脱机主程序 3.16.0+）。提供 setHelperTimeout/setParam 设置、uploadToAlbum 插入相册、setPasteboard/getPasteboard 剪贴板和 openUrl。标签：代码示例 / 注意事项 / 版本要求
- [设备函数](funcs/device-api.md)：`device` 前缀，内容有 getDeviceMsg 运行信息、getOrientationNoAuto、getDeviceInfo/getDeviceId/中控别名/序列号/设备名、applist 程序列表、屏幕宽高与缩放比、机型、系统版本、电量和充电状态。标签：代码示例 / 版本要求
- [文件函数](funcs/file-api.md)：`file` 前缀。它操作的是电脑上的路径，每台设备有自己的沙盒目录（getSandBoxDir/getSandBoxFilePath）。内容有读写、追加、按行读取和删除行、列目录、创建/建目录/删除/判断存在/复制，以及读取 Excel 行。标签：代码示例 / 注意事项 / 版本要求
- [存储函数](funcs/storage-api.md)：用 `storages.create` 创建 key-value 存储对象。storage 实例提供 keys/all、putString/putInt/putBoolean/putFloat 写入、对应的 get 读取，以及 clear/remove/contains。标签：代码示例
- [网络函数](funcs/http-api.md)：`http` 前缀，内容有 request/requestEx 万能请求、downloadFile(Default)、httpGet/httpPost/postJSON，经代理 ipa 发出的 agentHttpGetString/agentHttpPostJson/agentRequestEx/agentForwardRequest，以及 newWebsocket 返回的 WebSocket 对象（连接、收发、回调、超时、心跳、自动重连）。标签：代码示例 / 注意事项 / 版本要求
- [线程函数](funcs/thread-api.md)：包括全局的 setTimeout/cancelTimeout、setInterval/cancelInterval、execSync，以及 `thread` 前缀的 execAsync/execSync、cancelThread/isCancelled、stopAll。标签：代码示例
- [工具函数](funcs/utils-api.md)：`utils` 前缀，内容有 zip/unzip/readFileInZip 解压缩、fileMd5/dataMd5、随机数、playMp3/stopMp3、相册授权与清空、agent 文件上传/删除与 uploadToAutoImage、开发者镜像挂载/卸载/检查（mountDevImage/checkDevImage）和 getPCIps。标签：代码示例 / 版本要求
- [JDBC MySQL函数](funcs/jdbcmysql-api.md)：`jdbc` 前缀，用于操作 MySQL/Oracle。内容有 init/connect、getLastError、query、createPreparedStatement 和 psqlSetString/Long/Int/Float/Boolean/Date/Timestamp 参数设置、psqlQuery/psqlAddBatch/psqlExecuteUpdate、psqlClose/connectionClose。标签：代码示例
- [网络验证函数](funcs/netcard-api.md)：`ecNetCard` 前缀，接入官方用户中心的卡密验证（自带心跳和脚本安全校验）。内容有 netCardInit/netCardBind/netCardUnbind 和云端变量 netCardGetCloudVar/netCardUpdateCloudVar；中控 6.12.0+ 已内置，低版本需下载 SDK 包。标签：代码示例 / 版本要求
- [脚本函数](funcs/index.md)：脚本函数概览入口，只列出 18 个模块页面的链接（不含“-手机内执行”系列、YOLO、输入法、辅助模块、网络验证），并指向指南和高级功能。标签：仅导航
- [开发工具](funcs/devtools/dev-tools.md)：开发工具子页的导航，链接到安装、工程相关、设备相关、屏幕相关、设置相关、远程调试 6 页。标签：仅导航
- [设备相关](funcs/devtools/dev-tools-device.md)：IDEA 插件「设备连接」菜单，介绍 USB/WIFI 连接、扫码安装调试版和设备激活（文字沿用安卓版说法）。配图在站点上已失效。标签：截图
- [开发环境安装](funcs/devtools/dev-tools-install.md)：开发环境的下载与安装，包括 123 网盘/QQ 群下载、推荐 IDEA 2026.2+（附 Classic UI 插件说明）和安装 EasyClick IDEA 插件，另有主界面预览图（已失效）。标签：截图 / 版本要求
- [工程相关](funcs/devtools/dev-tools-project.md)：IDEA 插件中创建、预览、运行和打包工程的菜单操作，主要靠截图说明（图片已失效；“输出 apk”是沿用安卓版的描述）。标签：截图
- [远程调试](funcs/devtools/dev-tools-remote.md)：远程调试步骤：手机安装 EC 调试版，插件开启本地端口（默认 10825），在路由器做端口映射后从手机连接，或改用 FRP 反向代理。标签：截图
- [屏幕相关](funcs/devtools/dev-tools-screen.md)：插件的节点抓取、同屏映射，以及在截图上取色、找图并生成代码的操作说明（截图已失效）。标签：截图
- [设置相关](funcs/devtools/dev-tools-settings.md)：只说明「EasyClick设置」菜单包含与屏幕映射相关的属性，附一张已失效的截图。标签：截图

## Part C 摘要：企业版开放接口、资源与支持、侧栏未列出页面

本部分共 26 页：企业版开放接口的 12 个模块页加 1 个总览页，「资源与支持」5 页（iOS 17+ 说明、常见问题、插件、版本记录、加入我们），以及 8 个侧栏未列出的页面（高级功能索引、AI 智能体手册、投屏教程、协议/声明、技术服务、安装说明等）。
开放接口统一向中控（默认 `http://127.0.0.1:8019`）发 HTTP POST + JSON，返回 `code=0` 表示成功（快捷指令助手例外：部分为 GET，路径不带 `/openapi`）。每个接口都给出请求 Body、Python/Node.js/cURL/C# 四种语言示例、参数表、返回示例和返回结构表。
控机链路有三条：代理自动化（截图、动作、IME、相册、文件）、蓝牙 BLE 开发板 HID，以及免硬件的 USB HID（10.7.0+，与代理自动化互斥，只能二选一）。另有开发者镜像、中控、辅助助手等管理类接口。
版本记录覆盖 1.0.0 至 11.7.0（2026-11-17），其余页面以图文教程、FAQ 和导航为主。

### 页面
1. [截图接口](advance/openapi/screenshot.md)：10 个截图接口，包括 captureFullScreenNoAutoByte（无需开启自动化抓 PNG）、startPreCapScreen 预抓图、resetCaptureScreenNoAutoEnv 重置无权限截图环境、captureFullScreenJpg/ByteEx/Png 抓 JPG/扩展/PNG，以及图片流的 start/stop/captureScreenStreamByte/isScreenStreamOk。中控 3.0.0+ 起由 GET 改为 POST。标签：代码示例 / 表格 / 注意事项 / 版本要求
2. [动作接口](advance/openapi/action.md)：26 个设备动作接口，分为五组：小白点开关 setAssistiveTouch；输入 typingText/inputText/ioHIDEvent；触控 clickPoint/doubleClickPoint/longClickPoint/swipeToPoint/press/multiTouch；应用管理 appLaunch/openApp/installApp/uninstallApp/stopApp/appKillByBundleId；系统操作 home/homeScreen/reboot/isLocked/lockScreen/unlockScreen。另有 set/getOrientation、adjustScreenOrientation 和 dumpXml 抓节点（3.0.0+）。标签：代码示例 / 表格 / 注意事项 / 版本要求
3. [启停自动化接口](advance/openapi/auto-env.md)：startAutoEnv 启动、closeEnv 释放自动化，resetUsbConn 重置 USB 链接，getDeviceInfo/getBattery/isCharging 查设备信息与电量，isServiceOk 检测服务，setAgentSetting 设置代理 IPA。startTunnel/closeTunnel 管理 iOS 17+ 隧道（9.22.0+）。标签：代码示例 / 表格 / 注意事项 / 版本要求
4. [相册接口](advance/openapi/album.md)：uploadInsertVideo/uploadInsertImage 上传视频、图片到相册（中控 6.5.0+、iOS 15+），requestPhotoAuthorization 请求相册权限，deleteAllPhotos/deleteAllVideos 清空相册中的图片或视频。标签：代码示例 / 表格 / 注意事项 / 版本要求
5. [文件操作接口](advance/openapi/files.md)：fsyncFileOpr 对设备文件执行 list/rm/mkdir，fsyncPullPush 在电脑与设备之间推送（push）或拉取（pull）文件，两者都要求中控 7.2.0+。标签：代码示例 / 表格 / 注意事项 / 版本要求
6. [IME 输入法接口](advance/openapi/ime.md)：15 个自定义输入法接口（6.37.0+）。imeOk 检测就绪；imeInput/imePaste/imePressDel/imePressEnter 负责输入与编辑；imeGetText/imeCopyToClipboard/imeGetClipboard/imeSetClipboard 处理文本和剪切板；另有 imeOpenUrl、imeRemoveAllContent、imeChangeKeyboard、imeDismiss。forwardImeServer/closeForwardImeServer 开关 IME 服务转发，不需要代理自动化（9.20.0+）。标签：代码示例 / 表格 / 注意事项 / 版本要求
7. [蓝牙 BLE 接口](advance/openapi/ble.md)：22 个蓝牙开发板 HID 接口（9.20.0+）。参数类：bleSetScale/bleSetScreenSize/bleSetStep；开发板通信：bleOpenSerial/bleCloseSerial/bleSetSendCmdType/bleSetWifiInfo/bleResetBle；触控与鼠标：click/doubleClick/press/swipe、TouchDown/Move/Up、MultiTouch、MouseMove/ResetZero；按键：bleSystemKey/bleKeyPress/bleKeyPressChar/bleToggleSoftKeyboard。页内提示：不想用硬件可改用 USB HID。标签：代码示例 / 表格 / 注意事项 / 版本要求
8. [USB HID 接口](advance/openapi/usbhid.md)：不需要蓝牙或 OTG 板，经数据线控制 iPhone 的 16 个接口（10.7.0+，建议 iOS 17+）。包括会话 usbhidSessionStart/Stop/Restart、usbhidSetScreenSize、点击/双击/长按/滑动/MultiTouch/MultiTouch2、InputText/TypeText/SystemKey/KeyPressChar 和 Set/GetClipboard，附与脚本 usbHidEvent 的对照表。页面警告它与代理自动化互斥，且 sessionStart 可能重挂开发者镜像。标签：代码示例 / 表格 / 注意事项 / 版本要求
9. [开发者镜像接口](advance/openapi/dev-image.md)：mountDevImage、unmountDevImage、checkDevImage 分别加载、卸载、查询开发者镜像（DDI），无需先启动自动化（10.8.0+）。页面说明了手机重启或 USB HID 重挂镜像后应先卸载再加载。标签：代码示例 / 表格 / 注意事项 / 版本要求
10. [中控接口](advance/openapi/center.md)：12 个中控级接口：reconnectUsb 闪断 USB（Windows）、deviceList 设备列表、脚本控制 startScript/stopScript/pauseScript/continueScript/isScriptPause、refreshMsg、setDeviceAlias，以及 erasePhone 恢复出厂、enableDeveloperMode 开启开发者模式、getOrientationNoAuto。各接口分别要求 3.0.0+、6.47.0+ 或 8.15.0+。标签：代码示例 / 表格 / 注意事项 / 版本要求
11. [辅助助手接口](advance/openapi/app-helper.md)：`/openapi/appHelper/*` 下的 8 个接口：setParam 设置助手参数，setPasteboard/getPasteboard 读写剪切板，openUrl，uploadToAlbum 上传到相册，startForwardPort/closeForwardPort/closeAllForwardPort 管理端口转发（6.0.0+/6.1.0+）。标签：代码示例 / 表格 / 注意事项 / 版本要求
12. [快捷指令助手接口](advance/openapi/shortcuts.md)：需要先运行「快捷指令助手」程序（在官方网盘）。接口为 POST /postText 上传文字、GET /cmd 让手机获取命令、POST /upload 上传文件、GET /suc 反馈结果、GET /ult 获取手机传回的指令结果。标签：代码示例 / 表格 / 注意事项
13. [iOS 17+使用说明](otherinc/ios17.md)：iOS 17+ 要通过虚拟网卡连接，并使用 7.1.0+ 代理 IPA。步骤为安装 USBNCM 驱动、以管理员身份运行中控和桥接、关闭 UAC 通知、启动自动化（建立 tun 隧道），并附常见问题。iOS 17.4+ 配合 7.5.0+ 中控可以不装驱动。标签：截图 / 注意事项 / 版本要求
14. [常见问题](question-answer.md)：涵盖桥接内存/CPU 抓取与回收地址、设备数量查看、重启后开发者镜像、越狱机装 IPA、com.js.main 异常（路径不要含中文）、屏幕宽高不准、3.2.0+ 横竖屏坐标系、opencv 复制失败（缺 VC 库）、JSON.stringify 栈溢出、java-js 闪退、精简 9.0+ 代理 IPA。标签：截图 / 注意事项 / 版本要求
15. [全部插件（插件生态圈）](allplugin.md)：列出三个第三方插件（老冷增强插件、全分辨率找图插件、EC 免字库毫秒级文字识别）及其文档/教程链接和作者联系方式。标签：仅导航
16. [版本更新记录](changelog.md)：从 1.0.0 到 11.7.0（2026-11-17），6.5.0 起带发布日期（最早 2023-05），开头附免签 IPA 下载。主要主题：iOS 17/18/26/27 的镜像与隧道适配；图色、OCR、YOLO、opencv、节点/xpath；投屏及蓝牙/OTG/USB HID 控制；输入法与辅助功能；10.1 前后的 AI 智能体及 10.6 迁移到 iEasyRun；v2 加密、IDEA 插件与 MCP 服务；中控 UI 改版与一键配置。不少条目注明需同步更新 IPA、中控或插件。标签：注意事项 / 版本要求
17. [加入我们](joinus.md)：官方微信二维码、沙书记和客服的 QQ，以及 10 个技术交流 QQ 群号。标签：截图
18. [高级功能](advance/index.md)：iOS USB 进阶能力的索引页，按 AI 与识别、硬件输入（蓝牙 BLE/OTG/USB 键盘）、中控与工程（脱机激活器、中控配置、Web 工具、Xcode 远程、部署、远程调试）、工程与发布（npm/TS、JS 混淆、自定义插件、Java-JS 互调）和相关入门分组列出链接。标签：仅导航
19. [AI 智能体使用手册](advance/ai-agent/index.md)：手册总览。10.1.0–10.5.x 的新中控内置 AI 智能体（AI 对话、可视化工作流、图色找图、OCR/VLM、外部工具），10.6.0+ 已独立为 iEasyRun。页内有授权、大模型、Token 的速查问答，一张界面截图，推荐阅读顺序表，以及与「AI 辅助编程」的对比表。标签：表格 / 截图 / 注意事项 / 版本要求
20. [iOS USB投屏教程](advance/ios-usb-screen.md)：基于 10.2.0 的投屏教程。先讲下载软件和购买授权，然后是两种模式：代理模式（配置开发者镜像、签名 IPA、选择新版或经典投屏界面）；蓝牙 HID 投屏（刷固件、蓝牙配置，用快捷键配合快捷指令实现文字输入、上传图片/视频、读写剪贴板、打开 URL）。最后是 OCR、其他操作和常见问题（闪退、白屏、带机量、蓝牙点击偏移等）。标签：表格 / 截图 / 注意事项 / 版本要求
21. [企业版开放接口](advance/openapiintro.md)：开放接口总览，说明如何用 HTTP + JSON 对接中控。给出 Python/Node.js/cURL/C# 完整示例（查设备列表，再用 USB HID 开会话、设分辨率、点击），模块目录表链接到 12 个模块页，并附历史 Apifox 备份链接。标签：代码示例 / 表格 / 注意事项 / 版本要求
22. [用户协议](agreement.md)：易点软件用户协议，条款包括使用规则、帐号管理、知识产权、免责事由、服务变更/中断/终止、违约责任、法律适用与争议解决、隐私政策等。标签：无
23. [免责声明](declare.md)：易点软件平台免责声明、侵权处理流程（权利通知、反通知需提交的材料和步骤）和知识产权归属。标签：无
24. [技术服务](device_solution.md)：沙书记提供的增值服务：手机采购（真机/云机、保回收）、机房搭建、脚本承接和整体工作室方案，附合作客户案例图。标签：截图
25. [其他文档说明](otherinc/index.md)：只有一个链接，指向「iOS17以上使用说明文档」。标签：仅导航
26. [安装开发工具](tools/installreadme.md)：先用开发和部署链路示意（IDEA → 中控 → 代理 IPA → 手机），再解释名词：代理 IPA 需要签名、中控程序、IDEA 开发插件（推荐 IDEA 2026.2+，可装 Classic UI 插件恢复旧界面）。最后给出三步安装链接：代理 IPA、中控、开发插件。标签：版本要求
