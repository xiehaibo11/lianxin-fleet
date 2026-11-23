using System.Text.Json;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Infrastructure;

// 手机媒体区（AFC，根目录即 /var/mobile/Media）的文件操作。手机路径与本机路径都在这里严格校验，
// 子进程参数前加 "--"，路径不会被当成命令行选项。
internal sealed partial class HostDeviceTools
{
    // 媒体区自带的系统文件夹：删除会破坏相册、音乐等数据，只允许删除其中的内容。
    private static readonly HashSet<string> ProtectedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "/AirFair", "/Airlock", "/Books", "/DCIM", "/Downloads", "/MediaAnalysis", "/PhotoData", "/Photos",
        "/PublicStaging", "/Recordings", "/iTunes_Control"
    };
    // 照片库数据库与音乐库：整个子树都不允许删除、覆盖或新建，否则会损坏「照片」和「音乐」。
    private static readonly string[] ProtectedTrees = ["/PhotoData", "/iTunes_Control"];

    internal static bool InProtectedTree(string devicePath) => ProtectedTrees.Any(root =>
        devicePath.Equals(root, StringComparison.OrdinalIgnoreCase)
        || devicePath.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase));

    private static void CheckWritable(string devicePath)
    {
        if (InProtectedTree(devicePath))
            throw new ArgumentException("PhotoData 和 iTunes_Control 是照片库和音乐库的数据，里面的内容不能删除、覆盖或新建。");
    }

    // 手机上的一项是否存在、是否为文件夹；不存在时返回 null。
    public async Task<DeviceFileEntry?> StatAsync(string udid, string devicePath, CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        devicePath = DeviceManager.NormalizeDevicePath(devicePath);
        using var json = Parse(await ManageAsync("stat", [udid, devicePath], false, cancellationToken).ConfigureAwait(false),
            JsonValueKind.Object, "文件信息");
        if (Bool(json.RootElement, "exists") != true)
            return null;
        var name = devicePath == "/" ? "/" : devicePath[(devicePath.LastIndexOf('/') + 1)..];
        return new DeviceFileEntry(devicePath, name, Bool(json.RootElement, "dir") == true,
            Math.Max(0, Long(json.RootElement, "size") ?? 0), null);
    }

    public async Task<DeviceFileListing> ListFilesAsync(string udid, string devicePath, CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        devicePath = DeviceManager.NormalizeDevicePath(devicePath);
        return ParseFiles(devicePath, await ManageAsync("files", [udid, devicePath], false, cancellationToken)
            .ConfigureAwait(false));
    }

    public async Task CreateDirectoryAsync(string udid, string devicePath, CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        devicePath = DeviceManager.NormalizeDevicePath(devicePath);
        if (devicePath == "/")
            throw new ArgumentException("请填写新文件夹的名称。");
        CheckWritable(devicePath);
        await ManageAsync("mkdir", [udid, devicePath], true, cancellationToken, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
    }

    // 把本机文件上传到手机文件夹，返回手机上的完整路径；同名文件会被覆盖。
    public async Task<string> PushFileAsync(string udid, string localFile, string deviceDirectory,
        CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        localFile = LocalPath(localFile, "本机文件");
        var info = new FileInfo(localFile);
        if (!info.Exists || info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            throw new FileNotFoundException("本机文件不存在，或者是链接、文件夹。");
        var target = DeviceManager.JoinDevicePath(DeviceManager.NormalizeDevicePath(deviceDirectory), info.Name);
        CheckWritable(target);
        // 推送中断时手机上的目标文件可能已被截断，结果待确认。
        await CliAsync(["afc", "push", "--udid", udid, "--", localFile, target], true, cancellationToken,
            TimeSpan.FromMinutes(10), failureUncertain: true).ConfigureAwait(false);
        return target;
    }

    // 把手机上的文件或文件夹下载到本机文件夹，返回本机路径。不覆盖本机已有的同名项。
    public async Task<string> PullFileAsync(string udid, string devicePath, string localDirectory,
        CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        devicePath = DeviceManager.NormalizeDevicePath(devicePath);
        if (devicePath == "/")
            throw new ArgumentException("不能下载整个媒体区，请选择其中的文件或文件夹。");
        localDirectory = LocalPath(localDirectory, "本机文件夹");
        var folder = new DirectoryInfo(localDirectory);
        if (!folder.Exists || folder.LinkTarget is not null || folder.Attributes.HasFlag(FileAttributes.ReparsePoint))
            throw new DirectoryNotFoundException("本机文件夹不存在，或者是链接。");
        var name = devicePath[(devicePath.LastIndexOf('/') + 1)..];
        if (!LocalName(name))
            throw new ArgumentException($"「{name}」不能作为本机文件名，请先在手机上改名。");
        var target = Path.Combine(folder.FullName, name);
        if (File.Exists(target) || Directory.Exists(target))
            throw new IOException($"本机文件夹里已有「{name}」，请换一个文件夹或先移走它。");
        // 先拉到同一文件夹里的临时子文件夹，成功后再改名；失败或中断时删掉半截的副本，不挡住重试。
        var staging = Directory.CreateDirectory(Path.Combine(folder.FullName, ".lianxin-pull-" + Guid.NewGuid().ToString("N")[..12]));
        try
        {
            await CliAsync(["afc", "pull", "--udid", udid, "--", devicePath, staging.FullName], false, cancellationToken,
                TimeSpan.FromMinutes(10)).ConfigureAwait(false);
            var pulled = Path.Combine(staging.FullName, name);
            if (Directory.Exists(pulled))
                Directory.Move(pulled, target);
            else if (File.Exists(pulled))
                File.Move(pulled, target, overwrite: false);
            else
                throw new DeviceOperationException("设备工具没有拉取到文件，请重试。");
            return target;
        }
        finally
        {
            try
            {
                staging.Delete(recursive: true);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { /* 只剩临时副本，留给下次手动清理。 */ }
        }
    }

    // 删除文件或整个文件夹（含内容）。媒体区根目录和系统文件夹本身不允许删除。
    public async Task DeleteFileAsync(string udid, string devicePath, CancellationToken cancellationToken)
    {
        CheckUdid(udid);
        devicePath = DeviceManager.NormalizeDevicePath(devicePath);
        if (devicePath == "/" || ProtectedFolders.Contains(devicePath))
            throw new ArgumentException("不能删除媒体区根目录或系统文件夹，只能删除其中的内容。");
        CheckWritable(devicePath);
        // 删除文件夹中途失败时可能已删掉一部分，结果待确认。
        await CliAsync(["afc", "rm", "--udid", udid, "--", devicePath], true, cancellationToken,
            TimeSpan.FromMinutes(2), failureUncertain: true).ConfigureAwait(false);
    }

    internal static DeviceFileListing ParseFiles(string devicePath, string output)
    {
        using var json = Parse(output, JsonValueKind.Object, "文件列表");
        var entries = new List<DeviceFileEntry>();
        if (Property(json.RootElement, "entries") is { ValueKind: JsonValueKind.Array } items)
            foreach (var item in items.EnumerateArray())
            {
                if (String(item, "name") is not { } name || !DeviceManager.ValidDeviceName(name))
                    continue;
                var mtime = Long(item, "mtime");
                entries.Add(new DeviceFileEntry(DeviceManager.JoinDevicePath(devicePath, name), name,
                    Bool(item, "dir") == true, Math.Max(0, Long(item, "size") ?? 0),
                    mtime is > 0 and < 253402300799 ? DateTimeOffset.FromUnixTimeSeconds(mtime.Value) : null));
            }
        return new DeviceFileListing(devicePath, entries.OrderByDescending(entry => entry.IsDirectory)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToArray(),
            Bool(json.RootElement, "truncated") == true);
    }

    private string LocalPath(string? path, string label)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 1000 || path.Any(char.IsControl) || !AbsolutePath(path))
            throw new ArgumentException($"{label}必须是本机绝对路径。");
        return Path.GetFullPath(path);
    }

    // 本机文件名：Windows 另外排除保留名、结尾的点和空格。
    private bool LocalName(string name)
    {
        if (!DeviceManager.ValidDeviceName(name))
            return false;
        if (platform != "win32")
            return !name.Contains(':');
        var stem = name.Split('.')[0].TrimEnd();
        return name.IndexOfAny(['<', '>', ':', '"', '\\', '|', '?', '*']) < 0 && !name.EndsWith('.') && !name.EndsWith(' ')
            && !(stem.ToUpperInvariant() is "CON" or "PRN" or "AUX" or "NUL"
                || (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                    || stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) && char.IsAsciiDigit(stem[3])));
    }
}
