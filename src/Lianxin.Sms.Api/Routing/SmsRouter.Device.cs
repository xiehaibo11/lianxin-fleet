using System.Text.Json;
using System.Text.RegularExpressions;
using Lianxin.Sms.Api.Persistence;
using Lianxin.Sms.Api.Protocol;
using Lianxin.Sms.Api.Security;
using Lianxin.Sms.Api.Validation;

namespace Lianxin.Sms.Api.Routing;

internal sealed partial class SmsRouter
{
    private IResult Device(HttpContext context, string route, JsonElement data)
    {
        var match = Regex.Match(context.Request.Headers.Authorization.ToString(), "^Bearer ([\\w-]{43})$");
        var device = match.Success ? store.One("SELECT id,name FROM devices WHERE mode='iphone' AND token=$p0", Credentials.Hash(match.Groups[1].Value)) : null;
        InputValidation.Require(device is not null, 401, "设备凭据无效。");
        var id = device!.Long("id");
        var time = SmsStore.Now;
        if (route == "/api/device/poll")
        {
            InputValidation.Require(data.TryGetProperty("canSend", out var ability) && ability.ValueKind is JsonValueKind.True or JsonValueKind.False, 400, "请报告设备短信能力。");
            var canSend = ability.GetBoolean();
            store.Run("UPDATE devices SET last_seen=$p0,can_send=$p1 WHERE id=$p2", time, canSend ? 1 : 0, id);
            var job = canSend ? store.One("""
                SELECT m.id,m.body,c.recipient FROM messages m JOIN conversations c ON c.id=m.conversation_id
                WHERE c.device_id=$p0 AND m.status='queued' AND NOT EXISTS(SELECT 1 FROM messages busy JOIN conversations b
                ON b.id=busy.conversation_id WHERE b.device_id=$p0 AND busy.status='sending') ORDER BY m.id LIMIT 1
                """, id) : null;
            var sync = store.All("""
                SELECT b.id,b.run_at AS runAt FROM sync_targets t JOIN sync_batches b ON b.id=t.batch_id
                WHERE t.device_id=$p0 AND t.confirmed_at IS NULL AND b.run_at<=$p1 AND b.deadline>=$p1 ORDER BY b.id LIMIT 20
                """, id, time);
            return Results.Json(new
            {
                device,
                job,
                sync
            });
        }
        if (route == "/api/device/claim")
        {
            var job = data.Integer("id");
            return Results.Json(store.Atomic(() =>
            {
                var changed = store.Run("""
                    UPDATE messages SET status='sending',updated_at=$p0 WHERE id=$p1 AND status='queued'
                    AND conversation_id IN(SELECT id FROM conversations WHERE device_id=$p2) AND NOT EXISTS(
                    SELECT 1 FROM messages m JOIN conversations c ON c.id=m.conversation_id WHERE c.device_id=$p2 AND m.status='sending')
                    """, time, job, id);
                InputValidation.Require(changed == 1, 409, "任务已被处理，或此设备已有发送中的任务。");
                return new
                {
                    ok = true
                };
            }));
        }
        if (route == "/api/device/report")
        {
            var jobId = data.Integer("id");
            var status = data.String("status");
            InputValidation.Require(status is "submitted" or "failed", 400, "回报状态无效。");
            return Results.Json(store.Atomic(() =>
            {
                var job = store.One("SELECT m.status,c.user_id FROM messages m JOIN conversations c ON c.id=m.conversation_id WHERE m.id=$p0 AND c.device_id=$p1 AND m.direction='out'", jobId, id);
                InputValidation.Require(job is not null, 404, "任务不存在。");
                if (job!.Text("status") == status)
                    return new
                    {
                        ok = true
                    };
                InputValidation.Require(job.Text("status") is "sending" or "unknown", 409, "此任务无法更新。");
                store.Run("UPDATE messages SET status=$p0,updated_at=$p1 WHERE id=$p2", status, time, jobId);
                if (status == "failed")
                    store.Quota(job.Long("user_id"), 1, "refund", jobId);
                return new
                {
                    ok = true
                };
            }));
        }
        if (route == "/api/device/inbound")
            return Results.Json(store.Receive(id, data));
        if (route == "/api/device/sync")
        {
            var batch = data.Integer("id");
            var changed = store.Run("""
                UPDATE sync_targets SET confirmed_at=coalesce(confirmed_at,$p0) WHERE batch_id=$p1 AND device_id=$p2
                AND(confirmed_at IS NOT NULL OR batch_id IN(SELECT id FROM sync_batches WHERE run_at<=$p0 AND deadline>=$p0))
                """, time, batch, id);
            InputValidation.Require(changed != 0, 404, "同步任务无效或已过期。");
            return Results.Json(new
            {
                ok = true
            });
        }
        throw new HttpFailure(404, "接口不存在。");
    }
}
