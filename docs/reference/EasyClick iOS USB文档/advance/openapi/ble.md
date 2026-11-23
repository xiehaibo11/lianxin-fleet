<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapi/ble/ -->

# 蓝牙 BLE 接口

## 说明

本页由企业版 OpenAPI 文档整理，模块：**自动化接口/蓝牙BLE动作**。

> **提示**
>
> - 请求方式：HTTP `POST`（个别快捷指令接口为 GET，见正文）
> - URL 前缀：中控地址，例如 `http://127.0.0.1:8019`
> - `Content-Type: application/json`
> - 通用返回：`code` 为 `0` 表示成功，其它为失败，`msg` 为错误信息
> - 返回总览：[企业版开放接口文档](../openapiintro.md)

> **提示**
>
> 若使用 **USB HID 免硬件** 方案，请看 [USB HID 开放接口](usbhid.md)，无需串口/开发板。

## 设置缩放比

`POST /openapi/bleSetScale`

设置鼠标补偿比率：鼠标移动 1 单位时，像素移动多少单位，默认是 `2.0`。

| 机型 | 分辨率 | 建议缩放比 | 说明 |
| --- | --- | --- | --- |
| iPhone 6/7/8 | 375 × 667 | 2.0 | 标准 16:9，无安全区干扰 |
| iPhone 11 / XR | 414 × 896 | 1.96 | 屏幕变长，系统加速补偿 Y 轴 |
| iPhone X/XS/11 Pro | 375 × 812 | 1.98 | 纵横比 19.5:9，存在微小加速 |
| iPhone 12/13/14/15 | 390 × 844 | 1.97 | 逻辑点数与 11 不同，加速曲线略有变动 |
| Plus / Max 系列 | 414 × 896 / 430 × 932 | 1.94 ~ 1.95 | 屏幕最高，系统为了操作效率会大幅增加 Y 轴增益 |

适配 **EC iOS USB 9.20.0+**。参数 `xScale` / `yScale` 为浮点数。需要动态计算时，可参考脚本 `bleEvent.getIPhoneScale`。

**请求 Body**

