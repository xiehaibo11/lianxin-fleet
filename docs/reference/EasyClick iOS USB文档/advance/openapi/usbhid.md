<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapi/usbhid/ -->

# USB HID 接口

## 说明

本页为 **自动化接口 / USB HID** 模块，与其它开放接口模块并列；对应脚本 [usbHidEvent](../../funcs/usb-hid-event-api.md)。

- USB HID 开放接口供第三方语言（Python、易语言、C++ 等）通过 **HTTP** 控制 iPhone，**无需蓝牙板 / OTG 板**
- 路径前缀：中控地址，例如 `http://127.0.0.1:8019`
- 全部为 `POST`，`Content-Type: application/json`
- 与脚本模块 [USB HID 函数（usbHidEvent）](../../funcs/usb-hid-event-api.md) 一一对应
- 适配 **EC iOS USB 10.7.0+**；手机建议 **iOS 17+**

> **警告：与代理自动化服务互斥**
>
> **代理自动化服务**（中控「启动自动化」、脚本 `startEnv` 等）和 **USB HID** 开放接口是两条控机链路，**互斥，只能二选一**，不要在同一台设备上混用。需要截图时请用无自动化截图接口，见 [截图开放接口](screenshot.md)。脚本侧说明见 [usbHidEvent](../../funcs/usb-hid-event-api.md)。

> **提示**
>
> - 使用前请保持 USB 已连接、手机已信任本机，并已启动中控桥接
> - 建议先调 `usbhidSessionStart`，再 `usbhidSetScreenSize`，然后点击/输入
> - 成功时 `code` 为 `0`；失败时 `code` 非 0，`msg` 为错误信息
> - 英文粘贴若多余空格：手机「设置 → 通用 → 键盘」关闭「智能标点」

### 通用返回

```json
{
  "code": 0,
  "msg": "",
  "data": ""
}
```

| 字段 | 说明 |
| --- | --- |
| code | `0` 成功，其它失败 |
| msg | 失败时的错误信息 |
| data | 部分接口返回业务数据（如剪贴板文本）；多数动作接口可为空 |

---

## 会话

### POST 开启 USB HID 会话

`POST /openapi/usbhidSessionStart`

对应 `usbHidEvent.sessionStart(gate)`。会话已存在时会复用。

> **注意：可能重挂开发者镜像**
>
> 为启用 HID，内部可能卸载并重新挂载较新的开发者镜像。这可能导致随后代理自动化（`startEnv`）启动失败。  
> 若失败：先卸载再加载开发者镜像，然后重新启动自动化——脚本用 `utils.unmountDevImage()` / `utils.mountDevImage()`，HTTP 用 [OpenAPI · 开发者镜像](dev-image.md)（`/openapi/unmountDevImage`、`/openapi/mountDevImage`）。详见 [USB HID 函数 · sessionStart](../../funcs/usb-hid-event-api.md#usbhideventsessionstart-%E5%BC%80%E5%90%AF%E4%BC%9A%E8%AF%9D) 与 [工具函数 · 开发者镜像](../../funcs/utils-api.md#%E5%BC%80%E5%8F%91%E8%80%85%E9%95%9C%E5%83%8F)（EC iOS USB **10.8.0+**）。

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "gate": true
}
```

| 参数 | 类型 | 必选 | 说明 |
| --- | --- | --- | --- |
| deviceId | string | 是 | 设备 ID |
| gate | boolean | 否 | 是否增强兼容模式，默认 `true` |

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "gate": True
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidSessionStart", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "gate": true
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidSessionStart", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidSessionStart" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","gate":true}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"gate\":true}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidSessionStart", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 关闭 USB HID 会话

`POST /openapi/usbhidSessionStop`

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidSessionStop", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidSessionStop", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidSessionStop" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidSessionStop", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 重建 USB HID 会话

`POST /openapi/usbhidSessionRestart`

点击无响应时可重建会话（对应投屏右键「重建 USBHID 会话」）。

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "gate": true
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "gate": True
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidSessionRestart", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "gate": true
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidSessionRestart", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidSessionRestart" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","gate":true}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"gate\":true}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidSessionRestart", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

---

## 屏幕与坐标

### POST 设置屏幕尺寸

`POST /openapi/usbhidSetScreenSize`

按截图/投屏实际分辨率填写。也可用 `width` / `height` 代替 `w` / `h`。

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "w": 1170,
  "h": 2532
}
```

