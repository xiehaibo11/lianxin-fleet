<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapi/action/ -->

# 动作接口

## 说明

本页由企业版 OpenAPI 文档整理，模块：**自动化接口/动作**。

> **提示**
>
> - 请求方式：HTTP `POST`（个别快捷指令接口为 GET，见正文）
> - URL 前缀：中控地址，例如 `http://127.0.0.1:8019`
> - `Content-Type: application/json`
> - 通用返回：`code` 为 `0` 表示成功，其它为失败，`msg` 为错误信息
> - 返回总览：[企业版开放接口文档](../openapiintro.md)

## 开关小白点

`POST /openapi/setAssistiveTouch`

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "open": false
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "open": False
}
r = requests.post("http://127.0.0.1:8019/openapi/setAssistiveTouch", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "open": false
};
const res = await fetch("http://127.0.0.1:8019/openapi/setAssistiveTouch", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/setAssistiveTouch" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","open":false}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"open\":false}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/setAssistiveTouch", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » open | body | boolean | 是 |  | true 代表打开小白点 false代表关闭小白点 |

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

## 模拟打字输入

`POST /openapi/typingText`

EC IOS 6.1.0

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "content": "hello"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "content": "hello"
}
r = requests.post("http://127.0.0.1:8019/openapi/typingText", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "content": "hello"
};
const res = await fetch("http://127.0.0.1:8019/openapi/typingText", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/typingText" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","content":"hello"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"content\":\"hello\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/typingText", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » content | body | string | 是 |  | 内容 |

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

## 点击坐标

`POST /openapi/clickPoint`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "x":300,
    "y":100
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "x": 300,
  "y": 100
}
r = requests.post("http://127.0.0.1:8019/openapi/clickPoint", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "x": 300,
  "y": 100
};
const res = await fetch("http://127.0.0.1:8019/openapi/clickPoint", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/clickPoint" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","x":300,"y":100}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"x\":300,\"y\":100}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/clickPoint", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备id |
| » x | body | integer | 是 |  | x坐标 |
| » y | body | integer | 是 |  | y坐标 |

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

## 双击坐标

`POST /openapi/doubleClickPoint`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "x":300,
    "y":100
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "x": 300,
  "y": 100
}
r = requests.post("http://127.0.0.1:8019/openapi/doubleClickPoint", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "x": 300,
  "y": 100
};
const res = await fetch("http://127.0.0.1:8019/openapi/doubleClickPoint", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/doubleClickPoint" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","x":300,"y":100}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"x\":300,\"y\":100}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/doubleClickPoint", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备id |
| » x | body | integer | 是 |  | x坐标 |
| » y | body | integer | 是 |  | y坐标 |

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

## 长按坐标

`POST /openapi/longClickPoint`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "x":300,
    "y":100,
    "duration":1000
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "x": 300,
  "y": 100,
  "duration": 1000
}
r = requests.post("http://127.0.0.1:8019/openapi/longClickPoint", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "x": 300,
  "y": 100,
  "duration": 1000
};
const res = await fetch("http://127.0.0.1:8019/openapi/longClickPoint", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/longClickPoint" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","x":300,"y":100,"duration":1000}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"x\":300,\"y\":100,\"duration\":1000}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/longClickPoint", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备id |
| » x | body | integer | 是 |  | x坐标 |
| » y | body | integer | 是 |  | y坐标 |
| » duration | body | integer | 是 |  | 持续时长 单位 毫秒 |

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

## 滑动

`POST /openapi/swipeToPoint`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "startX":300,
    "startY":700,
    "endX":300,
    "endY":10,
    "duration":1000
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "startX": 300,
  "startY": 700,
  "endX": 300,
  "endY": 10,
  "duration": 1000
}
r = requests.post("http://127.0.0.1:8019/openapi/swipeToPoint", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "startX": 300,
  "startY": 700,
  "endX": 300,
  "endY": 10,
  "duration": 1000
};
const res = await fetch("http://127.0.0.1:8019/openapi/swipeToPoint", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/swipeToPoint" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","startX":300,"startY":700,"endX":300,"endY":10,"duration":1000}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"startX\":300,\"startY\":700,\"endX\":300,\"endY\":10,\"duration\":1000}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/swipeToPoint", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » startX | body | integer | 是 |  | 起始X坐标 |
| » startY | body | integer | 是 |  | 起始Y坐标 |
| » endX | body | integer | 是 |  | 终点X坐标 |
| » endY | body | integer | 是 |  | 终点Y坐标 |
| » duration | body | integer | 是 |  | 持续时长 单位毫秒 |

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

