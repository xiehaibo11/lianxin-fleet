namespace Lianxin.Fleet.Core.OpenApi;

// 开放接口的电脑端文件中转目录（数据目录下的 transfer）。fsyncPullPush 的电脑端路径只能落在这里：
// 相对路径按中转目录解析，绝对路径必须位于其中；目录外的路径、经过链接（符号链接、目录联接）的路径和非法文件名一律拒绝。
internal sealed class TransferFolder
{
    private const string StagingName = ".fleet-staging";

    private TransferFolder(string root) => Root = root;

    public string Root
    {
        get;
    }

    public static TransferFolder Open(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new OpenApiException(OpenApiCodes.Unsupported, "联信 Fleet 暂不支持电脑端文件中转（当前中控没有配置中转目录）。");
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        var info = Directory.CreateDirectory(root);
        if (info.LinkTarget is not null || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            throw new OpenApiException(OpenApiCodes.DeviceFailed, "开放接口中转目录是链接，已拒绝使用：" + root);
        return new TransferFolder(info.FullName.TrimEnd(Path.DirectorySeparatorChar));
    }

    // 返回中转目录内的完整路径（可以还不存在）。
    public string Resolve(string path, string label)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 1000 || path.Any(char.IsControl))
            throw OpenApiCall.Invalid($"{label}不能为空，最多 1000 个字符，不能包含控制字符。");
        // 以下到边界判断为止都是纯字符串运算，UNC（\\电脑\共享）、设备路径和目录外的路径在访问文件系统之前就被拒绝。
        if (path.Replace('/', '\\') is var raw && (raw.StartsWith(@"\\?\", StringComparison.Ordinal) || raw.StartsWith(@"\\.\", StringComparison.Ordinal)))
            throw OutsideError(label);
        string full;
        try
        {
            full = Path.GetFullPath(Path.IsPathFullyQualified(path) ? path : Path.Combine(Root, path));
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw OutsideError(label);
        }
        var relative = Path.GetRelativePath(Root, full);
        if (relative == ".")
            return Root;
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
            throw OutsideError(label);
        var current = Root;
        foreach (var part in relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!ValidName(part))
                throw OpenApiCall.Invalid($"{label}里的「{OpenApiBackend.Short(part)}」不能作为电脑上的文件名。");
            current = Path.Combine(current, part);
            FileSystemInfo item = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
            if (item.Exists && (item.LinkTarget is not null || item.Attributes.HasFlag(FileAttributes.ReparsePoint)))
                throw OpenApiCall.Invalid($"{label}经过链接（{OpenApiBackend.Short(part)}），已拒绝：中转目录里不能使用符号链接或目录联接。");
        }
        return full;
    }

    public string ExistingFile(string path, string label)
    {
        var full = Resolve(path, label);
        if (!File.Exists(full))
            throw OpenApiCall.Invalid($"中转目录里没有这个文件：{Display(full)}");
        return full;
    }

    // 推送改名或拉取改名时使用的临时文件夹，用完即删。
    public Staging CreateStaging()
    {
        var path = Path.Combine(Root, StagingName, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new Staging(path);
    }

    // 相对中转目录的显示路径，用于提示。
    public string Display(string full) => Path.GetRelativePath(Root, full) is var relative && relative != "." ? relative : Root;

    public static bool ValidName(string name)
    {
        if (name.Length is 0 or > 255 || name is "." or ".." || name.Trim().Length == 0
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains(':'))
            return false;
        if (!OperatingSystem.IsWindows())
            return true;
        var stem = name.Split('.')[0].TrimEnd().ToUpperInvariant();
        return !name.EndsWith('.') && !name.EndsWith(' ')
            && stem is not ("CON" or "PRN" or "AUX" or "NUL")
            && !(stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal))
                && char.IsAsciiDigit(stem[3]));
    }

    private OpenApiException OutsideError(string label) => OpenApiCall.Invalid(
        $"{label}必须位于开放接口中转目录 {Root} 内：可以写相对路径（例如 a.png 或 sub/a.png），也可以写这个目录里的绝对路径。");

    internal sealed class Staging(string path) : IDisposable
    {
        public string Path { get; } = path;

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { /* 下次清理；里面只有临时副本。 */ }
        }
    }
}
