# 开放接口

联信中控内置一个 HTTP 服务，接口路径、请求字段和返回格式与 EasyClick iOS USB 中控的「企业版开放接口」一致。已经按 EasyClick 文档写好的 Python、易语言、C#、Node.js 对接代码，把地址换成联信中控即可调用。EasyClick 文档里的 142 个路径全部登记：51 个已实现，其余 91 个没有对应能力，会明确返回失败（见[不支持的接口](#不支持的接口)），不会假装成功。

Windows 版和 macOS 版行为相同。服务运行在中控进程里，关闭中控即停止。

## 启用

1. 打开「设置 → 开放接口」，勾选「启用开放接口」，端口默认 `8019`（EasyClick 文档的默认端口），点「保存设置」。
2. 底部状态栏出现「接口 :8019」表示已启动；设置页下方显示地址、已处理的请求数和文件中转目录。
3. 端口被占用时状态栏显示「接口 端口占用」。EasyClick 中控本身也默认监听 8019，两者在同一台电脑上运行时请改用其他端口（例如 8020），再把对接代码里的地址改成新端口。

不需要安装任何组件。修改端口或局域网选项后点「保存设置」，服务立即按新设置重启。

## 安全模型

| 项目 | 规则 |
| --- | --- |
| 监听地址 | 默认只监听 `127.0.0.1`，只有本机程序能访问，不需要令牌。 |
| 局域网模式 | 勾选「允许局域网访问」后监听全部网卡（`0.0.0.0`），**所有请求（包括本机发出的）都必须带访问令牌**：请求头 `X-Fleet-Token: <令牌>` 或 `Authorization: Bearer <令牌>`。缺少或错误时返回 HTTP 401。Windows 首次开启时系统防火墙会询问是否允许联信中控接受网络连接。 |
| 访问令牌 | 第一次勾选局域网模式时自动生成：32 字节随机数（URL 安全的 base64，43 个字符）。设置页可以显示、复制和重新生成，重新生成后旧令牌立即失效。Windows 上按当前用户 DPAPI 加密保存在 `settings.json` 的 `openApiTokenProtected`，与 AI 密钥相同；macOS 上单独保存在数据目录的 `openapi-token` 文件（权限 600），与 AI 密钥相同，不写进 `settings.json`。 |
| 传输 | 没有 HTTPS：局域网模式下令牌和数据都是明文传输，只在可信的局域网里开启。 |
| 防浏览器跨站（CSRF） | 带 `Origin` 或 `Sec-Fetch-Site` 请求头的请求一律返回 403（浏览器发出的跨站请求都会带）。只带 `Sec-Fetch-Mode` 不算浏览器请求：Node.js 18+ 自带的 `fetch` 总会发送 `sec-fetch-mode: cors`，照常接受；POST 必须是 `Content-Type: application/json`，否则 415。只有文档里本来就是 GET 的三个快捷指令接口接受 GET。 |
| 防 DNS 重绑定 | `Host` 请求头只接受 `127.0.0.1`、`localhost`、`[::1]`；局域网模式另外接受 IP 地址和本机电脑名。带端口时必须等于监听端口。其他域名返回 403。 |
| 请求限制 | 只支持 HTTP/1.1 和 `Content-Length` 正文（分块传输或 POST 缺少长度返回 411）；请求头最多 16 KB、100 个（超出 431）；正文最多 1 MB（超出 413；已实现的接口只收 JSON 参数，文件经中转目录交换）；所有连接同时在接收的正文合计最多 16 MB（超出 503）；同时最多 32 个连接（超出 503，这样的 503 同时最多 16 个，再多的连接直接关闭）；局域网模式下同一台电脑最多占 8 个连接（本机程序只受 32 的总数限制）；连接后 2 秒内要发来请求行、10 秒内发完请求头、120 秒内发完正文，否则返回 408；单个操作最长 10 分钟。每个连接只处理一个请求（响应带 `Connection: close`），支持 `Expect: 100-continue`。 |
| 安装应用 | `installApp` 的 `path` 必须位于中控的应用批准目录（设置页「目录」）内。先只做字符串判断、不访问文件系统：UNC 路径（`\\电脑\共享`，访问它会让本机去连接外部共享）、设备路径（`\\?\`、`\\.\`）和目录外的路径直接拒绝，存在与否回复相同；之后才解析符号链接，链接逃逸同样拒绝。 |
| 电脑端文件 | `fsyncPullPush` 的电脑端路径只能位于数据目录下的 `transfer` 文件夹（见[文件接口](#文件接口)）：中转目录外的绝对路径、`..` 逃逸、经过符号链接或目录联接的路径、非法文件名一律返回 1001，接口不能读写电脑上的其他文件。拉取时已有同名文件不覆盖。 |
| 手机端文件与电源 | 手机路径只限媒体区（`/var/mobile/Media`），不能含 `.`/`..` 段、反斜杠或控制字符，媒体区根目录和系统文件夹本身不能删除；照片库和音乐库的数据（`/PhotoData`、`/iTunes_Control` 整个子树）不能删除、覆盖或新建。`uninstallApp` 不能卸载系统应用（`com.apple.*`）。卸载镜像、重启前先停止这台手机的脚本和自动化。 |

安全检查在读取正文之前完成：未授权的请求不会让中控接收大正文，也不会得知接口是否存在。

## 返回格式

除截图类接口和 `dumpXml` 外，所有响应都是 UTF-8 JSON 信封，字段依次为 `code`、`msg`、`data`：

```json
{"code":0,"msg":"成功","data":true}
```

截图类接口成功时响应体就是图片本身（`Content-Type` 为 `image/png`、`image/jpeg` 或 `image/tiff`）；`dumpXml` 成功时响应体就是界面树 XML（`application/xml; charset=utf-8`）。它们失败时仍返回 JSON 信封，可以先看 `Content-Type` 再解析。

| code | HTTP 状态 | 含义 |
| --- | --- | --- |
| 0 | 200 | 成功 |
| 1001 | 200 | 参数无效（缺少字段、坐标超出屏幕、时长超出范围、路径不在批准目录或中转目录等） |
| 1002 | 200 | 设备未在中控登记 |
| 1003 | 200 | 设备操作失败（自动化未就绪、WDA 拒绝执行、设备工具报错、读不到电量、数据损坏、超时等），`msg` 为原因。电脑上的文件读写出错时只回「电脑上的文件操作失败」这类通用说明，带本机路径的原始信息只写进中控状态栏 |
| 1004 | 200 | 结果待确认：操作中途超时或中断，可能已在手机上生效，请检查手机（例如推送、删除文件、卸载应用、卸载镜像、重启时连接断开；找不到设备、没有信任、未解锁这类没送达手机的失败是 1003） |
| 1005 | 200 | 脚本不存在 |
| 1006 | 200 | 设备忙（正在运行其他脚本、USB 服务正在重启） |
| 2001 | 200 | 联信 Fleet 暂不支持该接口（没有对应能力），`msg` 说明原因 |
| 2002 | 200 | 保留给「功能开发中」的接口；目前没有接口返回它 |
| 400、401、403、404、405、408、411、413、415、431、500、503 | 与 code 相同 | 请求本身被拒绝，见上面的安全模型；`msg` 为中文原因 |

失败时 `data` 为 `false`。

## 设备、坐标与时长

- **deviceId**：`deviceList` 返回的 `deviceId`（即 UDID），大小写和连字符不敏感；也可以直接传中控表格里的设备编号（数字或数字字符串）。`deviceIds` 是这样的数组，其中任意一台没有登记时整体拒绝，不会只对部分设备执行。
- **坐标**：与 EasyClick 相同，是**截图像素**，即 `captureFullScreenPng` 图片里的像素位置，原点在左上角，范围 `0 … screenWidth/screenHeight`（见 `getDeviceInfo`）。坐标系跟随当前屏幕方向：横屏时宽高互换（`orientation` 为 2），`setOrientation` 返回时界面已经转好，之后的坐标按新方向换算。中控内部把像素除以像素比例得到 WDA 的点坐标再执行：普通机型比例为 2 或 3，开启「放大显示」时为小数；比例由该设备第一次截图测得并缓存（与方向无关），点尺寸变化时重新测量。超出屏幕的坐标返回 1001。`dumpXml` 里节点的 `x`、`y`、`width`、`height` 也已换算成截图像素。
- **时长**：毫秒。长按 200–5000 毫秒（缺省 1000），滑动 50–5000 毫秒（缺省 500）。
- 数字和布尔字段写成字符串也可以（EasyClick 示例里常见 `"quality": "50"`、`"maxDepth": "50"`）。
- 设备动作直接经 WDA 执行，不进入中控的任务记录；同一台手机上的动作由设备桥接串行执行。
- **需不需要自动化**：点按、输入、截图（`captureFullScreenNoAutoByte` 除外）、`getDeviceInfo`、`isLocked`、`setOrientation`、`dumpXml` 需要先 `startAutoEnv`；电量、开发者镜像、卸载应用、重启和文件接口经 USB（或已开启 Wi-Fi 连接）的系统服务完成，不需要自动化。

## 示例代码的公共部分

下面的例子都假设：

```python
import requests