## 长按

`POST /openapi/press`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "x":300,
    "y":100,
    "duration":1000
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "x": 300,
  "y": 100,
  "duration": 1000
}
r = requests.post("http://127.0.0.1:8019/openapi/press", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "x": 300,
  "y": 100,
  "duration": 1000
};
const res = await fetch("http://127.0.0.1:8019/openapi/press", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/press" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","x":300,"y":100,"duration":1000}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"x\":300,\"y\":100,\"duration\":1000}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/press", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » x | body | integer | 是 |  | x坐标 |
| » y | body | integer | 是 |  | Y坐标 |
| » duration | body | integer | 是 |  | 持续时长 单位毫秒 |

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

## 多点触摸

`POST /openapi/multiTouch`

**请求 Body**

```json
{
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "finger1": [

    {
      "action": 0,
      "x": 500,
      "y": 200,
      "pointer": 1,
      "delay": 1
    }, {
      "action": 2,
      "x": 500,
      "y": 200,
      "pointer": 1,
      "delay": 20
    }, {
      "action": 2,
      "x": 500,
      "y": 300,
      "pointer": 1,
      "delay": 20
    }
  ],
  "finger2": [

    {
      "action": 0,
      "x": 500,
      "y": 200,
      "pointer": 2,
      "delay": 1
    }, {
      "action": 2,
      "x": 500,
      "y": 200,
      "pointer": 2,
      "delay": 20
    }, {
      "action": 2,
      "x": 500,
      "y": 300,
      "pointer": 2,
      "delay": 20
    }
  ],
  "finger3": [

    {
      "action": 0,
      "x": 200,
      "y": 200,
      "pointer": 3,
      "delay": 1
    }, {
      "action": 2,
      "x": 300,
      "y": 200,
      "pointer": 3,
      "delay": 201
    }, {
      "action": 2,
      "x": 400,
      "y": 300,
      "pointer": 3,
      "delay": 201
    }
  ],

  "finger4": [

    {
      "action": 0,
      "x": 200,
      "y": 200,
      "pointer": 4,
      "delay": 1
    }, {
      "action": 2,
      "x": 300,
      "y": 200,
      "pointer": 4,
      "delay": 201
    }, {
      "action": 2,
      "x": 400,
      "y": 300,
      "pointer": 4,
      "delay": 201
    }
  ],
    "finger5": [

    {
      "action": 0,
      "x": 200,
      "y": 200,
      "pointer": 5,
      "delay": 1
    }, {
      "action": 2,
      "x": 300,
      "y": 200,
      "pointer": 5,
      "delay": 201
    }, {
      "action": 2,
      "x": 400,
      "y": 300,
      "pointer": 5,
      "delay": 201
    }
  ]

}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
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
  "finger2": [
    {
      "action": 0,
      "x": 500,
      "y": 200,
      "pointer": 2,
      "delay": 1
    },
    {
      "action": 2,
      "x": 500,
      "y": 200,
      "pointer": 2,
      "delay": 20
    },
    {
      "action": 2,
      "x": 500,
      "y": 300,
      "pointer": 2,
      "delay": 20
    }
  ],
  "finger3": [
    {
      "action": 0,
      "x": 200,
      "y": 200,
      "pointer": 3,
      "delay": 1
    },
    {
      "action": 2,
      "x": 300,
      "y": 200,
      "pointer": 3,
      "delay": 201
    },
    {
      "action": 2,
      "x": 400,
      "y": 300,
      "pointer": 3,
      "delay": 201
    }
  ],
  "finger4": [
    {
      "action": 0,
      "x": 200,
      "y": 200,
      "pointer": 4,
      "delay": 1
    },
    {
      "action": 2,
      "x": 300,
      "y": 200,
      "pointer": 4,
      "delay": 201
    },
    {
      "action": 2,
      "x": 400,
      "y": 300,
      "pointer": 4,
      "delay": 201
    }
  ],
  "finger5": [
    {
      "action": 0,
      "x": 200,
      "y": 200,
      "pointer": 5,
      "delay": 1
    },
    {
      "action": 2,
      "x": 300,
      "y": 200,
      "pointer": 5,
      "delay": 201
    },
    {
      "action": 2,
      "x": 400,
      "y": 300,
      "pointer": 5,
      "delay": 201
    }
  ]
}
r = requests.post("http://127.0.0.1:8019/openapi/multiTouch", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
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
  "finger2": [
    {
      "action": 0,
      "x": 500,
      "y": 200,
      "pointer": 2,
      "delay": 1
    },
    {
      "action": 2,
      "x": 500,
      "y": 200,
      "pointer": 2,
      "delay": 20
    },
    {
      "action": 2,
      "x": 500,
      "y": 300,
      "pointer": 2,
      "delay": 20
    }
  ],
  "finger3": [
    {
      "action": 0,
      "x": 200,
      "y": 200,
      "pointer": 3,
      "delay": 1
    },
    {
      "action": 2,
      "x": 300,
      "y": 200,
      "pointer": 3,
      "delay": 201
    },
    {
      "action": 2,
      "x": 400,
      "y": 300,
      "pointer": 3,
      "delay": 201
    }
  ],
  "finger4": [
    {
      "action": 0,
      "x": 200,
      "y": 200,
      "pointer": 4,
      "delay": 1
    },
    {
      "action": 2,
      "x": 300,
      "y": 200,
      "pointer": 4,
      "delay": 201
    },
    {
      "action": 2,
      "x": 400,
      "y": 300,
      "pointer": 4,
      "delay": 201
    }
  ],
  "finger5": [
    {
      "action": 0,
      "x": 200,
      "y": 200,
      "pointer": 5,
      "delay": 1
    },
    {
      "action": 2,
      "x": 300,
      "y": 200,
      "pointer": 5,
      "delay": 201
    },
    {
      "action": 2,
      "x": 400,
      "y": 300,
      "pointer": 5,
      "delay": 201
    }
  ]
};
const res = await fetch("http://127.0.0.1:8019/openapi/multiTouch", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/multiTouch" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","finger1":[{"action":0,"x":500,"y":200,"pointer":1,"delay":1},{"action":2,"x":500,"y":200,"pointer":1,"delay":20},{"action":2,"x":500,"y":300,"pointer":1,"delay":20}],"finger2":[{"action":0,"x":500,"y":200,"pointer":2,"delay":1},{"action":2,"x":500,"y":200,"pointer":2,"delay":20},{"action":2,"x":500,"y":300,"pointer":2,"delay":20}],"finger3":[{"action":0,"x":200,"y":200,"pointer":3,"delay":1},{"action":2,"x":300,"y":200,"pointer":3,"delay":201},{"action":2,"x":400,"y":300,"pointer":3,"delay":201}],"finger4":[{"action":0,"x":200,"y":200,"pointer":4,"delay":1},{"action":2,"x":300,"y":200,"pointer":4,"delay":201},{"action":2,"x":400,"y":300,"pointer":4,"delay":201}],"finger5":[{"action":0,"x":200,"y":200,"pointer":5,"delay":1},{"action":2,"x":300,"y":200,"pointer":5,"delay":201},{"action":2,"x":400,"y":300,"pointer":5,"delay":201}]}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"finger1\":[{\"action\":0,\"x\":500,\"y\":200,\"pointer\":1,\"delay\":1},{\"action\":2,\"x\":500,\"y\":200,\"pointer\":1,\"delay\":20},{\"action\":2,\"x\":500,\"y\":300,\"pointer\":1,\"delay\":20}],\"finger2\":[{\"action\":0,\"x\":500,\"y\":200,\"pointer\":2,\"delay\":1},{\"action\":2,\"x\":500,\"y\":200,\"pointer\":2,\"delay\":20},{\"action\":2,\"x\":500,\"y\":300,\"pointer\":2,\"delay\":20}],\"finger3\":[{\"action\":0,\"x\":200,\"y\":200,\"pointer\":3,\"delay\":1},{\"action\":2,\"x\":300,\"y\":200,\"pointer\":3,\"delay\":201},{\"action\":2,\"x\":400,\"y\":300,\"pointer\":3,\"delay\":201}],\"finger4\":[{\"action\":0,\"x\":200,\"y\":200,\"pointer\":4,\"delay\":1},{\"action\":2,\"x\":300,\"y\":200,\"pointer\":4,\"delay\":201},{\"action\":2,\"x\":400,\"y\":300,\"pointer\":4,\"delay\":201}],\"finger5\":[{\"action\":0,\"x\":200,\"y\":200,\"pointer\":5,\"delay\":1},{\"action\":2,\"x\":300,\"y\":200,\"pointer\":5,\"delay\":201},{\"action\":2,\"x\":400,\"y\":300,\"pointer\":5,\"delay\":201}]}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/multiTouch", content);
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
| » finger2 | body | [object] | 是 |  | 手指2 |
| »» action | body | integer | 是 |  | none |
| »» x | body | integer | 是 |  | none |
| »» y | body | integer | 是 |  | none |
| »» pointer | body | integer | 是 |  | none |
| »» delay | body | integer | 是 |  | none |
| » finger3 | body | [object] | 是 |  | 手指3 |
| »» action | body | integer | 是 |  | none |
| »» x | body | integer | 是 |  | none |
| »» y | body | integer | 是 |  | none |
| »» pointer | body | integer | 是 |  | none |
| »» delay | body | integer | 是 |  | none |
| » finger4 | body | [object] | 是 |  | 手指4 |
| »» action | body | integer | 是 |  | none |
| »» x | body | integer | 是 |  | none |
| »» y | body | integer | 是 |  | none |
| »» pointer | body | integer | 是 |  | none |
| »» delay | body | integer | 是 |  | none |
| » finger5 | body | [object] | 是 |  | 手指5 |
| »» action | body | integer | 是 |  | none |
| »» x | body | integer | 是 |  | none |
| »» y | body | integer | 是 |  | none |
| »» pointer | body | integer | 是 |  | none |
| »» delay | body | integer | 是 |  | none |

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

## 输入文本

`POST /openapi/inputText`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "content":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "duration":1000


}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "content": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "duration": 1000
}
r = requests.post("http://127.0.0.1:8019/openapi/inputText", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "content": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "duration": 1000
};
const res = await fetch("http://127.0.0.1:8019/openapi/inputText", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/inputText" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","content":"6a290cdc0b6db0565955355b157acc090e33f76e","duration":1000}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"content\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"duration\":1000}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/inputText", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » content | body | string | 是 |  | 内容 |
| » duration | body | integer | 是 |  | 持续时间单位毫秒 |

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

## 模拟键盘

`POST /openapi/ioHIDEvent`

模拟人机交互，例如键盘输入和快捷键，具体健值请看 [http://ieasyclick.com/iosdocs/#/zh-cn/advance/keyboard](../../index.md#/zh-cn/advance/keyboard)

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "eventPageID": "0x0C",
    "delay": 0.2,
    "eventUsageID": "0x40"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "eventPageID": "0x0C",
  "delay": 0.2,
  "eventUsageID": "0x40"
}
r = requests.post("http://127.0.0.1:8019/openapi/ioHIDEvent", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "eventPageID": "0x0C",
  "delay": 0.2,
  "eventUsageID": "0x40"
};
const res = await fetch("http://127.0.0.1:8019/openapi/ioHIDEvent", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/ioHIDEvent" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","eventPageID":"0x0C","delay":0.2,"eventUsageID":"0x40"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"eventPageID\":\"0x0C\",\"delay\":0.2,\"eventUsageID\":\"0x40\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/ioHIDEvent", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » eventPageID | body | string | 是 |  | 人机交互类型 |
| » eventUsageID | body | string | 是 |  | 人机交互值 |
| » delay | body | number | 是 |  | 持续时间单位毫秒 |

> 返回示例

> 200 Response

```json
{"code":0,"data":true}
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

