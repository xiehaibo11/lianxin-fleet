<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapi/ime/ -->

# IME 输入法接口

## 说明

本页由企业版 OpenAPI 文档整理，模块：**自动化接口/IME输入法**。

> **提示**
>
> - 请求方式：HTTP `POST`（个别快捷指令接口为 GET，见正文）
> - URL 前缀：中控地址，例如 `http://127.0.0.1:8019`
> - `Content-Type: application/json`
> - 通用返回：`code` 为 `0` 表示成功，其它为失败，`msg` 为错误信息
> - 返回总览：[企业版开放接口文档](../openapiintro.md)

## 输入法是否就绪

`POST /openapi/imeOk`

查询自定义输入法是否可用。适配 **EC iOS USB 6.37.0+**。

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
r = requests.post("http://127.0.0.1:8019/openapi/imeOk", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imeOk", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imeOk" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imeOk", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |

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
| » code | integer | true | none | 0成功 | none |
| » data | boolean | true | none | true等于准备好了 false是未准备好 | none |

## 输入字符

`POST /openapi/imeInput`

向当前输入框输入字符串。适配 **EC iOS USB 6.37.0+**。

参数：`content` 为要输入的文本。返回中若无有效数据表示输入失败，有数据表示已输入内容。

**请求 Body**

```json
{
        "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "content": "我是输入的数据"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "content": "我是输入的数据"
}
r = requests.post("http://127.0.0.1:8019/openapi/imeInput", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d",
  "content": "我是输入的数据"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imeInput", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imeInput" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d","content":"我是输入的数据"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"content\":\"我是输入的数据\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imeInput", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |
| » content | body | string | 是 | 要输入的字符串 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": "string"
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
| » code | integer | true | none | 0=成功 | none |
| » data | string | true | none | 如果为空，代表输入不成功，如果不为空，代表输入的数据 | none |

## 粘贴数据

`POST /openapi/imePaste`

先写入剪切板再粘贴到输入框。适配 **EC iOS USB 6.37.0+**。

参数：`content` 为字符串；为空时直接使用当前剪切板内容。返回无有效数据表示失败。

**请求 Body**

```json
{
    "deviceId": "00008030-001529902ED0402E",
    "content": "我是输入的数据2"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "00008030-001529902ED0402E",
  "content": "我是输入的数据2"
}
r = requests.post("http://127.0.0.1:8019/openapi/imePaste", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E",
  "content": "我是输入的数据2"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imePaste", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imePaste" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E","content":"我是输入的数据2"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\",\"content\":\"我是输入的数据2\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imePaste", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |
| » content | body | string | 是 | 要输入的字符串 | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": "string"
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
| » code | integer | true | none | 0=成功 | none |
| » data | string | true | none | 如果为空，代表输入不成功，如果不为空，代表输入的数据 | none |

## 删除数据

`POST /openapi/imePressDel`

删除输入框中的字符。适配 **EC iOS USB 6.37.0+**。

返回无数据表示输入框已无内容；有数据表示删除后的剩余文本。

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
r = requests.post("http://127.0.0.1:8019/openapi/imePressDel", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imePressDel", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imePressDel" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imePressDel", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": "string"
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
| » code | integer | true | none | 0=成功 | none |
| » data | string | true | none | 如果为空，代表输入框无数据，如果不为空，代表输入框剩余数据 | none |

## 回车键

`POST /openapi/imePressEnter`

发送回车键。适配 **EC iOS USB 6.37.0+**。

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
r = requests.post("http://127.0.0.1:8019/openapi/imePressEnter", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imePressEnter", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imePressEnter" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imePressEnter", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |

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
| » code | integer | true | none | 0=成功 | none |
| » data | boolean | true | none | true 代表成功 false 代表失败 | none |

## 复制输入框的数据到剪切板

`POST /openapi/imeCopyToClipboard`

将输入框内容复制到剪切板。适配 **EC iOS USB 6.37.0+**。

返回无数据表示输入框为空；有数据表示已复制的内容（输入框剩余文本）。

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
r = requests.post("http://127.0.0.1:8019/openapi/imeCopyToClipboard", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imeCopyToClipboard", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imeCopyToClipboard" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imeCopyToClipboard", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": "string"
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
| » code | integer | true | none | 0=成功 | none |
| » data | string | true | none | 如果为空，代表输入框无数据，如果不为空，代表输入框剩余数据，并且已经复制到剪切板了 | none |

## 获取输入框数据

`POST /openapi/imeGetText`

获取输入框当前文本。适配 **EC iOS USB 6.37.0+**。

返回空表示无数据或未获取到。

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
r = requests.post("http://127.0.0.1:8019/openapi/imeGetText", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imeGetText", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imeGetText" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imeGetText", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": "string"
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
| » code | integer | true | none | 0=成功 | none |
| » data | string | true | none | 空代表无数据或者未获取到，有数据代表获取到了 | none |

## 读取剪切板的数据

`POST /openapi/imeGetClipboard`

读取剪切板内容。适配 **EC iOS USB 6.37.0+**。

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
r = requests.post("http://127.0.0.1:8019/openapi/imeGetClipboard", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imeGetClipboard", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imeGetClipboard" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imeGetClipboard", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": "string"
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
| » code | integer | true | none | 0=成功 | none |
| » data | string | true | none | 空代表无数据或者未获取到，有数据代表获取到了 | none |

## 设置剪切板数据

`POST /openapi/imeSetClipboard`

设置剪切板内容。适配 **EC iOS USB 6.37.0+**。

参数：`content` 为字符串；`type` 为 `1`（普通文本）或 `2`（URL）。

**请求 Body**

```json
{
    "deviceId": "00008030-001529902ED0402E",
    "type": "1",
    "content": "我是输入的dfdsfs数据2"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "00008030-001529902ED0402E",
  "type": "1",
  "content": "我是输入的dfdsfs数据2"
}
r = requests.post("http://127.0.0.1:8019/openapi/imeSetClipboard", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E",
  "type": "1",
  "content": "我是输入的dfdsfs数据2"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imeSetClipboard", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imeSetClipboard" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E","type":"1","content":"我是输入的dfdsfs数据2"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\",\"type\":\"1\",\"content\":\"我是输入的dfdsfs数据2\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imeSetClipboard", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |
| » type | body | string | 是 | 1 代表是普通的字符串，2 代表是URL数据 | none |
| » content | body | string | 是 | 要输入的字符串 | none |

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
| » code | integer | true | none | 0=成功 | none |
| » data | boolean | true | none | true 代表设置成功 false 代表失败 | none |

## 打开URL链接

`POST /openapi/imeOpenUrl`

打开 URL。适配 **EC iOS USB 6.37.0+**。

参数：`url` 为链接地址，例如 `http://baidu.com`。

**请求 Body**

```json
{
    "deviceId": "00008030-001529902ED0402E",
    "url": "https://baidu.com"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "00008030-001529902ED0402E",
  "url": "https://baidu.com"
}
r = requests.post("http://127.0.0.1:8019/openapi/imeOpenUrl", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E",
  "url": "https://baidu.com"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imeOpenUrl", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imeOpenUrl" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E","url":"https://baidu.com"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\",\"url\":\"https://baidu.com\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imeOpenUrl", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |
| » url | body | string | 是 | url地址,例如 <http://baidu.com> 之类的数据 | none |

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
| » code | integer | true | none | 0=成功 | none |
| » data | boolean | true | none | true 代表设置成功 false 代表失败 | none |

## 清空输入框的内容

`POST /openapi/imeRemoveAllContent`

清空输入框内容。适配 **EC iOS USB 6.37.0+**。

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
r = requests.post("http://127.0.0.1:8019/openapi/imeRemoveAllContent", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imeRemoveAllContent", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imeRemoveAllContent" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imeRemoveAllContent", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |

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
| » code | integer | true | none | 0=成功 | none |
| » data | boolean | true | none | true 代表设置成功 false 代表失败 | none |

## 切换到其他键盘

`POST /openapi/imeChangeKeyboard`

切换到其他键盘。接口返回后约等待 2 秒再完成切换。适配 **EC iOS USB 6.37.0+**。

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
r = requests.post("http://127.0.0.1:8019/openapi/imeChangeKeyboard", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imeChangeKeyboard", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imeChangeKeyboard" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imeChangeKeyboard", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |

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
| » code | integer | true | none | 0=成功 | none |
| » data | boolean | true | none | true 代表设置成功 false 代表失败 | none |

## 隐藏键盘

`POST /openapi/imeDismiss`

隐藏键盘。适配 **EC iOS USB 6.37.0+**。

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
r = requests.post("http://127.0.0.1:8019/openapi/imeDismiss", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "00008030-001529902ED0402E"
};
const res = await fetch("http://127.0.0.1:8019/openapi/imeDismiss", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/imeDismiss" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"00008030-001529902ED0402E"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"00008030-001529902ED0402E\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/imeDismiss", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |

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
| » code | integer | true | none | 0=成功 | none |
| » data | boolean | true | none | true 代表设置成功 false 代表失败 | none |

## 开启IME服务转发-不需要开代理自动化

`POST /openapi/forwardImeServer`

开启 IME 服务转发，可不必再开代理自动化；若走转发则需开启代理服务转发。适配 **EC iOS USB 9.20.0+**。

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
r = requests.post("http://127.0.0.1:8019/openapi/forwardImeServer", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/forwardImeServer", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/forwardImeServer" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/forwardImeServer", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |

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
| » code | integer | true | none | 0成功 | none |
| » msg | string | true | none | 错误信息 | none |

## 关闭IME服务转发-不需要开代理自动化

`POST /openapi/closeForwardImeServer`

关闭 IME 服务转发。适配 **EC iOS USB 9.20.0+**。

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
r = requests.post("http://127.0.0.1:8019/openapi/closeForwardImeServer", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/closeForwardImeServer", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/closeForwardImeServer" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/closeForwardImeServer", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » deviceId | body | string | 是 | 设备ID | none |

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
| » code | integer | true | none | 0成功 | none |
| » data | boolean | true | none | true等于准备好了 false是未准备好 | none |