BASE = "http://127.0.0.1:8019"
DEVICE = "00008030-001234567890A001"   # deviceList 返回的 deviceId
HEADERS = {}                             # 局域网模式：{"X-Fleet-Token": "设置页里的令牌"}

def api(path, **body):
    r = requests.post(BASE + path, json=body, headers=HEADERS, timeout=600)
    kind = r.headers.get("Content-Type", "")
    if kind.startswith("image/"):
        return r.content
    if kind.startswith("application/xml"):
        return r.text
    data = r.json()
    if data["code"] != 0:
        raise RuntimeError(f'{data["code"]} {data["msg"]}')
    return data["data"]
```

curl 例子为 bash 写法。Windows 命令提示符里请把单引号换成双引号，并把 JSON 里的双引号写成 `\"`；局域网模式再加 `-H "X-Fleet-Token: <令牌>"`。挂载镜像、开启开发者模式、安装应用、推送大文件可能持续几分钟，客户端超时请设长一些。

## 已实现的接口

### 中控接口

**设备列表** `POST /openapi/deviceList`：`deviceId` 按 UDID 模糊匹配（空为全部），`groupId` 为联信的分组名称（空为全部）。`bridgeUrl`、`serialNo` 联信没有，返回空字符串；另附联信字段 `id`、`productType`、`wdaPort`、`serviceOk`、`usbConnected`、`wifiConnected`。

