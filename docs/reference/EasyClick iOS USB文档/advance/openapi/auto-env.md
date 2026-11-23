<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapi/auto-env/ -->

# 启停自动化接口

## 说明

本页由企业版 OpenAPI 文档整理，模块：**自动化接口/启停自动化**。

> **提示**
>
> - 请求方式：HTTP `POST`（个别快捷指令接口为 GET，见正文）
> - URL 前缀：中控地址，例如 `http://127.0.0.1:8019`
> - `Content-Type: application/json`
> - 通用返回：`code` 为 `0` 表示成功，其它为失败，`msg` 为错误信息
> - 返回总览：[企业版开放接口文档](../openapiintro.md)

## 启动自动化

`POST /openapi/startAutoEnv`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
}
r = requests.post("http://127.0.0.1:8019/openapi/startAutoEnv", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/startAutoEnv", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/startAutoEnv" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/startAutoEnv", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备的ID |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "msg": "string",
  "data": true
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
| » code | integer | true | none |  | 0 成功 其他失败 |
| » msg | string | true | none |  | 错误的消息 |
| » data | boolean | true | none |  | true 代表成功 false 代表失败 |

## 重置USB链接

`POST /openapi/resetUsbConn`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
}
r = requests.post("http://127.0.0.1:8019/openapi/resetUsbConn", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/resetUsbConn", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/resetUsbConn" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/resetUsbConn", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": true,
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
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 成功 false 失败 |
| » msg | string | true | none |  | 错误消息 |

## 释放自动化

`POST /openapi/closeEnv`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
}
r = requests.post("http://127.0.0.1:8019/openapi/closeEnv", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/closeEnv", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/closeEnv" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/closeEnv", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": true,
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
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 代表成功 false代表失败 |
| » msg | string | true | none |  | 错误消息 |

## 获取设备信息

`POST /openapi/getDeviceInfo`

获取设备信息。若缺少所需字段，请改用「获取刷新信息」接口 `/openapi/refreshMsg` 拉取更全量数据。

**请求 Body**

```json
{
    "deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
}
r = requests.post("http://127.0.0.1:8019/openapi/getDeviceInfo", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/getDeviceInfo", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/getDeviceInfo" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/getDeviceInfo", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |

> 返回示例

> 200 Response

```json
{"code":0,"data":{"deviceName":"iPhone7","deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","serialNo":"F4GSPUAZHG6W","deviceAlias":"12","battery":98,"orientation":2,"orientationClick":1,"screenWidth":1334,"screenHeight":750,"productVersion":"15.2","model":"iPhone","charging":true}}
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

状态码 **200**

| 名称 | 类型 | 必选 | 约束 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| » code | integer | true | none |  | none |
| » data | object | true | none |  | none |
| »» deviceName | string | true | none |  | 设备名称 |
| »» deviceId | string | true | none |  | 设备id |
| »» serialNo | string | true | none |  | none |
| »» deviceAlias | string | true | none |  | 设备别名 |
| »» battery | integer | true | none |  | 当前电量 |
| »» orientation | integer | true | none |  | 屏幕实际方向 1竖屏 2横屏 |
| »» orientationClick | integer | true | none |  | 屏幕点击坐标系 1 竖屏 2横屏 |
| »» screenWidth | integer | true | none |  | 屏幕宽度 |
| »» screenHeight | integer | true | none |  | 屏幕高度 |
| »» productVersion | string | true | none |  | 系统版本 |
| »» model | string | true | none |  | 系统的型号 |
| »» charging | boolean | true | none |  | 是否在充电 |

## 获取电量

`POST /openapi/getBattery`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
}
r = requests.post("http://127.0.0.1:8019/openapi/getBattery", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/getBattery", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/getBattery" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/getBattery", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": 0,
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
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | integer | true | none |  | 0 - 100 手机电量 |
| » msg | string | true | none |  | 错误消息 |

## 充电状态

`POST /openapi/isCharging`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
}
r = requests.post("http://127.0.0.1:8019/openapi/isCharging", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/isCharging", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/isCharging" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/isCharging", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": true
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
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 充电 false 未充电 |

## 自动化服务是否正常

`POST /openapi/isServiceOk`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"


}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
}
r = requests.post("http://127.0.0.1:8019/openapi/isServiceOk", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/isServiceOk", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/isServiceOk" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/isServiceOk", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": true,
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
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 成功 false 失败 |
| » msg | string | true | none |  | 错误消息 |

## 代理IPA设置

`POST /openapi/setAgentSetting`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "screenStreamQuality":2,
    "nodeDepth":20,
    "screenStreamFramerate":30
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "screenStreamQuality": 2,
  "nodeDepth": 20,
  "screenStreamFramerate": 30
}
r = requests.post("http://127.0.0.1:8019/openapi/setAgentSetting", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "screenStreamQuality": 2,
  "nodeDepth": 20,
  "screenStreamFramerate": 30
};
const res = await fetch("http://127.0.0.1:8019/openapi/setAgentSetting", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/setAgentSetting" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","screenStreamQuality":2,"nodeDepth":20,"screenStreamFramerate":30}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"screenStreamQuality\":2,\"nodeDepth\":20,\"screenStreamFramerate\":30}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/setAgentSetting", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备id |
| » nodeDepth | body | integer | 是 |  | 获取节点的深度 1 - 100 越大 ，获取时间越长 |
| » screenStreamQuality | body | integer | 是 |  | 投屏质量 1-100 |
| » screenStreamFramerate | body | integer | 是 |  | 投屏帧率 1-60 |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": true,
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
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 成功 false 失败 |
| » msg | string | true | none |  | 错误消息 |

## 关闭隧道

`POST /openapi/closeTunnel`

关闭设备隧道。会关闭此前已建立的隧道，对 **iOS 17+** 有效。适配 **EC iOS USB 9.22.0+**。

**请求 Body**

```json
{
    "deviceId":"00008030-001529902ED0402E"


}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "00008030-001529902ED0402E"
}
r = requests.post("http://127.0.0.1:8019/openapi/closeTunnel", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/closeTunnel", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/closeTunnel" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/closeTunnel", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": true,
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
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 成功 false 失败 |
| » msg | string | true | none |  | 错误消息 |

## 启动隧道

`POST /openapi/startTunnel`

启动设备隧道。会先关闭此前隧道再启动，对 **iOS 17+** 有效。适配 **EC iOS USB 9.22.0+**。

**请求 Body**

```json
{
    "deviceId":"00008030-001529902ED0402E"


}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "00008030-001529902ED0402E"
}
r = requests.post("http://127.0.0.1:8019/openapi/startTunnel", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/startTunnel", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/startTunnel" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/startTunnel", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": true,
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
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 成功 false 失败 |
| » msg | string | true | none |  | 错误消息 |

