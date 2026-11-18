using System.Text.Json;
using Lianxin.Sms.Api.Persistence;
using Lianxin.Sms.Api.Protocol;
using Lianxin.Sms.Api.Security;
using Lianxin.Sms.Api.Validation;

namespace Lianxin.Sms.Api.Routing;

internal sealed partial class SmsRouter
{
    private IResult Admin(HttpRequest request, string route, JsonElement data, Dictionary<string, object?> user)
    {
        var get = HttpMethods.IsGet(request.Method);
        var post = HttpMethods.IsPost(request.Method);
        if (get && route == "/api/admin/overview")
        {
            var offset = request.QueryNumber("offset", 0);
            return Results.Json(new
            {
                stats = store.One("SELECT count(*) total,count(last_seen >= $p0 OR NULL) online FROM devices", SmsStore.Now - 15_000),
                lastSync = store.One("SELECT b.id,b.run_at AS runAt,b.deadline,count(*) total,count(t.confirmed_at) confirmed FROM sync_batches b JOIN sync_targets t ON t.batch_id=b.id GROUP BY b.id ORDER BY b.id DESC LIMIT 1"),
                devices = store.All("SELECT id,name,mode,line,can_send AS canSend,last_seen >= $p0 AS online,last_seen AS lastSeen FROM devices ORDER BY id LIMIT 50 OFFSET $p1", SmsStore.Now - 15_000, offset),
                accounts = store.All("SELECT u.id,u.name,u.email,u.quota,t.name tenant FROM users u JOIN tenants t ON t.id=u.tenant_id ORDER BY t.id,u.id"),
                tenants = store.All("SELECT id,name FROM tenants ORDER BY id")
            });
        }
        if (post && route == "/api/admin/sync")
            return Results.Json(store.Atomic(() =>
        {
            var time = SmsStore.Now;
            InputValidation.Require(store.One("SELECT 1 FROM sync_batches WHERE run_at>$p0 LIMIT 1", time) is null, 409, "已有同步任务即将开始。");
            var runAt = time + 3000;
            var id = store.Insert("INSERT INTO sync_batches(run_at,deadline) VALUES($p0,$p1)", runAt, runAt + 30_000);
            var total = store.Run("INSERT INTO sync_targets(batch_id,device_id) SELECT $p0,id FROM devices WHERE last_seen>=$p1", id, time - 15_000);
            InputValidation.Require(total > 0, 409, "当前没有在线设备。");
            return new
            {
                id,
                total,
                runAt
            };
        }));
        if (post && route == "/api/admin/devices")
        {
            var (id, token) = RegisterPhone(data);
            return Results.Json(new
            {
                id,
                token
            }, statusCode: 201);
        }
        if (post && route == "/api/admin/device-assignment")
        {
            store.AssignDevice(data.Integer("deviceId"), data.Integer("tenantId"));
            return Results.Json(new
            {
                ok = true
            });
        }
        if (post && route == "/api/admin/accounts")
        {
            var name = InputValidation.Text(data.String("name"), 50, "用户名");
            var email = InputValidation.Email(data.String("email"));
            var lampUserId = lamp is null ? (long?)null : LampSessions.ReadId(data, "lampUserId");
            var password = Credentials.Password(lampUserId is null ? data.String("password") : Credentials.Token());
            var quota = data.Integer("quota", 0, 1_000_000);
            return Results.Json(store.Atomic(() =>
            {
                InputValidation.Require(store.One("SELECT 1 FROM users WHERE email=$p0", email) is null, 409, "邮箱已被使用。");
                long tenant;
                if (data.TryGetProperty("tenantId", out var value) && value.ValueKind is not JsonValueKind.Null && !(value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var zero) && zero == 0))
                {
                    tenant = data.Integer("tenantId");
                    InputValidation.Require(store.One("SELECT 1 FROM tenants WHERE id=$p0", tenant) is not null, 404, "租户不存在。");
                }
                else
                    tenant = store.Insert("INSERT INTO tenants(name) VALUES($p0)", InputValidation.Text(data.String("tenant"), 60, "租户名称"));
                var id = store.Insert("INSERT INTO users(tenant_id,email,name,password) VALUES($p0,$p1,$p2,$p3)", tenant, email, name, password);
                if (lampUserId is long external)
                    LinkLampUser(external, id);
                if (quota != 0)
                    store.Quota(id, quota, "initial");
                return new
                {
                    id
                };
            }), statusCode: 201);
        }
        if (post && route == "/api/admin/lamp-link" && lamp is not null)
        {
            LinkLampUser(LampSessions.ReadId(data, "lampUserId"), data.Integer("id"));
            return Results.Json(new
            {
                ok = true
            });
        }
        if (post && route == "/api/admin/quota")
        {
            var id = data.Integer("id");
            var delta = data.Integer("delta", -1_000_000, 1_000_000);
            InputValidation.Require(delta != 0, 400, "调整额度不能为零。");
            return Results.Json(store.Atomic(() =>
            {
                InputValidation.Require(store.One("SELECT 1 FROM users WHERE id=$p0 AND tenant_id IS NOT NULL", id) is not null, 404, "账号不存在。");
                store.Quota(id, delta, "admin", user.Long("id"));
                return new
                {
                    ok = true
                };
            }));
        }
        if (get && route == "/api/admin/quota")
        {
            var id = request.QueryNumber("id", 0, 1, InputValidation.MaxId);
            return Results.Json(new
            {
                items = store.All("SELECT delta,reason,created_at AS createdAt FROM quota_entries WHERE user_id=$p0 ORDER BY id DESC LIMIT 100", id)
            });
        }
        throw new HttpFailure(404, "接口不存在。");
    }

    private void LinkLampUser(long lampUserId, long smsUserId)
    {
        InputValidation.Require(lampUserId != options.LampAdminUserId, 409, "灯灯平台管理员不能绑定为租户成员。");
        InputValidation.Require(store.One("SELECT 1 FROM users WHERE id=$p0 AND tenant_id IS NOT NULL",
            smsUserId) is not null, 404, "短信成员不存在。");
        InputValidation.Require(store.One("""
            SELECT 1 FROM lamp_user_links WHERE (lamp_user_id=$p0 OR sms_user_id=$p1)
            AND NOT (lamp_user_id=$p0 AND sms_user_id=$p1)
            """, lampUserId, smsUserId) is null, 409, "该灯灯账号或短信成员已与其他账号绑定。");
        store.Run("INSERT OR IGNORE INTO lamp_user_links(lamp_user_id,sms_user_id) VALUES($p0,$p1)", lampUserId, smsUserId);
    }

    private (long Id, string Token) RegisterPhone(JsonElement data)
    {
        InputValidation.Require(!options.Demo, 409, "请在真机模式中登记设备。");
        var name = InputValidation.Text(data.String("name"), 80, "设备名称");
        var line = InputValidation.Phone(data.String("line"));
        var tenant = data.OptionalInteger("tenantId");
        return store.Atomic(() =>
        {
            InputValidation.Require(store.One("SELECT 1 FROM devices WHERE line=$p0", line) is null, 409, "此短信号码已登记。");
            if (tenant is long assigned)
                InputValidation.Require(store.One("SELECT 1 FROM tenants WHERE id=$p0", assigned) is not null, 404, "租户不存在。");
            var token = Credentials.Token();
            var id = store.Insert("INSERT INTO devices(name,mode,line,token) VALUES($p0,'iphone',$p1,$p2)", name, line, Credentials.Hash(token));
            if (tenant is long tenantId)
                store.Run("INSERT INTO tenant_devices(device_id,tenant_id) VALUES($p0,$p1)", id, tenantId);
            return (id, token);
        });
    }
}
