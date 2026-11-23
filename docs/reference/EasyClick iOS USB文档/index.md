<!-- 来源：https://ieasyclick.com/iosdocs/ -->

# 概览

# EasyClick iOS USB 版概览

EasyClick iOS USB 版面向免越狱真机自动化：可写 EC 脚本，也可在新中控用 **AI 智能体**（中文对话 / 可视化工作流）批量操作 iPhone。支持 iOS 12–27+。跑已保存工作流**不消耗**大模型 Token。

> **提示：新中控 10.2.0+：AI 智能体**
>
> 在新中控点击 **「AI 智能体」**，用中文对话驱动已连 iPhone，或用可视化工作流批量执行。使用前请先读 [环境与授权](advance/ai-agent/prerequisites.md)。
>
> → [AI 智能体手册](advance/ai-agent/index.md) · [快速开始](advance/ai-agent/getting-started.md) · [10.2.0 更新说明](changelog.md)

## 你可以用它做什么

- 免越狱自动化；纯软件或蓝牙 HID 等方案，按场景选择
- **AI 智能体**：不写代码即可对话 / 工作流操作设备
- **EC 脚本**：JavaScript + IDE，打包独立 `.iec` 发布
- 对接企业版开放接口；多机管理见 [苹果群控系统](https://ieasyclick.com/apple_qunkong)

## 两种使用方式

| 方式 | 适合谁 | 从哪里开始 |
| --- | --- | --- |
| **AI 智能体** | 不写代码；运维、测试、批量操作 | [环境与授权](advance/ai-agent/prerequisites.md) → [快速开始](advance/ai-agent/getting-started.md) |
| **EC 脚本开发** | 开发者；复杂业务逻辑 | [创建第一个工程](firstproject.md) → [脚本函数](funcs/index.md) |

> **注意：AI 智能体 ≠ Cursor 写脚本**
>
> - **AI 智能体**：在新中控窗口里直接操作已连设备（对话 + 工作流）。
> - **与 AI 结合辅助编程**：在 IDE 里用 Cursor 等编写 EC 脚本，见 [与 AI 结合辅助编程](advance/ios-usb-ai.md)。

## 文档怎么读

| 章节 | 适合谁 | 去哪 |
| --- | --- | --- |
| [开始使用](tools/download_resources.md) | 第一次接触脚本开发 | 下载 → [安装开发工具](tools/installreadme.md) → [创建第一个工程](firstproject.md) |
| [指南](advance/ai-agent/index.md) | 要完成具体任务 | [AI 智能体](advance/ai-agent/index.md)、[新手安装](advance/ios-usb-beginner-install.md)、[投屏教程](advance/ios-usb-screen.md)、[高级功能](advance/index.md) |
| [参考](funcs/index.md) | 查 API | [脚本函数](funcs/index.md) |
| [企业版](advance/openapiintro.md) | 二次开发中控 | [开放接口](advance/openapiintro.md) |
| [资源与支持](question-answer.md) | 排错与更新 | [常见问题](question-answer.md)、[版本记录](changelog.md) |

## 下一步

- 用 AI 智能体：先读 [环境与授权](advance/ai-agent/prerequisites.md)
- 写 EC 脚本：从 [下载开发工具](tools/download_resources.md) 开始

