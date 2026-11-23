# 第 3 部分摘要（07、09、10、12、14、15）

## 07-EasyClick Android相关教程

| 文章 | 摘要 | 与 iOS 群控相关 |
|---|---|---|
| [ADB shell 之 adb shell之am、pm、dpm命令大全](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/01-ADB%20shell%20%E4%B9%8B%20adb%20shell%E4%B9%8Bam%E3%80%81pm%E3%80%81dpm%E5%91%BD%E4%BB%A4%E5%A4%A7%E5%85%A8.md) | 整理 am/pm/dpm 命令（start、force-stop、broadcast、pm install/list/grant 等）的参数表。 | 低 |
| [EasyClick adb、shell命令大全](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/02-EasyClick%20adb%E3%80%81shell%E5%91%BD%E4%BB%A4%E5%A4%A7%E5%85%A8.md) | adb 安装与环境变量，常用 adb/shell 命令汇总；提示 shell 命令需代理模式而非无障碍模式。 | 低 |
| [EasyClick 执行 shell命令](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/03-EasyClick%20%E6%89%A7%E8%A1%8C%20shell%E5%91%BD%E4%BB%A4.md) | EC 中用 shell.sudo / shell.execAgentCommand 执行命令（无需带 adb shell），示例为 chmod 与查看权限，需 ROOT。 | 低 |
| [EasyClick shell 命令压缩文件](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/04-EasyClick%20shell%20%E5%91%BD%E4%BB%A4%E5%8E%8B%E7%BC%A9%E6%96%87%E4%BB%B6.md) | 在 Android 上用 tar 压缩/解压文件（多数机器没有 zip），含检测命令是否存在的方法。 | 无（Android 专用） |
| [EasyClick shell分割大文件](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/05-EasyClick%20shell%E5%88%86%E5%89%B2%E5%A4%A7%E6%96%87%E4%BB%B6.md) | 用 split -b 分割大文件、cat 合并并清理分片，均通过 shell.execAgentCommand 执行。 | 低 |
| [Android Intent action 介绍](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/06-Android%20Intent%20action%20%E4%BB%8B%E7%BB%8D.md) | Android Intent 的 action、data、category、extra 等组成及常用 action 常量清单。 | 无（Android 专用） |
| [Android FLAG标志位解释](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/07-Android%20FLAG%E6%A0%87%E5%BF%97%E4%BD%8D%E8%A7%A3%E9%87%8A.md) | Intent FLAG 标志位逐项解释（URI 权限、NEW_TASK、CLEAR_TOP 等）。 | 无（Android 专用） |
| [EasyClick Intent 打开应用详情页](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/08-EasyClick%20Intent%20%E6%89%93%E5%BC%80%E5%BA%94%E7%94%A8%E8%AF%A6%E6%83%85%E9%A1%B5.md) | 用 Intent 的 APPLICATION_DETAILS_SETTINGS action 打开指定包名的应用详情页。 | 无（Android 专用） |
| [EasyClick intent方式安装卸载APP](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/09-EasyClick%20intent%E6%96%B9%E5%BC%8F%E5%AE%89%E8%A3%85%E5%8D%B8%E8%BD%BDAPP.md) | 通过 Intent 跳转系统安装/卸载页完成 APK 安装卸载；提醒 setFlags 会覆盖、addFlags 为追加，勿混用。 | 无（Android 专用） |
| [Intent教学培训](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/10-Intent%E6%95%99%E5%AD%A6%E5%9F%B9%E8%AE%AD.md) | Intent 入门课：显式/隐式 Intent、打开 Activity、启动 Service、广播与传参。 | 无（Android 专用） |
| [Activity](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/11-Activity.md) | Activity 与 Intent 课堂笔记，含显式 Intent 跳转与 putExtra 传值示例。 | 无（Android 专用） |
| [Android](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/12-Android.md) | Android 开发环境搭建笔记（Android Studio 4.x、JDK1.8、Gradle 6、SDK/adb）。 | 无（Android 专用） |
| [安卓 版本API 对照表](../07-EasyClick%20Android%E7%9B%B8%E5%85%B3%E6%95%99%E7%A8%8B/13-%E5%AE%89%E5%8D%93%20%E7%89%88%E6%9C%ACAPI%20%E5%AF%B9%E7%85%A7%E8%A1%A8.md) | Android 版本号与 API 级别对照表。 | 无（Android 专用） |

