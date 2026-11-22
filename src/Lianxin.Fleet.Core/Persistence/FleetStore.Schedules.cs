using System.Text.Json;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;
using Microsoft.Data.Sqlite;

namespace Lianxin.Fleet.Core.Persistence;

public sealed partial class FleetStore
{
    private const string ScheduleColumns =
        "id,name,script_id,kind,at_minutes,weekdays,interval_minutes,target_group,target_ids,enabled,created_at,last_run_at";

    public IReadOnlyList<FleetSchedule> GetSchedules()
    {
        lock (gate)
        {
            using var command = Command($"SELECT {ScheduleColumns} FROM native_schedules ORDER BY id");
            using var reader = command.ExecuteReader();
            var schedules = new List<FleetSchedule>();
            while (reader.Read())
                schedules.Add(ReadSchedule(reader));
            return schedules;
        }
    }

    public FleetSchedule GetSchedule(long id)
    {
        lock (gate)
        {
            using var command = Command($"SELECT {ScheduleColumns} FROM native_schedules WHERE id=$p0", id);
            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadSchedule(reader) : throw new KeyNotFoundException("定时任务不存在。");
        }
    }

    private static FleetSchedule ReadSchedule(SqliteDataReader reader) => new(reader.GetInt64(0), reader.GetString(1),
        reader.GetInt64(2), Enum.Parse<ScheduleKind>(reader.GetString(3)), TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(reader.GetInt32(4))),
        reader.GetInt32(5), reader.GetInt32(6), reader.IsDBNull(7) ? null : reader.GetString(7),
        JsonSerializer.Deserialize<long[]>(reader.GetString(8)) ?? [], reader.GetInt64(9) == 1,
        DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(10)),
        reader.IsDBNull(11) ? null : DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(11)));

    // Id 为 0 时新建；其余字段整体覆盖，上次运行时间保持不变。
    public FleetSchedule SaveSchedule(FleetSchedule schedule)
    {
        var name = FleetValidation.Text(schedule.Name, 60, "定时任务名称");
        if (schedule.Kind == ScheduleKind.Daily && schedule.Weekdays is < 1 or > 127)
            throw new ArgumentException("每天模式至少选择一天。");
        if (schedule.Kind == ScheduleKind.Interval && schedule.IntervalMinutes is < 1 or > 10080)
            throw new ArgumentException("间隔须为 1 至 10080 分钟。");
        var group = string.IsNullOrWhiteSpace(schedule.TargetGroup) ? null : FleetValidation.Text(schedule.TargetGroup, 50, "分组名称");
        if (schedule.TargetIds.Count > 1000)
            throw new ArgumentException("定时任务最多指定 1000 台设备。");
        var ids = JsonSerializer.Serialize(schedule.TargetIds.Distinct().Order().ToArray());
        var minutes = schedule.At.Hour * 60 + schedule.At.Minute;
        var weekdays = schedule.Kind == ScheduleKind.Daily ? schedule.Weekdays : 0;
        var interval = schedule.Kind == ScheduleKind.Interval ? schedule.IntervalMinutes : 0;
        lock (gate)
        {
            GetScript(schedule.ScriptId);
            if (schedule.Id == 0)
            {
                // 新建的间隔任务从现在开始计时，不立即执行。
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                Execute($"""
                    INSERT INTO native_schedules({ScheduleColumns[3..]}) VALUES($p0,$p1,$p2,$p3,$p4,$p5,$p6,$p7,$p8,$p9,$p10)
                    """, name, schedule.ScriptId, schedule.Kind.ToString(), minutes, weekdays, interval, group, ids,
                    schedule.Enabled ? 1 : 0, now, now);
                return GetSchedule(Convert.ToInt64(Scalar("SELECT last_insert_rowid()")));
            }
            if (Execute("""
                UPDATE native_schedules SET name=$p0,script_id=$p1,kind=$p2,at_minutes=$p3,weekdays=$p4,
                interval_minutes=$p5,target_group=$p6,target_ids=$p7,enabled=$p8 WHERE id=$p9
                """, name, schedule.ScriptId, schedule.Kind.ToString(), minutes, weekdays, interval, group, ids,
                schedule.Enabled ? 1 : 0, schedule.Id) != 1)
                throw new KeyNotFoundException("定时任务不存在。");
            return GetSchedule(schedule.Id);
        }
    }

    public void SetScheduleEnabled(long id, bool enabled)
    {
        lock (gate)
            if (Execute("UPDATE native_schedules SET enabled=$p0 WHERE id=$p1", enabled ? 1 : 0, id) != 1)
                throw new KeyNotFoundException("定时任务不存在。");
    }

    public void DeleteSchedule(long id)
    {
        lock (gate)
            if (Execute("DELETE FROM native_schedules WHERE id=$p0", id) != 1)
                throw new KeyNotFoundException("定时任务不存在。");
    }

    public void MarkScheduleRun(long id, DateTimeOffset at)
    {
        lock (gate)
            Execute("UPDATE native_schedules SET last_run_at=$p0 WHERE id=$p1", at.ToUnixTimeMilliseconds(), id);
    }
}
