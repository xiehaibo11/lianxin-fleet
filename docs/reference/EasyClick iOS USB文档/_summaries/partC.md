# Part C 摘要：企业版开放接口、资源与支持、侧栏未列出页面

本部分共 26 页：企业版开放接口的 12 个模块页加 1 个总览页，「资源与支持」5 页（iOS 17+ 说明、常见问题、插件、版本记录、加入我们），以及 8 个侧栏未列出的页面（高级功能索引、AI 智能体手册、投屏教程、协议/声明、技术服务、安装说明等）。
开放接口统一向中控（默认 `http://127.0.0.1:8019`）发 HTTP POST + JSON，返回 `code=0` 表示成功（快捷指令助手例外：部分为 GET，路径不带 `/openapi`）。每个接口都给出请求 Body、Python/Node.js/cURL/C# 四种语言示例、参数表、返回示例和返回结构表。
控机链路有三条：代理自动化（截图、动作、IME、相册、文件）、蓝牙 BLE 开发板 HID，以及免硬件的 USB HID（10.7.0+，与代理自动化互斥，只能二选一）。另有开发者镜像、中控、辅助助手等管理类接口。
版本记录覆盖 1.0.0 至 11.7.0（2026-11-17），其余页面以图文教程、FAQ 和导航为主。

## 页面

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