## 09-EasyClick Android常用文档

| 文章 | 摘要 | 与 iOS 群控相关 |
|---|---|---|
| [EasyClick Android常用文档](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3.md) | 栏目首页，无正文 | 无 |
| [EasyClick 常见错误](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/01-EasyClick%20%E5%B8%B8%E8%A7%81%E9%94%99%E8%AF%AF.md) | 常见错误：dex 打包时 JS 文件名不能用中文；运行提示选择项目、脚本突然结束时查看 sdcard 日志目录。 | 低 |
| [常见网络返回代码解释](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/02-%E5%B8%B8%E8%A7%81%E7%BD%91%E7%BB%9C%E8%BF%94%E5%9B%9E%E4%BB%A3%E7%A0%81%E8%A7%A3%E9%87%8A.md) | HTTP 状态码（1xx-5xx）含义速查。 | 低 |
| [模拟器连接端口汇总](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/03-%E6%A8%A1%E6%8B%9F%E5%99%A8%E8%BF%9E%E6%8E%A5%E7%AB%AF%E5%8F%A3%E6%B1%87%E6%80%BB.md) | 各安卓模拟器的 adb connect 端口汇总（MuMu 7555、夜神 62001 等）。 | 无（Android 专用） |
| [EasyClick官网文档](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/04-EasyClick%E5%AE%98%E7%BD%91%E6%96%87%E6%A1%A3.md) | 指向 EasyClick 官网文档及打开本地文档的方法。 | 低 |
| [EC无法停止的问题](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/05-EC%E6%97%A0%E6%B3%95%E5%81%9C%E6%AD%A2%E7%9A%84%E9%97%AE%E9%A2%98.md) | EC 无法停止的排查：检查设置项、死循环、adb 冲突或数据线/接口不良。 | 低 |
| [模拟器自动获取root权限](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/06-%E6%A8%A1%E6%8B%9F%E5%99%A8%E8%87%AA%E5%8A%A8%E8%8E%B7%E5%8F%96root%E6%9D%83%E9%99%90.md) | 模拟器自动获取 root、去除重复授权提示的设置截图说明。 | 无（Android 专用） |
| [夜神截图黑图解决办法](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/07-%E5%A4%9C%E7%A5%9E%E6%88%AA%E5%9B%BE%E9%BB%91%E5%9B%BE%E8%A7%A3%E5%86%B3%E5%8A%9E%E6%B3%95.md) | 夜神模拟器截图黑图：先 startEnv/isServiceOk，再 image.requestScreenCapture 并重试。 | 无（Android 专用） |
| [去除Windows文件^M的办法](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/08-%E5%8E%BB%E9%99%A4Windows%E6%96%87%E4%BB%B6%5EM%E7%9A%84%E5%8A%9E%E6%B3%95.md) | 用 sed 去除 Windows 文件的 ^M 行尾字符。 | 低 |
| [EC设置为默认输入法](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/09-EC%E8%AE%BE%E7%BD%AE%E4%B8%BA%E9%BB%98%E8%AE%A4%E8%BE%93%E5%85%A5%E6%B3%95.md) | 把 EC 设为默认输入法的操作截图。 | 无（Android 专用） |
| [EasyClick 启动APP 替代方法](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/10-EasyClick%20%E5%90%AF%E5%8A%A8APP%20%E6%9B%BF%E4%BB%A3%E6%96%B9%E6%B3%95.md) | 用 PackageManager.getLaunchIntentForPackage 启动 App 的替代函数。 | 无（Android 专用） |
| [EasyClick 插件项目问题](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/11-EasyClick%20%E6%8F%92%E4%BB%B6%E9%A1%B9%E7%9B%AE%E9%97%AE%E9%A2%98.md) | IDEA 新建 EC 单项目结构异常的两种解决：修改模块源码目录，或改建多项目。 | 中 |
| [EasyClick 热更乱码问题](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/12-EasyClick%20%E7%83%AD%E6%9B%B4%E4%B9%B1%E7%A0%81%E9%97%AE%E9%A2%98.md) | 热更新乱码问题的编码设置修正。 | 中 |
| [EasyClick IEC 热更新 问题](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/13-EasyClick%20IEC%20%E7%83%AD%E6%9B%B4%E6%96%B0%20%E9%97%AE%E9%A2%98.md) | IEC 热更新排错：update.json 字段（download_url、version、dialog、msg、force、md5）须为合法 JSON，字符串用双引号。 | 中 |
| [EC 编译错误](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/14-EC%20%E7%BC%96%E8%AF%91%E9%94%99%E8%AF%AF.md) | EC 编译错误：仅支持 ES5 及部分 ES6，不支持函数参数默认值等写法，ui.js 同样受限。 | 中 |
| [打包选项和APP 设置属性对比](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/15-%E6%89%93%E5%8C%85%E9%80%89%E9%A1%B9%E5%92%8CAPP%20%E8%AE%BE%E7%BD%AE%E5%B1%9E%E6%80%A7%E5%AF%B9%E6%AF%94.md) | 打包选项与 APP 设置属性对比（仅截图）。 | 低 |
| [常用时间戳API](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/16-%E5%B8%B8%E7%94%A8%E6%97%B6%E9%97%B4%E6%88%B3API.md) | 用 http.httpGet 从北京/淘宝/苏宁等接口取 13 位网络时间戳。 | 中 |
| [雷电模拟器开启指针](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/17-%E9%9B%B7%E7%94%B5%E6%A8%A1%E6%8B%9F%E5%99%A8%E5%BC%80%E5%90%AF%E6%8C%87%E9%92%88.md) | 雷电模拟器开启指针位置：在 vms 目录建 debug 文本文件后再开开发者选项。 | 无（Android 专用） |
| [无障碍音量下键停止脚本](../09-EasyClick%20Android%E5%B8%B8%E7%94%A8%E6%96%87%E6%A1%A3/18-%E6%97%A0%E9%9A%9C%E7%A2%8D%E9%9F%B3%E9%87%8F%E4%B8%8B%E9%94%AE%E5%81%9C%E6%AD%A2%E8%84%9A%E6%9C%AC.md) | 用 observeEvent("key-up") 监听音量下键(keyCode 25)并 exit() 停止脚本，setStopCallback 里取消监听。 | 无（Android 专用） |