| 参数 | 类型 | 必选 | 说明 |
| --- | --- | --- | --- |
| deviceId | string | 是 | 设备 ID |
| w / width | number | 是 | 宽 |
| h / height | number | 是 | 高 |

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "w": 1170,
  "h": 2532
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidSetScreenSize", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "w": 1170,
  "h": 2532
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidSetScreenSize", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidSetScreenSize" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","w":1170,"h":2532}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"w\":1170,\"h\":2532}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidSetScreenSize", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

---

## 触控

坐标与投屏画面、截图像素坐标一致。

### POST 点击

`POST /openapi/usbhidClickPoint`

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 400
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 400
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidClickPoint", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 400
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidClickPoint", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidClickPoint" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","x":200,"y":400}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"x\":200,\"y\":400}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidClickPoint", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 双击

`POST /openapi/usbhidDoubleClickPoint`

参数同点击：`deviceId`、`x`、`y`。

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 400
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidDoubleClickPoint", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 400
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidDoubleClickPoint", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidDoubleClickPoint" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","x":200,"y":400}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"x\":200,\"y\":400}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidDoubleClickPoint", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 长按

`POST /openapi/usbhidPress`

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 400,
  "delay": 500
}
```

| 参数 | 类型 | 必选 | 说明 |
| --- | --- | --- | --- |
| delay | number | 否 | 长按毫秒，默认 `500` |

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 400,
  "delay": 500
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidPress", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 400,
  "delay": 500
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidPress", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidPress" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","x":200,"y":400,"delay":500}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"x\":200,\"y\":400,\"delay\":500}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidPress", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 滑动

`POST /openapi/usbhidSwipeToPoint`

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 100,
  "y": 800,
  "ex": 100,
  "ey": 200,
  "duration": 500
}
```

| 参数 | 类型 | 必选 | 说明 |
| --- | --- | --- | --- |
| x, y | number | 是 | 起点 |
| ex, ey | number | 是 | 终点 |
| duration / delay | number | 否 | 滑动时长毫秒，默认 `500` |

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 100,
  "y": 800,
  "ex": 100,
  "ey": 200,
  "duration": 500
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidSwipeToPoint", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 100,
  "y": 800,
  "ex": 100,
  "ey": 200,
  "duration": 500
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidSwipeToPoint", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidSwipeToPoint" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","x":100,"y":800,"ex":100,"ey":200,"duration":500}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"x\":100,\"y\":800,\"ex\":100,\"ey\":200,\"duration\":500}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidSwipeToPoint", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 按下 / 移动 / 抬起

- `POST /openapi/usbhidTouchDown`
- `POST /openapi/usbhidTouchMove`
- `POST /openapi/usbhidTouchUp`

参数均为：`deviceId`、`x`、`y`。

#### `POST /openapi/usbhidTouchDown`

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 400
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidTouchDown", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 400
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidTouchDown", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidTouchDown" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","x":200,"y":400}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"x\":200,\"y\":400}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidTouchDown", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

#### `POST /openapi/usbhidTouchMove`

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 220,
  "y": 420
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidTouchMove", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 220,
  "y": 420
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidTouchMove", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidTouchMove" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","x":220,"y":420}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"x\":220,\"y\":420}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidTouchMove", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

#### `POST /openapi/usbhidTouchUp`

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 220,
  "y": 420
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidTouchUp", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 220,
  "y": 420
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidTouchUp", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidTouchUp" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","x":220,"y":420}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"x\":220,\"y\":420}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidTouchUp", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 多点触控轨迹