```bash
curl -s -X POST http://127.0.0.1:8019/openapi/deviceList -H "Content-Type: application/json" -d '{"deviceId":"","groupId":""}'
```
```python
devices = api("/openapi/deviceList", deviceId="", groupId="")["list"]
```
```json
{"code":0,"msg":"成功","data":{"list":[{"bridgeUrl":"","deviceAlias":"phone1","groupId":"未分组","deviceNo":"00008030-001234567890A001","deviceId":"00008030-001234567890A001","deviceName":"iPhone","serialNo":"","groupName":"未分组","productVersion":"17.5","id":1,"productType":"iPhone14,5","wdaPort":8100,"serviceOk":true,"usbConnected":true,"wifiConnected":false}],"total":1,"page":1}}
```

**执行脚本** `POST /openapi/startScript`：`name` 是联信「脚本」里保存的脚本名称（先精确匹配，再忽略大小写匹配），也可以改传联信的 `scriptId`。EasyClick 的 `iecPath` 工程包联信不能运行，会被忽略；请先在中控里按联信脚本语法保存同名脚本。与中控界面一样，只在自动化已就绪的设备上启动（调用时会先探测一次 WDA）；未就绪和正在运行其他脚本的设备会跳过并写在 `msg` 里，全部未就绪时返回 1003，全部都忙时返回 1006。

```bash
curl -s -X POST http://127.0.0.1:8019/openapi/startScript -H "Content-Type: application/json" -d '{"deviceIds":["00008030-001234567890A001"],"name":"太连指中","iecPath":""}'
```
```python
api("/openapi/startScript", deviceIds=[DEVICE], name="太连指中")
```
```json
{"code":0,"msg":"脚本「太连指中」已在 1 台设备启动。","data":true}
```

**停止脚本** `POST /openapi/stopScript`，`{"deviceIds":[...]}`，返回 `true`；暂停中的脚本同样会停止。

**暂停、继续脚本** `POST /openapi/pauseScript`、`POST /openapi/continueScript`，`{"deviceIds":[...]}`，返回 `true`，`msg` 说明实际暂停或继续了几台。暂停在当前这一步执行完后生效，之后不再发出新的动作；继续从下一步接着执行。没有运行脚本、已经暂停（或没有暂停）的设备不计入。暂停中的设备仍算在运行脚本，`startScript` 会返回设备忙。中控设备页的「暂停脚本」「继续脚本」按钮效果相同。

**脚本是否暂停** `POST /openapi/isScriptPause`，`{"deviceId":"…"}`：`data` 为 `true` 表示暂停中；没有运行脚本时为 `false`。文档示例里的 `deviceAlias` 不参与查询。

```bash
curl -s -X POST http://127.0.0.1:8019/openapi/pauseScript -H "Content-Type: application/json" -d '{"deviceIds":["00008030-001234567890A001"]}'
curl -s -X POST http://127.0.0.1:8019/openapi/isScriptPause -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001"}'
```
```python
api("/openapi/pauseScript", deviceIds=[DEVICE])
assert api("/openapi/isScriptPause", deviceId=DEVICE) is True
api("/openapi/continueScript", deviceIds=[DEVICE])
api("/openapi/stopScript", deviceIds=[DEVICE])
```
```json
{"code":0,"msg":"已暂停 1 台设备上的脚本（当前这一步执行完后停住）。","data":true}
```

**获取刷新信息** `POST /openapi/refreshMsg`：`deviceIds` 为空时返回全部设备。运行中报告当前步骤，否则报告最近一次运行结果；`serviceOk` 为 `"1"`/`"0"`；另附 `running`、`paused` 和 `state`（`running`、`paused`、`succeeded`、`failed`、`stopped`、`unknown`）。暂停中 `msg` 仍是停住的那一步。

```bash
curl -s -X POST http://127.0.0.1:8019/openapi/refreshMsg -H "Content-Type: application/json" -d '{"deviceIds":["00008030-001234567890A001"]}'
```
```python
info = api("/openapi/refreshMsg", deviceIds=[DEVICE])[DEVICE]
```
```json
{"code":0,"msg":"成功","data":{"00008030-001234567890A001":{"msg":"脚本已完成。","deviceNo":"00008030-001234567890A001","deviceId":"00008030-001234567890A001","scriptName":"太连指中","serviceOk":"1","reportTime":"2026-11-22 10:23:03","running":false,"paused":false,"state":"succeeded"}}}
```

**设置设备别名** `POST /openapi/setDeviceAlias`，`{"deviceId":"…","deviceAlias":"phone1"}`，最多 40 个字符，空字符串清除别名。

```bash
curl -s -X POST http://127.0.0.1:8019/openapi/setDeviceAlias -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001","deviceAlias":"phone1"}'
```

