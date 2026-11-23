using System.Globalization;
using System.Text.Json;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.OpenApi;

// 一次接口调用：请求体字段读取与设备解析。EasyClick 的示例里数字常写成字符串（如 "quality": "50"），两种写法都接受。
public sealed class OpenApiCall
{
    internal OpenApiCall(OpenApiBackend backend, OpenApiRequest request, JsonElement body, CancellationToken cancellation)
    {
        Backend = backend;
        Request = request;
        Body = body;
        Cancellation = cancellation;
    }

    public OpenApiBackend Backend
    {
        get;
    }
    public OpenApiRequest Request
    {
        get;
    }
    // POST 的 JSON 对象；GET 请求为 Undefined。
    public JsonElement Body
    {
        get;
    }
    public CancellationToken Cancellation
    {
        get;
    }

    private JsonElement? Field(string name) =>
        Body.ValueKind == JsonValueKind.Object && Body.TryGetProperty(name, out var value)
            && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined) ? value : null;

    public string? Text(string name) => Field(name) switch
    {
        null => null,
        { ValueKind: JsonValueKind.String } value => value.GetString(),
        { ValueKind: JsonValueKind.Number } value => value.GetRawText(),
        { ValueKind: JsonValueKind.True or JsonValueKind.False } value => value.GetRawText(),
        _ => throw Invalid($"参数 {name} 应为字符串。")
    };

    public string RequiredText(string name)
    {
        var value = Text(name)?.Trim();
        return string.IsNullOrEmpty(value) ? throw Invalid($"缺少参数 {name}。") : value;
    }

    public double? Number(string name)
    {
        var field = Field(name);
        if (field is null)
            return null;
        var value = field.Value;
        double number;
        if (value.ValueKind == JsonValueKind.Number)
            number = value.GetDouble();
        else if (value.ValueKind != JsonValueKind.String || !double.TryParse(value.GetString(), NumberStyles.Float,
            CultureInfo.InvariantCulture, out number))
            throw Invalid($"参数 {name} 应为数字。");
        return double.IsFinite(number) ? number : throw Invalid($"参数 {name} 应为有限数字。");
    }

    public double RequiredNumber(string name) => Number(name) ?? throw Invalid($"缺少参数 {name}。");

    public bool? Flag(string name) => Field(name) switch
    {
        null => null,
        { ValueKind: JsonValueKind.True } => true,
        { ValueKind: JsonValueKind.False } => false,
        { ValueKind: JsonValueKind.String } value when value.GetString() is "true" or "1" => true,
        { ValueKind: JsonValueKind.String } value when value.GetString() is "false" or "0" => false,
        { ValueKind: JsonValueKind.Number } value => value.GetDouble() != 0,
        _ => throw Invalid($"参数 {name} 应为布尔值。")
    };

    public IReadOnlyList<string> TextList(string name)
    {
        var field = Field(name);
        if (field is null)
            return [];
        if (field.Value.ValueKind != JsonValueKind.Array)
            throw Invalid($"参数 {name} 应为数组。");
        if (field.Value.GetArrayLength() > 1000)
            throw Invalid($"参数 {name} 最多 1000 项。");
        return field.Value.EnumerateArray().Select(item => item.ValueKind switch
        {
            JsonValueKind.String => item.GetString()!.Trim(),
            JsonValueKind.Number => item.GetRawText(),
            _ => throw Invalid($"参数 {name} 的元素应为字符串。")
        }).Where(item => item.Length > 0).ToArray();
    }

    // 单台设备：参数 deviceId，取 UDID 或中控内的设备编号。
    public FleetDevice Device(string name = "deviceId") => Backend.ResolveDevice(RequiredText(name));

    // 多台设备：参数 deviceIds；为空时表示全部已登记设备。
    public IReadOnlyList<FleetDevice> Devices(string name = "deviceIds", bool emptyMeansAll = false)
    {
        var ids = TextList(name);
        if (ids.Count == 0)
            return emptyMeansAll ? Backend.Store.GetDevices() : throw Invalid($"缺少参数 {name}。");
        return Backend.ResolveDevices(ids);
    }

    internal static OpenApiException Invalid(string message) => new(OpenApiCodes.InvalidArgument, message);
}