## 10-EasyClick IOS 常见问题

| 文章 | 摘要 | 与 iOS 群控相关 |
|---|---|---|
| [EasyClick IOS 常见问题](../10-EasyClick%20IOS%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98.md) | 栏目首页，无正文 | 低 |
| [EasyClick IOS 2.X  环境安装 教程](../10-EasyClick%20IOS%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98/01-EasyClick%20IOS%202.X%20%20%E7%8E%AF%E5%A2%83%E5%AE%89%E8%A3%85%20%E6%95%99%E7%A8%8B.md) | iOS 2.X 环境安装：中控 + 桥接(ios-bridge)分布式架构，Windows 用 PowerShell 运行 ios-bridge.exe 与 win-start-ios-center.bat；路径禁含中文空格；需装 iTunes/爱思助手、VC 运行库；Xcode 加开发者账号，自改包名要在中控同步；USB3.0 最多 32 设备，集线器不稳会掉线；实测 64 台以上、最高 100 台。 | 高 |
| [EasyClick 黑苹果安装教程、资源、坑点处理](../10-EasyClick%20IOS%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98/02-EasyClick%20%E9%BB%91%E8%8B%B9%E6%9E%9C%E5%AE%89%E8%A3%85%E6%95%99%E7%A8%8B%E3%80%81%E8%B5%84%E6%BA%90%E3%80%81%E5%9D%91%E7%82%B9%E5%A4%84%E7%90%86.md) | 黑苹果/Mac 环境：支持 iOS 10-15、Xcode 13.1+、macOS 12；VMware 装 macOS 的 smc.version、USB 键鼠 vmx 修复；Xcode 编译 ipa 代理源码并改 bundleId；免费 Apple ID 只能装 3 台、7 天过期，个人开发者 100 台、一年；手机需信任开发者。 | 高 |
| [MAC OS Monterey 支持机型列表](../10-EasyClick%20IOS%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98/03-MAC%20OS%20Monterey%20%E6%94%AF%E6%8C%81%E6%9C%BA%E5%9E%8B%E5%88%97%E8%A1%A8.md) | macOS Monterey 支持的 Mac 机型清单及升级所需磁盘空间。 | 中 |
| [工作室环境问题](../10-EasyClick%20IOS%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98/04-%E5%B7%A5%E4%BD%9C%E5%AE%A4%E7%8E%AF%E5%A2%83%E9%97%AE%E9%A2%98.md) | 工作室环境：用 PowerShell 命令行而非双击启动中控/桥接；中控/桥接闪退排查；分批(约 6 台)启动脚本防崩溃；换工业级 USB HUB，旧主板(X79/X99/B85)支撑 30+ 设备，禁用 USB3.0，必要时改用 Ubuntu。 | 高 |
| [XCODE签名问题](../10-EasyClick%20IOS%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98/05-XCODE%E7%AD%BE%E5%90%8D%E9%97%AE%E9%A2%98.md) | Xcode 签名：需 Xcode 13.1+，iOS 最低 13；绕 ID 且有基带的机器可签；官方未提供脱离 Xcode 的签名方式。 | 高 |
| [Windows10 系统盘 节省7G以上](../10-EasyClick%20IOS%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98/06-Windows10%20%E7%B3%BB%E7%BB%9F%E7%9B%98%20%E8%8A%82%E7%9C%817G%E4%BB%A5%E4%B8%8A.md) | 用 powercfg -h off 关闭休眠，释放 C 盘 7GB 以上。 | 低 |
| [VMware25H2汉化](../10-EasyClick%20IOS%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98/07-VMware25H2%E6%B1%89%E5%8C%96.md) | VMware 25H2 汉化：复制 zh_CN 语言包、preferences.ini 加 pref.locale 或快捷方式加 --locale 参数。 | 低 |