**闪断 USB** `POST /openapi/reconnectUsb`：停止全部脚本和自动化通道，重启 Apple 设备服务（Windows：Apple Mobile Device Service；macOS：usbmuxd）。**会断开所有手机**，并在电脑上弹出管理员授权，需要有人确认。在授权框点取消或服务没能重启时返回 1003，`msg` 以「已停止全部脚本和自动化通道，但 … 没有重启」开头：脚本和自动化此时已经停了，需要重新启动。与其他接口一样，只允许本机访问时不需要令牌（与 EasyClick 的对接代码兼容），局域网模式下必须带令牌。`deviceId` 必须是已登记的设备，但作用于全部设备。只想重连一台手机请用 `resetUsbConn`。

**开启开发者模式** `POST /openapi/enableDeveloperMode`：与「一键配置」相同。iOS 16 以下直接成功；手机会重启，需要先关闭锁屏密码并在手机上点「打开」，最长约 5 分钟。

```python
api("/openapi/reconnectUsb", deviceId=DEVICE)
api("/openapi/enableDeveloperMode", deviceId=DEVICE)
```

### 启停自动化、电量与开发者镜像

联信的「自动化」就是手机上的 WDA Runner，使用设置页里的 WDA Runner Bundle ID。

| 路径 | 说明 | 返回 data |
| --- | --- | --- |
| `/openapi/startAutoEnv` | WDA 已在线时直接成功；否则启动 WDA 进程并等待最多 40 秒就绪。起不来时返回 1003 和原因。 | `true` |
| `/openapi/closeEnv` | 停止该设备的脚本和由本中控启动的 WDA 通道，其他设备不受影响。 | `true` |
| `/openapi/resetUsbConn` | 只重建这一台手机的 USB 端口转发和 WDA 进程；没有通道时直接成功。 | `true` |
| `/openapi/isServiceOk` | WDA 是否就绪。 | `true`/`false` |
| `/openapi/getDeviceInfo` | 需要自动化。`screenWidth`/`screenHeight` 为截图像素；`battery`（0–100）、`charging` 与 `getBattery` 同一份读数，读不到时为 `null`；`model` 为机型标识（如 `iPhone14,5`）；另附 `pointWidth`、`pointHeight`、`scale`、`ecid`。 | 对象 |
| `/openapi/getBattery` | 电量 0–100，不需要自动化。经 USB 锁屏服务读取，读不到而自动化在线时改问 WDA；与设备页电量列共用读数（最多 60 秒前），联信扩展字段 `"refresh": true` 立即重新读取。读不到（未信任、未连接）时返回 1003。 | 整数 |
| `/openapi/isCharging` | 是否在充电，读法同 `getBattery`（也接受 `refresh`）。充满后仍接着电源时 iOS 报告 `false`。 | `true`/`false` |
| `/openapi/mountDevImage` | 按系统版本自动选择并挂载开发者镜像，首次需要联网下载，已挂载时直接成功，最长约 4 分钟。 | `true` |
| `/openapi/unmountDevImage` | 先停止这台手机的脚本和由本中控启动的自动化（它们依赖镜像），再按镜像类型卸载；本来就没有挂载时也返回 `true`（看 `msg`）。重新挂载后才能再启动自动化。卸载失败时 `msg` 以「已停止这台手机的脚本和自动化，但…」开头。 | `true` |
| `/openapi/checkDevImage` | 只查询，不会自动挂载。`mounted` 与 `result` 相同；`path` 是镜像在手机上的挂载点（如 `/Developer`、`/System/Developer`），未挂载为空字符串，联信不保存镜像文件在电脑上的路径；另附 `imageType`（`Developer` 为 iOS 16 及以下，`Personalized` 为 iOS 17 及以上）。 | 对象 |

```bash
curl -s -X POST http://127.0.0.1:8019/openapi/startAutoEnv -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001"}'
curl -s -X POST http://127.0.0.1:8019/openapi/getBattery -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001","refresh":true}'
curl -s -X POST http://127.0.0.1:8019/openapi/checkDevImage -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001"}'
```
```python
if not api("/openapi/checkDevImage", deviceId=DEVICE)["mounted"]:
    api("/openapi/mountDevImage", deviceId=DEVICE)
api("/openapi/startAutoEnv", deviceId=DEVICE)
print(api("/openapi/isServiceOk", deviceId=DEVICE))
print(api("/openapi/getBattery", deviceId=DEVICE), api("/openapi/isCharging", deviceId=DEVICE))
print(api("/openapi/getDeviceInfo", deviceId=DEVICE))
api("/openapi/closeEnv", deviceId=DEVICE)
```
```json
{"code":0,"msg":"成功","data":{"deviceName":"iPhone","deviceId":"00008030-001234567890A001","serialNo":"","deviceAlias":"phone1","battery":87,"orientation":1,"orientationClick":1,"screenWidth":1170,"screenHeight":2532,"productVersion":"17.5","model":"iPhone14,5","charging":true,"pointWidth":390,"pointHeight":844,"scale":3,"ecid":"000A68AE18D20000"}}
{"code":0,"msg":"开发者镜像已挂载。","data":{"mounted":true,"result":true,"path":"/System/Developer","imageType":"Personalized"}}
```

### 动作接口

