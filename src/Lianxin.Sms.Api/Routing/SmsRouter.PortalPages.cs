using Lianxin.Sms.Api.Persistence;
using Lianxin.Sms.Api.Security;

namespace Lianxin.Sms.Api.Routing;

internal sealed partial class SmsRouter
{
    private IResult WorkspacePage(Dictionary<string, object?> user, string csrf, bool queued)
    {
        var phones = store.All("""
            SELECT d.id,d.name,d.line,(d.can_send=1 AND d.last_seen>=$p1) online
            FROM tenant_devices t JOIN devices d ON d.id=t.device_id WHERE t.tenant_id=$p0 ORDER BY d.name,d.id
            """, user.Long("tenantId"), SmsStore.Now - 15_000);
        var phoneOptions = string.Concat(phones.Select(phone => $"""
            <option value="{phone.Long("id")}" {Disabled(phone["online"])}>{E(phone.Text("name"))} · {E(phone.Text("line"))} · {Availability(phone["online"])}</option>
            """));
        var enabled = phones.Any(p => Convert.ToInt32(p["online"]) == 1);
        var messages = store.All("""
            SELECT c.recipient,m.body,m.status,d.name device FROM messages m JOIN conversations c ON c.id=m.conversation_id
            JOIN devices d ON d.id=c.device_id WHERE c.user_id=$p0 AND m.direction='out' ORDER BY m.id DESC LIMIT 30
            """, user.Long("id"));
        var rows = string.Concat(messages.Select(row => $"""
            <tr><td>{E(row.Text("recipient"))}</td><td>{E(row.Text("device"))}</td><td>{E(row.Text("body"))}</td><td>{SendStatus(row.Text("status"))}</td></tr>
            """));
        return Page("短信工作台", $"""
            <div class="toolbar"><div>{E(user.Text("tenant"))} · {E(user.Text("name"))}<br><small>剩余 {user.Long("quota")} 条</small></div>{LogoutForm(csrf)}</div>
            {(queued ? "<p class=\"notice\">任务已排队，请在指定 iPhone 上确认发送。</p>" : "")}
            <section><h2>单台手机发短信</h2><form class="fields" method="post" action="/portal/send">{Csrf(csrf)}
            <input type="hidden" name="key" value="{Credentials.Token()}">
            <label>发送手机<select name="deviceId" required><option value="">选择已在线的手机</option>{phoneOptions}</select></label>
            <label>收件号码<input type="tel" name="recipient" placeholder="+ 号码（含国家区号）" maxlength="32" required></label>
            <label>短信正文<textarea name="body" maxlength="480" rows="4" required></textarea></label>
            <button {(enabled ? "" : "disabled")}>排队发送</button>
            <small>{(enabled ? "iPhone 收到任务后，操作员需在系统短信界面确认发送。" : "暂无在线可发送的手机。请管理员分配设备，并在 iPhone 打开联信设备 APP。")}</small></form></section>
            <section><div class="toolbar"><h2>最近发送</h2><a href="{PortalHome}">刷新状态</a></div><div class="table">
            <table><thead><tr><th>收件号码</th><th>发送手机</th><th>正文</th><th>状态</th></tr></thead><tbody>{rows}</tbody></table>
            </div></section>
            """);
    }

