using System.Text.Json;
using Lianxin.Sms.Api.Hosting;
using Lianxin.Sms.Api.Protocol;
using Lianxin.Sms.Api.Security;
using Lianxin.Sms.Api.Validation;
using Microsoft.Data.Sqlite;

namespace Lianxin.Sms.Api.Persistence;

internal sealed partial class SmsStore : IDisposable
{
    private readonly SqliteConnection connection;
    private readonly FileStream? lease;
    private SqliteTransaction? transaction;
    private readonly object gate = new();
    public bool Demo
    {
        get;
    }
    public static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public SmsStore(SmsOptions options)
    {
        Demo = options.Demo;
        var path = options.DatabasePath;
        if (path != ":memory:")
        {
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            try
            {
                lease = new FileStream(path + ".controller.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException error)
            {
                throw new IOException("短信数据库正在被另一个服务使用。", error);
            }
        }
        connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        try
        {
            connection.Open();
            using var stream = typeof(SmsStore).Assembly.GetManifestResourceStream("Lianxin.Sms.Api.Sql.Schema.sql")!;
            using var reader = new StreamReader(stream);
            Run(reader.ReadToEnd());
            var configured = One("SELECT mode FROM app_config WHERE singleton=1")?.Text("mode");
            var mode = Demo ? "demo" : "iphone";
            if (configured != mode)
            {
                if (configured is not null || One("SELECT 1 FROM users LIMIT 1") is not null)
                    throw new InvalidOperationException("Database mode does not match the selected environment.");
                var password = Credentials.Password(Demo ? "Demo-lianxin-2026" : options.AdminPassword);
                Atomic(() =>
                {
                    Run("INSERT INTO app_config VALUES(1,$p0)", mode);
                    if (Demo)
                        Seed(password);
                    else
                        Run("INSERT INTO users(email,name,password) VALUES($p0,'平台管理员',$p1)", InputValidation.Email(options.AdminEmail), password);
                    return true;
                });
            }
            Tick();
        }
        catch
        {
            connection.Dispose();
            lease?.Dispose();
            throw;
        }
    }
    public T Use<T>(Func<T> work)
    {
        lock (gate)
            return work();
    }
    public T Atomic<T>(Func<T> work)
    {
        using var active = connection.BeginTransaction();
        transaction = active;
        try
        {
            var result = work();
            active.Commit();
            return result;
        }
        finally
        {
            transaction = null;
        }
    }
    private SqliteCommand Command(string sql, object?[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        for (var i = 0; i < parameters.Length; i++)
            command.Parameters.AddWithValue("$p" + i, parameters[i] ?? DBNull.Value);
        return command;
    }
    public int Run(string sql, params object?[] values)
    {
        using var command = Command(sql, values);
        return command.ExecuteNonQuery();
    }
    public long Insert(string sql, params object?[] values)
    {
        Run(sql, values);
        return One("SELECT last_insert_rowid() id")!.Long("id");
    }
    public List<Dictionary<string, object?>> All(string sql, params object?[] values)
    {
        using var command = Command(sql, values);
        using var reader = command.ExecuteReader();
        var rows = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            rows.Add(row);
        }
        return rows;
    }
    public Dictionary<string, object?>? One(string sql, params object?[] values) => All(sql, values).FirstOrDefault();
    public void Quota(long user, long delta, string reason, long? reference = null)
    {
        InputValidation.Require(Run("UPDATE users SET quota=quota+$p0 WHERE id=$p1 AND quota+$p0>=0", delta, user) == 1, 409, "可用额度不足。");
        Run("INSERT INTO quota_entries(user_id,delta,reason,reference_id,created_at) VALUES($p0,$p1,$p2,$p3,$p4)", user, delta, reason, reference, Now);
    }
    public long Append(long conversation, string direction, string body, string status, long? time = null)
    {
        var id = Insert("INSERT INTO messages(conversation_id,direction,body,status,created_at,updated_at) VALUES($p0,$p1,$p2,$p3,$p4,$p4)", conversation, direction, body, status, time ?? Now);
        Run("UPDATE conversations SET last_message_id=$p0 WHERE id=$p1", id, conversation);
        return id;
    }
    public Dictionary<string, object?> Own(long user, long id) => One("SELECT * FROM conversations WHERE id=$p0 AND user_id=$p1", id, user) ?? throw new HttpFailure(404, "会话不存在。");
    public void Dispose()
    {
        lock (gate)
        {
            connection.Dispose();
            lease?.Dispose();
        }
    }

    public object Receive(long device, JsonElement data, long? allowedUser = null)
        => Receive(device, data.String("from"), data.String("event"), data.String("body"), allowedUser);
    public object Receive(long device, string? from, string? eventId, string? message, long? allowedUser = null)
    {
        var recipient = InputValidation.Phone(from);
        var eventKey = InputValidation.Text(eventId, 160, "回执编号");
        var body = InputValidation.Text(message, 4000, "回信正文");
        return Atomic<object>(() =>
        {
            var thread = One("SELECT id,user_id FROM conversations WHERE device_id=$p0 AND recipient=$p1", device, recipient);
            InputValidation.Require(thread is not null && (allowedUser is null || thread.Long("user_id") == allowedUser), 404, "回信没有可匹配的会话。");
            var seen = One("SELECT e.message_id,m.conversation_id,m.body FROM inbound_events e JOIN messages m ON m.id=e.message_id WHERE e.device_id=$p0 AND e.event_id=$p1", device, eventKey);
            if (seen is not null)
            {
                InputValidation.Require(seen.Long("conversation_id") == thread!.Long("id") && seen.Text("body") == body, 409, "回执编号已被使用。");
                return new
                {
                    id = seen.Long("message_id")
                };
            }
            var id = Append(thread!.Long("id"), "in", body, "received");
            Run("INSERT INTO inbound_events VALUES($p0,$p1,$p2)", device, eventKey, id);
            return new
            {
                id
            };
        });
    }

    private void Seed(string password)
    {
        var t1 = Insert("INSERT INTO tenants(name) VALUES('星禾服务')");
        var t2 = Insert("INSERT INTO tenants(name) VALUES('远山工作室')");
        Run("""
            INSERT INTO users(tenant_id,email,name,password) VALUES
            (NULL,'admin@lianxin.local','平台管理员',$p0),
            ($p1,'lin@lianxin.local','林小雨',$p0),($p1,'chen@lianxin.local','陈晨',$p0),($p2,'zhou@lianxin.local','周亦',$p0)
            """, password, t1, t2);
        Run("""
            WITH RECURSIVE sequence(n) AS(SELECT 1 UNION ALL SELECT n+1 FROM sequence WHERE n<1000)
            INSERT INTO devices(name,mode,last_seen,can_send) SELECT printf('iPhone · %04d',n),'demo',CASE WHEN n<=986 THEN $p0 END,1 FROM sequence
            """, Now);
        (string Number, string Out, string? In, int Minutes, bool Unread)[] conversations =
        [
            ("+12025550124", "王女士，感谢您参加今天的服务回访。稍后我们会将电子回执发给您。", "好的，麻烦了。", 29, true),
            ("+12025550123", "您好，您预约的上门服务时间是明天下午 2:00–4:00。请问这个时间方便吗？", "方便的，师傅到之前可以提前给我打个电话吗？", 37, true),
            ("+12025550119", "您好，您的服务申请已受理，工作人员将在一个工作日内联系您。", null, 125, false),
            ("+12025550108", "您好，您提交的地址变更已更新。后续服务会按新地址安排。", "收到了，谢谢！", 950, false),
            ("+12025550136", "您好，请确认下周三上午的设备巡检安排。如需改期，直接回复本条短信即可。", "周三上午可以。", 1540, false)
        ];
        for (var i = conversations.Length - 1; i >= 0; i--)
        {
            var item = conversations[i];
            var conversation = Insert("INSERT INTO conversations(user_id,device_id,recipient) VALUES(2,$p0,$p1)", i + 1, item.Number);
            Append(conversation, "out", item.Out, "submitted", Now - (item.Minutes + 8) * 60_000);
            if (item.In is not null)
                Append(conversation, "in", item.In, "received", Now - item.Minutes * 60_000);
            if (!item.Unread)
                Run("UPDATE conversations SET last_read_id=last_message_id WHERE id=$p0", conversation);
        }
        Quota(2, 1280, "initial");
        Quota(3, 320, "initial");
        Quota(4, 500, "initial");
    }
    public void Tick() => Atomic(() =>
    {
        var time = Now;
        Run("UPDATE messages SET status='unknown',updated_at=$p0 WHERE status='sending' AND updated_at<$p1", time, time - 300_000);
        if (!Demo)
            return true;
        Run("UPDATE devices SET last_seen=$p0 WHERE mode='demo' AND last_seen<=$p1", time, time - 5000);
        Run("""
            UPDATE messages SET status='submitted',updated_at=$p0 WHERE status='queued' AND created_at<=$p1
            AND conversation_id IN(SELECT c.id FROM conversations c JOIN devices d ON d.id=c.device_id WHERE d.mode='demo' AND d.last_seen IS NOT NULL)
            """, time, time - 600);
        Run("""
            UPDATE sync_targets SET confirmed_at=$p0 WHERE confirmed_at IS NULL AND device_id IN(SELECT id FROM devices WHERE mode='demo' AND last_seen IS NOT NULL)
            AND batch_id IN(SELECT id FROM sync_batches WHERE run_at<=$p0 AND deadline>=$p0)
            """, time);
        return true;
    });
}

internal static class Rows
{
    public static long Long(this Dictionary<string, object?> row, string key) => Convert.ToInt64(row[key]);
    public static string Text(this Dictionary<string, object?> row, string key) => Convert.ToString(row[key]) ?? "";
}