点按、输入、应用启停、锁屏状态和方向需要先启动自动化；卸载应用和重启不需要。成功时 `data` 为 `true`（另有说明的除外）。

| 路径 | 请求字段 | 说明 |
| --- | --- | --- |
| `/openapi/clickPoint` | `deviceId`、`x`、`y` | 点击 |
| `/openapi/doubleClickPoint` | `deviceId`、`x`、`y` | 双击 |
| `/openapi/longClickPoint`、`/openapi/press` | `deviceId`、`x`、`y`、`duration`（毫秒） | 长按 |
| `/openapi/swipeToPoint` | `deviceId`、`startX`、`startY`、`endX`、`endY`、`duration`（毫秒） | 滑动 |
| `/openapi/inputText`、`/openapi/typingText` | `deviceId`、`content`（最多 1000 字） | 向当前输入框输入；`inputText` 的 `duration` 被忽略 |
| `/openapi/appLaunch` | `deviceId`、`bundleId` | 启动应用；联信拿不到进程号，`data` 固定为 `0` |
| `/openapi/openApp` | `deviceId`、`bundleId` | 打开应用 |
| `/openapi/stopApp`、`/openapi/appKillByBundleId` | `deviceId`、`bundleId` | 关闭应用 |
| `/openapi/installApp` | `deviceId`、`path`、`bundleId`（仅说明） | 安装批准目录内的 `.ipa` 或 `.app`，`data` 为 `"ok"` |
| `/openapi/uninstallApp` | `deviceId`、`bundleId` | 经 USB 卸载，应用数据一并删除；系统应用（`com.apple.*`）返回 1001；`data` 为 `"ok"` |
| `/openapi/home` | `deviceId` | 回到主屏 |
| `/openapi/homeScreen` | `deviceId` | 先解锁（无锁屏密码时）再回到主屏；解锁后回主屏失败时 `msg` 以「手机已解锁」开头 |
| `/openapi/reboot` | `deviceId` | 先停止这台手机的脚本和自动化，再重启手机。连接中途断开时返回 1004（可能已经在重启），找不到设备、没有信任时返回 1003，`msg` 都说明脚本和自动化已停止。开机后开发者镜像失效，需要重新 `mountDevImage` 再 `startAutoEnv` |
| `/openapi/lockScreen`、`/openapi/unlockScreen` | `deviceId` | 锁屏、解锁（不能有锁屏密码） |
| `/openapi/isLocked` | `deviceId` | 经 WDA 查询：`data` 为 `true` 表示锁屏中 |
| `/openapi/getOrientation` | `deviceId` | `data` 为 1（竖屏）或 2（横屏） |
| `/openapi/setOrientation` | `deviceId`、`orientation`（1 竖屏、2 横屏） | 经 WDA 旋转（WDA 的 `PORTRAIT` / `LANDSCAPE`），等界面转好（最多约 3 秒）再返回，之后的坐标与截图都按新方向。当前界面不支持该方向（例如多数机型的主屏不能横屏）或开启了竖排方向锁定时返回 1003；其他值返回 1001 |
| `/openapi/adjustScreenOrientation` | `deviceId`、`orientation` | 联信坐标系始终跟随当前方向：0 或与当前方向一致时返回 `{"orientation","screenWidth","screenHeight"}`，强制另一方向返回 2001（需要改方向请用 `setOrientation`） |
| `/openapi/appHelper/setPasteboard` | `deviceId`、`content` | 经 WDA 写剪贴板，不需要 EasyClick 辅助助手，`openApp` 被忽略，`data` 为 `0` |
| `/openapi/appHelper/openUrl` | `deviceId`、`url` | 经 WDA 打开链接（也支持 `weixin://` 等自定义协议），`openApp` 被忽略，`data` 为 `0` |

```bash
curl -s -X POST http://127.0.0.1:8019/openapi/clickPoint -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001","x":585,"y":1266}'
curl -s -X POST http://127.0.0.1:8019/openapi/swipeToPoint -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001","startX":585,"startY":2100,"endX":585,"endY":300,"duration":500}'
curl -s -X POST http://127.0.0.1:8019/openapi/installApp -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001","bundleId":"com.example.app","path":"C:/Users/me/AppData/Local/LianxinFleet/apps/Example.ipa"}'
curl -s -X POST http://127.0.0.1:8019/openapi/setOrientation -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001","orientation":2}'
```
```python
api("/openapi/clickPoint", deviceId=DEVICE, x=585, y=1266)
api("/openapi/doubleClickPoint", deviceId=DEVICE, x=585, y=1266)
api("/openapi/longClickPoint", deviceId=DEVICE, x=585, y=1266, duration=1000)
api("/openapi/swipeToPoint", deviceId=DEVICE, startX=585, startY=2100, endX=585, endY=300, duration=500)
api("/openapi/appLaunch", deviceId=DEVICE, bundleId="com.apple.Preferences")
api("/openapi/inputText", deviceId=DEVICE, content="你好")
api("/openapi/appHelper/setPasteboard", deviceId=DEVICE, content="hello", openApp=False)
api("/openapi/appHelper/openUrl", deviceId=DEVICE, url="https://www.example.com", openApp=False)
api("/openapi/stopApp", deviceId=DEVICE, bundleId="com.apple.Preferences")
if api("/openapi/isLocked", deviceId=DEVICE):
    api("/openapi/unlockScreen", deviceId=DEVICE)
api("/openapi/setOrientation", deviceId=DEVICE, orientation=2)    # 之后的坐标按横屏截图换算
api("/openapi/setOrientation", deviceId=DEVICE, orientation=1)
api("/openapi/uninstallApp", deviceId=DEVICE, bundleId="com.example.app")
api("/openapi/home", deviceId=DEVICE)
```

