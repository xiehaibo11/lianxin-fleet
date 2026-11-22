using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;
using Microsoft.Data.Sqlite;

namespace Lianxin.Fleet.Core.Persistence;

public sealed partial class FleetStore
{
    public IReadOnlyList<FleetScript> GetScripts()
    {
        lock (gate)
        {
            using var command = Command("SELECT id,name,source,repeat,updated_at FROM native_scripts ORDER BY name COLLATE NOCASE,id");
            using var reader = command.ExecuteReader();
            var scripts = new List<FleetScript>();
            while (reader.Read())
                scripts.Add(ReadScript(reader));
            return scripts;
        }
    }

    public FleetScript GetScript(long id)
    {
        lock (gate)
        {
            using var command = Command("SELECT id,name,source,repeat,updated_at FROM native_scripts WHERE id=$p0", id);
            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadScript(reader) : throw new KeyNotFoundException("脚本不存在。");
        }
    }

    private static FleetScript ReadScript(SqliteDataReader reader) => new(reader.GetInt64(0), reader.GetString(1),
        reader.GetString(2), reader.GetInt32(3), DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(4)));

    // id 为 null 时新建；保存前先完整解析一遍，语法错误的脚本不会入库。
    public FleetScript SaveScript(long? id, string name, string source, int repeat)
    {
        name = FleetValidation.Text(name, 60, "脚本名称");
        ScriptParser.Parse(source);
        if (repeat is < 0 or > 100_000)
            throw new ArgumentException("运行次数须为 0 至 100000，0 表示一直循环。");
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        lock (gate)
        {
            try
            {
                if (id is null)
                {
                    Execute("INSERT INTO native_scripts(name,source,repeat,created_at,updated_at) VALUES($p0,$p1,$p2,$p3,$p3)",
                        name, source, repeat, now);
                    return GetScript(Convert.ToInt64(Scalar("SELECT last_insert_rowid()")));
                }
                if (Execute("UPDATE native_scripts SET name=$p0,source=$p1,repeat=$p2,updated_at=$p3 WHERE id=$p4",
                    name, source, repeat, now, id) != 1)
                    throw new KeyNotFoundException("脚本不存在。");
                return GetScript(id.Value);
            }
            catch (SqliteException error) when (error.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("已有同名脚本。", error);
            }
        }
    }

    public void DeleteScript(long id)
    {
        lock (gate)
        {
            if (Convert.ToInt64(Scalar("SELECT count(*) FROM native_schedules WHERE script_id=$p0", id)) > 0)
                throw new InvalidOperationException("有定时任务正在使用这个脚本，请先删除或修改定时任务。");
            if (Execute("DELETE FROM native_scripts WHERE id=$p0", id) != 1)
                throw new KeyNotFoundException("脚本不存在。");
        }
    }

    public long StartScriptRun(FleetScript script, long deviceId, string trigger)
    {
        lock (gate)
        {
            Execute("""
                INSERT INTO native_script_runs(script_id,script_name,device_id,state,message,trigger,started_at)
                VALUES($p0,$p1,$p2,'Running','排队中',$p3,$p4)
                """, script.Id, script.Name, deviceId, trigger[..Math.Min(trigger.Length, 80)],
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return Convert.ToInt64(Scalar("SELECT last_insert_rowid()"));
        }
    }

    public void FinishScriptRun(long runId, ScriptRunState state, string? message)
    {
        if (state == ScriptRunState.Running)
            throw new ArgumentException("结果状态无效。");
        lock (gate)
            Execute("UPDATE native_script_runs SET state=$p0,message=$p1,finished_at=$p2 WHERE id=$p3 AND state='Running'",
                state.ToString(), message?[..Math.Min(message.Length, 600)], DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), runId);
    }

    // 每台设备最近一次运行，用于设备表格的「脚本」「消息」列。
    public IReadOnlyDictionary<long, ScriptRun> GetLatestScriptRuns()
    {
        lock (gate)
        {
            using var command = Command("""
                SELECT r.id,r.script_id,r.script_name,r.device_id,r.state,r.message,r.trigger,r.started_at,r.finished_at
                FROM native_script_runs r JOIN (SELECT device_id,max(id) id FROM native_script_runs GROUP BY device_id) latest
                ON latest.id=r.id
                """);
            using var reader = command.ExecuteReader();
            var runs = new Dictionary<long, ScriptRun>();
            while (reader.Read())
                runs[reader.GetInt64(3)] = new(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2), reader.GetInt64(3),
                    Enum.Parse<ScriptRunState>(reader.GetString(4)), reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.GetString(6), DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(7)),
                    reader.IsDBNull(8) ? null : DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(8)));
            return runs;
        }
    }

    // 启动时调用：上次异常退出时仍在运行的脚本标为「结果待确认」，不自动重跑。
    public void RecoverInterruptedScriptRuns()
    {
        lock (gate)
            Execute("UPDATE native_script_runs SET state='Unknown',message='程序中断，脚本可能只执行了一部分，请核对设备。',finished_at=$p0 WHERE state='Running'",
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }
}
