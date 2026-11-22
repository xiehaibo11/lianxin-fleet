using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Persistence;

public sealed partial class FleetStore
{
    public const string DefaultGroup = "未分组";

    public void SetAlias(long id, string? alias)
    {
        alias = string.IsNullOrWhiteSpace(alias) ? null : FleetValidation.Text(alias, 40, "别名");
        lock (gate)
        {
            if (Execute("UPDATE native_devices SET alias=$p0 WHERE id=$p1", alias, id) != 1)
                throw new KeyNotFoundException("设备不存在。");
        }
    }

    // 发现时补充硬件信息；只覆盖本次拿到的字段，拿不到的保留旧值。
    public void UpdateDeviceInfo(DiscoveredDevice device)
    {
        static string? Clean(string? value, int max) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
        lock (gate)
            Execute("""
                UPDATE native_devices SET ecid=coalesce($p0,ecid),product_type=coalesce($p1,product_type),
                os_version=coalesce($p2,os_version) WHERE upper(replace(udid,'-',''))=$p3
                """, Clean(device.Ecid, 32), Clean(device.ProductType, 40), Clean(device.OsVersion, 20),
                device.Udid.Replace("-", "", StringComparison.Ordinal).ToUpperInvariant());
    }

    // 分组 = 手动添加的空分组 ∪ 设备上已使用的分组名。
    public IReadOnlyList<string> GetGroups()
    {
        lock (gate)
        {
            using var command = Command("""
                SELECT name FROM native_groups UNION SELECT group_name FROM native_devices
                UNION SELECT $p0 ORDER BY 1
                """, DefaultGroup);
            using var reader = command.ExecuteReader();
            var groups = new List<string>();
            while (reader.Read())
                groups.Add(reader.GetString(0));
            return groups;
        }
    }

    public void AddGroup(string name)
    {
        name = FleetValidation.Text(name, 50, "分组名称");
        lock (gate)
            Execute("INSERT OR IGNORE INTO native_groups(name,created_at) VALUES($p0,$p1)",
                name, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    // 删除分组时组内设备回到「未分组」，设备本身不删除。
    public void DeleteGroup(string name)
    {
        name = FleetValidation.Text(name, 50, "分组名称");
        if (name == DefaultGroup)
            throw new InvalidOperationException("「未分组」不能删除。");
        lock (gate)
            Atomic(() =>
            {
                if (Convert.ToInt64(Scalar("SELECT count(*) FROM native_schedules WHERE target_group=$p0", name)) > 0)
                    throw new InvalidOperationException("有定时任务正在使用这个分组，请先修改定时任务。");
                Execute("UPDATE native_devices SET group_name=$p0 WHERE group_name=$p1", DefaultGroup, name);
                Execute("DELETE FROM native_groups WHERE name=$p0", name);
            });
    }
}
