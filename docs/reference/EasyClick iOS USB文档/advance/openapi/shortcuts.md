<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapi/shortcuts/ -->

# 快捷指令助手接口

## 说明

本页由企业版 OpenAPI 文档整理，模块：**快捷指令助手**。

> **提示**
>
> - 请求方式：HTTP `POST`（个别快捷指令接口为 GET，见正文）
> - URL 前缀：中控地址，例如 `http://127.0.0.1:8019`
> - `Content-Type: application/json`
> - 通用返回：`code` 为 `0` 表示成功，其它为失败，`msg` 为错误信息
> - 返回总览：[企业版开放接口文档](../openapiintro.md)

> **警告**
>
> 本模块需配合「快捷指令助手」程序使用，请按正文中的实际路径调用。

## 上传文字到服务

`POST /postText`

这个是需要运行快捷指令助手的程序，用来辅助快捷指令获取文字的
到EC官方网盘。文件夹是--- **iOS资源-iOS快捷指令助手.zip***下载这个文件，然后解压后双击运行

**请求 Body**

```json
{
    "key": "4eb21ec4",
    "content":"我是需要复制的文字"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "key": "4eb21ec4",
  "content": "我是需要复制的文字"
}
r = requests.post("http://127.0.0.1:8019/postText", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "key": "4eb21ec4",
  "content": "我是需要复制的文字"
};
const res = await fetch("http://127.0.0.1:8019/postText", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/postText" \
  -H "Content-Type: application/json" \
  -d '{"key":"4eb21ec4","content":"我是需要复制的文字"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"key\":\"4eb21ec4\",\"content\":\"我是需要复制的文字\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/postText", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » key | body | string | 是 |  | 快捷指令助手中的 key |
| » content | body | string | 是 |  | 要写入的文字内容 |

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

## 获取命令到手机

`GET /cmd`

这个是需要运行快捷指令助手的程序，用来辅助快捷指令获取文字的
到EC官方网盘。文件夹是--- **iOS资源-iOS快捷指令助手.zip***下载这个文件，然后解压后双击运行

### 请求示例

**Python**

```python
import requests

r = requests.get("http://127.0.0.1:8019/cmd?key=4eb21ec4", timeout=30)
print(r.json())
```

**Node.js**

```javascript
const res = await fetch("http://127.0.0.1:8019/cmd?key=4eb21ec4");
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X GET "http://127.0.0.1:8019/cmd?key=4eb21ec4"
```

**C#**

```csharp
using var http = new HttpClient();
using var res = await http.GetAsync("http://127.0.0.1:8019/cmd?key=4eb21ec4");
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| key | query | string | 否 |  | none |

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

## 上传文件服务

`POST /upload`

这个是需要运行快捷指令助手的程序，用来辅助快捷指令获取文字的
到EC官方网盘。文件夹是--- **iOS资源-iOS快捷指令助手.zip***下载这个文件，然后解压后双击运行

**请求 Body**

```yaml
file: file:///Users/wangbx/Downloads/VID_20231206_103138.mp4
key: 4eb21ec4
```

### 请求示例

**Python**

```python
import requests

url = "http://127.0.0.1:8019/upload"
files = {
    "file": open(r"/Users/wangbx/Downloads/VID_20231206_103138.mp4", "rb"),
}
data = {
    "key": "4eb21ec4",
}
r = requests.post(url, files=files, data=data, timeout=30)
print(r.json())
```

**Node.js**

```javascript
import fs from "fs";
import FormData from "form-data";

const form = new FormData();
form.append("file", fs.createReadStream("/Users/wangbx/Downloads/VID_20231206_103138.mp4"));
form.append("key", "4eb21ec4");
const res = await fetch("http://127.0.0.1:8019/upload", { method: "POST", body: form, headers: form.getHeaders() });
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/upload" \
  -F "file=@/Users/wangbx/Downloads/VID_20231206_103138.mp4" \
  -F "key=4eb21ec4"
```

**C#**

```csharp
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
using var form = new MultipartFormDataContent();
await using var stream = File.OpenRead(@"/Users/wangbx/Downloads/VID_20231206_103138.mp4");
form.Add(new StreamContent(stream), "file", Path.GetFileName(stream.Name));
form.Add(new StringContent("4eb21ec4"), "key");
using var res = await http.PostAsync("http://127.0.0.1:8019/upload", form);
Console.WriteLine(await res.Content.ReadAsStringAsync());
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 是 |  | none |
| » file | body | string(binary) | 是 |  | none |
| » key | body | string | 否 |  | none |

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

## 反馈结果到服务

`GET /suc`

这个是需要运行快捷指令助手的程序，用来辅助快捷指令获取文字的
到EC官方网盘。文件夹是--- **iOS资源-iOS快捷指令助手.zip***下载这个文件，然后解压后双击运行

**请求 Body**

```yaml
{}
```

### 请求示例

**Python**

```python
import requests

r = requests.get("http://127.0.0.1:8019/suc?key=4eb21ec4", timeout=30)
print(r.json())
```

**Node.js**

```javascript
const res = await fetch("http://127.0.0.1:8019/suc?key=4eb21ec4");
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X GET "http://127.0.0.1:8019/suc?key=4eb21ec4"
```

**C#**

```csharp
using var http = new HttpClient();
using var res = await http.GetAsync("http://127.0.0.1:8019/suc?key=4eb21ec4");
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| key | query | string | 否 |  | none |
| body | body | object | 是 |  | none |

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

## 获取手机传递的指令结果

`GET /ult`

这个是需要运行快捷指令助手的程序，用来辅助快捷指令获取文字的
到EC官方网盘。文件夹是--- **iOS资源-iOS快捷指令助手.zip***下载这个文件，然后解压后双击运行

### 请求示例

**Python**

```python
import requests

r = requests.get("http://127.0.0.1:8019/ult?key=4eb21ec4", timeout=30)
print(r.json())
```

**Node.js**

```javascript
const res = await fetch("http://127.0.0.1:8019/ult?key=4eb21ec4");
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X GET "http://127.0.0.1:8019/ult?key=4eb21ec4"
```

**C#**

```csharp
using var http = new HttpClient();
using var res = await http.GetAsync("http://127.0.0.1:8019/ult?key=4eb21ec4");
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| key | query | string | 否 |  | none |

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

