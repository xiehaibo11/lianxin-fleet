using System.Globalization;
using System.Xml;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Validation;

// 抓取节点：把 WDA 界面树 XML 整理成可见元素清单，并给出能直接写进脚本的选择器。
// 选择器按整棵树核对唯一性（与 WDA 查找一样不区分是否可见），找不到唯一的就标为「不唯一」，不会悄悄点到别的元素。
public static partial class ScriptParser
{
    public const int MaxListedElements = 300;
    // 文字太长或含换行时，「包含=」使用的片段长度上限。
    private const int MaxFragment = 100;

    private sealed record Node(string Type, string? Name, string? Label, string? Value, bool Visible,
        double X, double Y, double Width, double Height);

    public static IReadOnlyList<ScreenElement> DescribeElements(string? pageSource, int limit = MaxListedElements)
    {
        if (string.IsNullOrWhiteSpace(pageSource))
            return [];
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreWhitespace = true,
            MaxCharactersInDocument = 64_000_000
        };
        var nodes = new List<Node>();
        try
        {
            using var reader = XmlReader.Create(new StringReader(pageSource), settings);
            while (reader.Read())
                if (reader.NodeType == XmlNodeType.Element)
                    nodes.Add(new Node(reader.GetAttribute("type") ?? reader.LocalName, Attribute(reader, "name"),
                        Attribute(reader, "label"), Attribute(reader, "value"), reader.GetAttribute("visible") != "false",
                        Size(reader, "x"), Size(reader, "y"), Size(reader, "width"), Size(reader, "height")));
        }
        catch (XmlException error)
        {
            throw new ArgumentException("界面树不是有效的 XML：" + error.Message, error);
        }
        var index = new SelectorIndex(nodes);
        var elements = new List<ScreenElement>();
        foreach (var node in nodes)
        {
            if (elements.Count >= limit)
                break;
            if (!node.Visible || node.Width <= 0 || node.Height <= 0 || node.Type is "XCUIElementTypeApplication" or "XCUIElementTypeWindow"
                || (node.Name is null && node.Label is null && node.Value is null && !Interactive(node.Type)))
                continue;
            var (selector, unique) = Suggest(node, index);
            elements.Add(new ScreenElement(node.Type, Display(node.Name), Display(node.Label), Display(node.Value),
                node.X, node.Y, node.Width, node.Height, selector, unique));
        }
        return elements;
    }

    // 抓取节点列表里的一行：选择器、类型和位置（点）；不唯一的选择器会注明。
    public static string Summary(ScreenElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        var type = element.Type.StartsWith(TypePrefix, StringComparison.Ordinal) ? element.Type[TypePrefix.Length..] : element.Type;
        return string.Create(CultureInfo.InvariantCulture,
            $"{element.Selector}{(element.Unique ? "" : "（不唯一，会点第一个）")}    {type} ({element.X:0},{element.Y:0} {element.Width:0}×{element.Height:0})");
    }

    // 候选依次为：id（name 与 label 不同时）、显示文字、值、name 的精确匹配，文字太长或含换行时的「包含」片段，最后是类型。
    // 取第一个在整棵树里唯一的；都不唯一时用第一个候选并标为不唯一。
    private static (string Selector, bool Unique) Suggest(Node node, SelectorIndex index)
    {
        var candidates = new List<(ElementSelector Selector, int Count)>();
        void Add(ElementSelectorKind kind, string? value, Func<string, int> count)
        {
            if (value is null)
                return;
            try
            {
                var selector = CheckSelector(new ElementSelector(kind, value));
                candidates.Add((selector, count(selector.Value)));
            }
            catch (ArgumentException) { /* 值不能写成这种选择器，换下一种。 */ }
        }
        if (node.Name is not null && node.Name != node.Label)
            Add(ElementSelectorKind.Id, node.Name, index.WithName);
        foreach (var text in new[] { node.Label, node.Value, node.Name })
            Add(ElementSelectorKind.Text, text, index.WithText);
        foreach (var text in new[] { node.Label, node.Value, node.Name })
            Add(ElementSelectorKind.Contains, Fragment(text), index.Containing);
        Add(ElementSelectorKind.Type, node.Type, index.WithType);
        var best = candidates.FirstOrDefault(item => item.Count == 1);
        return best.Selector is not null ? (FormatSelector(best.Selector), true)
            : candidates.Count > 0 ? (FormatSelector(candidates[0].Selector), false)
            : (FormatSelector(new ElementSelector(ElementSelectorKind.Type, TypePrefix + "Other")), false);
    }

    // 能放进「包含=」的一段：按换行等控制字符切开取最长的一段，截到 MaxFragment 个字符；本来就能精确匹配的文字不需要片段。
    private static string? Fragment(string? text)
    {
        if (text is null || (text.Length <= MaxSelectorLength && !text.Any(char.IsControl)))
            return null;
        var piece = text.Split(text.Where(char.IsControl).Distinct().ToArray(), StringSplitOptions.TrimEntries)
            .MaxBy(part => part.Length) ?? "";
        piece = piece.Length > MaxFragment ? piece[..MaxFragment].Trim() : piece;
        return piece.Length >= 2 ? piece : null;
    }

    private static string? Display(string? value) =>
        value is null ? null : value.Length > MaxSelectorLength ? value[..MaxSelectorLength] : value;

    private static string? Attribute(XmlReader reader, string name)
    {
        var value = reader.GetAttribute(name);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static double Size(XmlReader reader, string name) =>
        double.TryParse(reader.GetAttribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            && double.IsFinite(value) ? value : 0;

    private static bool Interactive(string type) => type is "XCUIElementTypeButton" or "XCUIElementTypeTextField"
        or "XCUIElementTypeSecureTextField" or "XCUIElementTypeTextView" or "XCUIElementTypeSwitch"
        or "XCUIElementTypeSearchField" or "XCUIElementTypeCell";

    // 各种选择器在整棵树里命中几个元素，语义与 DeviceBridge.Locator 发给 WDA 的定位方式一致。
    private sealed class SelectorIndex
    {
        private readonly List<Node> nodes;
        private readonly Dictionary<string, int> names = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> texts = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> types = new(StringComparer.Ordinal);

        public SelectorIndex(List<Node> nodes)
        {
            this.nodes = nodes;
            foreach (var node in nodes)
            {
                Count(names, node.Name);
                Count(types, node.Type);
                foreach (var text in new[] { node.Label, node.Name, node.Value }.OfType<string>().Distinct())
                    Count(texts, text);
            }
        }

        public int WithName(string value) => names.GetValueOrDefault(value);
        public int WithText(string value) => texts.GetValueOrDefault(value);
        public int WithType(string value) => types.GetValueOrDefault(value);

        // WDA 的 CONTAINS 区分大小写。
        public int Containing(string value) =>
            nodes.Count(node => Has(node.Label, value) || Has(node.Name, value) || Has(node.Value, value));

        private static bool Has(string? text, string value) => text is not null && text.Contains(value, StringComparison.Ordinal);

        private static void Count(Dictionary<string, int> map, string? key)
        {
            if (key is not null)
                map[key] = map.GetValueOrDefault(key) + 1;
        }
    }
}