### 抓取节点

`POST /openapi/dumpXml` 需要自动化。成功时响应体就是当前界面的 XML（`application/xml; charset=utf-8`），失败时为 JSON 信封。过滤参数与 EasyClick 的 `setFetchNodeParam` 含义相同，数字写成字符串或数字都可以：

| 字段 | 说明 |
| --- | --- |
| `visibleFilter` | `1`（缺省）全部节点；`2` 只要 `visible` 不为 `false` 的节点 |
| `labelFilter` | `1`（缺省）全部节点；`2` 只要 `label` 有值的节点 |
| `maxDepth` | 保留的层级，根节点（应用）为第 1 层，1–1000；`0` 或不填表示不限 |
| `excludedAttributes` | 去掉的属性名，英文逗号分隔，不区分大小写，例如 `visible,enabled` |

整理规则：超出 `maxDepth` 的节点连同子节点一起去掉；不满足 `visibleFilter`/`labelFilter` 的节点本身去掉，其中满足条件的子节点提升到上一层，根节点始终保留；`x`、`y`、`width`、`height` 从 WDA 的点换算成截图像素（四舍五入为整数），可以直接用于 `clickPoint`。其他属性（`type`、`name`、`label`、`value`、`enabled`、`visible` 等）保持 WDA 原样。

```bash
curl -s -X POST http://127.0.0.1:8019/openapi/dumpXml -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001","visibleFilter":"2","labelFilter":"1","maxDepth":"50","excludedAttributes":"visible,enabled"}' -o page.xml
```
```python
import xml.etree.ElementTree as ET
xml = api("/openapi/dumpXml", deviceId=DEVICE, visibleFilter="2", labelFilter="2", maxDepth="50", excludedAttributes="enabled")
for node in ET.fromstring(xml.encode("utf-8")).iter():
    if node.get("label") == "登录":
        x, y, w, h = (int(node.get(k)) for k in ("x", "y", "width", "height"))
        api("/openapi/clickPoint", deviceId=DEVICE, x=x + w // 2, y=y + h // 2)
```

### 截图接口

| 路径 | 说明 |
| --- | --- |
| `/openapi/captureFullScreenPng` | WDA 截图，PNG 原图，像素与点击坐标一致。需要自动化。 |
| `/openapi/captureFullScreenNoAutoByte` | 不需要自动化：经 USB 截图服务获取（需要已挂载开发者镜像，较慢）。多数系统为 PNG，个别旧系统为 TIFF，以 `Content-Type` 为准。 |
| `/openapi/captureFullScreenJpg` | WDA 截图转 JPG（质量 80）。编码用 Windows 自带的 WPF 编码器或 macOS 版自带的 SkiaSharp，不需要额外组件。 |
| `/openapi/captureFullScreenByteEx` | `type` 为 `"1"` 或 `"2"`，`quality` 为 1–100，按给定质量编码 JPG；响应头 `X-Fleet-Jpeg-Quality` 为实际质量。 |

```bash
curl -s -X POST http://127.0.0.1:8019/openapi/captureFullScreenPng -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001"}' -o screen.png
```
```python
open("screen.png", "wb").write(api("/openapi/captureFullScreenPng", deviceId=DEVICE))
open("screen.jpg", "wb").write(api("/openapi/captureFullScreenByteEx", deviceId=DEVICE, type="2", quality="60"))
open("usb.png", "wb").write(api("/openapi/captureFullScreenNoAutoByte", deviceId=DEVICE))
```

### 文件接口

经 USB（或已开启 Wi-Fi 连接）访问手机的媒体区 `/var/mobile/Media`（相册 `DCIM`、`Downloads` 等），不需要自动化，与「设备详情 → 文件」是同一套功能。手机路径以 `/` 开头、相对媒体区根目录，不能含 `.`/`..` 段、连续的 `/`、反斜杠或控制字符，结尾的 `/` 会被去掉。

**文件操作** `POST /openapi/fsyncFileOpr`，字段 `deviceId`、`srcPath`（手机路径）、`action`：

| action | 说明 | data |
| --- | --- | --- |
| `list` | 列出文件夹，文件夹在前、按名称排序，最多 1000 项（超出时 `msg` 说明）。联信扩展 `"detail": true` 时返回对象数组 `{"name","path","dir","size","modified"}`（`modified` 为 Unix 秒） | 名称数组 |
| `rm` | 删除文件或整个文件夹（含内容）；媒体区根目录和 `DCIM` 等系统文件夹本身、`PhotoData` 与 `iTunes_Control` 里的任何内容返回 1001；中途断开时返回 1004（可能已删掉一部分） | `[路径]` |
| `mkdir` | 新建文件夹（缺少的上级一并创建） | `[路径]` |

