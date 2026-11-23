<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapi/screenshot/ -->

# 截图接口

## 说明

本页由企业版 OpenAPI 文档整理，模块：**自动化接口/截图**。

> **提示**
>
> - 请求方式：HTTP `POST`（个别快捷指令接口为 GET，见正文）
> - URL 前缀：中控地址，例如 `http://127.0.0.1:8019`
> - `Content-Type: application/json`
> - 通用返回：`code` 为 `0` 表示成功，其它为失败，`msg` 为错误信息
> - 返回总览：[企业版开放接口文档](../openapiintro.md)

## 抓PNG图片-无需开启自动化

`POST /openapi/captureFullScreenNoAutoByte`

EC 中控3.0.0+修改为POST，之前的都是GET

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
r = requests.post("http://127.0.0.1:8019/openapi/captureFullScreenNoAutoByte", json=body, timeout=30)
print(r.status_code, len(r.content))
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/captureFullScreenNoAutoByte", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const buf = await res.arrayBuffer();
console.log(res.status, buf.byteLength);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/captureFullScreenNoAutoByte" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/captureFullScreenNoAutoByte", content);
var bytes = await res.Content.ReadAsByteArrayAsync();
Console.WriteLine($"{(int)res.StatusCode} {bytes.Length}");
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 说明 |
| --- | --- | --- | --- | --- |
| body | body | object | 否 | none |
| » deviceId | body | string | 是 | 设备ID |

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

## 预抓图片

`POST /openapi/startPreCapScreen`

EC 中控3.0.0+修改为POST，之前的都是GET

配合[抓图片流] 和 [关闭图片流接口] 使用
如果不想要开启自动化 ，types参数设置为1

**请求 Body**

```json
{
    "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "delay": 32,
    "quality": 1,
    "types": 10
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "delay": 32,
  "quality": 1,
  "types": 10
}
r = requests.post("http://127.0.0.1:8019/openapi/startPreCapScreen", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "delay": 32,
  "quality": 1,
  "types": 10
};
const res = await fetch("http://127.0.0.1:8019/openapi/startPreCapScreen", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/startPreCapScreen" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","delay":32,"quality":1,"types":10}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"delay\":32,\"quality\":1,\"types\":10}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/startPreCapScreen", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » delay | body | integer | 是 |  | none |
| » quality | body | integer | 是 |  | 图片质量，type=1的时候，支持 1， 50， 100，三种不同的质量标准 |
| » types | body | integer | 是 | 类型 | 10 代表PNG格式，没有任何压缩，不开自动化也能截图 |

#### 详细说明

**» quality**: 图片质量，type=1的时候，支持 1， 50， 100，三种不同的质量标准
当type =2 的时候，支持1-100图片质量

**» types**: 10 代表PNG格式，没有任何压缩，不开自动化也能截图
1 代表截图 jpg格式的方式1
2 代表截图 jpg格式方式2
3 代表png格式，png不支持质量参数 ，根据自己机器情况调用

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

## 重置无权限截图的环境

`POST /openapi/resetCaptureScreenNoAutoEnv`

对应 image.resetCaptureScreenNoAutoEnv 函数

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
r = requests.post("http://127.0.0.1:8019/openapi/resetCaptureScreenNoAutoEnv", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/resetCaptureScreenNoAutoEnv", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/resetCaptureScreenNoAutoEnv" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/resetCaptureScreenNoAutoEnv", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

## 抓取JPG图片

`POST /openapi/captureFullScreenJpg`

EC 中控3.0.0+修改为POST，之前的都是GET

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
r = requests.post("http://127.0.0.1:8019/openapi/captureFullScreenJpg", json=body, timeout=30)
print(r.status_code, len(r.content))
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/captureFullScreenJpg", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const buf = await res.arrayBuffer();
console.log(res.status, buf.byteLength);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/captureFullScreenJpg" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/captureFullScreenJpg", content);
var bytes = await res.Content.ReadAsByteArrayAsync();
Console.WriteLine($"{(int)res.StatusCode} {bytes.Length}");
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

## 抓取JPG图片扩展

`POST /openapi/captureFullScreenByteEx`

EC 中控3.1.0+修改为POST，之前的都是GET

**请求 Body**

```json
{
    "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "type": "1",
    "quality": "50"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "type": "1",
  "quality": "50"
}
r = requests.post("http://127.0.0.1:8019/openapi/captureFullScreenByteEx", json=body, timeout=30)
print(r.status_code, len(r.content))
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "type": "1",
  "quality": "50"
};
const res = await fetch("http://127.0.0.1:8019/openapi/captureFullScreenByteEx", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const buf = await res.arrayBuffer();
console.log(res.status, buf.byteLength);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/captureFullScreenByteEx" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","type":"1","quality":"50"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"type\":\"1\",\"quality\":\"50\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/captureFullScreenByteEx", content);
var bytes = await res.Content.ReadAsByteArrayAsync();
Console.WriteLine($"{(int)res.StatusCode} {bytes.Length}");
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » type | body | string | 是 |  | 抓图模式 分别为1 和 2 |
| » quality | body | string | 是 |  | 抓图质量，type=1的时候支持 1，50，100 |

#### 详细说明

**» quality**: 抓图质量，type=1的时候支持 1，50，100
type=2的时候 支持 1 - 100

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

## 抓取图片流

`POST /openapi/captureScreenStreamByte`

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
r = requests.post("http://127.0.0.1:8019/openapi/captureScreenStreamByte", json=body, timeout=30)
print(r.status_code, len(r.content))
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/captureScreenStreamByte", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const buf = await res.arrayBuffer();
console.log(res.status, buf.byteLength);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/captureScreenStreamByte" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/captureScreenStreamByte", content);
var bytes = await res.Content.ReadAsByteArrayAsync();
Console.WriteLine($"{(int)res.StatusCode} {bytes.Length}");
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

## 抓取PNG图片

`POST /openapi/captureFullScreenPng`

**请求 Body**

```json
{
  "deviceId": "string"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "string"
}
r = requests.post("http://127.0.0.1:8019/openapi/captureFullScreenPng", json=body, timeout=30)
print(r.status_code, len(r.content))
```

**Node.js**

```javascript
const body = {
  "deviceId": "string"
};
const res = await fetch("http://127.0.0.1:8019/openapi/captureFullScreenPng", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const buf = await res.arrayBuffer();
console.log(res.status, buf.byteLength);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/captureFullScreenPng" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"string"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"string\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/captureFullScreenPng", content);
var bytes = await res.Content.ReadAsByteArrayAsync();
Console.WriteLine($"{(int)res.StatusCode} {bytes.Length}");
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

## 关闭图片流

`POST /openapi/stopScreenStream`

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
r = requests.post("http://127.0.0.1:8019/openapi/stopScreenStream", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/stopScreenStream", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/stopScreenStream" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/stopScreenStream", content);
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

## 图片流是否正常

`POST /openapi/isScreenStreamOk`

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
r = requests.post("http://127.0.0.1:8019/openapi/isScreenStreamOk", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/isScreenStreamOk", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/isScreenStreamOk" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/isScreenStreamOk", content);
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

## 开启图片流

`POST /openapi/startScreenStream`

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
r = requests.post("http://127.0.0.1:8019/openapi/startScreenStream", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/startScreenStream", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/startScreenStream" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/startScreenStream", content);
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