    private IResult ManagementPage(Dictionary<string, object?> user, string csrf)
    {
        var tenants = store.All("SELECT id,name FROM tenants ORDER BY id");
        var tenantOptions = string.Concat(tenants.Select(t => $"<option value=\"{t.Long("id")}\">{E(t.Text("name"))}</option>"));
        var users = store.All("""
            SELECT u.id,u.name,u.email,u.quota,t.name tenant,l.lamp_user_id lampId
            FROM users u JOIN tenants t ON t.id=u.tenant_id LEFT JOIN lamp_user_links l ON l.sms_user_id=u.id ORDER BY u.id
            """);
        var accounts = string.Concat(users.Select(u => $"""
            <tr><td>{E(u.Text("tenant"))}</td><td>{E(u.Text("name"))}</td><td>{E(u.Text("email"))}</td><td>{u.Long("quota")}</td>{(lamp is null ? "" : $"<td>{E(u.Text("lampId"))}</td>")}</tr>
            """));
        var unlinked = string.Concat(users.Where(u => u["lampId"] is null).Select(u =>
            $"<option value=\"{u.Long("id")}\">{E(u.Text("tenant"))} · {E(u.Text("email"))}</option>"));
        var lampIdField = """<label>灯灯用户 ID<input name="lampUserId" inputmode="numeric" pattern="[0-9]+" maxlength="19" required></label>""";
        var phones = store.All("""
            SELECT d.id,d.name,d.line,t.name tenant,(d.can_send=1 AND d.last_seen>=$p0) online FROM devices d
            LEFT JOIN tenant_devices td ON td.device_id=d.id LEFT JOIN tenants t ON t.id=td.tenant_id
            WHERE d.mode='iphone' ORDER BY d.id
            """, SmsStore.Now - 15_000);
        var phoneOptions = string.Concat(phones.Select(p => $"<option value=\"{p.Long("id")}\">{E(p.Text("name"))} · {E(p.Text("line"))}</option>"));
        var phoneRows = string.Concat(phones.Select(p => $"""
            <tr><td>{E(p.Text("name"))}</td><td>{E(p.Text("line"))}</td><td>{E(p["tenant"] as string ?? "未分配")}</td><td>{Availability(p["online"])}</td></tr>
            """));
        return Page("租户管理台", $"""
            <div class="toolbar"><div>平台管理员 · {E(user.Text("name"))}</div>{LogoutForm(csrf)}</div>
            <div class="grid"><section><h2>建立租户账号</h2><form method="post" action="/portal/accounts" class="fields">{Csrf(csrf)}
            <label>所属租户<select name="tenantId"><option value="">新建租户</option>{tenantOptions}</select></label>
            <label>新租户名<input name="tenant" maxlength="60" placeholder="新建租户时填写"></label>
            <label>用户名<input name="name" maxlength="50" required></label>
            <label>{(lamp is null ? "登录邮箱" : "短信档案邮箱")}<input type="email" name="email" maxlength="120" required></label>
            {(lamp is null ? """<label>初始密码<input type="password" name="password" autocomplete="new-password" minlength="12" maxlength="128" required></label>""" : lampIdField)}
            <label>初始短信额度<input type="number" name="quota" min="0" max="1000000" value="100" required></label>
            <button>建立账号</button></form>
            {(lamp is not null && unlinked.Length > 0 ? $"""
                <h2 style="margin-top:22px">绑定现有短信成员</h2><form method="post" action="/portal/lamp-link" class="fields">{Csrf(csrf)}
                <label>短信成员<select name="id" required>{unlinked}</select></label>{lampIdField}
                <button class="secondary">保存绑定</button></form>
                """ : "")}
            </section>
            <section><h2>登记短信手机</h2><form method="post" action="/portal/devices" class="fields">{Csrf(csrf)}
            <label>所属租户<select name="tenantId" required><option value="">选择租户</option>{tenantOptions}</select></label>
            <label>设备名<input name="name" maxlength="80" required></label>
            <label>手机发送号码<input type="tel" name="line" maxlength="32" placeholder="+ 号码（含国家区号）" required></label>
            <button>登记并生成凭据</button></form>
            {(phones.Count == 0 ? "" : $"""
                <h2 style="margin-top:22px">分配已登记手机</h2><form method="post" action="/portal/assign" class="fields">{Csrf(csrf)}
                <label>手机<select name="deviceId" required>{phoneOptions}</select></label>
                <label>所属租户<select name="tenantId" required><option value="">选择租户</option>{tenantOptions}</select></label>
                <button class="secondary">保存分配</button></form>
                """)}
            </section></div>
            <section><h2>租户账号</h2><div class="table"><table><thead><tr><th>租户</th><th>用户名</th><th>邮箱</th><th>额度</th>{(lamp is null ? "" : "<th>灯灯用户 ID</th>")}</tr></thead><tbody>{accounts}</tbody></table></div></section>
            <section><h2>短信手机</h2><div class="table"><table><thead><tr><th>设备</th><th>发送号码</th><th>租户</th><th>状态</th></tr></thead><tbody>{phoneRows}</tbody></table></div></section>
            """);
    }

    private static string Disabled(object? online) => Convert.ToInt32(online) == 1 ? "" : "disabled";
    private static string Availability(object? online) => Convert.ToInt32(online) == 1 ? "在线可发送" : "离线或无法发送";
    private static string SendStatus(string status) => status switch
    {
        "queued" => "等待手机领取",
        "sending" => "等待确认发送",
        "submitted" => "已提交发送",
        "failed" => "发送失败或取消",
        "unknown" => "结果待核对",
        _ => "未知状态"
    };
}