## 12-IDEA 教程

| 文章 | 摘要 | 与 iOS 群控相关 |
|---|---|---|
| [EasyClick IDEA 教程](../12-IDEA%20%E6%95%99%E7%A8%8B/01-EasyClick%20IDEA%20%E6%95%99%E7%A8%8B.md) | EC 开发所用 IDEA 的安装、工具包、插件和项目配置的总体教程。 | 中 |
| [EasyClick IDEA 汉化](../12-IDEA%20%E6%95%99%E7%A8%8B/02-EasyClick%20IDEA%20%E6%B1%89%E5%8C%96.md) | IDEA 汉化：新版在 Language and Region 设置中文，或安装对应版本汉化插件。 | 低 |
| [IDEA 常用快捷键](../12-IDEA%20%E6%95%99%E7%A8%8B/03-IDEA%20%E5%B8%B8%E7%94%A8%E5%BF%AB%E6%8D%B7%E9%94%AE.md) | IDEA 常用快捷键表（Ctrl 系列等）。 | 低 |
| [IDEA 常用开发插件](../12-IDEA%20%E6%95%99%E7%A8%8B/04-IDEA%20%E5%B8%B8%E7%94%A8%E5%BC%80%E5%8F%91%E6%8F%92%E4%BB%B6.md) | IDEA 常用开发插件，含国内 AI 编码助手。 | 低 |
| [IDEA 创建多项目](../12-IDEA%20%E6%95%99%E7%A8%8B/05-IDEA%20%E5%88%9B%E5%BB%BA%E5%A4%9A%E9%A1%B9%E7%9B%AE.md) | 新版 EC 已弃用单项目，须按教程建文件夹后用多项目模式创建。 | 中 |
| [IDEA 恢复历史记录](../12-IDEA%20%E6%95%99%E7%A8%8B/06-IDEA%20%E6%81%A2%E5%A4%8D%E5%8E%86%E5%8F%B2%E8%AE%B0%E5%BD%95.md) | 用 IDEA 本地历史记录恢复被误改误删的代码。 | 低 |
| [IDEA 常见问题](../12-IDEA%20%E6%95%99%E7%A8%8B/07-IDEA%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98.md) | IDEA 常见问题：激活后无法打开等排查。 | 低 |

## 14-付费产品

