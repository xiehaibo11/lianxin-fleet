# C# 短信兼容服务

`src/Lianxin.Sms.Api` 提供 ASP.NET Core 短信 JSON 接口，供本机管理和 iPhone「联信设备」APP 使用。服务不包含网页工作台，也不接入 Lamp Cloud 登录。USB 控制仍由桌面版负责。

## 使用

1. 平台管理员通过 `POST /api/login` 登录，用 `/api/admin/accounts` 建立租户和用户账号，用 `/api/admin/devices` 登记短信手机（响应中的设备凭据只返回这一次），再用 `/api/admin/device-assignment` 分配给相应租户。
2. 在该 iPhone 的「联信设备」中填入同一服务的 HTTPS 地址和登记时返回的设备凭据，连接后保持 APP 在前台。
3. 租户用户登录后调用 `POST /api/messages` 排队，可传 `deviceId` 指定本租户已分配且在线的手机，收件号码须含国家区号。
4. 在选定的 iPhone 打开短信任务，由操作员在系统短信界面确认发送。用 `GET /api/inbox` 或 `GET /api/conversations/:id` 查看回报。

同一个号码的已有会话保持原发送手机，避免回信归属混乱。「已提交发送」表示 iPhone 返回了发送结果，送达由运营商决定。

## 启动

从源码运行演示接口：

```sh
dotnet run --project src/Lianxin.Sms.Api -- --demo
```

Windows 发布包中的兼容服务位于 `sms-api/Lianxin.Sms.Api.exe`。首次创建真机数据库前设置初始管理员密码：

```powershell
$initialPassword = Read-Host '初始管理员密码，至少12字符' -AsSecureString
$env:ADMIN_PASSWORD = [System.Net.NetworkCredential]::new('', $initialPassword).Password
.\sms-api\Lianxin.Sms.Api.exe
```

仅监听 `127.0.0.1`，默认端口 4173，可用 `--port` 或 `PORT` 修改。真实 Swift 客户端通过受控 HTTPS 反向代理访问，使用 `PUBLIC_ORIGIN` 指定完整 HTTPS 源地址，末尾不加斜杠。反向代理须与服务同机，并在 `X-Forwarded-For` 末尾追加客户端地址（如 nginx `$proxy_add_x_forwarded_for`），登录限流按该地址计数；否则所有用户共用一个限流计数。
## 数据与升级

- 默认真机数据为 `data/sms-native.sqlite`，演示为 `data/sms-demo-native.sqlite`，均相对于启动目录。
- 可用 `--db-path` 或 `DB_PATH` 显式指定旧格式数据库；切换前关闭旧服务并备份 SQLite 及其 WAL。
- 表结构和 SHA-256 token 摘要兼容旧库；密码保留原 Node scrypt 参数和盐编码，现有会话按原摘要验证。
- 同一数据库只允许一个 C# 服务实例持有占用锁；演示和真机模式不能混用。
- 不会自动覆盖、转换或删除项目原有的 `data/demo.sqlite`。
- 管理员只在创建新库时初始化；修改环境变量不会重置已有密码。

## 协议

| 接口 | 用途 |
| --- | --- |
| `GET /api/config` | 环境标记 |
| `POST /api/login`、`POST /api/logout`、`GET /api/me` | 账号与 HttpOnly Cookie 会话 |
| `GET /api/inbox` | 当前成员会话、额度和未读统计 |
| `GET /api/conversations/:id` | 当前成员消息历史与分页 |
| `POST /api/conversations/:id/read` | 标记已读 |
| `POST /api/messages` | 幂等发送、号码去重和批量扣额度。传 `deviceId` 指定租户已分配且在线的手机 |
| `/api/admin/overview`、`sync`、`devices`、`accounts`、`quota`、`device-assignment` | 管理账号、设备、租户分配、额度及同步 |
| `POST /api/device/poll` | 设备心跳与任务发现 |
| `POST /api/device/claim` | 原子领取任务 |
| `POST /api/device/report` | `submitted` / `failed` 回报与幂等退款 |
| `POST /api/device/sync` | 确认状态同步 |
| `POST /api/device/inbound` | 线路与事件编号匹配的入站回信 |

设备接口使用 `Authorization: Bearer <设备凭据>` 和 JSON。JSON 请求上限 40 KB。后台和成员权限继续按原协议区分，同租户成员之间也隔离会话和额度。

任务状态、结果待确认、迟到回报、固定设备续发、失败去重退款，以及按“设备+号码”匹配回信的规则继续保留。`submitted` 不代表运营商实际送达；Swift MessageUI 仍需用户确认发送，也不能直接读取 iPhone 系统回信。

## 检查

```sh
dotnet run --project checks/Lianxin.Sms.Checks
```

检查使用真实 Kestrel 随机端口和临时数据库，覆盖隔离、千号码任务、同步、领取、回信、并发额度、退款、旧密码和 token 格式兼容，以及请求边界。不调用真实运营商或发送实际短信。
