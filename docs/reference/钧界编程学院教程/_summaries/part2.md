# 第 2 部分摘要（06-EasyClick 通用教程）

| 文章 | 摘要 | 与 iOS 群控相关 |
|---|---|---|
| [JavaScript基础教程](../06-EasyClick%20通用教程/01-JavaScript基础教程.md) | 老鬼的 JS 入门长文（约 150 KB）：词法、变量 var/let/const、数据类型、数组方法、字符串、数值、类型转换、运算符、循环、函数、字符串查找/截取等，基本是通用 JS 语法教材（含 HTML 内嵌 JS 等与 EC 无关内容）。 | 低 |
| [JavaScript调用java函数](../06-EasyClick%20通用教程/02-JavaScript调用java函数.md) | EC 的 Rhino 引擎里访问 Java：`java.lang.reflect.Array.newInstance` 建 byte/对象/Class 数组、`importPackage/importClass`、`JavaAdapter` 实现多接口、打印 Java 类属性与方法。 | 中（iOS 版 JS 是否开放 Java 桥需核实，写法可参考） |
| [JavaScript 调用java函数检测服务器连通性](../06-EasyClick%20通用教程/03-JavaScript%20调用java函数检测服务器连通性.md) | 用 `Runtime.getRuntime().exec("ping -c 3 host")` + `waitFor()` 判断服务器是否可达。 | 低（依赖 Java，iOS 端不一定可用；思路可用于中控侧） |
| [JavaScript中的正则表达式总结](../06-EasyClick%20通用教程/04-JavaScript中的正则表达式总结.md) | 正则定义方式、直接量字符、锚、字符类、量词、分组、先行断言、RegExp/String 方法速查。 | 低 |
| [JavaScript 异常处理（try+catch+finally+throw）](../06-EasyClick%20通用教程/05-JavaScript%20异常处理（try+catch+finally+throw）.md) | 7 种 Error 类型，try/catch/finally 语法与执行顺序，throw 自定义异常，附示例。 | 低（脚本健壮性通用写法） |
| [常用数据类型检测结果表](../06-EasyClick%20通用教程/06-常用数据类型检测结果表.md) | 表格列出常见值的 `typeof` 与 `constructor` 结果。 | 无 |
| [JavaScript 常见问题集锦](../06-EasyClick%20通用教程/07-JavaScript%20常见问题集锦.md) | var/let/const 区别、const 对象属性可改、解构赋值，及 SyntaxError/ReferenceError/RangeError/TypeError/URIError/EvalError 的成因。 | 低 |
| [JavaScript调用java函数获取MP3文件时长](../06-EasyClick%20通用教程/08-JavaScript调用java函数获取MP3文件时长.md) | 通过 `android.media.MediaPlayer` 的 setDataSource/prepare/getDuration 取音频时长（毫秒）。 | 无（Android 专用） |
| [判断类型](../06-EasyClick%20通用教程/09-JavaScript判断变量类型.md) | 判断变量类型四法：`Object.prototype.toString.call`（推荐）、constructor、instanceof、typeof 及其局限。 | 无 |
| [JavaScript 提取数字](../06-EasyClick%20通用教程/10-JavaScript%20提取数字.md) | `str.replace(/[^0-9]/ig,"")` 提取数字，并附大量常用校验正则（数字、邮箱、汉字等）。 | 低（可用于清洗 OCR 结果） |
| [JavaScript 正则Replace方法](../06-EasyClick%20通用教程/11-JavaScript%20正则Replace方法.md) | 正则 g/i/m 标志，()/[]/{} 区别，^$ \d \s \w 含义，replace 的用法与分组引用。 | 低 |
| [JavaScript 运算符使用技巧](../06-EasyClick%20通用教程/12-JavaScript%20运算符使用技巧.md) | 可选链 `?.`、空值合并 `??` 等 ES2020 运算符，简化嵌套属性与默认值判断。 | 低 |
| [JavaScript 字符串 转数组](../06-EasyClick%20通用教程/13-JavaScript%20字符串%20转数组.md) | `Array.from`、扩展运算符、split，并演示按 emoji 正确切分字符串。 | 低（处理含 emoji 的文本输入时有用） |
| [JavaScript取整数、取余数的方法](../06-EasyClick%20通用教程/14-JavaScript取整取余数的方法.md) | parseInt/Math.ceil/floor/round 取整与 `%` 取余。 | 无 |
| [JavaScript 数组随机取值](../06-EasyClick%20通用教程/15-JavaScript%20数组随机取值.md) | `getRandomArrayElements(arr,count)`，洗牌式不重复随机取 N 个元素。 | 低（可做随机行为/话术） |
| [JavaScript随机生成汉字](../06-EasyClick%20通用教程/16-JavaScript%20随机生成汉字.md) | 在 Unicode 19968–40870 内随机取码点转汉字。 | 低（可生成随机输入文本） |
| [JSON 语法](../06-EasyClick%20通用教程/17-JSON%20基础.md) | JSON 语法、取值、`JSON.stringify/parse` 互转、对象增删改。 | 低（脚本与中控交换数据格式） |
| [JavaScript随机生成字符串](../06-EasyClick%20通用教程/18-JavaScript%20随机生成字符串.md) | `randomString(n)` 从字母数字表随机生成 n 位字符串。 | 低 |
| [JavaScript 正则 常用写法](../06-EasyClick%20通用教程/19-JavaScript%20正则%20常用写法.md) | test/exec/match/replace 等的大量实例，含 ^$、\s、字符集、分组、`logd` 输出。 | 低 |
| [EC生成UUID](../06-EasyClick%20通用教程/20-EC%20生成UUID.md) | 官方设备唯一码函数失效时的替代：13 位时间戳转 8 位 Base36 + 24 位随机串组成 32 位 ID，可反解创建时间；建议首次生成后存入文件或 storage，之后只读取。 | 中（可作设备/任务 ID 方案参考） |
| [EC 官方 CLI 与 Trae 智能体介绍](../06-EasyClick%20通用教程/21-EC%20官方%20CLI%20与%20Trae%20智能体介绍.md) | 介绍 EC 官方 CLI（安卓 ec-android-cli、iOS ec-ios-cli）命令 preview/run/stop/build/monitor/device/config，及基于 LLM 的 Trae 智能体用自然语言驱动 CLI；iOS 版要求 9.27.0+ 插件与已启动的 IDEA。 | 高 |

