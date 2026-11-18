# C# 短信兼容服务

`src/Lianxin.Sms.Api` 提供 ASP.NET Core 短信接口和服务端渲染的短信工作台。`bocail.com` 使用 Lamp Cloud 登录，登录后从右上角账号菜单进入「短信工作台」。独立运行短信服务时，仍可从该服务的根路径登录。iPhone APP 使用 `https://bocail.com` 和设备凭据，USB 控制仍由桌面版负责。

## Lamp Cloud 接入

部署使用官方开源分支 `java17/5.x` 的单体模块。Lamp Cloud 管理登录、组织、用户和角色。短信平台按服务端核验的 Lamp 用户 ID 绑定短信账号，并继续强制执行短信租户、手机、会话和额度的隔离。此开源版本运行在 NONE 模式。

- Lamp Cloud `d61184d7d402023fc3ea63b6bdbd7a1bfdb8ce92`，Lamp Util `30fe260`，Lamp Web `58e08ccb91583c6590f9830fb1d89333100ad62a`。Lamp Cloud 单体构建移除了 `BootWebConfiguration.java` 的一条未使用导入。
- 网页入口使用官方 Lamp Web。`scripts/lamp/lamp-web-sms.patch` 接入同源会话交换。`scripts/lamp/boot-production.properties` 和 `scripts/lamp/nginx-https-locations.conf` 记录服务和代理配置。
- 网页通过 `/portal/lamp-session` 进入 `/sms`。短信页在每个授权请求中向本机 Lamp 服务核验会话。启用后，旧的短信密码登录入口失效。
- 短信服务配置 `LAMP_INTERNAL_ORIGIN=http://127.0.0.1:28760`、`LAMP_ADMIN_USER_ID=1`、`ADMIN_EMAIL=admin@bocail.com` 和 `PUBLIC_ORIGIN=https://bocail.com`。Lamp、MySQL、Redis 和短信服务的源站均绑定本机回环地址。
- 管理员先在 Lamp 建立用户，再在短信工作台用该 Lamp 用户 ID 建立或绑定短信租户账号。旧 iPhone 设备 ID、凭据及 `/api/device/*` 协议继续有效。

## 使用短信工作台

1. 平台管理员登录，在「租户管理台」建立租户和用户账号，登记短信手机并分配给相应租户。
2. 在该 iPhone 的「联信设备」中填入同一服务的 HTTPS 地址和登记时显示的设备凭据，连接后保持 APP 在前台。
3. 租户用户登录，选择自己租户的一台已在线手机，输入含国家区号的收件号码和内容后排队。
4. 在选定的 iPhone 打开短信任务，由操作员在系统短信界面确认发送。工作台刷新后显示该任务回报。

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

域名设置为 `PUBLIC_ORIGIN=https://bocail.com` 时，HTTPS 反向代理将整个站点转发至 `http://127.0.0.1:4173`。保留原始 Host，并将可信客户端地址作为 `X-Forwarded-For` 的最右项。配置受信任证书后，检查 `https://bocail.com/health`、网页登录和真机心跳。生产环境使用真机数据库和独立的管理员密码。

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
