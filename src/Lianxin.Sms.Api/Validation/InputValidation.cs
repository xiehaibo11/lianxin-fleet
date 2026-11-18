using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lianxin.Sms.Api.Protocol;

namespace Lianxin.Sms.Api.Validation;

internal static class InputValidation
{
    public const long MaxId = 9_007_199_254_740_991;
    public static void Require([DoesNotReturnIf(false)] bool condition, int status, string message)
    {
        if (!condition)
            throw new HttpFailure(status, message);
    }
    public static string Text(string? value, int max, string label)
    {
        Require(!string.IsNullOrWhiteSpace(value) && value.Length <= max, 400, $"{label}不能为空，最多 {max} 个字符。");
        return value!.Trim();
    }
    public static long Number(long value, long min, long max, string label)
    {
        Require(value >= min && value <= max, 400, label + "超出允许范围。");
        return value;
    }
    public static string Phone(string? value)
    {
        var normalized = Regex.Replace(value ?? "", "[ ()-]", "");
        Require(Regex.IsMatch(normalized, "^\\+[1-9][0-9]{7,14}$"), 400, "请使用带国家区号的号码，例如 +12025550120。");
        return normalized;
    }
    public static string Email(string? value)
    {
        var email = Text(value, 120, "邮箱").ToLowerInvariant();
        Require(Regex.IsMatch(email, "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$"), 400, "邮箱格式无效。");
        return email;
    }
    public static string? String(this JsonElement data, string key)
    {
        if (!data.TryGetProperty(key, out var value) || value.ValueKind != JsonValueKind.String)
            return null;
        try
        {
            return value.GetString();
        }
        catch (InvalidOperationException)
        {
            throw new HttpFailure(400, "JSON 字符串格式无效。");
        }
    }
    public static long Integer(this JsonElement data, string key, long min = 1, long max = MaxId)
    {
        Require(data.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _), 400, key + "编号无效。");
        return Number(value.GetInt64(), min, max, key);
    }
    public static long? OptionalInteger(this JsonElement data, string key) => data.TryGetProperty(key, out var value) && value.ValueKind != JsonValueKind.Null ? data.Integer(key) : null;
    public static long QueryNumber(this HttpRequest request, string key, long fallback, long min = 0, long max = 1_000_000)
    {
        if (!request.Query.TryGetValue(key, out var raw))
            return Number(fallback, min, max, key);
        Require(long.TryParse(raw, out _), 400, key + "无效。");
        return Number(long.Parse(raw!), min, max, key);
    }
}
