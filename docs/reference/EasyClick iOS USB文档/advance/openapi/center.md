<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapi/center/ -->

# 中控接口

## 说明

本页由企业版 OpenAPI 文档整理，模块：**中控接口**。

> **提示**
>
> - 请求方式：HTTP `POST`（个别快捷指令接口为 GET，见正文）
> - URL 前缀：中控地址，例如 `http://127.0.0.1:8019`
> - `Content-Type: application/json`
> - 通用返回：`code` 为 `0` 表示成功，其它为失败，`msg` 为错误信息
> - 返回总览：[企业版开放接口文档](../openapiintro.md)

## 闪断USB（Windows）

`POST /openapi/reconnectUsb`

需要EC IOS USB版本8.15.0+

**请求 Body**

```json
{
    "deviceId": "20"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "20"
}
r = requests.post("http://127.0.0.1:8019/openapi/reconnectUsb", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "20"
};
const res = await fetch("http://127.0.0.1:8019/openapi/reconnectUsb", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/reconnectUsb" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"20"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"20\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/reconnectUsb", content);
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
  "msg": "string",
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
| » msg | string | true | none |  | 错误消息 |
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 成功 false 失败 |

## 设备列表

`POST /openapi/deviceList`

**请求 Body**

```json
{
    "deviceId": "",
    "groupId": ""
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "",
  "groupId": ""
}
r = requests.post("http://127.0.0.1:8019/openapi/deviceList", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "",
  "groupId": ""
};
const res = await fetch("http://127.0.0.1:8019/openapi/deviceList", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/deviceList" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"","groupId":""}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"\",\"groupId\":\"\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/deviceList", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备的ID，模糊匹配 |
| » groupId | body | string | 是 |  | 分组的Id |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": {
    "list": [
      {
        "bridgeUrl": "http://127.0.0.1:8020",
        "deviceAlias": "222333",
        "groupId": "1656302284985",
        "deviceNo": "6a290cdc0b6db0565955355b157acc090e33f76e",
        "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
        "deviceName": "iPhone (2)",
        "serialNo": "F4GSPUAZHG6W",
        "groupName": "test",
        "productVersion": "15.2"
      }
    ],
    "total": 1,
    "page": 1
  },
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
| » data | object | true | none |  | 数据 |
| »» list | [object] | true | none |  | 设备列表数组 |
| »»» bridgeUrl | string | false | none |  | 设备桥接地址 |
| »»» deviceAlias | string | false | none |  | 设备别名 |
| »»» groupId | string | false | none |  | 设备所在组 |
| »»» deviceNo | string | false | none |  | 设备id |
| »»» deviceId | string | false | none |  | 设备id |
| »»» deviceName | string | false | none |  | 设备名称 |
| »»» serialNo | string | false | none |  | 设备序列号 |
| »»» groupName | string | false | none |  | 组名 |
| »»» productVersion | string | false | none |  | 设备版本 |
| »» total | integer | true | none |  | none |
| »» page | integer | true | none |  | none |
| » msg | string | true | none |  | 错误消息 |

## 执行脚本

`POST /openapi/startScript`

**请求 Body**

