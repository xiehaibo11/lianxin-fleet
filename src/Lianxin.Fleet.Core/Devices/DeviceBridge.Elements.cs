using System.Text.Json;
using System.Text.RegularExpressions;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Devices;

// 界面元素：查找、位置、点击、输入与界面树。与截图、动作共用 WDA 会话，并按设备串行。
public sealed partial class DeviceBridge
{
    public const int MaxElements = 200;
    private const string W3cElementKey = "element-6066-11e4-a52e-4f735466cecf";

    public async Task<IReadOnlyList<string>> FindElementsAsync(FleetDevice device, ElementSelector selector,
        CancellationToken cancellationToken = default)
    {
        var (strategy, value) = Locator(selector);
        return await SerializedAsync(device, async () =>
        {
            var session = await SessionAsync(device, cancellationToken).ConfigureAwait(false);
            // 复杂页面查找可能较慢；结果只含元素编号，限制响应大小即可。
            var found = await RequestAsync(device, $"/session/{session}/elements", new
            {
                @using = strategy,
                value
            },
                timeoutSeconds: 30, limit: 2 * 1024 * 1024, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (found.ValueKind != JsonValueKind.Array)
                throw new DeviceOperationException("WDA 元素查找结果无效。");
            var ids = new List<string>();
            foreach (var item in found.EnumerateArray())
            {
                if (ids.Count >= MaxElements)
                    break;
                if (item.ValueKind == JsonValueKind.Object && (item.TryGetProperty("ELEMENT", out var id)
                    || item.TryGetProperty(W3cElementKey, out id)) && id.ValueKind == JsonValueKind.String
                    && ValidElementId(id.GetString()))
                    ids.Add(id.GetString()!);
            }
            return (IReadOnlyList<string>)ids;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ElementBounds> GetElementBoundsAsync(FleetDevice device, string elementId,
        CancellationToken cancellationToken = default)
    {
        RequireElementId(elementId);
        return await SerializedAsync(device, async () =>
        {
            var session = await SessionAsync(device, cancellationToken).ConfigureAwait(false);
            var rect = await RequestAsync(device, $"/session/{session}/element/{elementId}/rect",
                cancellationToken: cancellationToken).ConfigureAwait(false);
            double Read(string name) => rect.ValueKind == JsonValueKind.Object && rect.TryGetProperty(name, out var item)
                && item.TryGetDouble(out var number) && double.IsFinite(number) && Math.Abs(number) <= 100_000
                ? number : throw new DeviceOperationException("WDA 元素位置无效。");
            return new ElementBounds(Read("x"), Read("y"), Read("width"), Read("height"));
        }, cancellationToken).ConfigureAwait(false);
    }

    public Task ClickElementAsync(FleetDevice device, string elementId, CancellationToken cancellationToken = default)
    {
        RequireElementId(elementId);
        return ElementActionAsync(device, $"/element/{elementId}/click", new
        {
        }, cancellationToken);
    }

    // WDA 会先点中输入框再逐字输入，效果与在该框里打字相同。
    public Task SetElementValueAsync(FleetDevice device, string elementId, string text,
        CancellationToken cancellationToken = default)
    {
        RequireElementId(elementId);
        var value = FleetValidation.Text(text, 1000, "输入文本");
        return ElementActionAsync(device, $"/element/{elementId}/value", new
        {
            value = new[] { value }
        }, cancellationToken);
    }

    public async Task<string> GetPageSourceAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        return await SerializedAsync(device, async () =>
        {
            var session = await SessionAsync(device, cancellationToken).ConfigureAwait(false);
            var source = await RequestAsync(device, $"/session/{session}/source?format=xml", timeoutSeconds: 60,
                limit: 32 * 1024 * 1024, cancellationToken: cancellationToken).ConfigureAwait(false);
            return source.ValueKind == JsonValueKind.String ? source.GetString() ?? ""
                : throw new DeviceOperationException("WDA 未返回界面树。");
        }, cancellationToken).ConfigureAwait(false);
    }

    // 选择器到 WDA 定位方式。文字与包含匹配 label、name、value 任一属性；字符串按 NSPredicate 规则转义后放进单引号。
    public static (string Using, string Value) Locator(ElementSelector selector)
    {
        selector = ScriptParser.CheckSelector(selector);
        var literal = PredicateLiteral(selector.Value);
        return selector.Kind switch
        {
            ElementSelectorKind.Text => ("predicate string", $"label == {literal} OR name == {literal} OR value == {literal}"),
            ElementSelectorKind.Contains => ("predicate string",
                $"label CONTAINS {literal} OR name CONTAINS {literal} OR value CONTAINS {literal}"),
            ElementSelectorKind.Id => ("accessibility id", selector.Value),
            ElementSelectorKind.Type => ("class name", selector.Value),
            ElementSelectorKind.XPath => ("xpath", selector.Value),
            _ => ("predicate string", selector.Value)
        };
    }

    // 引号内的 %@、$变量 等按字面处理，只需转义反斜杠和单引号。
    public static string PredicateLiteral(string value) =>
        "'" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("'", "\\'", StringComparison.Ordinal) + "'";

    public static bool ValidElementId(string? id) => id is not null && Regex.IsMatch(id, "^[A-Za-z0-9-]{1,128}$");

    private static void RequireElementId(string elementId)
    {
        if (!ValidElementId(elementId))
            throw new ArgumentException("元素编号无效。");
    }

    private Task ElementActionAsync(FleetDevice device, string path, object body, CancellationToken cancellationToken) =>
        SerializedAsync(device, async () =>
        {
            var session = await SessionAsync(device, cancellationToken).ConfigureAwait(false);
            await RequestAsync(device, $"/session/{session}{path}", body, action: true, timeoutSeconds: 30,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return true;
        }, cancellationToken);
}
