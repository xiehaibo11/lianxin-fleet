using System.Text.Json;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Devices;

public sealed partial class DeviceBridge
{
    // WDA 实时信息：名称、屏幕（逻辑点）与电量。电量接口需要会话，和截图一样按设备串行。
    public async Task<WdaDeviceInfo?> GetWdaInfoAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Validate(device);
        var info = RequestAsync(device, "/wda/device/info", timeoutSeconds: 4, cancellationToken: cancellationToken);
        var size = DimensionsAsync(device, 4, cancellationToken);
        var screen = OptionalAsync(() => RequestAsync(device, "/wda/screen", timeoutSeconds: 4,
            cancellationToken: cancellationToken));
        var battery = OptionalAsync(() => SerializedAsync(device, async () =>
        {
            var id = await SessionAsync(device, cancellationToken).ConfigureAwait(false);
            return await RequestAsync(device, $"/session/{id}/wda/batteryInfo", timeoutSeconds: 4,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }, cancellationToken));
        await Task.WhenAll(info, size, screen, battery).ConfigureAwait(false);
        var scale = screen.Result is { ValueKind: JsonValueKind.Object } value && value.TryGetProperty("scale", out var factor)
            && factor.TryGetDouble(out var number) && number is > 0 and <= 10 ? number : (double?)null;
        return new WdaDeviceInfo(Text(info.Result, "name"), Text(info.Result, "model"), size.Result.Width,
            size.Result.Height, scale, ParseWdaBattery(battery.Result));
    }

    // level 为 0–1（未知时为 -1）；state：1 未充电，2 充电中，3 已充满，0 未知。
    internal static BatteryInfo? ParseWdaBattery(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("level", out var level)
            || !level.TryGetDouble(out var fraction) || !double.IsFinite(fraction) || fraction is < 0 or > 1)
            return null;
        var state = value.TryGetProperty("state", out var item) && item.TryGetInt32(out var code) ? code : 0;
        return new BatteryInfo((int)Math.Round(fraction * 100), state switch
        {
            2 => true,
            1 or 3 => false,
            _ => null
        }, state is 2 or 3 ? true : state == 1 ? false : null, state == 3 ? true : state is 1 or 2 ? false : null, "WDA");
    }

    private static async Task<JsonElement> OptionalAsync(Func<Task<JsonElement>> request)
    {
        try
        {
            return await request().ConfigureAwait(false);
        }
        catch (DeviceOperationException)
        {
            return default;
        }
    }

    private static string? Text(JsonElement value, string key)
    {
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(key, out var item)
            || item.ValueKind != JsonValueKind.String)
            return null;
        var text = item.GetString() ?? "";
        return text.Length > 120 ? text[..120] : text;
    }
}