```json
{
    "deviceIds": [
        "6a290cdc0b6db0565955355b157acc090e33f76e",
        "64",
        "23",
        "70"
    ],
    "name": "太连指中",
    "iecPath": "c:/dsdds/build/release.iec"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceIds": [
    "6a290cdc0b6db0565955355b157acc090e33f76e",
    "64",
    "23",
    "70"
  ],
  "name": "太连指中",
  "iecPath": "c:/dsdds/build/release.iec"
}
r = requests.post("http://127.0.0.1:8019/openapi/startScript", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceIds": [
    "6a290cdc0b6db0565955355b157acc090e33f76e",
    "64",
    "23",
    "70"
  ],
  "name": "太连指中",
  "iecPath": "c:/dsdds/build/release.iec"
};
const res = await fetch("http://127.0.0.1:8019/openapi/startScript", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/startScript" \
  -H "Content-Type: application/json" \
  -d '{"deviceIds":["6a290cdc0b6db0565955355b157acc090e33f76e","64","23","70"],"name":"太连指中","iecPath":"c:/dsdds/build/release.iec"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceIds\":[\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"64\",\"23\",\"70\"],\"name\":\"太连指中\",\"iecPath\":\"c:/dsdds/build/release.iec\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/startScript", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceIds | body | [string] | 是 |  | 设备ID数组 |
| » name | body | string | 是 |  | 脚本名称 |
| » iecPath | body | string | 是 |  | 脚本所在的本机路径 |

> 返回示例

> 200 Response

```json
{
  "msg": "string",
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
| » msg | string | true | none |  | 错误消息 |
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 成功 false 失败 |

## 停止脚本

`POST /openapi/stopScript`

**请求 Body**

```json
{
    "deviceIds": [
        "6a290cdc0b6db0565955355b157acc090e33f76e",
        "32",
        "97"
    ]
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceIds": [
    "6a290cdc0b6db0565955355b157acc090e33f76e",
    "32",
    "97"
  ]
}
r = requests.post("http://127.0.0.1:8019/openapi/stopScript", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceIds": [
    "6a290cdc0b6db0565955355b157acc090e33f76e",
    "32",
    "97"
  ]
};
const res = await fetch("http://127.0.0.1:8019/openapi/stopScript", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/stopScript" \
  -H "Content-Type: application/json" \
  -d '{"deviceIds":["6a290cdc0b6db0565955355b157acc090e33f76e","32","97"]}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceIds\":[\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"32\",\"97\"]}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/stopScript", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceIds | body | [string] | 是 |  | 设备的ID数组 |

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
| » data | boolean | true | none |  | 数据 |
| » msg | string | true | none |  | 错误消息 |

## 获取刷新信息

`POST /openapi/refreshMsg`

**请求 Body**

```json
{
    "deviceIds": [
        "90e2f3834c0977205e441aa664916a9bdde81e8d",
        "32",
        "97"
    ]
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceIds": [
    "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "32",
    "97"
  ]
}
r = requests.post("http://127.0.0.1:8019/openapi/refreshMsg", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceIds": [
    "90e2f3834c0977205e441aa664916a9bdde81e8d",
    "32",
    "97"
  ]
};
const res = await fetch("http://127.0.0.1:8019/openapi/refreshMsg", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/refreshMsg" \
  -H "Content-Type: application/json" \
  -d '{"deviceIds":["90e2f3834c0977205e441aa664916a9bdde81e8d","32","97"]}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceIds\":[\"90e2f3834c0977205e441aa664916a9bdde81e8d\",\"32\",\"97\"]}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/refreshMsg", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceIds | body | [string] | 是 |  | 设备的ID数组 |

> 返回示例

> 200 Response

```json
{
  "code": 0,
  "data": {
    "6a290cdc0b6db0565955355b157acc090e33f76e": {
      "msg": "脚本结束执行",
      "deviceNo": "6a290cdc0b6db0565955355b157acc090e33f76e",
      "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
      "scriptName": "太连指中",
      "serviceOk": "0",
      "reportTime": "2022-07-23 10:23:03"
    }
  },
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
| » data | object | true | none |  | none |
| »» 6a290cdc0b6db0565955355b157acc090e33f76e | object | true | none |  | 设备ID |
| »»» msg | string | true | none |  | 运行消息 |
| »»» deviceNo | string | true | none |  | 设备id |
| »»» deviceId | string | true | none |  | 设备id |
| »»» scriptName | string | true | none |  | 脚本名称 |
| »»» serviceOk | string | true | none |  | 服务是否正常 |
| »»» reportTime | string | true | none |  | 上报时间 |
| » msg | string | true | none |  | msg 错误消息 |

## 设置设备别名

`POST /openapi/setDeviceAlias`

支持EC iOS中控 3.0.0+

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "deviceAlias": "phone1"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "deviceAlias": "phone1"
}
r = requests.post("http://127.0.0.1:8019/openapi/setDeviceAlias", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "deviceAlias": "phone1"
};
const res = await fetch("http://127.0.0.1:8019/openapi/setDeviceAlias", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/setDeviceAlias" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","deviceAlias":"phone1"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"deviceAlias\":\"phone1\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/setDeviceAlias", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » deviceAlias | body | string | 是 |  | 别名，不要用特殊字符 空格等 |

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

## 暂停执行脚本

`POST /openapi/pauseScript`

需要EC IOS USB版本6.47.0+

**请求 Body**

```json
{
    "deviceIds": [
        "6a290cdc0b6db0565955355b157acc090e33f76e",
        "64",
        "23",
        "70"
    ]
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceIds": [
    "6a290cdc0b6db0565955355b157acc090e33f76e",
    "64",
    "23",
    "70"
  ]
}
r = requests.post("http://127.0.0.1:8019/openapi/pauseScript", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceIds": [
    "6a290cdc0b6db0565955355b157acc090e33f76e",
    "64",
    "23",
    "70"
  ]
};
const res = await fetch("http://127.0.0.1:8019/openapi/pauseScript", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/pauseScript" \
  -H "Content-Type: application/json" \
  -d '{"deviceIds":["6a290cdc0b6db0565955355b157acc090e33f76e","64","23","70"]}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceIds\":[\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"64\",\"23\",\"70\"]}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/pauseScript", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceIds | body | [string] | 是 |  | 设备ID数组 |

> 返回示例

> 200 Response

```json
{
  "msg": "string",
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
| » msg | string | true | none |  | 错误消息 |
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 成功 false 失败 |

## 继续执行脚本

`POST /openapi/continueScript`

需要EC IOS USB版本6.47.0+

**请求 Body**

```json
{
    "deviceIds": [
        "6a290cdc0b6db0565955355b157acc090e33f76e",
        "64",
        "23",
        "70"
    ]
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceIds": [
    "6a290cdc0b6db0565955355b157acc090e33f76e",
    "64",
    "23",
    "70"
  ]
}
r = requests.post("http://127.0.0.1:8019/openapi/continueScript", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceIds": [
    "6a290cdc0b6db0565955355b157acc090e33f76e",
    "64",
    "23",
    "70"
  ]
};
const res = await fetch("http://127.0.0.1:8019/openapi/continueScript", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/continueScript" \
  -H "Content-Type: application/json" \
  -d '{"deviceIds":["6a290cdc0b6db0565955355b157acc090e33f76e","64","23","70"]}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceIds\":[\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"64\",\"23\",\"70\"]}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/continueScript", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceIds | body | [string] | 是 |  | 设备ID数组 |

> 返回示例

> 200 Response

```json
{
  "msg": "string",
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
| » msg | string | true | none |  | 错误消息 |
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 成功 false 失败 |

## 脚本是否暂停状态

`POST /openapi/isScriptPause`

需要EC IOS USB版本6.47.0+，用于判断这个设备的脚本是否是暂停状态

**请求 Body**

```json
{
    "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
    "deviceAlias": "phone1"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "deviceAlias": "phone1"
}
r = requests.post("http://127.0.0.1:8019/openapi/isScriptPause", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e",
  "deviceAlias": "phone1"
};
const res = await fetch("http://127.0.0.1:8019/openapi/isScriptPause", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/isScriptPause" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e","deviceAlias":"phone1"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\",\"deviceAlias\":\"phone1\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/isScriptPause", content);
var text = await res.Content.ReadAsStringAsync();
Console.WriteLine(text);
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 中文名 | 说明 |
| --- | --- | --- | --- | --- | --- |
| body | body | object | 否 |  | none |
| » deviceId | body | string | 是 |  | 设备ID |
| » deviceAlias | body | string | 是 |  | 别名，不要用特殊字符 空格等 |

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
| » data | boolean | true | none |  | true 暂停中 false 不是暂停状态 |
| » msg | string | true | none |  | 错误消息 |

## 恢复出厂

`POST /openapi/erasePhone`

需要EC IOS USB版本8.15.0+

**请求 Body**

```json
{
    "deviceId": "20"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "20"
}
r = requests.post("http://127.0.0.1:8019/openapi/erasePhone", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "20"
};
const res = await fetch("http://127.0.0.1:8019/openapi/erasePhone", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/erasePhone" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"20"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"20\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/erasePhone", content);
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
  "msg": "string",
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
| » msg | string | true | none |  | 错误消息 |
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 成功 false 失败 |

## 开启开发者模式

`POST /openapi/enableDeveloperMode`

需要EC IOS USB版本8.15.0+

**请求 Body**

```json
{
    "deviceId": "20"
}
```

### 请求示例

**Python**

```python
import requests

body = {
  "deviceId": "20"
}
r = requests.post("http://127.0.0.1:8019/openapi/enableDeveloperMode", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "20"
};
const res = await fetch("http://127.0.0.1:8019/openapi/enableDeveloperMode", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/enableDeveloperMode" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"20"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"20\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/enableDeveloperMode", content);
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
  "msg": "string",
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
| » msg | string | true | none |  | 错误消息 |
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 成功 false 失败 |

## 获取屏幕方向-无需自动化

`POST /openapi/getOrientationNoAuto`

需要EC IOS USB版本8.15.0+

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
r = requests.post("http://127.0.0.1:8019/openapi/getOrientationNoAuto", json=body, timeout=30)
print(r.json())
```

**Node.js**

```javascript
const body = {
  "deviceId": "90e2f3834c0977205e441aa664916a9bdde81e8d"
};
const res = await fetch("http://127.0.0.1:8019/openapi/getOrientationNoAuto", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
const data = await res.json();
console.log(data);
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/getOrientationNoAuto" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"90e2f3834c0977205e441aa664916a9bdde81e8d"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var json = "{\"deviceId\":\"90e2f3834c0977205e441aa664916a9bdde81e8d\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/getOrientationNoAuto", content);
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
  "msg": "string",
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
| » msg | string | true | none |  | 错误消息 |
| » code | integer | true | none |  | 0 成功 其他失败 |
| » data | boolean | true | none |  | true 成功 false 失败 |

