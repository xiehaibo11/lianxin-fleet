namespace Lianxin.Fleet.Cli.Commands;

internal sealed class Options
{
    private readonly Dictionary<string, string?> values = new(StringComparer.Ordinal);
    public static Options Parse(IEnumerable<string> arguments)
    {
        var args = arguments.ToArray();
        var result = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var key = args[i];
            if (!key.StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"参数无效：{key}");
            if (result.values.ContainsKey(key))
                throw new ArgumentException($"参数重复：{key}");
            result.values[key] = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal) ? args[++i] : null;
        }
        return result;
    }
    public bool Has(string key) => values.ContainsKey(key);
    public string? Get(string key) => values.GetValueOrDefault(key);
    public string Required(string key) => Get(key) is { Length: > 0 } value ? value : throw new ArgumentException($"缺少 {key}。");
    public long[] Ids() => Required("--ids").Split(',').Select(long.Parse).Distinct().ToArray();
}