`POST /openapi/usbhidMultiTouch`

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "finger1": [
    { "action": 0, "x": 100, "y": 500, "delay": 20 },
    { "action": 2, "x": 100, "y": 300, "delay": 30 },
    { "action": 1, "x": 100, "y": 300, "delay": 20 }
  ],
  "timeout": 10000
}
```

| 参数 | 类型 | 必选 | 说明 |
| --- | --- | --- | --- |
| finger1 / data | array/string | 是 | 触点轨迹；`action`：`0` 按下、`2` 移动、`1` 抬起 |
| timeout / delay | number | 否 | 总超时毫秒，默认 `10000` |

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "finger1": [
    {
      "action": 0,
      "x": 100,
      "y": 500,
      "delay": 20
    },
    {
      "action": 2,
      "x": 100,
      "y": 300,
      "delay": 30
    },
    {
      "action": 1,
      "x": 100,
      "y": 300,
      "delay": 20
    }
  ],
  "timeout": 10000
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidMultiTouch", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "finger1": [
    {
      "action": 0,
      "x": 100,
      "y": 500,
      "delay": 20
    },
    {
      "action": 2,
      "x": 100,
      "y": 300,
      "delay": 30
    },
    {
      "action": 1,
      "x": 100,
      "y": 300,
      "delay": 20
    }
  ],
  "timeout": 10000
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidMultiTouch", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidMultiTouch" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","finger1":[{"action":0,"x":100,"y":500,"delay":20},{"action":2,"x":100,"y":300,"delay":30},{"action":1,"x":100,"y":300,"delay":20}],"timeout":10000}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"finger1\":[{\"action\":0,\"x\":100,\"y\":500,\"delay\":20},{\"action\":2,\"x\":100,\"y\":300,\"delay\":30},{\"action\":1,\"x\":100,\"y\":300,\"delay\":20}],\"timeout\":10000}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidMultiTouch", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 真多指触控（最多 5 指）

`POST /openapi/usbhidMultiTouch2`

对应 `usbHidEvent.multiTouch2(finger1, finger2, finger3, finger4, finger5, timeout)`。各指轨迹并行播放，同毫秒点合成一帧多触点 HID report。

> **提示**
>
> 缩放要平顺：中间点请密采样（例如 24 步 × 20ms）；先 `usbhidSessionStart`，再用无自动化截图得到的宽高调 `usbhidSetScreenSize`。建议先打开「照片」全屏再测双指缩放。完整脚本示例见 [usbHidEvent.multiTouch2](../../funcs/usb-hid-event-api.md#usbhideventmultitouch2-%E7%9C%9F%E5%A4%9A%E6%8C%87%E8%A7%A6%E6%91%B8)。

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "finger1": [
    { "action": 0, "x": 495, "y": 1266, "delay": 10 },
    { "action": 2, "x": 495, "y": 1266, "delay": 60 },
    { "action": 2, "x": 435, "y": 1266, "delay": 20 },
    { "action": 2, "x": 375, "y": 1266, "delay": 20 },
    { "action": 2, "x": 315, "y": 1266, "delay": 20 },
    { "action": 2, "x": 245, "y": 1266, "delay": 20 },
    { "action": 1, "x": 245, "y": 1266, "delay": 40 }
  ],
  "finger2": [
    { "action": 0, "x": 675, "y": 1266, "delay": 10 },
    { "action": 2, "x": 675, "y": 1266, "delay": 60 },
    { "action": 2, "x": 735, "y": 1266, "delay": 20 },
    { "action": 2, "x": 795, "y": 1266, "delay": 20 },
    { "action": 2, "x": 855, "y": 1266, "delay": 20 },
    { "action": 2, "x": 925, "y": 1266, "delay": 20 },
    { "action": 1, "x": 925, "y": 1266, "delay": 40 }
  ],
  "timeout": 15000
}
```

上例为双指外扩（放大）示意（约 1170×2532 屏中心）；实机请按截图像素改坐标。缩小则把两指起点/终点对调（远 → 近）。

| 参数 | 类型 | 必选 | 说明 |
| --- | --- | --- | --- |
| finger1～finger5 | array | 至少一指 | 各指触点轨迹；不用的指可省略；`action`：`0` 按下、`2` 移动、`1` 抬起 |
| timeout / delay | number | 否 | 总超时毫秒，默认 `10000` |

### 请求示例

**Python**

