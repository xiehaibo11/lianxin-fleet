using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Lianxin.Fleet.Core.Devices;

namespace Lianxin.Fleet.Core.OpenApi;

// dumpXml 的过滤参数，含义与 EasyClick 的 setFetchNodeParam 相同。MaxDepth 为空表示不限层级。
internal sealed record DumpOptions(bool VisibleOnly, bool LabelOnly, int? MaxDepth, IReadOnlySet<string> Excluded);

// 抓取节点：按 EasyClick 的过滤参数整理 WDA 界面树。
// - 层级按原始树计算，根节点（应用）为第 1 层，超出 maxDepth 的节点连同子节点一起去掉；
// - 不满足 visible / label 条件的节点本身去掉，其中满足条件的子节点提升到上一层；根节点始终保留；
// - x、y、width、height 从 WDA 的点换算成截图像素，与 clickPoint 等接口的坐标一致。
internal static class PageSourceDump
{
    private const int MaxNesting = 1000;
    private static readonly string[] PixelAttributes = ["x", "y", "width", "height"];

    internal static DumpOptions Options(OpenApiCall call)
    {
        bool Only(string name) => (call.Text(name)?.Trim() ?? "") switch
        {
            "" or "0" or "1" => false,
            "2" => true,
            _ => throw OpenApiCall.Invalid($"{name} 只能是 1（全部）或 2（只取满足条件的节点）。")
        };
        var depthText = call.Text("maxDepth")?.Trim() ?? "";
        int? depth = depthText.Length == 0 || depthText == "0" ? null
            : int.TryParse(depthText, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value is >= 1 and <= MaxNesting
                ? value : throw OpenApiCall.Invalid($"maxDepth 须为 1 至 {MaxNesting} 的整数（0 或不填表示不限）。");
        var excluded = (call.Text("excludedAttributes") ?? "").Split(',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (excluded.Count > 64 || excluded.Any(name => name.Length > 64))
            throw OpenApiCall.Invalid("excludedAttributes 最多 64 个属性名，每个最多 64 个字符。");
        return new DumpOptions(Only("visibleFilter"), Only("labelFilter"), depth, excluded);
    }

    internal static string Filter(string source, DumpOptions options, double ratio)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!double.IsFinite(ratio) || ratio <= 0)
            throw new ArgumentException("屏幕像素比例无效。");
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            MaxCharactersInDocument = 64_000_000
        };
        XElement root;
        try
        {
            using var reader = XmlReader.Create(new StringReader(source ?? ""), settings);
            root = XDocument.Load(reader).Root ?? throw new XmlException("没有根节点。");
        }
        catch (XmlException error)
        {
            throw new DeviceOperationException("WDA 返回的界面树不是有效的 XML：" + error.Message, false, error);
        }
        var result = Copy(root, options, ratio);
        Append(root, result, 1, options, ratio);
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + result.ToString(SaveOptions.None);
    }

    private static void Append(XElement source, XElement target, int depth, DumpOptions options, double ratio)
    {
        var childDepth = depth + 1;
        if (childDepth > (options.MaxDepth ?? MaxNesting))
            return;
        foreach (var child in source.Elements())
        {
            if (Keep(child, options))
            {
                var copy = Copy(child, options, ratio);
                target.Add(copy);
                Append(child, copy, childDepth, options, ratio);
            }
            else
                Append(child, target, childDepth, options, ratio);
        }
    }

    // 缺少 visible 属性的节点按可见处理（与抓取节点列表一致）。
    private static bool Keep(XElement element, DumpOptions options) =>
        (!options.VisibleOnly || (string?)element.Attribute("visible") != "false")
        && (!options.LabelOnly || !string.IsNullOrWhiteSpace((string?)element.Attribute("label")));

    private static XElement Copy(XElement element, DumpOptions options, double ratio)
    {
        var copy = new XElement(element.Name);
        foreach (var attribute in element.Attributes())
        {
            if (attribute.IsNamespaceDeclaration || options.Excluded.Contains(attribute.Name.LocalName))
                continue;
            var value = attribute.Value;
            if (PixelAttributes.Contains(attribute.Name.LocalName) && double.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out var points) && double.IsFinite(points))
                value = ((long)Math.Round(points * ratio)).ToString(CultureInfo.InvariantCulture);
            copy.SetAttributeValue(attribute.Name, value);
        }
        return copy;
    }
}
