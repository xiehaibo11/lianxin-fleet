namespace Lianxin.Fleet.Mac.Interaction;

// Anthropic API 密钥单独保存在数据目录的 agent-key 文件，macOS 上权限为 600（只有当前用户可读写），
// 不写进 settings.json，导出或拷贝设置时不会带走密钥。
public static class AgentKeyStore
{
    private const string FileName = "agent-key";

    public static string? Load(string dataDirectory)
    {
        try
        {
            var key = File.ReadAllText(Path.Combine(dataDirectory, FileName)).Trim();
            return key.Length == 0 ? null : key;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static void Save(string dataDirectory, string key)
    {
        if (key.Length > 400 || key.Any(char.IsWhiteSpace))
            throw new ArgumentException("API 密钥格式无效。");
        var path = Path.Combine(dataDirectory, FileName);
        var pending = path + ".tmp";
        File.Delete(pending);
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using (var writer = new StreamWriter(new FileStream(pending, options)))
            writer.Write(key);
        File.Move(pending, path, overwrite: true);
    }

    public static void Delete(string dataDirectory) => File.Delete(Path.Combine(dataDirectory, FileName));
}
