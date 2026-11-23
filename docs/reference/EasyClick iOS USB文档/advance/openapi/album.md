<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapi/album/ -->

# 相册接口

## 说明

本页由企业版 OpenAPI 文档整理，模块：**自动化接口/相册**。

> **提示**
>
> - 请求方式：HTTP `POST`（个别快捷指令接口为 GET，见正文）
> - URL 前缀：中控地址，例如 `http://127.0.0.1:8019`
> - `Content-Type: application/json`
> - 通用返回：`code` 为 `0` 表示成功，其它为失败，`msg` 为错误信息
> - 返回总览：[企业版开放接口文档](../openapiintro.md)

## Agent上传视频到相册

`POST /openapi/uploadInsertVideo`

支持EC iOS中控 6.5.0+，iOS15+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "path": "C:/video.mp4"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "path": "C:/video.mp4"
}
r = requests.post("http://127.0.0.1:8019/openapi/uploadInsertVideo", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "path": "C:/video.mp4"
};
const res = await fetch("http://127.0.0.1:8019/openapi/uploadInsertVideo", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/uploadInsertVideo" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","path":"C:/video.mp4"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"path\":\"C:/video.mp4\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/uploadInsertVideo", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » path | body | string | 是 |  | 视频文件路径 |

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
| » code | integer | true | none |  | none |
| » data | boolean | true | none |  | none |

## Agent上传图片到相册

`POST /openapi/uploadInsertImage`

支持EC iOS中控 6.5.0+，iOS15+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "path": "C:/a.png"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "path": "C:/a.png"
}
r = requests.post("http://127.0.0.1:8019/openapi/uploadInsertImage", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "path": "C:/a.png"
};
const res = await fetch("http://127.0.0.1:8019/openapi/uploadInsertImage", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/uploadInsertImage" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","path":"C:/a.png"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"path\":\"C:/a.png\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/uploadInsertImage", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » path | body | string | 是 |  | 图片文件路径 |

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
| » code | integer | true | none |  | none |
| » data | boolean | true | none |  | none |

## 请求相册权限

`POST /openapi/requestPhotoAuthorization`

对应utils.requestPhotoAuthorization 函数

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
r = requests.post("http://127.0.0.1:8019/openapi/requestPhotoAuthorization", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/requestPhotoAuthorization", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/requestPhotoAuthorization" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/requestPhotoAuthorization", content);
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
| » code | integer | true | none |  | none |
| » data | boolean | true | none |  | none |

## 清空相册中的图片

`POST /openapi/deleteAllPhotos`

对应utils.deleteAllPhotos 函数

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
r = requests.post("http://127.0.0.1:8019/openapi/deleteAllPhotos", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/deleteAllPhotos", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/deleteAllPhotos" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/deleteAllPhotos", content);
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
| » code | integer | true | none |  | none |
| » data | boolean | true | none |  | none |

## 清空相册中的视频

`POST /openapi/deleteAllVideos`

对应utils.deleteAllVideos 函数

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
r = requests.post("http://127.0.0.1:8019/openapi/deleteAllVideos", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/deleteAllVideos", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/deleteAllVideos" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/deleteAllVideos", content);
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
| » code | integer | true | none |  | none |
| » data | boolean | true | none |  | none |