```python
import requests

DEVICE_ID = "90e2f3834c0977205e441aa664916a9bdde81e8d"
BASE = "http://127.0.0.1:8019"

def build_pinch_finger(cx, cy, start_offset, end_offset, steps=24, step_delay=20):
    pts = [
        {"action": 0, "x": cx + start_offset, "y": cy, "delay": 10},
        {"action": 2, "x": cx + start_offset, "y": cy, "delay": 60},
    ]
    for i in range(1, steps + 1):
        t = i / steps
        ox = int(start_offset + (end_offset - start_offset) * t)
        pts.append({"action": 2, "x": cx + ox, "y": cy, "delay": step_delay})
    pts.append({"action": 1, "x": cx + end_offset, "y": cy, "delay": 40})
    return pts

# 先 sessionStart + setScreenSize（宽高建议来自无自动化截图）
cx, cy = 1170 // 2, 2532 // 2
body = {
    "deviceId": DEVICE_ID,
    "finger1": build_pinch_finger(cx, cy, -90, -340),
    "finger2": build_pinch_finger(cx, cy, 90, 340),
    "timeout": 15000,
}
r = requests.post(f"{BASE}/openapi/usbhidMultiTouch2", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const DEVICE_ID = "90e2f3834c0977205e441aa664916a9bdde81e8d";
const BASE = "http://127.0.0.1:8019";

function buildPinchFinger(cx, cy, startOffset, endOffset, steps = 24, stepDelay = 20) {
  const pts = [
    { action: 0, x: cx + startOffset, y: cy, delay: 10 },
    { action: 2, x: cx + startOffset, y: cy, delay: 60 },
  ];
  for (let i = 1; i <= steps; i++) {
    const t = i / steps;
    const ox = Math.floor(startOffset + (endOffset - startOffset) * t);
    pts.push({ action: 2, x: cx + ox, y: cy, delay: stepDelay });
  }
  pts.push({ action: 1, x: cx + endOffset, y: cy, delay: 40 });
  return pts;
}

const cx = Math.floor(1170 / 2);
const cy = Math.floor(2532 / 2);
const body = {
  deviceId: DEVICE_ID,
  finger1: buildPinchFinger(cx, cy, -90, -340),
  finger2: buildPinchFinger(cx, cy, 90, 340),
  timeout: 15000,
};
const res = await fetch(`${BASE}/openapi/usbhidMultiTouch2`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
console.log(await res.json());
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidMultiTouch2" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","finger1":[{"action":0,"x":495,"y":1266,"delay":10},{"action":2,"x":495,"y":1266,"delay":60},{"action":2,"x":245,"y":1266,"delay":20},{"action":1,"x":245,"y":1266,"delay":40}],"finger2":[{"action":0,"x":675,"y":1266,"delay":10},{"action":2,"x":675,"y":1266,"delay":60},{"action":2,"x":925,"y":1266,"delay":20},{"action":1,"x":925,"y":1266,"delay":40}],"timeout":15000}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"finger1\":[{\"action\":0,\"x\":495,\"y\":1266,\"delay\":10},{\"action\":2,\"x\":495,\"y\":1266,\"delay\":60},{\"action\":2,\"x\":245,\"y\":1266,\"delay\":20},{\"action\":1,\"x\":245,\"y\":1266,\"delay\":40}],\"finger2\":[{\"action\":0,\"x\":675,\"y\":1266,\"delay\":10},{\"action\":2,\"x\":675,\"y\":1266,\"delay\":60},{\"action\":2,\"x\":925,\"y\":1266,\"delay\":20},{\"action\":1,\"x\":925,\"y\":1266,\"delay\":40}],\"timeout\":15000}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidMultiTouch2", content);
Console.WriteLine(await res.Content.ReadAsStringAsync());
```

---

## 输入与按键

### POST 粘贴输入

`POST /openapi/usbhidInputText`

统一剪贴板粘贴，适合中英文。也可用字段 `content`。

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "text": "你好"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "text": "你好"
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidInputText", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "text": "你好"
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidInputText", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidInputText" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","text":"你好"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"text\":\"你好\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidInputText", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 打字输入

`POST /openapi/usbhidTypeText`

