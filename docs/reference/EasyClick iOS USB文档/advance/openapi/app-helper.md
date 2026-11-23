<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapi/app-helper/ -->

# 辅助助手接口

## 说明

本页由企业版 OpenAPI 文档整理，模块：**辅助助手接口**。

> **提示**
>
> - 请求方式：HTTP `POST`（个别快捷指令接口为 GET，见正文）
> - URL 前缀：中控地址，例如 `http://127.0.0.1:8019`
> - `Content-Type: application/json`
> - 通用返回：`code` 为 `0` 表示成功，其它为失败，`msg` 为错误信息
> - 返回总览：[企业版开放接口文档](../openapiintro.md)

## 设置辅助助手参数

`POST /openapi/appHelper/setParam`

支持EC iOS中控 6.1.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "bundleIdPrefix": "com.example.helper",
    "appHelperPort": 8199
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleIdPrefix": "com.example.helper",
  "appHelperPort": 8199
}
r = requests.post("http://127.0.0.1:8019/openapi/appHelper/setParam", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleIdPrefix": "com.example.helper",
  "appHelperPort": 8199
};
const res = await fetch("http://127.0.0.1:8019/openapi/appHelper/setParam", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/appHelper/setParam" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","bundleIdPrefix":"com.example.helper","appHelperPort":8199}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"bundleIdPrefix\":\"com.example.helper\",\"appHelperPort\":8199}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/appHelper/setParam", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » bundleIdPrefix | body | string | 是 |  | 助手app的bundleId，多个用英文都好隔开 |
| » appHelperPort | body | integer | 是 |  | 辅助app的运行端口 |

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
| » data | boolean | true | none |  | none |
| » msg | string | true | none |  | 错误消息 |

## 设置剪切板

`POST /openapi/appHelper/setPasteboard`

支持EC iOS中控 6.0.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "content": "hello",
    "openApp": true
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "content": "hello",
  "openApp": True
}
r = requests.post("http://127.0.0.1:8019/openapi/appHelper/setPasteboard", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "content": "hello",
  "openApp": true
};
const res = await fetch("http://127.0.0.1:8019/openapi/appHelper/setPasteboard", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/appHelper/setPasteboard" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","content":"hello","openApp":true}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"content\":\"hello\",\"openApp\":true}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/appHelper/setPasteboard", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » content | body | string | 是 |  | 要粘贴的内容 |
| » openApp | body | boolean | 是 |  | 运行的时候是否打开辅助助手app |

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
| » data | integer | true | none |  | none |
| » msg | string | true | none |  | 错误消息 |

## 读取剪切板

`POST /openapi/appHelper/getPasteboard`

支持EC iOS中控 6.1.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "openApp": true
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "openApp": True
}
r = requests.post("http://127.0.0.1:8019/openapi/appHelper/getPasteboard", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "openApp": true
};
const res = await fetch("http://127.0.0.1:8019/openapi/appHelper/getPasteboard", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/appHelper/getPasteboard" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","openApp":true}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"openApp\":true}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/appHelper/getPasteboard", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » openApp | body | boolean | 是 |  | 运行的时候是否打开辅助助手app |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": "11111",
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
| » data | string | true | none |  | 剪切板的数据 |
| » msg | string | true | none |  | 错误消息 |

## 打开URL

`POST /openapi/appHelper/openUrl`

支持EC iOS中控 6.1.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "openApp": true,
    "url": "https://www.baidu.com"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "openApp": True,
  "url": "https://www.baidu.com"
}
r = requests.post("http://127.0.0.1:8019/openapi/appHelper/openUrl", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "openApp": true,
  "url": "https://www.baidu.com"
};
const res = await fetch("http://127.0.0.1:8019/openapi/appHelper/openUrl", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/appHelper/openUrl" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","openApp":true,"url":"https://www.baidu.com"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"openApp\":true,\"url\":\"https://www.baidu.com\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/appHelper/openUrl", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » openApp | body | boolean | 是 |  | 运行的时候是否打开辅助助手app |
| » url | body | string | 是 |  | 网址 |

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
| » data | integer | true | none |  | none |
| » msg | string | true | none |  | 错误消息 |

## 上传到相册

`POST /openapi/appHelper/uploadToAlbum`

支持EC iOS中控 6.1.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "openApp": true,
    "path": "C:/a.png"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "openApp": True,
  "path": "C:/a.png"
}
r = requests.post("http://127.0.0.1:8019/openapi/appHelper/uploadToAlbum", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "openApp": true,
  "path": "C:/a.png"
};
const res = await fetch("http://127.0.0.1:8019/openapi/appHelper/uploadToAlbum", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/appHelper/uploadToAlbum" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","openApp":true,"path":"C:/a.png"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"openApp\":true,\"path\":\"C:/a.png\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/appHelper/uploadToAlbum", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » openApp | body | boolean | 是 |  | 运行的时候是否打开辅助助手app |
| » path | body | string | 是 |  | 图片或者视频地址 在电脑上的路径 |

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
| » data | integer | true | none |  | 方向 1 正常的竖屏，2 向右旋转90度(顺时针) |
| » msg | string | true | none |  | 错误消息 |

## 开启端口转发

`POST /openapi/appHelper/startForwardPort`

支持EC iOS中控 6.1.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "openApp": true,
    "remotePort": 8080
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "openApp": True,
  "remotePort": 8080
}
r = requests.post("http://127.0.0.1:8019/openapi/appHelper/startForwardPort", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "openApp": true,
  "remotePort": 8080
};
const res = await fetch("http://127.0.0.1:8019/openapi/appHelper/startForwardPort", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/appHelper/startForwardPort" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","openApp":true,"remotePort":8080}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"openApp\":true,\"remotePort\":8080}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/appHelper/startForwardPort", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » openApp | body | boolean | 是 |  | 运行的时候是否打开辅助助手app |
| » remotePort | body | integer | 是 |  | 远程端口 |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": 51947,
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
| » data | integer | true | none |  | 远程端口 映射到本机的端口地址 |
| » msg | string | true | none |  | 错误消息 |

## 关闭端口转发

`POST /openapi/appHelper/closeForwardPort`

支持EC iOS中控 6.1.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "localPort": 51947
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "localPort": 51947
}
r = requests.post("http://127.0.0.1:8019/openapi/appHelper/closeForwardPort", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "localPort": 51947
};
const res = await fetch("http://127.0.0.1:8019/openapi/appHelper/closeForwardPort", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/appHelper/closeForwardPort" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","localPort":51947}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"localPort\":51947}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/appHelper/closeForwardPort", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » localPort | body | integer | 是 |  | 本机的端口 |

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
| » data | integer | true | none |  | 成功或者失败 |
| » msg | string | true | none |  | 错误消息 |

## 关闭所有端口转发

`POST /openapi/appHelper/closeAllForwardPort`

支持EC iOS中控 6.1.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
}
r = requests.post("http://127.0.0.1:8019/openapi/appHelper/closeAllForwardPort", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/appHelper/closeAllForwardPort", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/appHelper/closeAllForwardPort" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/appHelper/closeAllForwardPort", content);
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
| » data | integer | true | none |  | 方向 1 正常的竖屏，2 向右旋转90度(顺时针) |
| » msg | string | true | none |  | 错误消息 |