**推送和拉取** `POST /openapi/fsyncPullPush`，字段 `deviceId`、`action`、`srcPath`、`destPath`。EasyClick 文档里这两个路径一端是手机、一端是电脑（例如 `"destPath": "C:/a.png"`）。联信把电脑一端限制在**文件中转目录**——数据目录下的 `transfer` 文件夹（Windows 默认 `%LOCALAPPDATA%\LianxinFleet\transfer`，macOS 默认 `~/Library/Application Support/LianxinFleet.Mac/transfer`，设置页「开放接口」下方显示完整路径，第一次调用时创建）：

- 电脑路径可以写相对路径（`a.png`、`sub/a.png`，相对中转目录），也可以写中转目录里的绝对路径；其他位置、`..` 逃逸、经过符号链接或目录联接、含非法字符或保留名（Windows 的 `CON`、`NUL` 等）的路径返回 1001，`msg` 里写着中转目录的位置。
- 需要交换的文件请先放进中转目录，或从中转目录取走；接口本身不会读写电脑上的其他文件。

| action | srcPath | destPath | 说明 | data |
| --- | --- | --- | --- | --- |
| `push`（电脑 → 手机） | 中转目录里的文件 | 手机路径 | `destPath` 以 `/` 结尾或是手机上已有的文件夹时放进去并保留原文件名，否则就是完整的目标文件路径（文件名不同时先复制一份改名再推送）；同名文件覆盖 | 手机上的路径 |
| `pull`（手机 → 电脑） | 手机上的文件或文件夹 | 中转目录里的路径 | `destPath` 以 `/` 结尾或是中转目录里已有的文件夹时放进去并保留手机上的名称，否则就是完整的目标路径；缺少的上级文件夹自动创建；电脑上已有同名项时返回 1001，不覆盖 | 电脑上的完整路径 |

```bash
curl -s -X POST http://127.0.0.1:8019/openapi/fsyncFileOpr -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001","srcPath":"/","action":"list"}'
curl -s -X POST http://127.0.0.1:8019/openapi/fsyncPullPush -H "Content-Type: application/json" -d '{"deviceId":"00008030-001234567890A001","srcPath":"/DCIM/100APPLE/IMG_0001.JPG","action":"pull","destPath":"photos/a.jpg"}'
```
```python
print(api("/openapi/fsyncFileOpr", deviceId=DEVICE, srcPath="/DCIM", action="list"))
api("/openapi/fsyncFileOpr", deviceId=DEVICE, srcPath="/Downloads/批量", action="mkdir")
# 先把 note.txt 放进中转目录，再推送到手机
api("/openapi/fsyncPullPush", deviceId=DEVICE, action="push", srcPath="note.txt", destPath="/Downloads/批量/")
local = api("/openapi/fsyncPullPush", deviceId=DEVICE, action="pull", srcPath="/DCIM/100APPLE/IMG_0001.JPG", destPath="photos/")
api("/openapi/fsyncFileOpr", deviceId=DEVICE, srcPath="/Downloads/批量", action="rm")
```
```json
{"code":0,"msg":"已拉取到电脑：photos\\IMG_0001.JPG","data":"C:\\Users\\me\\AppData\\Local\\LianxinFleet\\transfer\\photos\\IMG_0001.JPG"}
{"code":1001,"msg":"srcPath（电脑文件）必须位于开放接口中转目录 C:\\Users\\me\\AppData\\Local\\LianxinFleet\\transfer 内：可以写相对路径（例如 a.png 或 sub/a.png），也可以写这个目录里的绝对路径。","data":false}
```

## 不支持的接口

返回 code 2001，`msg` 以「联信 Fleet 暂不支持」开头并说明原因：

| 接口 | 原因 |
| --- | --- |
| 蓝牙 BLE：`/openapi/ble*`（22 个） | 需要 EasyClick 的蓝牙 HID 开发板和固件。 |
| USB HID：`/openapi/usbhid*`（24 个） | 需要 EasyClick 桥接的 USB HID 会话；联信通过 WDA 控制手机，请改用动作接口。 |
| IME 输入法：`/openapi/ime*`、`forwardImeServer`、`closeForwardImeServer`（15 个） | 需要 EasyClick 自定义输入法；写剪贴板请用 `appHelper/setPasteboard`，输入文字请用 `inputText`。 |
| 相册：`uploadInsertVideo`、`uploadInsertImage`、`requestPhotoAuthorization`、`deleteAllPhotos`、`deleteAllVideos` | 需要 EasyClick 代理 IPA 写入相册。 |
| 辅助助手：`appHelper/setParam`、`getPasteboard`、`uploadToAlbum`、`startForwardPort`、`closeForwardPort`、`closeAllForwardPort` | 需要 EasyClick 辅助助手 App。 |
| 快捷指令助手：`POST /postText`、`POST /upload`、`GET /cmd`、`GET /suc`、`GET /ult` | 需要 EasyClick 快捷指令助手程序。 |
| 图片流：`startPreCapScreen`、`startScreenStream`、`captureScreenStreamByte`、`stopScreenStream`、`isScreenStreamOk` | 联信的实时画面是约 30% 尺寸、5 帧/秒的预览流，不能作为抓图来源；请改用 `captureFullScreenPng`/`captureFullScreenJpg`。 |
| `resetCaptureScreenNoAutoEnv` | EasyClick 无权限截图环境专用；联信直接调用 `captureFullScreenNoAutoByte` 即可。 |
| `setAgentSetting`、`setAssistiveTouch`、`ioHIDEvent` | 需要 EasyClick 代理 IPA。 |
| `multiTouch` | WDA 通道目前只提供单指手势。 |
| `getOrientationNoAuto` | 不启动自动化无法读取屏幕方向；启动自动化后用 `getOrientation`。 |
| `startTunnel`、`closeTunnel` | iOS 17+ 隧道暂不支持（目前只支持 USB 与 iOS 16 及以下的无线连接）。 |
| `erasePhone` | 恢复出厂会清空手机全部数据，联信不通过接口提供。 |

