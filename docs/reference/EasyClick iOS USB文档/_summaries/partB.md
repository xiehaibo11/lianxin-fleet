# Part B：脚本函数 API 参考与未列入侧栏的 funcs 页面

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
