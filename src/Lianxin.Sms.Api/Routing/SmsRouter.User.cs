using System.Text.Json;
using System.Text.RegularExpressions;
using Lianxin.Sms.Api.Persistence;
using Lianxin.Sms.Api.Protocol;
using Lianxin.Sms.Api.Security;
using Lianxin.Sms.Api.Validation;

namespace Lianxin.Sms.Api.Routing;

internal sealed partial class SmsRouter
{
    private IResult User(HttpRequest request, string route, JsonElement data, Dictionary<string, object?> user)
    {
        var id = user.Long("id");
        var get = HttpMethods.IsGet(request.Method);
        var post = HttpMethods.IsPost(request.Method);
        if (get && route == "/api/inbox")
        {
            var search = request.Query["q"].ToString();
            search = Regex.Replace(search[..Math.Min(search.Length, 40)], "[ ()-]", "");
            var offset = request.QueryNumber("offset", 0);
            var unread = request.Query["unread"] == "1" ? 1 : 0;
            var stats = store.One("""
                SELECT count(m.id) total,count((m.direction='out' AND m.status IN('queued','sending','unknown')) OR NULL) pending,
                count((m.direction='out' AND m.status='submitted') OR NULL) submitted,count((m.direction='in' AND m.id>c.last_read_id) OR NULL) unread
                FROM conversations c JOIN messages m ON m.conversation_id=c.id WHERE c.user_id=$p0
                """, id);
            var items = store.All("""
                SELECT c.id,c.recipient,m.body AS preview,m.created_at AS updatedAt,
                (SELECT count(*) FROM messages im WHERE im.conversation_id=c.id AND im.direction='in' AND im.id>c.last_read_id) AS unread
                FROM conversations c JOIN messages m ON m.id=c.last_message_id WHERE c.user_id=$p0 AND instr(c.recipient,$p1)>0
                AND($p2=0 OR EXISTS(SELECT 1 FROM messages im WHERE im.conversation_id=c.id AND im.direction='in' AND im.id>c.last_read_id))
                ORDER BY c.last_message_id DESC LIMIT 51 OFFSET $p3
                """, id, search, unread, offset);
            return Results.Json(new
            {
                quota = user.Long("quota"),
                stats,
                items = items.Take(50),
                more = items.Count > 50
            });
        }
        if (post && route == "/api/messages")
            return Results.Json(store.Send(id, data), statusCode: 201);
        var match = Regex.Match(route, "^/api/conversations/(\\d+)(/read|/simulate)?$");
        if (match.Success)
        {
            InputValidation.Require(long.TryParse(match.Groups[1].Value, out var threadId), 400, "会话编号无效。");
            InputValidation.Number(threadId, 1, InputValidation.MaxId, "会话编号");
            var thread = store.Own(id, threadId);
            var suffix = match.Groups[2].Value;
            if (get && suffix.Length == 0)
            {
                var before = request.QueryNumber("before", InputValidation.MaxId, 1, InputValidation.MaxId);
                var messages = store.All("SELECT id,direction,body,status,created_at AS createdAt FROM messages WHERE conversation_id=$p0 AND id<$p1 ORDER BY id DESC LIMIT 101", threadId, before);
                return Results.Json(new
                {
                    recipient = thread.Text("recipient"),
                    device = store.One("SELECT name FROM devices WHERE id=$p0", thread.Long("device_id"))!.Text("name"),
                    messages = messages.Take(100).Reverse(),
                    more = messages.Count > 100
                });
            }
            if (post && suffix == "/read")
            {
                var through = data.Integer("through");
                InputValidation.Require(store.One("SELECT 1 FROM messages WHERE id=$p0 AND conversation_id=$p1", through, threadId) is not null, 404, "消息不存在。");
                store.Run("UPDATE conversations SET last_read_id=max(last_read_id,$p0) WHERE id=$p1", through, threadId);
                return Results.Json(new
                {
                    ok = true
                });
            }
            if (post && suffix == "/simulate" && options.Demo)
                return Results.Json(store.Receive(thread.Long("device_id"), thread.Text("recipient"), Credentials.Token(), data.String("body"), id));
        }
        throw new HttpFailure(404, "接口不存在。");
    }
}