## 运行App

`POST /openapi/appLaunch`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "bundleId":"rn.notes.best"



}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleId": "rn.notes.best"
}
r = requests.post("http://127.0.0.1:8019/openapi/appLaunch", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleId": "rn.notes.best"
};
const res = await fetch("http://127.0.0.1:8019/openapi/appLaunch", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/appLaunch" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","bundleId":"rn.notes.best"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"bundleId\":\"rn.notes.best\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/appLaunch", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » bundleId | body | string | 是 |  | 包名 |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": 312,
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
| » data | integer | true | none |  | 进程pid |
| » msg | string | true | none |  | 错误消息 |

## 打开App

`POST /openapi/openApp`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "bundleId":"rn.notes.best"



}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleId": "rn.notes.best"
}
r = requests.post("http://127.0.0.1:8019/openapi/openApp", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleId": "rn.notes.best"
};
const res = await fetch("http://127.0.0.1:8019/openapi/openApp", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/openApp" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","bundleId":"rn.notes.best"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"bundleId\":\"rn.notes.best\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/openApp", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » bundleId | body | string | 是 |  | 包名 |

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

## 安装App

`POST /openapi/installApp`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "bundleId":"com.cctv.yangshipin.app.iphone",
    "path":"/Users/ub/Downloads/央视频_2.4.3.ipa"



}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleId": "com.cctv.yangshipin.app.iphone",
  "path": "/Users/ub/Downloads/央视频_2.4.3.ipa"
}
r = requests.post("http://127.0.0.1:8019/openapi/installApp", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleId": "com.cctv.yangshipin.app.iphone",
  "path": "/Users/ub/Downloads/央视频_2.4.3.ipa"
};
const res = await fetch("http://127.0.0.1:8019/openapi/installApp", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/installApp" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","bundleId":"com.cctv.yangshipin.app.iphone","path":"/Users/ub/Downloads/央视频_2.4.3.ipa"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"bundleId\":\"com.cctv.yangshipin.app.iphone\",\"path\":\"/Users/ub/Downloads/央视频_2.4.3.ipa\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/installApp", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » bundleId | body | string | 是 |  | 包名 |
| » path | body | string | 是 |  | 要安装的ipa本机路径 |

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
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | string | true | none |  | ok 代表成功 |

## 卸载App

`POST /openapi/uninstallApp`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "bundleId":"com.cctv.yangshipin.app.iphone"



}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleId": "com.cctv.yangshipin.app.iphone"
}
r = requests.post("http://127.0.0.1:8019/openapi/uninstallApp", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleId": "com.cctv.yangshipin.app.iphone"
};
const res = await fetch("http://127.0.0.1:8019/openapi/uninstallApp", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/uninstallApp" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","bundleId":"com.cctv.yangshipin.app.iphone"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"bundleId\":\"com.cctv.yangshipin.app.iphone\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/uninstallApp", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » bundleId | body | string | 是 |  | 包名 |

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
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | string | true | none |  | ok 代表成功 |

## 关闭App

`POST /openapi/stopApp`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "bundleId":"rn.notes.best"



}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleId": "rn.notes.best"
}
r = requests.post("http://127.0.0.1:8019/openapi/stopApp", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleId": "rn.notes.best"
};
const res = await fetch("http://127.0.0.1:8019/openapi/stopApp", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/stopApp" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","bundleId":"rn.notes.best"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"bundleId\":\"rn.notes.best\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/stopApp", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » bundleId | body | string | 是 |  | 包名 |

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

## 杀死进程

`POST /openapi/appKillByBundleId`

**请求 Body**

```json
{
    "deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e",
    "bundleId":"rn.notes.best"



}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleId": "rn.notes.best"
}
r = requests.post("http://127.0.0.1:8019/openapi/appKillByBundleId", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "bundleId": "rn.notes.best"
};
const res = await fetch("http://127.0.0.1:8019/openapi/appKillByBundleId", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/appKillByBundleId" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","bundleId":"rn.notes.best"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"bundleId\":\"rn.notes.best\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/appKillByBundleId", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | none |
| » bundleId | body | string | 是 |  | none |

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

## 主页

`POST /openapi/home`

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
r = requests.post("http://127.0.0.1:8019/openapi/home", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/home", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/home" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/home", content);
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

## 重启设备

`POST /openapi/reboot`

适合版本 EC iOS 3.5.0+

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
r = requests.post("http://127.0.0.1:8019/openapi/reboot", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/reboot", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/reboot" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/reboot", content);
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

## 强制进入主页

`POST /openapi/homeScreen`

适配版本EC iOS中控 3.0.0+，包含解锁屏幕功能

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
r = requests.post("http://127.0.0.1:8019/openapi/homeScreen", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/homeScreen", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/homeScreen" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/homeScreen", content);
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

## 屏幕是否锁定

`POST /openapi/isLocked`

适配版本EC iOS中控 3.0.0+

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
r = requests.post("http://127.0.0.1:8019/openapi/isLocked", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/isLocked", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/isLocked" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/isLocked", content);
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

## 锁定屏幕

`POST /openapi/lockScreen`

适配版本EC iOS中控 3.0.0+

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
r = requests.post("http://127.0.0.1:8019/openapi/lockScreen", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/lockScreen", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/lockScreen" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/lockScreen", content);
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

## 解锁屏幕

`POST /openapi/unlockScreen`

适配版本EC iOS中控 3.0.0+， 不能设置屏幕有密码、手势等

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
r = requests.post("http://127.0.0.1:8019/openapi/unlockScreen", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/unlockScreen", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/unlockScreen" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/unlockScreen", content);
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

## 设置屏幕方向

`POST /openapi/setOrientation`

支持EC iOS中控 3.0.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "orientation": 1
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "orientation": 1
}
r = requests.post("http://127.0.0.1:8019/openapi/setOrientation", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "orientation": 1
};
const res = await fetch("http://127.0.0.1:8019/openapi/setOrientation", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/setOrientation" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","orientation":1}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"orientation\":1}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/setOrientation", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » orientation | body | integer | 是 |  | 方向 1 正常的竖屏，2 向右旋转90度(顺时针) |

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

## 获取屏幕方向

`POST /openapi/getOrientation`

支持EC iOS中控 3.0.0+

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
r = requests.post("http://127.0.0.1:8019/openapi/getOrientation", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"
};
const res = await fetch("http://127.0.0.1:8019/openapi/getOrientation", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/getOrientation" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/getOrientation", content);
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

## 校正屏幕坐标系

`POST /openapi/adjustScreenOrientation`

支持EC iOS中控 3.0.0+

**请求 Body**

```json
{
    "deviceId": "51",
    "orientation": 37
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "51",
  "orientation": 37
}
r = requests.post("http://127.0.0.1:8019/openapi/adjustScreenOrientation", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "51",
  "orientation": 37
};
const res = await fetch("http://127.0.0.1:8019/openapi/adjustScreenOrientation", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/adjustScreenOrientation" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"51","orientation":37}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"51\",\"orientation\":37}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/adjustScreenOrientation", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » orientation | body | integer | 是 |  | 0 自动校正坐标系 1 强制竖屏坐标系，2 强制向右旋转90度(顺时针)坐标系 |

> 返回示例

> 200 Response

```json
{"code":0,"data":{"orientation":1,"screenWidth":750,"screenHeight":1334}}
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
| »» orientation | integer | true | none |  | 1 强制竖屏坐标系，2 强制向右旋转90度(顺时针)坐标系 |
| »» screenWidth | integer | true | none |  | 当前屏幕宽度 |
| »» screenHeight | integer | true | none |  | 当前屏幕高度 |

## 抓取节点

`POST /openapi/dumpXml`

支持EC iOS中控 3.0.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "visibleFilter": "1",
    "labelFilter": "1",
    "maxDepth": "50",
    "excludedAttributes": "visible,enabled"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "visibleFilter": "1",
  "labelFilter": "1",
  "maxDepth": "50",
  "excludedAttributes": "visible,enabled"
}
r = requests.post("http://127.0.0.1:8019/openapi/dumpXml", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "visibleFilter": "1",
  "labelFilter": "1",
  "maxDepth": "50",
  "excludedAttributes": "visible,enabled"
};
const res = await fetch("http://127.0.0.1:8019/openapi/dumpXml", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/dumpXml" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","visibleFilter":"1","labelFilter":"1","maxDepth":"50","excludedAttributes":"visible,enabled"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"visibleFilter\":\"1\",\"labelFilter\":\"1\",\"maxDepth\":\"50\",\"excludedAttributes\":\"visible,enabled\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/dumpXml", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » visibleFilter | body | string | 是 |  | 1 代表不管visible是true还是false都获取，2 代表只获取 visible=true的节点 |
| » labelFilter | body | string | 是 |  | 1 代表不管label是否有值都获取，2 代表只获取label有值的节点 |
| » maxDepth | body | string | 是 |  | maxDepth 代表要获取节点的层级，越少速度越快，建议 1 - 500 |
| » excludedAttributes | body | string | 是 |  | 不包含的属性节点，多个用英文逗号隔开 |

> 返回示例

> 200 Response

```xml
<?xml version="1.0" encoding="UTF-8" ?>
```

### 返回结果

| 状态码 | 状态码含义 | 说明 | 数据模型 |
| --- | --- | --- | --- |
| 200 | OK | none | Inline |

### 返回数据结构

