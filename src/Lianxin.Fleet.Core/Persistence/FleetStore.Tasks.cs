using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Serialization;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Persistence;

public sealed partial class FleetStore
{
    public IReadOnlyList<FleetJob> GetJobs(int limit = 30)
    {
        if (limit is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(nameof(limit));
        lock (gate)
        {
            var ids = new List<long>();
            using (var command = Command("SELECT id FROM native_jobs ORDER BY id DESC LIMIT $p0", limit))
            using (var reader = command.ExecuteReader())
                while (reader.Read())
                    ids.Add(reader.GetInt64(0));
            return ids.Select(id => ReadJob(id)).ToArray();
        }
    }
    private FleetJob ReadJob(long id, bool withTargets = true)
    {
        var results = new List<JobTarget>();
        if (withTargets)
        {
            using var targets = Command("SELECT t.device_id,d.name,t.state,t.error FROM native_targets t JOIN native_devices d ON d.id=t.device_id WHERE t.job_id=$p0 ORDER BY d.id", id);
            using var reader = targets.ExecuteReader();
            while (reader.Read())
                results.Add(new(reader.GetInt64(0),
                reader.GetString(1), Enum.Parse<JobState>(reader.GetString(2)), reader.IsDBNull(3) ? null : reader.GetString(3)));
        }
        using var command = Command("SELECT request_key,action,parameters,state,created_at,finished_at FROM native_jobs WHERE id=$p0", id);
        using var row = command.ExecuteReader();
        if (!row.Read())
            throw new KeyNotFoundException("任务不存在。");
        return new(id, row.GetString(0), Enum.Parse<FleetAction>(row.GetString(1)),
            JsonSerializer.Deserialize<ActionParameters>(row.GetString(2), FleetJson.Options)!,
            Enum.Parse<JobState>(row.GetString(3)), DateTimeOffset.FromUnixTimeMilliseconds(row.GetInt64(4)), results,
            row.IsDBNull(5) ? null : DateTimeOffset.FromUnixTimeMilliseconds(row.GetInt64(5)));
    }
    // 任务线程和界面刷新同时读写同一个连接：不加锁时 SqliteConnection 的命令列表会被并发改坏（RemoveAt 越界）。
    public FleetJob GetJobHeader(long id)
    {
        lock (gate)
            return ReadJob(id, withTargets: false);
    }
    // 按编号读取任务和每台设备的结果（不受 GetJobs 的条数限制）。
    public FleetJob GetJob(long id)
    {
        lock (gate)
            return ReadJob(id);
    }
    public FleetJob Enqueue(string key, IReadOnlyList<long> ids, FleetAction action, ActionParameters parameters)
    {
        key = FleetValidation.Text(key, 80, "请求编号");
        var devices = ResolveDevices(ids);
        var serialized = JsonSerializer.Serialize(parameters, FleetJson.Options);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new
            {
                ids = devices.Select(d => d.Id),
                action,
                parameters
            }, FleetJson.Options))));
        lock (gate)
        {
            FleetJob? result = null;
            Atomic(() =>
            {
                using (var previous = Command("SELECT id,fingerprint FROM native_jobs WHERE request_key=$p0", key))
                using (var row = previous.ExecuteReader())
                    if (row.Read())
                    {
                        var id = row.GetInt64(0);
                        if (row.GetString(1) != fingerprint)
                            throw new InvalidOperationException("请求编号已用于其他动作。");
                        row.Close();
                        result = ReadJob(id);
                        return;
                    }
                if (Convert.ToInt64(Scalar("SELECT count(*) FROM native_targets WHERE state IN ('Queued','Running')")) + devices.Count > 2000)
                    throw new InvalidOperationException("待执行动作过多，请等待当前任务完成。");
                Execute("INSERT INTO native_jobs(request_key,fingerprint,action,parameters,state,created_at) VALUES($p0,$p1,$p2,$p3,'Queued',$p4)",
                    key, fingerprint, action.ToString(), serialized, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                var jobId = Convert.ToInt64(Scalar("SELECT last_insert_rowid()"));
                foreach (var device in devices)
                    Execute("INSERT INTO native_targets VALUES($p0,$p1,'Queued',NULL)", jobId, device.Id);
                result = ReadJob(jobId);
            });
            return result!;
        }
    }
    // 每台空闲设备只取其最早排队的任务，按任务编号排序，最多 limit 台。
    public IReadOnlyList<(long JobId, long DeviceId)> GetNextQueuedTargets(int limit)
    {
        lock (gate)
        {
            var targets = new List<(long, long)>();
            using var command = Command("""
                SELECT MIN(job_id),device_id FROM native_targets
                WHERE state='Queued' AND device_id NOT IN (SELECT device_id FROM native_targets WHERE state='Running')
                GROUP BY device_id ORDER BY 1,2 LIMIT $p0
                """, limit);
            using var rows = command.ExecuteReader();
            while (rows.Read())
                targets.Add((rows.GetInt64(0), rows.GetInt64(1)));
            return targets;
        }
    }
    public void StartTarget(long jobId, long deviceId)
    {
        lock (gate)
            Atomic(() =>
        {
            if (Execute("UPDATE native_targets SET state='Running' WHERE job_id=$p0 AND device_id=$p1 AND state='Queued'", jobId, deviceId) != 1)
                throw new InvalidOperationException("任务已被领取。");
            Execute("UPDATE native_jobs SET state='Running' WHERE id=$p0", jobId);
        });
    }
    public void FinishTarget(long jobId, long deviceId, JobState state, string? error = null)
    {
        if (state is not (JobState.Succeeded or JobState.Failed or JobState.Unknown))
            throw new ArgumentException("结果状态无效。");
        lock (gate)
            Atomic(() =>
        {
            Execute("UPDATE native_targets SET state=$p0,error=$p1 WHERE job_id=$p2 AND device_id=$p3 AND state='Running'",
                state.ToString(), error?[..Math.Min(error.Length, 600)], jobId, deviceId);
            FinishJob(jobId);
        });
    }
    private void FinishJob(long id)
    {
        var states = new HashSet<JobState>();
        using (var command = Command("SELECT state FROM native_targets WHERE job_id=$p0 GROUP BY state", id))
        using (var rows = command.ExecuteReader())
            while (rows.Read())
                states.Add(Enum.Parse<JobState>(rows.GetString(0)));
        if (states.Overlaps([JobState.Queued, JobState.Running]))
            return;
        var state = states.SetEquals([JobState.Succeeded]) ? JobState.Succeeded
            : states.SetEquals([JobState.Cancelled]) ? JobState.Cancelled
            : states.Contains(JobState.Unknown) ? JobState.Unknown
            : states.Contains(JobState.Succeeded) ? JobState.Partial : JobState.Failed;
        Execute("UPDATE native_jobs SET state=$p0,finished_at=$p1 WHERE id=$p2", state.ToString(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), id);
    }
    public void RecoverInterruptedJobs()
    {
        lock (gate)
            Atomic(() =>
        {
            Execute("UPDATE native_targets SET state='Unknown',error='服务中断，动作可能已经执行，请核对设备。' WHERE state='Running'");
            Execute("UPDATE native_targets SET state='Cancelled',error='服务重启，尚未执行的动作已取消。' WHERE state='Queued'");
            var ids = new List<long>();
            using (var command = Command("SELECT id FROM native_jobs WHERE state IN ('Queued','Running')"))
            using (var rows = command.ExecuteReader())
                while (rows.Read())
                    ids.Add(rows.GetInt64(0));
            foreach (var id in ids)
                FinishJob(id);
        });
    }
}
