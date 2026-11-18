using System.Text.Json;
using Lianxin.Sms.Api.Protocol;
using Lianxin.Sms.Api.Security;
using Lianxin.Sms.Api.Validation;

namespace Lianxin.Sms.Api.Persistence;

internal sealed partial class SmsStore
{
    public object Send(long user, JsonElement data)
    {
        var key = InputValidation.Text(data.String("key"), 80, "请求编号");
        var body = InputValidation.Text(data.String("body"), 480, "短信正文");
        string[] recipients;
        var conversation = data.OptionalInteger("conversation");
        var suppliedDevice = data.OptionalInteger("deviceId");
        if (conversation is not null)
            recipients = [Own(user, conversation.Value).Text("recipient")];
        else
        {
            InputValidation.Require(data.TryGetProperty("recipients", out var numbers) && numbers.ValueKind == JsonValueKind.Array && numbers.GetArrayLength() is > 0 and <= 1000, 400, "每次可提交 1 至 1,000 个收件号码。");
            recipients = numbers.EnumerateArray().Select(number => InputValidation.Phone(number.ValueKind == JsonValueKind.String ? number.GetString() : null)).ToArray();
        }
        recipients = recipients.Distinct(StringComparer.Ordinal).ToArray();
        var fingerprint = Credentials.Hash("[[" + string.Join(',', recipients.Select(LegacyJson.Quote)) + "]," + LegacyJson.Quote(body)
            + (suppliedDevice is long selected ? "," + selected.ToString(System.Globalization.CultureInfo.InvariantCulture) : "") + "]");
        return Atomic<object>(() =>
        {
            var previous = One("SELECT fingerprint,result FROM send_requests WHERE user_id=$p0 AND request_key=$p1", user, key);
            if (previous is not null)
            {
                InputValidation.Require(previous.Text("fingerprint") == fingerprint, 409, "此请求编号已用于其他内容。");
                return JsonSerializer.Deserialize<JsonElement>(previous.Text("result"));
            }
            var tenant = One("SELECT tenant_id FROM users WHERE id=$p0", user)!.Long("tenant_id");
            if (suppliedDevice is long requested)
                InputValidation.Require(One("SELECT 1 FROM tenant_devices WHERE device_id=$p0 AND tenant_id=$p1", requested, tenant)
                    is not null, 404, "此租户没有获分配该手机。");
            Quota(user, -recipients.Length, "send");
            long? first = null;
            foreach (var recipient in recipients)
            {
                var thread = One("SELECT id,device_id FROM conversations WHERE user_id=$p0 AND recipient=$p1", user, recipient);
                if (thread is null)
                {
                    var device = One("""
                        SELECT d.id FROM devices d LEFT JOIN tenant_devices td ON td.device_id=d.id
                        WHERE d.can_send=1 AND d.last_seen>=$p0 AND ($p2 IS NULL OR d.id=$p2)
                        AND (td.tenant_id=$p3 OR ($p2 IS NULL AND td.tenant_id IS NULL)) AND NOT EXISTS(
                        SELECT 1 FROM conversations c WHERE c.device_id=d.id AND c.recipient=$p1) ORDER BY d.last_assigned,d.id LIMIT 1
                        """, Now - 15_000, recipient, suppliedDevice, tenant);
                    InputValidation.Require(device is not null, 409, "指定手机离线、无法发送短信，或此号码已由其他帐号使用。");
                    var id = Insert("INSERT INTO conversations(user_id,device_id,recipient) VALUES($p0,$p1,$p2)", user, device!.Long("id"), recipient);
                    thread = new()
                    {
                        ["id"] = id,
                        ["device_id"] = device.Long("id")
                    };
                }
                else if (suppliedDevice is long target)
                {
                    InputValidation.Require(thread.Long("device_id") == target, 409, "这个号码已有会话，请选择该会话原先使用的手机。");
                    InputValidation.Require(One("SELECT 1 FROM devices WHERE id=$p0 AND can_send=1 AND last_seen>=$p1",
                        target, Now - 15_000) is not null, 409, "指定手机离线或无法发送短信。");
                }
                var message = Append(thread.Long("id"), "out", body, "queued");
                Run("UPDATE devices SET last_assigned=$p0 WHERE id=$p1", message, thread.Long("device_id"));
                first ??= thread.Long("id");
            }
            var result = new
            {
                conversation = first,
                count = recipients.Length
            };
            Run("INSERT INTO send_requests VALUES($p0,$p1,$p2,$p3)", user, key, fingerprint, JsonSerializer.Serialize(result));
            return result;
        });
    }

    public void AssignDevice(long device, long tenant) => Atomic(() =>
    {
        InputValidation.Require(One("SELECT 1 FROM tenants WHERE id=$p0", tenant) is not null, 404, "租户不存在。");
        InputValidation.Require(One("SELECT 1 FROM devices WHERE id=$p0 AND mode='iphone'", device) is not null, 404, "手机不存在。");
        InputValidation.Require(One("""
            SELECT 1 FROM conversations c JOIN users u ON u.id=c.user_id WHERE c.device_id=$p0 AND u.tenant_id<>$p1 LIMIT 1
            """, device, tenant) is null, 409, "该手机已有其他租户的短信会话，不能分配。");
        Run("INSERT INTO tenant_devices(device_id,tenant_id) VALUES($p0,$p1) ON CONFLICT(device_id) DO UPDATE SET tenant_id=excluded.tenant_id",
            device, tenant);
        return true;
    });
}
