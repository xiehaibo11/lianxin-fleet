<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapi/files/ -->

# 文件操作接口

## 说明

本页由企业版 OpenAPI 文档整理，模块：**自动化接口/其他**。

> **提示**
>
> - 请求方式：HTTP `POST`（个别快捷指令接口为 GET，见正文）
> - URL 前缀：中控地址，例如 `http://127.0.0.1:8019`
> - `Content-Type: application/json`
> - 通用返回：`code` 为 `0` 表示成功，其它为失败，`msg` 为错误信息
> - 返回总览：[企业版开放接口文档](../openapiintro.md)

## iOS设备文件操作

`POST /openapi/fsyncFileOpr`

支持EC iOS中控 7.2.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "srcPath": "/",
    "action": "list"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "srcPath": "/",
  "action": "list"
}
r = requests.post("http://127.0.0.1:8019/openapi/fsyncFileOpr", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "srcPath": "/",
  "action": "list"
};
const res = await fetch("http://127.0.0.1:8019/openapi/fsyncFileOpr", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/fsyncFileOpr" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","srcPath":"/","action":"list"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"srcPath\":\"/\",\"action\":\"list\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/fsyncFileOpr", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » srcPath | body | string | 是 |  | ios设备中文件路径 |
| » action | body | string | 是 |  | 动作类型 分别有 list rm mkdir |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": [
    "string"
  ],
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
| » code | integer | true | none |  | 0代表正确 |
| » data | [string] | true | none |  | none |
| » msg | string | true | none | 错误消息 | none |

## iOS设备文件推送和拉取

`POST /openapi/fsyncPullPush`

支持EC iOS中控 7.2.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "srcPath": "/",
    "action": "pull",
    "destPath": "C:/a.png"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "srcPath": "/",
  "action": "pull",
  "destPath": "C:/a.png"
}
r = requests.post("http://127.0.0.1:8019/openapi/fsyncPullPush", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "srcPath": "/",
  "action": "pull",
  "destPath": "C:/a.png"
};
const res = await fetch("http://127.0.0.1:8019/openapi/fsyncPullPush", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/fsyncPullPush" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","srcPath":"/","action":"pull","destPath":"C:/a.png"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"srcPath\":\"/\",\"action\":\"pull\",\"destPath\":\"C:/a.png\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/fsyncPullPush", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » srcPath | body | string | 是 |  | 源文件路径，action=push的时候，这个是电脑的文件路径，action= pull 的时候，这个是iOS中的文件路径 |
| » destPath | body | string | 是 |  | 目标文件路径 action= push 的时候，这个是iOS中的文件路径，action= pull 的时候，这个是电脑的文件路径 |
| » action | body | string | 是 |  | 动作，分别有 push = 从电脑推送文件到远程设备中，pull = 从设备中拉取文件到电脑 |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": "string",
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
| » code | integer | true | none |  | 0代表正确 |
| » data | string | true | none |  | none |
| » msg | string | true | none | 错误消息 | none |