| 文章 | 摘要 | 与 iOS 群控相关 |
|---|---|---|
| [EasyClick   Android 自动化测试 ROM定制](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/01-EasyClick%20%20%20Android%20%E8%87%AA%E5%8A%A8%E5%8C%96%E6%B5%8B%E8%AF%95%20ROM%E5%AE%9A%E5%88%B6.md) | 安卓自动化测试用定制 ROM 服务介绍（一机型一包、需解锁 BL）。 | 无（Android 专用、付费产品介绍） |
| [EasyClick bug反馈工具 OSS版](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/02-EasyClick%20bug%E5%8F%8D%E9%A6%88%E5%B7%A5%E5%85%B7%20OSS%E7%89%88.md) | EC bug 反馈工具 OSS 版介绍与演示。 | 低（付费产品介绍） |
| [卡密管理系统](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/03-%E5%8D%A1%E5%AF%86%E7%AE%A1%E7%90%86%E7%B3%BB%E7%BB%9F.md) | 卡密管理系统功能介绍：代理、一机一卡、EC 热更新与 apk 更新接口、UI 更新。 | 低（付费产品介绍） |
| [卡密二次验证系统](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/04-%E5%8D%A1%E5%AF%86%E4%BA%8C%E6%AC%A1%E9%AA%8C%E8%AF%81%E7%B3%BB%E7%BB%9F.md) | 卡密二次验证系统功能介绍。 | 低（付费产品介绍） |
| [EasyClick BUG 反馈 公共版](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/05-EasyClick%20BUG%20%E5%8F%8D%E9%A6%88%20%E5%85%AC%E5%85%B1%E7%89%88.md) | EC BUG 反馈公共版：用户自助抓节点并提交崩溃信息，提供接口。 | 低（付费产品介绍） |
| [云控定制](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/06-%E4%BA%91%E6%8E%A7%E5%AE%9A%E5%88%B6.md) | 云控定制介绍：用户登录、任务管理、设备分组与状态、数据管理、手机端参数配置。 | 中（付费产品介绍） |
| [积分管理系统](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/07-%E7%A7%AF%E5%88%86%E7%AE%A1%E7%90%86%E7%B3%BB%E7%BB%9F.md) | 积分管理系统：用户、扣点充值、余额查询签名接口。 | 低（付费产品介绍） |
| [EasyClick 热更新版本管理系统](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/08-EasyClick%20%E7%83%AD%E6%9B%B4%E6%96%B0%E7%89%88%E6%9C%AC%E7%AE%A1%E7%90%86%E7%B3%BB%E7%BB%9F.md) | 热更新版本管理系统：自建服务器上传 iec 并自动算 md5，V2.0 起同时支持 iOS/Android。 | 中（付费产品介绍） |
| [文件上传下载删除查询接口](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/09-%E6%96%87%E4%BB%B6%E4%B8%8A%E4%BC%A0%E4%B8%8B%E8%BD%BD%E5%88%A0%E9%99%A4%E6%9F%A5%E8%AF%A2%E6%8E%A5%E5%8F%A3.md) | 文件上传/下载/删除/查询接口说明（含 multipart 多文件上传），代码在作者开源仓库。 | 低 |
| [项目管理系统](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/10-%E9%A1%B9%E7%9B%AE%E7%AE%A1%E7%90%86%E7%B3%BB%E7%BB%9F.md) | 项目管理系统概述：硬件绑定等安全机制（产品介绍）。 | 低（付费产品介绍） |
| [用户业务端认证系统](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/11-%E7%94%A8%E6%88%B7%E4%B8%9A%E5%8A%A1%E7%AB%AF%E8%AE%A4%E8%AF%81%E7%B3%BB%E7%BB%9F.md) | 用户业务端认证授权系统介绍：前后端分离，覆盖用户/项目/业务端流程。 | 低（付费产品介绍） |
| [通用云控系统](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/12-%E9%80%9A%E7%94%A8%E4%BA%91%E6%8E%A7%E7%B3%BB%E7%BB%9F.md) | 通用云控系统介绍：设备管控、任务调度、脚本与资源共享。 | 中（付费产品介绍） |
| [EC 项目管理系统开源免费](../14-%E4%BB%98%E8%B4%B9%E4%BA%A7%E5%93%81/13-EC%20%E9%A1%B9%E7%9B%AE%E7%AE%A1%E7%90%86%E7%B3%BB%E7%BB%9F%E5%BC%80%E6%BA%90%E5%85%8D%E8%B4%B9.md) | 开源免费的 EC 项目管理系统（Rust + Vue3）：项目、热更新、Bug 跟踪、资源管理。 | 低 |

