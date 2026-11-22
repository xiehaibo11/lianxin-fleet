using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;
using Microsoft.Data.Sqlite;

namespace Lianxin.Fleet.Core.Persistence;

public sealed partial class FleetStore : IDisposable
{
    private readonly object gate = new();
    private readonly SqliteConnection connection;
    private readonly FileStream? lease;
    private SqliteTransaction? transaction;
    private bool disposed;

    public FleetStore(string databasePath)
    {
        var path = databasePath == ":memory:" ? databasePath : Path.GetFullPath(databasePath);
        connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        try
        {
            if (path != ":memory:")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                try
                {
                    lease = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException error) { throw new InvalidOperationException("此数据库已被另一工作站占用，请先关闭使用同一数据库的桌面或命令行实例。", error); }
            }
            connection.Open();
            Execute("PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON; PRAGMA busy_timeout=3000;");
            Execute("""
                CREATE TABLE IF NOT EXISTS native_config (id INTEGER PRIMARY KEY CHECK(id=1), mode TEXT NOT NULL);
                CREATE TABLE IF NOT EXISTS native_devices (
                  id INTEGER PRIMARY KEY, name TEXT NOT NULL, udid TEXT NOT NULL UNIQUE,
                  wda_port INTEGER NOT NULL UNIQUE CHECK(wda_port BETWEEN 1024 AND 65535),
                  group_name TEXT NOT NULL, width INTEGER NOT NULL DEFAULT 390,
                  height INTEGER NOT NULL DEFAULT 844, last_seen INTEGER);
                CREATE TABLE IF NOT EXISTS native_jobs (
                  id INTEGER PRIMARY KEY, request_key TEXT NOT NULL UNIQUE, fingerprint TEXT NOT NULL,
                  action TEXT NOT NULL, parameters TEXT NOT NULL, state TEXT NOT NULL,
                  created_at INTEGER NOT NULL, finished_at INTEGER);
                CREATE TABLE IF NOT EXISTS native_targets (
                  job_id INTEGER NOT NULL REFERENCES native_jobs(id), device_id INTEGER NOT NULL REFERENCES native_devices(id),
                  state TEXT NOT NULL, error TEXT, PRIMARY KEY(job_id,device_id));
                CREATE INDEX IF NOT EXISTS native_targets_queue ON native_targets(state,job_id);
                """);
            var mode = Convert.ToString(Scalar("SELECT mode FROM native_config WHERE id=1"));
            if (string.IsNullOrEmpty(mode))
                Execute("INSERT INTO native_config VALUES(1,'real')");
            else if (mode != "real")
            {
                // 演示模式已移除；旧版演示库只含模拟设备，拒绝打开以免当成真机数据。
                throw new InvalidOperationException("这是旧版演示数据库，不能用于真机。");
            }
            Migrate();
        }
        catch
        {
            try
            {
                connection.Dispose();
            }
            finally { lease?.Dispose(); }
            throw;
        }
    }

    private SqliteCommand Command(string sql, params object?[] values)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        for (var i = 0; i < values.Length; i++)
            command.Parameters.AddWithValue("$p" + i, values[i] ?? DBNull.Value);
        return command;
    }
    private int Execute(string sql, params object?[] values)
    {
        using var command = Command(sql, values);
        return command.ExecuteNonQuery();
    }
    private object? Scalar(string sql, params object?[] values)
    {
        using var command = Command(sql, values);
        return command.ExecuteScalar();
    }
    private void Atomic(Action work)
    {
        using var active = connection.BeginTransaction();
        transaction = active;
        try
        {
            work();
            active.Commit();
        }
        finally { transaction = null; }
    }
    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
                return;
            disposed = true;
            try
            {
                connection.Dispose();
            }
            finally { lease?.Dispose(); }
        }
    }
    public IReadOnlyList<FleetDevice> GetDevices()
    {
        lock (gate)
        {
            using var command = Command($"SELECT {DeviceColumns} FROM native_devices ORDER BY id");
            using var reader = command.ExecuteReader();
            var devices = new List<FleetDevice>();
            while (reader.Read())
                devices.Add(ReadDevice(reader));
            return devices;
        }
    }
    public FleetDevice GetDevice(long id)
    {
        if (id <= 0)
            throw new ArgumentException("设备编号无效。");
        lock (gate)
        {
            using var command = Command($"SELECT {DeviceColumns} FROM native_devices WHERE id=$p0", id);
            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadDevice(reader) : throw new KeyNotFoundException("设备不存在。");
        }
    }
    private const string DeviceColumns = "id,name,udid,wda_port,group_name,width,height,last_seen,alias,ecid,product_type,os_version";
    private static FleetDevice ReadDevice(SqliteDataReader reader) => new(reader.GetInt64(0), reader.GetString(1),
        reader.GetString(2), reader.GetInt32(3), reader.GetString(4), reader.GetInt32(5), reader.GetInt32(6),
        reader.IsDBNull(7) ? null : DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(7)),
        Optional(reader, 8), Optional(reader, 9), Optional(reader, 10), Optional(reader, 11));
    private static string? Optional(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);

    public FleetDevice RegisterDevice(string name, string udid, int wdaPort, string groupName = "未分组")
    {
        name = FleetValidation.Text(name, 80, "设备名称");
        FleetValidation.Device(udid, wdaPort);
        groupName = FleetValidation.Text(groupName, 50, "分组名称");
        lock (gate)
        {
            try
            {
                Execute("INSERT INTO native_devices(name,udid,wda_port,group_name) VALUES($p0,$p1,$p2,$p3)", name, udid, wdaPort, groupName);
                return GetDevice(Convert.ToInt64(Scalar("SELECT last_insert_rowid()")));
            }
            catch (SqliteException error) when (error.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException("设备 UDID 或 WDA 本地端口已登记。", error);
            }
        }
    }
    public void AssignGroup(IReadOnlyList<long> ids, string groupName)
    {
        var devices = ResolveDevices(ids);
        var name = FleetValidation.Text(groupName, 50, "分组名称");
        lock (gate)
            Atomic(() =>
        {
            foreach (var device in devices)
                Execute("UPDATE native_devices SET group_name=$p0 WHERE id=$p1", name, device.Id);
        });
    }
    public IReadOnlyList<FleetDevice> ResolveDevices(IReadOnlyList<long> ids)
    {
        if (ids.Count is < 1 or > 1000)
            throw new ArgumentException("每次选择 1 至 1,000 台设备。");
        lock (gate)
            return ids.Distinct().Order().Select(GetDevice).ToArray();
    }
    public void UpdateDeviceStatus(long id, DeviceStatus status)
    {
        if (!status.Online)
            return;
        if (status.Width is <= 0 or > 20000 || status.Height is <= 0 or > 20000)
            throw new ArgumentException("屏幕尺寸无效。");
        lock (gate)
            Execute("UPDATE native_devices SET width=$p0,height=$p1,last_seen=$p2 WHERE id=$p3",
            status.Width, status.Height, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), id);
    }
}
