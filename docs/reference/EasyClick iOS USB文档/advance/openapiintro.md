<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapiintro/ -->

# 企业版开放接口

## 说明

企业版开放接口给第三方语言使用：即使不会 EC 的 JS，也可用 Python、易语言、C++ 等通过 **HTTP** 对接中控，编写自动化。

- 请求方式：HTTP（多数为 `POST`，`Content-Type: application/json`）
- URL 前缀：中控地址，例如 `http://127.0.0.1:8019`，后面跟上接口路径
- 通用返回：`code` 为 `0` 表示成功，其它为失败，`msg` 为错误信息
- 文档按模块拆分（来源：Apifox 导出 + USB HID 补充），请从下表进入

> **提示**
>
> USB HID 原先未收录在 Apifox 导出中，现已单独成章，与脚本 [usbHidEvent](../funcs/usb-hid-event-api.md) 对应。详见 [自动化 · USB HID](openapi/usbhid.md)。

## 请求示例

把 `BASE` 换成你的中控地址。下面先查设备列表，再演示 USB HID：开会话 → 设分辨率 → 点击。

`deviceId` 从 [设备列表](openapi/center.md) 接口返回值里取。

**Python**

```python
import requests

BASE = "http://127.0.0.1:8019"


def openapi_post(path, body=None):
    r = requests.post(f"{BASE}{path}", json=body or {}, timeout=30)
    r.raise_for_status()
    data = r.json()
    if data.get("code") != 0:
        raise RuntimeError(data.get("msg") or data)
    return data


# 1) 设备列表
print(openapi_post("/openapi/deviceList", {"deviceId": "", "groupId": ""}))

# 2) USB HID：开会话 → 设分辨率 → 点击
DEVICE_ID = "90e2f3834c0977205e441aa664916a9bdde81e8d"
openapi_post("/openapi/usbhidSessionStart", {"deviceId": DEVICE_ID, "gate": True})
openapi_post("/openapi/usbhidSetScreenSize", {"deviceId": DEVICE_ID, "w": 1170, "h": 2532})
openapi_post("/openapi/usbhidClickPoint", {"deviceId": DEVICE_ID, "x": 200, "y": 400})
```

依赖：`pip install requests`

**Node.js**

```javascript
const BASE = "http://127.0.0.1:8019";

async function openapiPost(path, body = {}) {
  const res = await fetch(`${BASE}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  const data = await res.json();
  if (data.code !== 0) {
    throw new Error(data.msg || JSON.stringify(data));
  }
  return data;
}

async function main() {
  // 1) 设备列表
  console.log(await openapiPost("/openapi/deviceList", { deviceId: "", groupId: "" }));

  // 2) USB HID：开会话 → 设分辨率 → 点击
  const DEVICE_ID = "90e2f3834c0977205e441aa664916a9bdde81e8d";
  await openapiPost("/openapi/usbhidSessionStart", { deviceId: DEVICE_ID, gate: true });
  await openapiPost("/openapi/usbhidSetScreenSize", { deviceId: DEVICE_ID, w: 1170, h: 2532 });
  await openapiPost("/openapi/usbhidClickPoint", { deviceId: DEVICE_ID, x: 200, y: 400 });
}

main().catch(console.error);
```

Node.js 18+ 自带 `fetch`；更低版本可用 `node-fetch` 或 `axios`。

**cURL**

```bash
BASE="http://127.0.0.1:8019"
DEVICE_ID="90e2f3834c0977205e441aa664916a9bdde81e8d"

# 1) 设备列表
curl -s -X POST "$BASE/openapi/deviceList" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"","groupId":""}'

# 2) USB HID：开会话 → 设分辨率 → 点击
curl -s -X POST "$BASE/openapi/usbhidSessionStart" \
  -H "Content-Type: application/json" \
  -d "{\"deviceId\":\"$DEVICE_ID\",\"gate\":true}"