## 开发者：增加接口

服务代码在 `src/Lianxin.Fleet.Core/OpenApi`：

- `OpenApiServer`（`TcpListener` 上的 HTTP/1.1 服务）、`OpenApiRequest`（请求解析与上限）、`OpenApiSecurity`（Host、Origin、令牌、内容类型）、`OpenApiReply`（信封、图片与 XML 响应）。
- `OpenApiRouter` 保存路径到处理函数的映射，`OpenApiRouter.CreateDefault()` 依次调用各模块的 `Register`，模块按 EasyClick 文档页划分：`UnsupportedRoutes`、`CenterRoutes`（中控）、`AutomationRoutes`（自动化、电量、开发者镜像）、`ActionRoutes`（动作、锁屏与方向、抓取节点）、`ScreenshotRoutes`（截图）、`FileRoutes`（文件）。`PageSourceDump` 整理界面树，`TransferFolder` 负责电脑端中转目录的路径边界。
- 处理函数的签名是 `Task<OpenApiReply> Handler(OpenApiCall call)`。`call.Device()` 解析 `deviceId`，`call.Devices()` 解析 `deviceIds`，`call.Text/Number/Flag` 读取字段，`call.Backend` 提供中控的数据库、设备桥接、脚本执行器、WDA 主机、一键配置、设备管理（`Manager`）和中转目录（`TransferDirectory`），`call.Cancellation` 在服务停止或超时时取消。抛出 `OpenApiException(code, msg)` 返回指定错误；`ArgumentException`、`DeviceOperationException`、`NotSupportedException`（设备通道没有该能力，返回 2001）等会自动换成对应的 code。

在对应模块里加一行 `Post` 即可增加接口；`Post` 会覆盖同一路径的 `Pending`/`Unsupported` 登记（登记顺序无关），同一路径重复 `Post` 会报错。例如 `ActionRoutes` 里 `isLocked` 的写法：

```csharp
.Post("/openapi/isLocked", async call =>
{
    var locked = await call.Backend.Bridge.IsLockedAsync(call.Device(), call.Cancellation);
    return OpenApiReply.Ok(locked, locked ? "屏幕已锁定。" : "屏幕未锁定。");
})
```

新接口请在 `checks/Lianxin.Fleet.Checks/Scenarios/OpenApiChecks*.cs` 里通过回环地址上的真实 HTTP 请求、假设备桥接和假设备工具进程覆盖；检查会核对登记的接口总数（142 个，与 EasyClick 文档一致）以及已实现、不支持的数量。

## 验证范围

`checks/Lianxin.Fleet.Checks` 在临时端口上用真实 HTTP 请求、假设备桥接和假设备工具进程覆盖了请求解析上限、安全检查、局域网令牌、返回信封、每个已实现接口的参数换算（含横屏、旋转后和放大显示的坐标换算）、脚本暂停与继续、电量缓存与刷新、镜像查询与卸载、卸载应用与重启的命令参数、文件接口的手机路径校验和电脑端中转目录边界（目录外路径、`..`、符号链接、不覆盖）、抓取节点的过滤与像素换算、不支持接口的返回码，以及启停、端口占用、连接上限和停止时取消；另有 WDA 锁屏查询与旋转请求的协议检查，以及加固项：Node.js fetch 的请求头、1 MB 正文上限与同时接收的正文总量、请求行 2 秒限时、每个地址的连接数与 503 回复上限、系统异常不回传本机路径、installApp 和 fsyncPullPush 的 UNC / 设备路径在访问文件系统前被拒绝、半途失败时的副作用说明，以及 pymobiledevice3 11.57.1 真实错误输出下的确定失败与待确认。两个平台的界面冒烟测试用假 WDA 和假设备工具调用过这些接口。

尚未在真机上逐个验证的部分：截图像素与 WDA 点坐标在各机型上的对应关系、`setOrientation` 在真机上的旋转与横屏点击、`isLocked`、抓取节点在复杂界面上的耗时、真实 USB 上的电量与文件推送拉取、`reboot`/`unmountDevImage` 的实际效果、JPG 编码、`reconnectUsb` 的管理员授权流程，以及 macOS 上的运行。