可打印英文走键盘；含中文、emoji 等自动改粘贴。

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "text": "hello"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "text": "hello"
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidTypeText", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "text": "hello"
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidTypeText", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidTypeText" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","text":"hello"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"text\":\"hello\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidTypeText", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 系统按键

`POST /openapi/usbhidSystemKey`

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "key": "home"
}
```

| key | 说明 |
| --- | --- |
| home | 主屏 |
| recents | 多任务 |
| lock | 锁屏 |

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "key": "home"
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidSystemKey", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "key": "home"
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidSystemKey", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidSystemKey" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","key":"home"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"key\":\"home\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidSystemKey", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 字符按键 / 组合键

`POST /openapi/usbhidKeyPressChar`

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "prefix": "gui",
  "code": "v"
}
```

| 参数 | 说明 |
| --- | --- |
| prefix | 可空：`alt` / `ctrl` / `gui` / `shift` / `r_ctrl` / `r_shift` |
| code | 字符或键名，如 `a` / `enter` / `BS` |

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "prefix": "gui",
  "code": "v"
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidKeyPressChar", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "prefix": "gui",
  "code": "v"
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidKeyPressChar", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidKeyPressChar" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","prefix":"gui","code":"v"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"prefix\":\"gui\",\"code\":\"v\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidKeyPressChar", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 按下单个键 / 抬起

- `POST /openapi/usbhidKeyPress` — body：`deviceId`、`key`（或 `code`）
- `POST /openapi/usbhidKeyUp` — body：`deviceId`

#### `POST /openapi/usbhidKeyPress`

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "key": "enter"
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidKeyPress", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "key": "enter"
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidKeyPress", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidKeyPress" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","key":"enter"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"key\":\"enter\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidKeyPress", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

#### `POST /openapi/usbhidKeyUp`

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidKeyUp", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidKeyUp", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidKeyUp" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidKeyUp", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 音量 / 静音

仅需 `deviceId`：

- `POST /openapi/usbhidVolumeUp`
- `POST /openapi/usbhidVolumeDown`
- `POST /openapi/usbhidMute`

#### `POST /openapi/usbhidVolumeUp`

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidVolumeUp", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidVolumeUp", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidVolumeUp" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidVolumeUp", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

#### `POST /openapi/usbhidVolumeDown`

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidVolumeDown", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidVolumeDown", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidVolumeDown" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidVolumeDown", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

#### `POST /openapi/usbhidMute`

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidMute", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidMute", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidMute" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidMute", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

---

## 剪贴板

### POST 设置剪贴板

`POST /openapi/usbhidSetClipboard`

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "text": "剪贴板内容"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "text": "剪贴板内容"
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidSetClipboard", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "text": "剪贴板内容"
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidSetClipboard", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidSetClipboard" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","text":"剪贴板内容"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"text\":\"剪贴板内容\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidSetClipboard", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### POST 读取剪贴板

`POST /openapi/usbhidGetClipboard`

```json
{
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
}
```

成功时文本在返回的 `data` 中。

---

## 与脚本 / BLE 对照

| 脚本 `usbHidEvent` | OpenAPI 接口名 | 说明 |
| --- | --- | --- |
| sessionStart | usbhidSessionStart | 开启会话 |
| sessionStop | usbhidSessionStop | 关闭会话 |
| sessionRestart | usbhidSessionRestart | 重建会话 |
| setScreenSize | usbhidSetScreenSize | 屏幕尺寸 |
| clickPoint | usbhidClickPoint | 点击 |
| press | usbhidPress | 长按 |
| swipeToPoint | usbhidSwipeToPoint | 滑动 |
| typeText / inputText | usbhidTypeText / usbhidInputText | 打字 / 粘贴 |
| … | 见本页各接口 | 其余同名对应 |

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
}
r = requests.post("http://127.0.0.1:8019/openapi/usbhidGetClipboard", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/usbhidGetClipboard", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/usbhidGetClipboard" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/usbhidGetClipboard", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

蓝牙板方案见 [自动化 · 蓝牙 BLE](ble.md)；USB HID **不需要** 打开串口、配对开发板等步骤。

更多脚本说明：[USB HID 函数](../../funcs/usb-hid-event-api.md) · [投屏场景指引](../ios-usb-screen-new.md)

