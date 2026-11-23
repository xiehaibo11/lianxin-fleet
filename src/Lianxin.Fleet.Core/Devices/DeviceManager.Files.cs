using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Devices;

// 媒体区（/var/mobile/Media）文件操作与电源。手机路径一律以 / 开头、相对媒体区根目录。
public sealed partial class DeviceManager
{
    public const int MaxDevicePathLength = 1024;

    public Task<DeviceFileListing> ListFilesAsync(FleetDevice device, string devicePath = "/",
        CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.ListFilesAsync(device.Udid, devicePath, cancellationToken);
    }

    // 直接问手机这一项是否存在（不依赖列表缓存和 1000 项上限）；不存在时返回 null。
    public Task<DeviceFileEntry?> GetFileAsync(FleetDevice device, string devicePath, CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.StatAsync(device.Udid, devicePath, cancellationToken);
    }

    public Task CreateDirectoryAsync(FleetDevice device, string devicePath, CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.CreateDirectoryAsync(device.Udid, devicePath, cancellationToken);
    }

    // 上传本机文件到手机文件夹，返回手机上的路径；同名文件会被覆盖（需要时先用 GetFileAsync 确认）。中断时结果待确认。
    public Task<string> PushFileAsync(FleetDevice device, string localFile, string deviceDirectory,
        CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.PushFileAsync(device.Udid, localFile, deviceDirectory, cancellationToken);
    }

    // 下载手机文件或文件夹到本机已有的文件夹，返回本机路径；本机已有同名项时拒绝，不覆盖。失败时不留下半截的副本。
    public Task<string> PullFileAsync(FleetDevice device, string devicePath, string localDirectory,
        CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.PullFileAsync(device.Udid, devicePath, localDirectory, cancellationToken);
    }

    // 删除文件或文件夹（含内容）；根目录与 DCIM 等系统文件夹本身不允许删除，PhotoData、iTunes_Control 整个子树都不允许删除。
    public Task DeleteFileAsync(FleetDevice device, string devicePath, CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.DeleteFileAsync(device.Udid, devicePath, cancellationToken);
    }

    // 重启与关机：中断或连接断开时结果标为待确认。手机重启后开发者镜像失效，需要重新挂载。
    public Task RestartAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.RestartAsync(device.Udid, cancellationToken);
    }

    public Task ShutdownAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.ShutdownAsync(device.Udid, cancellationToken);
    }

    // 规范化手机路径：必须以 / 开头，不含 . 或 .. 段、反斜杠和控制字符，去掉结尾的 /。
    public static string NormalizeDevicePath(string? path)
    {
        if (string.IsNullOrEmpty(path) || path.Length > MaxDevicePathLength || path[0] != '/')
            throw new ArgumentException($"手机路径必须以 / 开头，最多 {MaxDevicePathLength} 个字符。");
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(part => !ValidDeviceName(part)) || path.Contains("//", StringComparison.Ordinal))
            throw new ArgumentException("手机路径无效：不能包含 . 或 .. 段、连续的 /、反斜杠或控制字符。");
        return "/" + string.Join('/', parts);
    }

    public static string JoinDevicePath(string directory, string name)
    {
        if (!ValidDeviceName(name))
            throw new ArgumentException("文件名无效：不能为空，不能包含 /、反斜杠或控制字符，也不能是 . 或 ..。");
        var parent = NormalizeDevicePath(directory);
        return NormalizeDevicePath(parent == "/" ? "/" + name : parent + "/" + name);
    }

    public static bool ValidDeviceName(string name) =>
        name.Length is > 0 and <= 255 && name is not ("." or "..") && !name.Contains('/') && !name.Contains('\\')
        && !name.Any(char.IsControl) && name.Trim().Length > 0;
}
