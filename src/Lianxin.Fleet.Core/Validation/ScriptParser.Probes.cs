using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Validation;

// 条件、元素选择器、颜色、区域与模板图片名称的解析和校验。
public static partial class ScriptParser
{
    public const int MaxSelectorLength = 500;
    private const string TypePrefix = "XCUIElementType";

    private static readonly Dictionary<string, ElementSelectorKind> SelectorKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["文字"] = ElementSelectorKind.Text,
        ["text"] = ElementSelectorKind.Text,
        ["包含"] = ElementSelectorKind.Contains,
        ["contains"] = ElementSelectorKind.Contains,
        ["id"] = ElementSelectorKind.Id,
        ["类型"] = ElementSelectorKind.Type,
        ["type"] = ElementSelectorKind.Type,
        ["xpath"] = ElementSelectorKind.XPath,
        ["谓词"] = ElementSelectorKind.Predicate,
        ["predicate"] = ElementSelectorKind.Predicate
    };

    private static ScriptCondition Condition(string text)
    {
        // 开头的「非」可以连写多个：循环剥掉并切换取反，不递归（上万个「非」也不会栈溢出），整行只扫描一遍。
        var negate = false;
        string word, rest;
        var start = 0;
        while (true)
        {
            while (start < text.Length && text[start] is ' ' or '\t')
                start++;
            var space = text.IndexOfAny([' ', '\t'], start);
            word = space < 0 ? text[start..] : text[start..space];
            if (!word.Equals("非", StringComparison.Ordinal) && !word.Equals("not", StringComparison.OrdinalIgnoreCase))
            {
                rest = space < 0 ? "" : text[(space + 1)..].Trim();
                break;
            }
            negate = !negate;
            start = space < 0 ? text.Length : space + 1;
        }
        var condition = Single(word, rest);
        if (!negate)
            return condition;
        return condition with
        {
            Negate = !condition.Negate
        };
    }

    private static ScriptCondition Single(string word, string rest)
    {
        var args = rest.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        switch (word.ToLowerInvariant())
        {
            case "存在" or "exists":
                return new(SoleElement(rest));
            case "不存在" or "notexists":
                return new(SoleElement(rest), true);
            case "比色" or "color":
                return new(ColorAt(args));
            case "找到颜色" or "hascolor":
                return new(ColorSearch(args));
            case "找到图片" or "hasimage":
                return new(ImageProbe(args, 1, 2));
            case "":
                throw new ArgumentException("缺少条件，例如：如果 存在 文字=登录。");
            default:
                throw new ArgumentException($"无法识别的条件「{word}」。可用：存在、不存在、比色、找到颜色、找到图片，前面加「非」表示取反。");
        }
    }

    private static ScriptProbe SoleElement(string text)
    {
        var probe = ElementProbe(text, out var left);
        return left.Length == 0 ? probe
            : throw new ArgumentException($"选择器后面多了「{left}」。值含空格时请用英文双引号括起来，例如 文字=\"立即 登录\"。");
    }

    // 键=值；值以英文双引号开头时读到配对的引号（\" 与 \\ 转义），否则读到第一个空白。left 是剩余内容。
    private static ScriptProbe ElementProbe(string text, out string left)
    {
        text = text.TrimStart();
        var equals = text.IndexOf('=');
        var key = equals < 0 ? "" : text[..equals];
        if (equals <= 0 || key.Any(char.IsWhiteSpace) || !SelectorKeys.TryGetValue(key, out var kind))
            throw new ArgumentException("选择器格式为 键=值，键可以是 文字、包含、id、类型、xpath、谓词，例如 文字=登录。");
        var after = text[(equals + 1)..];
        string value;
        if (after.StartsWith('"'))
        {
            var builder = new StringBuilder();
            var index = 1;
            for (; index < after.Length && after[index] != '"'; index++)
            {
                if (after[index] == '\\' && index + 1 < after.Length && after[index + 1] is '"' or '\\')
                    index++;
                builder.Append(after[index]);
            }
            if (index >= after.Length)
                throw new ArgumentException("选择器的英文双引号没有闭合。");
            value = builder.ToString();
            left = after[(index + 1)..].Trim();
        }
        else
        {
            var end = after.IndexOfAny([' ', '\t']);
            value = end < 0 ? after : after[..end];
            left = end < 0 ? "" : after[end..].Trim();
        }
        return new ScriptProbe(ScriptProbeKind.Element, CheckSelector(new ElementSelector(kind, value)));
    }

    // 选择器校验；类型统一补全为 XCUIElementType 前缀。设备通道发送前也会再校验一次。
    public static ElementSelector CheckSelector(ElementSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var value = selector.Value ?? "";
        if (!Enum.IsDefined(selector.Kind))
            throw new ArgumentException("不支持此选择器。");
        if (value.Length == 0 || value.Length > MaxSelectorLength)
            throw new ArgumentException($"选择器的值不能为空，最多 {MaxSelectorLength} 个字符。");
        if (value.Any(char.IsControl))
            throw new ArgumentException("选择器不能包含换行等控制字符。");
        if (selector.Kind != ElementSelectorKind.Type)
            return selector;
        var name = value.StartsWith(TypePrefix, StringComparison.Ordinal) ? value[TypePrefix.Length..] : value;
        if (!Regex.IsMatch(name, "^[A-Za-z]{1,40}$"))
            throw new ArgumentException("元素类型只能是英文字母，例如 类型=Button 或 类型=XCUIElementTypeButton。");
        return selector with
        {
            Value = TypePrefix + char.ToUpperInvariant(name[0]) + name[1..]
        };
    }

    // 写回脚本用的选择器文本，与 ElementProbe 的读法互逆。
    public static string FormatSelector(ElementSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var key = selector.Kind switch
        {
            ElementSelectorKind.Text => "文字",
            ElementSelectorKind.Contains => "包含",
            ElementSelectorKind.Id => "id",
            ElementSelectorKind.Type => "类型",
            ElementSelectorKind.XPath => "xpath",
            _ => "谓词"
        };
        var value = selector.Kind == ElementSelectorKind.Type && selector.Value.StartsWith(TypePrefix, StringComparison.Ordinal)
            ? selector.Value[TypePrefix.Length..] : selector.Value;
        var quote = value.StartsWith('"') || value.Any(char.IsWhiteSpace);
        return key + "=" + (quote ? "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal) + "\"" : value);
    }

    // 模板与截图名称：不含路径和扩展名（.png 可省略），防止访问 templates/captures 以外的文件。
    public static string TemplateName(string? value)
    {
        var name = value?.Trim() ?? "";
        if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];
        if (!Regex.IsMatch(name, @"^[\p{L}\p{N}_-]{1,64}$"))
            throw new ArgumentException("图片名称只能包含文字、数字、下划线和短横线，不能带路径，最多 64 个字符。");
        return name;
    }

    // 比色 x y #RRGGBB [容差]
    private static ScriptProbe ColorAt(string[] args)
    {
        if (args.Length is < 3 or > 4)
            throw new ArgumentException("格式为：比色 x y #RRGGBB [容差]。");
        return new(ScriptProbeKind.ColorAt, X: Coordinate(args[0]), Y: Coordinate(args[1]), Rgb: Color(args[2]),
            Tolerance: args.Length > 3 ? Integer(args[3], 0, 255, "容差") : DefaultTolerance);
    }

    // #RRGGBB [容差] [区域 x1 y1 x2 y2]
    private static ScriptProbe ColorSearch(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("缺少颜色，格式为：#RRGGBB [容差] [区域 x1 y1 x2 y2]。");
        var rgb = Color(args[0]);
        var index = 1;
        var tolerance = DefaultTolerance;
        if (index < args.Length && !IsRegion(args[index]))
            tolerance = Integer(args[index++], 0, 255, "容差");
        var region = ScreenRegion.Full;
        if (index < args.Length)
        {
            if (!IsRegion(args[index]) || args.Length - index != 5)
                throw new ArgumentException("区域格式为：区域 x1 y1 x2 y2（0–1 的相对位置）。");
            region = new(Coordinate(args[index + 1]), Coordinate(args[index + 2]), Coordinate(args[index + 3]),
                Coordinate(args[index + 4]));
            if (region.Left >= region.Right || region.Top >= region.Bottom)
                throw new ArgumentException("区域的左上角须在右下角的左上方。");
        }
        return new(ScriptProbeKind.ColorSearch, Rgb: rgb, Tolerance: tolerance, Region: region);
    }

    private static ScriptProbe ImageProbe(string[] args, int min, int max)
    {
        if (args.Length < min || args.Length > max)
            throw new ArgumentException("格式为：图片名称 [相似度]，相似度为 0.5 至 1，默认 0.9。");
        var similarity = args.Length > 1 ? ParseNumber(args[1]) : DefaultSimilarity;
        if (similarity is < 0.5 or > 1)
            throw new ArgumentException("相似度须为 0.5 至 1 之间的数字。");
        return new(ScriptProbeKind.Image, Template: TemplateName(args[0]), Similarity: similarity);
    }

    private static bool IsRegion(string word) => word is "区域" || word.Equals("region", StringComparison.OrdinalIgnoreCase);

    private static double Coordinate(string text)
    {
        var value = ParseNumber(text);
        return value is >= 0 and <= 1 ? value : throw new ArgumentException("屏幕坐标须在 0 到 1 之间。");
    }

    private static int Color(string text)
    {
        var hex = text.StartsWith('#') ? text[1..] : text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text[2..] : "";
        return hex.Length == 6 && int.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var rgb)
            ? rgb : throw new ArgumentException($"「{text}」不是有效颜色，格式为 #RRGGBB，例如 #FF8800。");
    }
}
