using System.Text.RegularExpressions;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Validation;

public static class FleetValidation
{
    public static string Text(string? value, int max, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > max)
            throw new ArgumentException($"{label}不能为空，最多 {max} 个字符。");
        return value.Trim();
    }
    public static void Device(string udid, int port)
    {
        if (!Regex.IsMatch(udid ?? "", "^[A-Za-z0-9][A-Za-z0-9-]{7,79}$"))
            throw new ArgumentException("设备 UDID 格式无效。");
        if (port is < 1024 or > 65535)
            throw new ArgumentException("WDA 本机端口须为 1024 至 65535。");
    }
    private static double Coordinate(double? value)
    {
        if (value is null || !double.IsFinite(value.Value) || value < 0 || value > 1)
            throw new ArgumentException("屏幕坐标须在 0 到 1 之间。");
        return value.Value;
    }
    public static ActionParameters Action(FleetAction action, ActionParameters input, string approvedDirectory)
    {
        ArgumentNullException.ThrowIfNull(input);
        return action switch
        {
            FleetAction.Home or FleetAction.Lock or FleetAction.Unlock or FleetAction.VolumeUp
                or FleetAction.VolumeDown or FleetAction.AppSwitcher => new(),
            FleetAction.Tap or FleetAction.DoubleTap => new(X: Coordinate(input.X), Y: Coordinate(input.Y)),
            FleetAction.LongPress => LongPress(input),
            FleetAction.Swipe => Swipe(input),
            FleetAction.Text => new(Text: Text(input.Text, 1000, "输入文本")),
            FleetAction.Clipboard => new(Text: Text(input.Text, 1000, "剪贴板文本")),
            FleetAction.OpenUrl => new(Text: Url(input.Text)),
            FleetAction.Launch or FleetAction.Terminate => new(BundleId: BundleId(input.BundleId)),
            FleetAction.Install => new(AppPath: AppPath(input.AppPath, approvedDirectory)),
            _ => throw new ArgumentException("不支持此动作。")
        };
    }
    private static ActionParameters LongPress(ActionParameters input)
    {
        if (!double.IsFinite(input.Duration) || input.Duration is < 0.2 or > 5)
            throw new ArgumentException("长按时长须为 0.2 至 5 秒。");
        return new(X: Coordinate(input.X), Y: Coordinate(input.Y), Duration: input.Duration);
    }
    public static string Url(string? value)
    {
        var url = Text(value, 2000, "链接");
        // 允许 App 自定义协议（如 weixin://），但拒绝空白和控制字符，避免拼接出额外参数。
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !Regex.IsMatch(uri.Scheme, "^[a-z][a-z0-9+.-]{0,31}$")
            || url.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
            throw new ArgumentException("链接格式无效，例如 https://example.com 或 app://path。");
        return url;
    }
    private static ActionParameters Swipe(ActionParameters input)
    {
        if (!double.IsFinite(input.Duration) || input.Duration is < 0.05 or > 5)
            throw new ArgumentException("滑动时长须为 0.05 至 5 秒。");
        return new(FromX: Coordinate(input.FromX), FromY: Coordinate(input.FromY), ToX: Coordinate(input.ToX),
            ToY: Coordinate(input.ToY), Duration: input.Duration);
    }
    public static string BundleId(string? value)
    {
        var bundle = Text(value, 160, "应用 Bundle ID");
        if (!Regex.IsMatch(bundle, "^[A-Za-z0-9-]+(?:\\.[A-Za-z0-9-]+)+$"))
            throw new ArgumentException("Bundle ID 格式无效。");
        return bundle;
    }
    // 抛出的异常都带本程序自己的中文说明（不含系统错误里的本机路径），开放接口可以原样转给调用方。
    public static string AppPath(string? value, string approvedDirectory)
    {
        var path = Text(value, 1000, "已签名应用路径");
        if (!Path.IsPathFullyQualified(path))
            throw new ArgumentException("应用路径必须为本机绝对路径。");
        // 先只做字符串判断、不访问文件系统：设备路径（\\?\、\\.\）一律拒绝，规范化后必须落在批准目录内。
        // 否则 \\某台电脑\共享 这样的路径会让本机去连接外部共享（可能泄露登录凭据），不同的错误信息还能探测目录外的文件。
        if (IsDevicePath(path) || !Inside(Path.GetFullPath(approvedDirectory), Path.GetFullPath(path)))
            throw new UnauthorizedAccessException("只能安装批准目录内的应用。");
        if (!Directory.Exists(approvedDirectory))
            throw new ArgumentException("应用批准目录不存在。");
        string approved, app;
        try
        {
            approved = CanonicalPath(approvedDirectory);
            app = CanonicalPath(path);
        }
        catch (Exception error) when (error is UnauthorizedAccessException
            || (error is IOException && error.Message is not (MissingError or LinkError)))
        {
            throw new IOException("无法读取应用路径，请确认文件存在且有访问权限。", error);
        }
        // 符号链接解析后再检查一次，链接指向批准目录外时拒绝。
        if (!Inside(approved, app))
            throw new UnauthorizedAccessException("只能安装批准目录内的应用。");
        if (!(app.EndsWith(".ipa", StringComparison.OrdinalIgnoreCase) && File.Exists(app))
            && !(app.EndsWith(".app", StringComparison.OrdinalIgnoreCase) && Directory.Exists(app)))
            throw new ArgumentException("仅支持签名 .ipa 文件或 .app 目录。");
        return app;
    }

    private const string LinkError = "无法解析应用路径中的链接。";
    private const string MissingError = "应用或批准目录不存在。";

    private static bool IsDevicePath(string path) =>
        path.Replace('/', '\\') is var value && (value.StartsWith(@"\\?\", StringComparison.Ordinal)
            || value.StartsWith(@"\\.\", StringComparison.Ordinal));

    // 纯字符串比较：child 在 root 之下（不含 root 本身）。
    private static bool Inside(string root, string child)
    {
        var relative = Path.GetRelativePath(root, child);
        return !(relative == "." || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || Path.IsPathRooted(relative));
    }
    private static string CanonicalPath(string path)
    {
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full)!;
        var current = root;
        foreach (var part in full[root.Length..].Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            FileSystemInfo item = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);
            if (!item.Exists)
                throw new FileNotFoundException(MissingError);
            if (item.LinkTarget is not null)
                current = item.ResolveLinkTarget(true)?.FullName ?? throw new IOException(LinkError);
        }
        return Path.GetFullPath(current);
    }
}
