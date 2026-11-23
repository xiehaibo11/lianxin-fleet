# 钧界编程学院教程（本地镜像）

- 来源：https://www.laoguicom.top/doc/893/（MrDoc 文集，共 172 篇）
- 抓取时间：2026-11-22，单线程慢速抓取（站点约 20–30 次请求就会封 IP，请勿并行重抓）
- 内容为网站原文的 Markdown，每篇开头注释里写了原文地址和抓取状态

## 抓取结果

| 项目 | 状态 |
|---|---|
| 正文 | 172 篇全部抓取；其中 3 个栏目首页本身没有正文（见下） |
| 图片 | 站内图片 493 张全部下载到 `_assets/<文章 id>/`（约 65 MB），文章里已改成本地相对路径；站外图片和视频共 25 处保留原链接 |
| 摘要 | `_summaries/part1.md`～`part3.md`，每篇 1–3 句中文摘要和与 iOS 群控的相关度 |

没有正文的 3 页是 `03-原生UI 教程展示/04-番外避坑`、`09-EasyClick Android常用文档`、`10-EasyClick IOS 常见问题`。它们是栏目首页，网站上本身就是空的（已复查原网页），子文章全部抓到了。

`07-EasyClick Android相关教程` 里的 `11-Activity` 有 1 张图、`12-Android` 有 3 张图显示不出来：原文引用的是作者本机的 `images/...` 相对路径，网站上同样是坏图。

`群控运行脚本.md` 不是本站文章，是单独放进来的一篇转载博文（USB HID + 切换控制做 iOS 群控），不在 172 篇之内。

## 栏目

| 栏目 | 篇数 | 内容 | 摘要 |
|---|---|---|---|
| 根目录各栏目首页 | 15 | 产品介绍、工具对比、付费课程宣传、内网穿透工具 | [part1](_summaries/part1.md) |
| 01-钧界编程学院 | 2 | 讲师介绍、AI 编程服务广告 | [part1](_summaries/part1.md) |
| 02-EasyClick 官方 | 1 | 官方资源与账号说明 | [part1](_summaries/part1.md) |
| 03-原生UI 教程展示 | 71 | Android 原生 UI 模板展示，多为截图和视频 | [part1](_summaries/part1.md) |
| 06-EasyClick 通用教程 | 21 | JS 语法与通用工具函数，第 21 篇是 EC 官方 CLI | [part2](_summaries/part2.md) |
| 07-EasyClick Android相关教程 | 13 | Android 专用 | [part3](_summaries/part3.md) |
| 09-EasyClick Android常用文档 | 18 | Android 常用 API、热更新 | [part3](_summaries/part3.md) |
| 10-EasyClick IOS 常见问题 | 7 | iOS 环境安装、黑苹果、工作室环境、Xcode 签名 | [part3](_summaries/part3.md) |
| 12-IDEA 教程 | 7 | 开发工具使用 | [part3](_summaries/part3.md) |
| 14-付费产品 | 13 | 卡密、云控、热更新服务端等产品介绍 | [part3](_summaries/part3.md) |
| 15-设备购买 | 4 | 手机与集线器购买渠道 | [part3](_summaries/part3.md) |

## 与 iOS 群控最相关的文章

- [10-EasyClick IOS 常见问题/01-EasyClick IOS 2.X 环境安装 教程](10-EasyClick%20IOS%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98/01-EasyClick%20IOS%202.X%20%20%E7%8E%AF%E5%A2%83%E5%AE%89%E8%A3%85%20%E6%95%99%E7%A8%8B.md)：中控 + 桥接分布式架构，实测 64–100 台
- [10-EasyClick IOS 常见问题/04-工作室环境问题](10-EasyClick%20IOS%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98/04-%E5%B7%A5%E4%BD%9C%E5%AE%A4%E7%8E%AF%E5%A2%83%E9%97%AE%E9%A2%98.md)：命令行启动、约 6 台一批启动、USB HUB 与主板选择
- [10-EasyClick IOS 常见问题/05-XCODE签名问题](10-EasyClick%20IOS%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98/05-XCODE%E7%AD%BE%E5%90%8D%E9%97%AE%E9%A2%98.md)：签名条件与免费 Apple ID 的限制
- [10-EasyClick IOS 常见问题/02-EasyClick 黑苹果安装教程、资源、坑点处理](10-EasyClick%20IOS%20%E5%B8%B8%E8%A7%81%E9%97%AE%E9%A2%98/02-EasyClick%20%E9%BB%91%E8%8B%B9%E6%9E%9C%E5%AE%89%E8%A3%85%E6%95%99%E7%A8%8B%E3%80%81%E8%B5%84%E6%BA%90%E3%80%81%E5%9D%91%E7%82%B9%E5%A4%84%E7%90%86.md)：macOS 虚拟机与 Xcode 版本要求
- [06-EasyClick 通用教程/21-EC 官方 CLI 与 Trae 智能体介绍](06-EasyClick%20%E9%80%9A%E7%94%A8%E6%95%99%E7%A8%8B/21-EC%20%E5%AE%98%E6%96%B9%20CLI%20%E4%B8%8E%20Trae%20%E6%99%BA%E8%83%BD%E4%BD%93%E4%BB%8B%E7%BB%8D.md)：ec-ios-cli 的命令和参数（依赖运行中的 IDEA 插件）
- [02-EasyClick 官方](02-EasyClick%20%E5%AE%98%E6%96%B9.md)：iOS USB 版与脱机版的区别
- [群控运行脚本](%E7%BE%A4%E6%8E%A7%E8%BF%90%E8%A1%8C%E8%84%9A%E6%9C%AC.md)：USB HID 路线（非本站内容，技术细节未经核实）

各部分摘要末尾的“对联信 Fleet 有参考价值的要点”汇总了可以借鉴的具体做法。

## 其他文件

- `_toc.json`：网站目录（文章 id、标题、层级、本地文件名）
- `_status/<id>.json`：每篇的抓取状态、字数、图片数和下载失败的图片
- `_assets/`：下载的图片

文章里的价格、联系方式和购买链接是网站原文，未核实。
