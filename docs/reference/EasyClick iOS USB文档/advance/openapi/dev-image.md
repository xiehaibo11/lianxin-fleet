<!-- 来源：https://ieasyclick.com/iosdocs/advance/openapi/dev-image/ -->

# 开发者镜像接口

## 说明

本页为 **自动化接口 / 开发者镜像** 模块，对应脚本 [工具函数 · 开发者镜像](../../funcs/utils-api.md#%E5%BC%80%E5%8F%91%E8%80%85%E9%95%9C%E5%83%8F)（`utils.mountDevImage` / `unmountDevImage` / `checkDevImage`）。

> **提示**
>
> - 请求方式：HTTP `POST`
> - URL 前缀：中控地址，例如 `http://127.0.0.1:8019`
> - `Content-Type: application/json`
> - 通用返回：`code` 为 `0` 表示成功，其它为失败，`msg` 为错误信息
> - **无需先启动自动化**
> - 适配 **EC iOS USB 10.8.0+**
> - 返回总览：[企业版开放接口文档](../openapiintro.md)

手机重启后开发者镜像通常会失效，需重新加载。USB HID `sessionStart` 可能为启用 HID 重挂较新镜像，导致随后自动化启动失败时，可先卸载再加载镜像。详见 [USB HID · sessionStart](usbhid.md)。

---

## 加载开发者镜像

`POST /openapi/mountDevImage`

对应 `utils.mountDevImage()`。加载（挂载/刷入）开发者镜像。

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

body = {"deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"}
r = requests.post("http://127.0.0.1:8019/openapi/mountDevImage", json=body, timeout=180)
print(r.json())
```

**Node.js**

```javascript
const body = { deviceId: "6a290cdc0b6db0565955355b157acc090e33f76e" };
const res = await fetch("http://127.0.0.1:8019/openapi/mountDevImage", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
console.log(await res.json());
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/mountDevImage" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/mountDevImage", content);
Console.WriteLine(await res.Content.ReadAsStringAsync());
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 说明 |
| --- | --- | --- | --- | --- |
| deviceId | body | string | 是 | 设备 ID |

### 返回结果

```json
{
  "code": 0,
  "msg": "",
  "data": true
}
```

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| code | number | `0` 成功 |
| data | boolean | 是否加载成功 |

---

## 卸载开发者镜像

`POST /openapi/unmountDevImage`

对应 `utils.unmountDevImage()`。卸载后依赖镜像的自动化能力不可用，需重新挂载。

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

body = {"deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"}
r = requests.post("http://127.0.0.1:8019/openapi/unmountDevImage", json=body, timeout=180)
print(r.json())
```

**Node.js**

```javascript
const body = { deviceId: "6a290cdc0b6db0565955355b157acc090e33f76e" };
const res = await fetch("http://127.0.0.1:8019/openapi/unmountDevImage", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
console.log(await res.json());
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/unmountDevImage" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(180) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/unmountDevImage", content);
Console.WriteLine(await res.Content.ReadAsStringAsync());
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 说明 |
| --- | --- | --- | --- | --- |
| deviceId | body | string | 是 | 设备 ID |

### 返回结果

```json
{
  "code": 0,
  "msg": "",
  "data": true
}
```

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| code | number | `0` 成功 |
| data | boolean | 是否卸载成功 |

---

## 查看镜像挂载状态

`POST /openapi/checkDevImage`

对应 `utils.checkDevImage()`。仅查询是否已挂载及镜像路径，**不会自动刷入**。

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

body = {"deviceId": "6a290cdc0b6db0565955355b157acc090e33f76e"}
r = requests.post("http://127.0.0.1:8019/openapi/checkDevImage", json=body, timeout=60)
print(r.json())
```

**Node.js**

```javascript
const body = { deviceId: "6a290cdc0b6db0565955355b157acc090e33f76e" };
const res = await fetch("http://127.0.0.1:8019/openapi/checkDevImage", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(body),
});
console.log(await res.json());
```

**cURL**

```bash
curl -s -X POST "http://127.0.0.1:8019/openapi/checkDevImage" \
  -H "Content-Type: application/json" \
  -d '{"deviceId":"6a290cdc0b6db0565955355b157acc090e33f76e"}'
```

**C#**

```csharp
using System.Text;
using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
var json = "{\"deviceId\":\"6a290cdc0b6db0565955355b157acc090e33f76e\"}";
using var content = new StringContent(json, Encoding.UTF8, "application/json");
using var res = await http.PostAsync("http://127.0.0.1:8019/openapi/checkDevImage", content);
Console.WriteLine(await res.Content.ReadAsStringAsync());
```

### 请求参数

| 名称 | 位置 | 类型 | 必选 | 说明 |
| --- | --- | --- | --- | --- |
| deviceId | body | string | 是 | 设备 ID |

### 返回结果

```json
{
  "code": 0,
  "msg": "",
  "data": {
    "mounted": true,
    "result": true,
    "path": "/path/to/DeveloperDiskImage.dmg"
  }
}
```

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| code | number | `0` 成功（查询本身成功；是否已挂载看 `data.mounted`） |
| data.mounted | boolean | 是否已挂载 |
| data.result | boolean | 与 `mounted` 相同 |
| data.path | string | 已挂载时的镜像路径；未挂载为空字符串 |