```json
{
    "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "xScale":2.0,
    "yScale": 1.96
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "xScale": 2.0,
  "yScale": 1.96
}
r = requests.post("http://127.0.0.1:8019/openapi/bleSetScale", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "xScale": 2.0,
  "yScale": 1.96
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleSetScale", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleSetScale" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","xScale":2.0,"yScale":1.96}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"xScale\":2.0,\"yScale\":1.96}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleSetScale", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » xScale | body | number | 是 | x的缩放比，浮点型 例如1.98 | none |
| » yScale | body | number | 是 | y的缩放比，浮点型 例如1.98 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | true | none | 0是成功其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 点击坐标

`POST /openapi/bleClickPoint`

点击指定坐标。适配 **EC iOS USB 9.20.0+**。

参数：`x` / `y` 为坐标。

**请求 Body**

```json
{
    "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "x": 36,
    "y": 34
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 36,
  "y": 34
}
r = requests.post("http://127.0.0.1:8019/openapi/bleClickPoint", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 36,
  "y": 34
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleClickPoint", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleClickPoint" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","x":36,"y":34}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"x\":36,\"y\":34}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleClickPoint", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » x | body | number | 是 | x坐标 | none |
| » y | body | number | 是 | y坐标 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 设置屏幕尺寸

`POST /openapi/bleSetScreenSize`

设置屏幕逻辑尺寸，避免鼠标移出屏幕导致偏移。适配 **EC iOS USB 9.20.0+**。

参数：`w` / `h` 为宽高。若未知尺寸，可用全屏截图（如 `image.captureFullScreenNoAuto`）后取图片宽高。

**请求 Body**

```json
{
       "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",

    "w": 1000,
    "h": 2000
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "w": 1000,
  "h": 2000
}
r = requests.post("http://127.0.0.1:8019/openapi/bleSetScreenSize", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "w": 1000,
  "h": 2000
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleSetScreenSize", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleSetScreenSize" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","w":1000,"h":2000}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"w\":1000,\"h\":2000}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleSetScreenSize", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » w | body | number | 是 | 宽度 | none |
| » h | body | number | 是 | 高度 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 打开串口通信

`POST /openapi/bleOpenSerial`

打开与开发板的串口通信。适配 **EC iOS USB 9.20.0+**。

参数：`timeout` 为超时时间（毫秒），默认 15000。

**请求 Body**

```json
{
    "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "timeout": 15000
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "timeout": 15000
}
r = requests.post("http://127.0.0.1:8019/openapi/bleOpenSerial", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "timeout": 15000
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleOpenSerial", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleOpenSerial" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","timeout":15000}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"timeout\":15000}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleOpenSerial", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » timeout | body | number | 是 | 串口通信超时时间 单位是毫秒 默认是15秒 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 设置通信方式

`POST /openapi/bleSetSendCmdType`

设置与开发板的通信方式：串口或网络，默认串口。适配 **EC iOS USB 9.20.0+**。

参数：`tt` 为 `1`（串口）或 `2`（网络）。

**请求 Body**

```json
{

    "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",

    "tt": 2
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "tt": 2
}
r = requests.post("http://127.0.0.1:8019/openapi/bleSetSendCmdType", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "tt": 2
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleSetSendCmdType", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleSetSendCmdType" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","tt":2}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"tt\":2}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleSetSendCmdType", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » tt | body | number | 是 | 1 串口 2 网络 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 设置网络信息

`POST /openapi/bleSetWifiInfo`

为开发板配置 Wi-Fi，便于联网。适配 **EC iOS USB 9.20.0+**。

参数：`name` 为 Wi-Fi 名称，`pwd` 为密码。

**请求 Body**

```json
{
    "deviceId": "00008030-001529902ED0402E",
    "name": "mynet",
    "pwd": "kof.2156.com"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "00008030-001529902ED0402E",
  "name": "mynet",
  "pwd": "kof.2156.com"
}
r = requests.post("http://127.0.0.1:8019/openapi/bleSetWifiInfo", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E",
  "name": "mynet",
  "pwd": "kof.2156.com"
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleSetWifiInfo", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleSetWifiInfo" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E","name":"mynet","pwd":"kof.2156.com"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\",\"name\":\"mynet\",\"pwd\":\"kof.2156.com\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleSetWifiInfo", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » name | body | string | 是 | WiFi名称 | none |
| » pwd | body | string | 是 | WiFi 密码 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 重启开发板

`POST /openapi/bleResetBle`

重启开发板（相当于按下 RST）。适配 **EC iOS USB 9.20.0+**。

**请求 Body**

```json
{
    "deviceId": "00008030-001529902ED0402E"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "00008030-001529902ED0402E"
}
r = requests.post("http://127.0.0.1:8019/openapi/bleResetBle", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleResetBle", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleResetBle" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleResetBle", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 关闭串口通信

`POST /openapi/bleCloseSerial`

关闭与开发板的串口通信。适配 **EC iOS USB 9.20.0+**。

**请求 Body**

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
r = requests.post("http://127.0.0.1:8019/openapi/bleCloseSerial", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleCloseSerial", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleCloseSerial" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleCloseSerial", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 移动鼠标

`POST /openapi/bleMouseMove`

仅移动鼠标指针到指定坐标，不触发按下。适配 **EC iOS USB 9.20.0+**。

参数：`x` / `y` 为坐标。

**请求 Body**

```json
{
  "deviceId": "00008030-001529902ED0402E",
    "x": 166,
    "y": 193
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "00008030-001529902ED0402E",
  "x": 166,
  "y": 193
}
r = requests.post("http://127.0.0.1:8019/openapi/bleMouseMove", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E",
  "x": 166,
  "y": 193
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleMouseMove", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleMouseMove" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E","x":166,"y":193}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\",\"x\":166,\"y\":193}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleMouseMove", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » x | body | integer | 是 | x坐标 | none |
| » y | body | integer | 是 | y坐标 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 鼠标归零

`POST /openapi/bleResetZero`

将鼠标移回屏幕右上角原点 `(0, 0)`。适配 **EC iOS USB 9.20.0+**。

**请求 Body**

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
r = requests.post("http://127.0.0.1:8019/openapi/bleResetZero", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleResetZero", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleResetZero" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleResetZero", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 开关软键盘

`POST /openapi/bleToggleSoftKeyboard`

尝试开关软键盘。部分旧机型（如 iPhone 7）在蓝牙连接后输入框无法弹出软键盘，可配合脱机主程序输入时使用；较新机型通常不需要。若不使用脱机主程序作为输入法，可忽略本接口。适配 **EC iOS USB 9.20.0+**。

**请求 Body**

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
r = requests.post("http://127.0.0.1:8019/openapi/bleToggleSoftKeyboard", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleToggleSoftKeyboard", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleToggleSoftKeyboard" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleToggleSoftKeyboard", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 按下坐标点

`POST /openapi/bleTouchDown`

在指定坐标按下。适配 **EC iOS USB 9.20.0+**。

参数：`x` / `y` 为坐标。

**请求 Body**

```json
{
    "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "x": 200,
    "y": 300
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 300
}
r = requests.post("http://127.0.0.1:8019/openapi/bleTouchDown", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 300
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleTouchDown", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleTouchDown" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","x":200,"y":300}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"x\":200,\"y\":300}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleTouchDown", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » x | body | integer | 是 | x坐标 | none |
| » y | body | integer | 是 | y坐标 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 移动坐标点

`POST /openapi/bleTouchMove`

在按住状态下移动到指定坐标。适配 **EC iOS USB 9.20.0+**。

参数：`x` / `y` 为坐标。

**请求 Body**

```json
{
    "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "x": 300,
    "y": 400
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 300,
  "y": 400
}
r = requests.post("http://127.0.0.1:8019/openapi/bleTouchMove", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 300,
  "y": 400
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleTouchMove", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleTouchMove" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","x":300,"y":400}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"x\":300,\"y\":400}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleTouchMove", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » x | body | integer | 是 | x坐标 | none |
| » y | body | integer | 是 | y坐标 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 抬起坐标点

`POST /openapi/bleTouchUp`

在指定坐标抬起。适配 **EC iOS USB 9.20.0+**。

参数：`x` / `y` 为坐标。

**请求 Body**

```json
{
    "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "x": 200,
    "y": 300
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 300
}
r = requests.post("http://127.0.0.1:8019/openapi/bleTouchUp", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 200,
  "y": 300
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleTouchUp", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleTouchUp" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","x":200,"y":300}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"x\":200,\"y\":300}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleTouchUp", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » x | body | integer | 是 | x坐标 | none |
| » y | body | integer | 是 | y坐标 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 长按坐标

`POST /openapi/blePress`

长按指定坐标。适配 **EC iOS USB 9.20.0+**。

参数：`x` / `y` 为坐标，`delay` 为长按时长（毫秒）。

**请求 Body**

```json
{
    "deviceId": "00008030-001529902ED0402E",
    "x": 300,
    "y": 400,
    "delay": 5000
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "00008030-001529902ED0402E",
  "x": 300,
  "y": 400,
  "delay": 5000
}
r = requests.post("http://127.0.0.1:8019/openapi/blePress", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E",
  "x": 300,
  "y": 400,
  "delay": 5000
};
const res = await fetch("http://127.0.0.1:8019/openapi/blePress", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/blePress" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E","x":300,"y":400,"delay":5000}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\",\"x\":300,\"y\":400,\"delay\":5000}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/blePress", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » x | body | integer | 是 | x坐标 | none |
| » y | body | integer | 是 | y坐标 | none |
| » delay | body | integer | 是 | 长按时间 毫秒 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 双击坐标

`POST /openapi/bleDoubleClickPoint`

双击指定坐标。适配 **EC iOS USB 9.20.0+**。

参数：`x` / `y` 为坐标。

**请求 Body**

```json
{
    "deviceId": "00008030-001529902ED0402E",
    "x": 494,
    "y": 671
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "00008030-001529902ED0402E",
  "x": 494,
  "y": 671
}
r = requests.post("http://127.0.0.1:8019/openapi/bleDoubleClickPoint", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E",
  "x": 494,
  "y": 671
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleDoubleClickPoint", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleDoubleClickPoint" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E","x":494,"y":671}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\",\"x\":494,\"y\":671}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleDoubleClickPoint", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » x | body | integer | 是 | x坐标 | none |
| » y | body | integer | 是 | y坐标 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 滑动

`POST /openapi/bleSwipeToPoint`

从起点滑动到终点。适配 **EC iOS USB 9.20.0+**。

参数：`startX` / `startY` 为起点，`endX` / `endY` 为终点，`duration` 为持续时长（毫秒）。

**请求 Body**

```json
{
    "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "x": 94,
    "y": 67,
    "ex": 264,
    "ey": 265,
    "duration": 1000
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 94,
  "y": 67,
  "ex": 264,
  "ey": 265,
  "duration": 1000
}
r = requests.post("http://127.0.0.1:8019/openapi/bleSwipeToPoint", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "x": 94,
  "y": 67,
  "ex": 264,
  "ey": 265,
  "duration": 1000
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleSwipeToPoint", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleSwipeToPoint" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","x":94,"y":67,"ex":264,"ey":265,"duration":1000}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"x\":94,\"y\":67,\"ex\":264,\"ey\":265,\"duration\":1000}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleSwipeToPoint", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » x | body | integer | 是 | x坐标 | none |
| » y | body | integer | 是 | y坐标 | none |
| » ex | body | integer | 是 | 结束x坐标 | none |
| » ey | body | integer | 是 | 结束y坐标 | none |
| » duration | body | integer | 否 | 持续时长 单位毫秒 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 系统按键

`POST /openapi/bleSystemKey`

触发系统按键。适配 **EC iOS USB 9.20.0+**。

参数：`key` 目前支持 `home`、`recents`（最近任务）。

**请求 Body**

```json
{
    "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "key": "recents"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "key": "recents"
}
r = requests.post("http://127.0.0.1:8019/openapi/bleSystemKey", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "key": "recents"
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleSystemKey", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleSystemKey" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","key":"recents"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"key\":\"recents\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleSystemKey", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » key | body | string | 否 | 目前有 home | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 按键

`POST /openapi/bleKeyPress`

按键（ASCII 码）。适配 **EC iOS USB 9.20.0+**。

参数：`prefix` 为可选组合键（`alt` / `ctrl` / `gui`（Win 或 Command）/ `r_ctrl` / `r_shift` / `shift`，可空）；`code` 为整型 ASCII 码（如 `65`），可参考 [ASCII 对照表](https://tool.oschina.net/commons?type=4)。

**请求 Body**

```json
{
   "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "prefix": "",
    "code": 9
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "prefix": "",
  "code": 9
}
r = requests.post("http://127.0.0.1:8019/openapi/bleKeyPress", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "prefix": "",
  "code": 9
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleKeyPress", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleKeyPress" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","prefix":"","code":9}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"prefix\":\"\",\"code\":9}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleKeyPress", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » prefix | body | string¦null | 否 | 组合键，可以为空 alt=alt按键，ctrl=CTRL按键,gui=win或者command键，r\_ctrl=右侧CTRL键，r\_shift=右侧shift键，shift=shift键 | none |
| » code | body | integer | 是 |  | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 字符按键

`POST /openapi/bleKeyPressChar`

按字符键。适配 **EC iOS USB 9.20.0+**。

参数：`prefix` 为可选组合键（同「按键」接口）；`code` 为字符，例如 `a`。

**请求 Body**

```json
{
    "deviceId": "00008030-001529902ED0402E",
    "prefix": "",
    "code": "4"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "00008030-001529902ED0402E",
  "prefix": "",
  "code": "4"
}
r = requests.post("http://127.0.0.1:8019/openapi/bleKeyPressChar", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E",
  "prefix": "",
  "code": "4"
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleKeyPressChar", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleKeyPressChar" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E","prefix":"","code":"4"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\",\"prefix\":\"\",\"code\":\"4\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleKeyPressChar", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » prefix | body | string¦null | 否 | 组合键，可以为空 alt=alt按键，ctrl=CTRL按键,gui=win或者command键，r\_ctrl=右侧CTRL键，r\_shift=右侧shift键，shift=shift键 | none |
| » code | body | string | 是 | 字符，例如 a | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

## 多点触摸

`POST /openapi/bleMultiTouch`

多点触摸。适配 **EC iOS USB 9.20.0+**。

触摸点字段：

- `action`：一般 `0` 按下、`1` 抬起、`2` 移动
- `x` / `y`：坐标
- `pointer`：第几个手指（`1`、`2`、`3`…）
- `delay`：该动作延迟执行的毫秒数

参数：`touch1` 等为触摸点数组；`timeout` 为整段多点触摸超时（毫秒）。

**请求 Body**

```json
{
    "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "finger1": [
        {
            "action": 0,
            "x": 500,
            "y": 200,
            "pointer": 1,
            "delay": 1
        },
        {
            "action": 2,
            "x": 500,
            "y": 200,
            "pointer": 1,
            "delay": 20
        },
        {
            "action": 2,
            "x": 500,
            "y": 300,
            "pointer": 1,
            "delay": 20
        }
    ],
    "delay": 10000
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "finger1": [
    {
      "action": 0,
      "x": 500,
      "y": 200,
      "pointer": 1,
      "delay": 1
    },
    {
      "action": 2,
      "x": 500,
      "y": 200,
      "pointer": 1,
      "delay": 20
    },
    {
      "action": 2,
      "x": 500,
      "y": 300,
      "pointer": 1,
      "delay": 20
    }
  ],
  "delay": 10000
}
r = requests.post("http://127.0.0.1:8019/openapi/bleMultiTouch", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "finger1": [
    {
      "action": 0,
      "x": 500,
      "y": 200,
      "pointer": 1,
      "delay": 1
    },
    {
      "action": 2,
      "x": 500,
      "y": 200,
      "pointer": 1,
      "delay": 20
    },
    {
      "action": 2,
      "x": 500,
      "y": 300,
      "pointer": 1,
      "delay": 20
    }
  ],
  "delay": 10000
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleMultiTouch", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleMultiTouch" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","finger1":[{"action":0,"x":500,"y":200,"pointer":1,"delay":1},{"action":2,"x":500,"y":200,"pointer":1,"delay":20},{"action":2,"x":500,"y":300,"pointer":1,"delay":20}],"delay":10000}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"finger1\":[{\"action\":0,\"x\":500,\"y\":200,\"pointer\":1,\"delay\":1},{\"action\":2,\"x\":500,\"y\":200,\"pointer\":1,\"delay\":20},{\"action\":2,\"x\":500,\"y\":300,\"pointer\":1,\"delay\":20}],\"delay\":10000}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleMultiTouch", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » finger1 | body | [object] | 是 |  | 手指1 |
| »» action | body | integer | 是 |  | 按下为0，弹起为1，移动为2 |
| »» x | body | integer | 是 |  | X坐标 |
| »» y | body | integer | 是 |  | Y坐标 |
| »» pointer | body | integer | 是 |  | 设置第几个手指触摸点，分别是 1，2，3等，代表第n个手指 |
| »» delay | body | integer | 是 |  | 该动作延迟多少毫秒执行，这个是值当前这个事件先延迟后执行 |
| » delay | body | integer | 是 | 这个没用，为了适配 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | true | none | 0是成功 | 0 成功 其他失败 |
| » msg | string | true | none | 错误信息 | 错误消息 |

## 设置步伐大小

`POST /openapi/bleSetStep`

设置鼠标移动步伐：每一步的最大步进值，值越大移动越快。适配 **EC iOS USB 9.20.0+**。

参数：`step` 建议范围 `10`–`120`，默认约 `100`。

**请求 Body**

```json
{
    "deviceId": "00008030-001529902ED0402E",
    "step": 50
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "00008030-001529902ED0402E",
  "step": 50
}
r = requests.post("http://127.0.0.1:8019/openapi/bleSetStep", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E",
  "step": 50
};
const res = await fetch("http://127.0.0.1:8019/openapi/bleSetStep", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/bleSetStep" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E","step":50}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\",\"step\":50}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/bleSetStep", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 |  | none |
| » step | body | number | 是 | 步伐大小，建议 10–120，默认约 100 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string"
}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | false | none | 0是成功 其他是失败 | none |
| » msg | string | true | none | 失败情况下的错误消息 | none |