## 15-设备购买

| 文章 | 摘要 | 与 iOS 群控相关 |
|---|---|---|
| [二手手机资源](../15-%E8%AE%BE%E5%A4%87%E8%B4%AD%E4%B9%B0/01-%E4%BA%8C%E6%89%8B%E6%89%8B%E6%9C%BA%E8%B5%84%E6%BA%90.md) | 二手 iPhone/Android 手机货源说明（购买类页面，不转录价格与联系方式）。 | 无（广告） |
| [Android 一手新机货源表](../15-%E8%AE%BE%E5%A4%87%E8%B4%AD%E4%B9%B0/02-Android%20%E4%B8%80%E6%89%8B%E6%96%B0%E6%9C%BA%E8%B4%A7%E6%BA%90%E8%A1%A8.md) | Android 新机货源表（广告）。 | 无（广告） |
| [IOS推荐集线器西普莱厂家直供集线器](../15-%E8%AE%BE%E5%A4%87%E8%B4%AD%E4%B9%B0/03-IOS%E6%8E%A8%E8%8D%90%E9%9B%86%E7%BA%BF%E5%99%A8%E8%A5%BF%E6%99%AE%E8%8E%B1%E5%8E%82%E5%AE%B6%E7%9B%B4%E4%BE%9B%E9%9B%86%E7%BA%BF%E5%99%A8.md) | iOS 推荐的 USB2.0 独立供电集线器厂家介绍（与 10 中推荐一致，广告性质）。 | 低（广告） |
| [ec 适配ESP 32 板子](../15-%E8%AE%BE%E5%A4%87%E8%B4%AD%E4%B9%B0/04-ec%20%E9%80%82%E9%85%8DESP%2032%20%E6%9D%BF%E5%AD%90.md) | EC 适配 ESP32 开发板（C3/S3/S2mini）选型与刷机的商品介绍。 | 无（广告） |

## 对联信 Fleet 有参考价值的要点

- 架构：EC iOS 为“中控 + 桥接(ios-bridge)”分布式，中控与桥接分开部署，实测 64 台以上、最高 100 台（10/01）。
- 启动方式：中控/桥接应在 PowerShell 命令行里启动而非双击，并处理防火墙放行；桥接可改长连接（10/04）。
- 稳定性：不要一次性全部启动脚本，建议约 6 台一批，避免资源占用过大造成掉线崩溃（10/04）。
- USB 层：USB3.0 规范限制约 32 设备，超量掉线；应使用工业级 USB2.0 独立供电 HUB，禁用 USB3.0，旧主板(X79/X99/B85)更能撑 30+ 设备，Windows 不行可换 Ubuntu（10/01、10/04）。
- 路径与依赖：安装路径禁含中文和空格；需 VC 运行库全版本，Windows 需装 iTunes/爱思助手才能识别 iPhone（10/01、10/04）。
- 签名：免费 Apple ID 仅 3 台、证书 7 天、bundleId 10 天内限改 3 次；个人开发者 100 台、一年；需 Xcode 13.1+，iOS 最低 13，自改包名须同步到中控（10/01、10/02、10/05）。
- 手机条件：正常机或“绕 ID 且有基带”的机器可签；首次需在手机“设备管理”里信任开发者（10/02、10/05）。
- 宿主 Mac：iOS 10-15 + Xcode 13.1+ 要求 macOS 12，真机需预留 60GB；虚拟机方案的 vmx 修复（smc.version、USB 键鼠）可供参考（10/02、10/03）。
- 热更新：iec 更新描述 update.json 含 download_url、version、dialog、msg、force、md5，自建服务端 V2 同时支持 iOS/Android，可借鉴到 Fleet 的脚本分发与校验（09/13、14/08）。
- 云控设计参考：设备分组与状态、任务参数按设备独立覆盖、数据与任务分离（14/06、14/12 的功能清单）。
