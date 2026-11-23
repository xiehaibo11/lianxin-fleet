<!-- 来源：https://ieasyclick.com/iosdocs/tools/installreadme/ -->

# 安装开发工具

- **开发可以在Windows/MacOS系统, 运行推荐在Windows[win10以上]系统**

> ---
>
> 开发: IDEA编写代码—>发送—>电脑中控执行逻辑代码—>发送—>手机代理程序IPA—>控制手机执行动作  
> 部署: 编译好的iec源码包—>电脑中控执行逻辑代码—>发送—>手机代理程序IPA—>控制手机执行动作

## 名词解析

```json
代理程序IPA:
  - 用于安装到iphone/ipad上的程序,打通电脑与手机的通讯,控制手机
  - 由于苹果限制,未上架苹果商店的三方ipa安装包,需要签名方可安装使用
    因此需要个人开发者或巨魔等进行签名[推荐],也可通过我们提供的源码,用macOS的xcode进行源码编译

中控程序:
  - 中控程序支持Windows[win10以上]，MacOS等系统
  - 用于执行脚本、管理设备、配置UI参数等
  - 也可投屏群控手机

开发插件:
  - 构建于IDEA基础上的开发插件
  - 开发插件支持windows，macos等系统
  - 推荐使用官方正版 IDEA 2026.2 及以上：请到 JetBrains 官网下载安装，再安装 EasyClick 开发插件即可。旧版本后续将不再支持，请统一使用 2026.2+
  - 如果不适应新的 IDEA UI：打开 IDEA **设置 → 插件**，搜索 **Classic UI** 并安装，**重启 IDEA** 后即可恢复经典界面
```

## 安装步骤

- [1、安装代理ipa](signagent.md)
- [2、安装中控程序](installcenter.md)
- [3、安装开发插件](installdevtools.md)