## 对联信 Fleet 有参考价值的要点

- 21 号：ec-ios-cli 是 IDEA 插件的命令行外壳，仅适用于 iOS USB 版与脱机版，依赖运行中的 IDEA 与 9.27.0+ 插件，不是独立的设备控制通道；Fleet 不能指望绕开 IDEA 直接调用它。
- 21 号：preview 仅脱机版可用，USB 版会报“非 iOS 脱机版项目不可预览”；run 前设备须已连接 IDEA，否则报无设备。
- 21 号：CLI 默认路径 `ec_work_config/ios/bin/ec-ios-cli`，同目录有给 AI 读的 SKILL.md；示例 `run -m <模块> -f json -o <日志文件>`、`build -m <模块> -w false`。
- 21 号：`-k/--stop-on` 用 `|||` 连接多个停止关键字，monitor 默认输出 JSON 日志，适合 Fleet 解析脚本进度与结束状态（默认结束词如“脚本已运行结束”“执行UI结束”）。
- 21 号：多工程/多窗口时必须用 `-p` 指定与 IDEA 打开路径一致的工程根目录，模块名需与 IDEA 一致；批量调度时需据此做工程与设备映射。
- 21 号：build 时 obfuscator.json 的 nodeBinPath 为空会自动跳过 JS 混淆，属正常行为，打包流水线不应当作错误。
- 20 号：设备唯一码函数可能失效，可自行生成 32 位 ID（时间戳 Base36 + 随机串）并持久化到文件或 storage，首次生成、之后只读，可借鉴为设备或脚本实例标识。
- 02 号：EC 的 JS 引擎可经 `importPackage/importClass/JavaAdapter` 访问 Java 类；iOS 端是否开放需实测，不能默认和 Android 一致。
- 10/15/16/18 号：随机数组元素、随机汉字、随机字符串、数字提取等小工具可用于 Fleet 脚本的输入文本随机化与 OCR 结果清洗。
- 本部分其余多为纯 JS 语法教材，对 iOS 群控无直接技术价值；08 号（MediaPlayer）为 Android 专用。
