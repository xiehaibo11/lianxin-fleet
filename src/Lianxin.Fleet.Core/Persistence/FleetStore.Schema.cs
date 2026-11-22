namespace Lianxin.Fleet.Core.Persistence;

public sealed partial class FleetStore
{
    // 只做增量迁移：给旧库补列、补表，不删除或改写已有数据。
    private void Migrate()
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var command = Command("PRAGMA table_info(native_devices)"))
        using (var reader = command.ExecuteReader())
            while (reader.Read())
                columns.Add(reader.GetString(1));
        foreach (var column in new[] { "alias", "ecid", "product_type", "os_version" })
            if (!columns.Contains(column))
                Execute($"ALTER TABLE native_devices ADD COLUMN {column} TEXT");
        Execute("""
            CREATE TABLE IF NOT EXISTS native_groups (name TEXT PRIMARY KEY, created_at INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS native_scripts (
              id INTEGER PRIMARY KEY, name TEXT NOT NULL UNIQUE, source TEXT NOT NULL,
              repeat INTEGER NOT NULL CHECK(repeat BETWEEN 0 AND 100000),
              created_at INTEGER NOT NULL, updated_at INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS native_script_runs (
              id INTEGER PRIMARY KEY, script_id INTEGER NOT NULL, script_name TEXT NOT NULL,
              device_id INTEGER NOT NULL REFERENCES native_devices(id), state TEXT NOT NULL, message TEXT,
              trigger TEXT NOT NULL, started_at INTEGER NOT NULL, finished_at INTEGER);
            CREATE INDEX IF NOT EXISTS native_script_runs_device ON native_script_runs(device_id, id);
            CREATE TABLE IF NOT EXISTS native_schedules (
              id INTEGER PRIMARY KEY, name TEXT NOT NULL, script_id INTEGER NOT NULL REFERENCES native_scripts(id),
              kind TEXT NOT NULL, at_minutes INTEGER NOT NULL CHECK(at_minutes BETWEEN 0 AND 1439),
              weekdays INTEGER NOT NULL CHECK(weekdays BETWEEN 0 AND 127),
              interval_minutes INTEGER NOT NULL CHECK(interval_minutes BETWEEN 0 AND 10080),
              target_group TEXT, target_ids TEXT NOT NULL, enabled INTEGER NOT NULL,
              created_at INTEGER NOT NULL, last_run_at INTEGER);
            """);
    }
}