curl -s -X POST "$BASE/openapi/usbhidSetScreenSize" \
  -H "Content-Type: application/json" \
  -d "{\"deviceId\":\"$DEVICE_ID\",\"w\":1170,\"h\":2532}"

curl -s -X POST "$BASE/openapi/usbhidClickPoint" \
  -H "Content-Type: application/json" \
  -d "{\"deviceId\":\"$DEVICE_ID\",\"x\":200,\"y\":400}"
```

**C#**

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

var baseUrl = "http://127.0.0.1:8019";
var deviceId = "90e2f3834c0977205e441aa664916a9bdde81e8d";
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

async Task<JsonElement> OpenApiPost(string path, object body)
{
    var json = JsonSerializer.Serialize(body);
    using var content = new StringContent(json, Encoding.UTF8, "application/json");
    using var res = await http.PostAsync(baseUrl + path, content);
    res.EnsureSuccessStatusCode();
    await using var stream = await res.Content.ReadAsStreamAsync();
    using var doc = await JsonDocument.ParseAsync(stream);
    var root = doc.RootElement.Clone();
    if (root.GetProperty("code").GetInt32() != 0)
        throw new Exception(root.TryGetProperty("msg", out var msg) ? msg.GetString() : root.ToString());
    return root;
}

// 1) 设备列表
Console.WriteLine(await OpenApiPost("/openapi/deviceList", new { deviceId = "", groupId = "" }));

// 2) USB HID
await OpenApiPost("/openapi/usbhidSessionStart", new { deviceId, gate = true });
await OpenApiPost("/openapi/usbhidSetScreenSize", new { deviceId, w = 1170, h = 2532 });
await OpenApiPost("/openapi/usbhidClickPoint", new { deviceId, x = 200, y = 400 });
```

其它接口同样是 `POST` + JSON body，把路径和参数换成各模块页中的说明即可。

## 模块目录

| 模块 | 说明 |
| --- | --- |
| [自动化 · 截图](openapi/screenshot.md) | 截图、图片流 |
| [自动化 · 动作](openapi/action.md) | 点击、滑动、输入、App |
| [自动化 · 启停自动化](openapi/auto-env.md) | 启动/释放自动化、隧道、电量 |
| [自动化 · 相册](openapi/album.md) | 上传图片/视频、清空相册 |
| [自动化 · 文件操作](openapi/files.md) | 设备文件推送与拉取 |
| [自动化 · IME 输入法](openapi/ime.md) | 自定义输入法、剪贴板转发 |
| [自动化 · 蓝牙 BLE](openapi/ble.md) | 蓝牙板 HID 动作 |
| [自动化 · USB HID](openapi/usbhid.md) | **免硬件**数据线控制手机 |
| [自动化 · 开发者镜像](openapi/dev-image.md) | 挂载 / 卸载 / 查询 DDI（10.8.0+） |
| [中控接口](openapi/center.md) | 设备列表、脚本启停等 |
| [辅助助手](openapi/app-helper.md) | 助手剪贴板、相册、端口转发 |
| [快捷指令助手](openapi/shortcuts.md) | 需配合快捷指令助手程序，路径见该页正文 |

## 接口分类说明

- **中控接口**：设备列表、执行/停止脚本等，HTTP 调用
- **自动化接口**：截图、点击、滑动、输入、相册、IME、蓝牙 BLE、USB HID、开发者镜像等
- **USB HID**
  - 无需蓝牙/OTG 开发板，经 USB 数据线模拟点击与输入
  - 投屏场景见 [新界面操作指引](ios-usb-screen-new.md)
- **开发者镜像**：无需先启动自动化；对应脚本 `utils.mountDevImage` / `unmountDevImage` / `checkDevImage`

## 历史 Apifox（备份）

在线链接（不含完整 USB HID，以站内文档为准）：

- 链接：<https://www.apifox.cn/apidoc/shared-de0cb92e-6f29-49c1-8386-7982b3b1e051>
- 访问密码：`ZwzKf51B`

