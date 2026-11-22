using System.Text.Json;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.Infrastructure;

internal sealed partial class HostDeviceTools
{
    // 一次进程读取所有 usbmuxd 设备（USB 与 Wi-Fi）的锁屏服务信息；只读，不配对。
    private const string DescribeDevices = """
        import asyncio, json
        from pymobiledevice3 import usbmux
        from pymobiledevice3.lockdown import create_using_usbmux

        KEYS = ("DeviceName", "DeviceClass", "ProductType", "ProductVersion", "UniqueChipID")

        async def describe(device):
            item = {"Identifier": device.serial, "ConnectionType": device.connection_type}
            try:
                async with await create_using_usbmux(device.serial, autopair=False, connection_type=device.connection_type) as lockdown:
                    values = lockdown.all_values
                    for key in KEYS:
                        if key in values:
                            item[key] = values[key]
            except Exception as error:
                item["Error"] = type(error).__name__
            return item

        async def main():
            print(json.dumps(await asyncio.gather(*(describe(d) for d in await usbmux.list_devices()))))

        asyncio.run(main())
        """;

    // 补充 ECID，并加入只通过 Wi-Fi 连接的设备。读取失败时原样返回 USB 列表。
    private async Task<IReadOnlyList<DiscoveredDevice>> EnrichAsync(IReadOnlyList<DiscoveredDevice> usb, CancellationToken cancellationToken)
    {
        List<DiscoveredDevice> details;
        try
        {
            var result = await runner.RunAsync(python, ["-c", DescribeDevices], TimeSpan.FromSeconds(25), cancellationToken)
                .ConfigureAwait(false);
            if (result.ExitCode != 0)
                return usb;
            details = ParseDetails(result.StandardOutput);
        }
        catch (Exception error) when (error is JsonException or ProcessRunException or InvalidDataException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return usb;
        }
        var byUdid = details.GroupBy(item => Usbmux.Normalize(item.Udid))
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.ConnectionType == "USB" ? 0 : 1).First());
        var merged = usb.Select(device => byUdid.TryGetValue(Usbmux.Normalize(device.Udid), out var extra)
            ? device with
            {
                Ecid = extra.Ecid ?? device.Ecid,
                ProductType = device.ProductType ?? extra.ProductType,
                OsVersion = device.OsVersion ?? extra.OsVersion
            }
            : device).ToList();
        var known = merged.Select(device => Usbmux.Normalize(device.Udid)).ToHashSet(StringComparer.Ordinal);
        merged.AddRange(byUdid.Values.Where(item => item.ConnectionType == "Network" && known.Add(Usbmux.Normalize(item.Udid))));
        return merged;
    }

    internal static List<DiscoveredDevice> ParseDetails(string output)
    {
        using var json = JsonDocument.Parse(output);
        if (json.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("设备信息不是 JSON 数组。");
        var devices = new List<DiscoveredDevice>();
        foreach (var item in json.RootElement.EnumerateArray())
        {
            var udid = String(item, "Identifier") ?? "";
            var kind = String(item, "DeviceClass");
            // 读不到设备类型（未配对或已断开）的条目不带任何可用信息，直接跳过。
            if (!DeviceBridge.ValidUdid(udid) || kind is not ("iPhone" or "iPad"))
                continue;
            string? ecid = null;
            if (item.TryGetProperty("UniqueChipID", out var chip) && chip.ValueKind == JsonValueKind.Number
                && chip.TryGetUInt64(out var value))
                ecid = value.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
            devices.Add(new(udid, String(item, "DeviceName") ?? "iPhone", ecid, String(item, "ProductType"),
                String(item, "ProductVersion"), String(item, "ConnectionType") == "Network" ? "Network" : "USB"));
        }
        return devices;
    }
}
