using System.Net;
using System.Text.Json;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Checks.Scenarios;

// 设备管理：子进程参数、真机输出格式解析、电量缓存、WDA 回退与批量结果。
internal static partial class DeviceManagementChecks
{
    private const string Udid = "82a02bd2ea951218e8598fa638005cc3d489b008";
    private const string OtherUdid = "00008030-0012345678900002";
    private const string Python = @"C:\Tools\Python\python.exe";
    private static readonly string[] Cli = ["-m", "pymobiledevice3", "--no-color"];
    private static readonly FleetDevice Phone = new(1, "6s", Udid, 18100, OsVersion: "15.8.8");

    // 与 iPhone 6s（iOS 15.8.8）上的实际输出相同，序列号已替换。
    private const string InfoOutput = """
        {"DeviceName": "iPhone", "ProductType": "iPhone8,1", "ProductVersion": "15.8.8", "BuildVersion": "19H422", "SerialNumber": "TESTSERIAL01", "ModelNumber": "ML7E2", "HardwareModel": "N71mAP", "RegionInfo": "CH/A", "ActivationState": "Activated", "battery": {"BatteryCurrentCapacity": 100, "BatteryIsCharging": true, "ExternalConnected": true, "FullyCharged": false}, "disk": {"TotalDiskCapacity": 16000000000, "TotalDataCapacity": 9123192832, "TotalDataAvailable": 6696353792}, "screen": {"ScreenWidth": 750, "ScreenHeight": 1334, "ScreenScaleFactor": 2.0}, "health": {"Temperature": 3240, "CycleCount": 1, "DesignCapacity": 1690, "NominalChargeCapacity": 1715}}
        """;
    private const string MounterOutput = """
        [{"BackingImage": "/private/var/run/mobile_image_mounter/x/U0opPw.dmg", "DeviceNode": "/dev/disk2", "DeviceType": "DiskImage", "DiskImageType": "Developer", "FilesystemType": "hfs", "ImageSignature": "1e53", "IsMounted": true, "IsReadOnly": true, "MountPath": "/Developer", "SupportsContentProtection": false}]
        """;

    public static async Task InfoAndBattery()
    {
        var runner = new RecordingRunner(arguments => arguments[0] == "-c" && arguments[2] == "info"
            ? RecordingRunner.Ok(InfoOutput) : new ProcessResult(1, "", "unexpected"));
        var manager = new DeviceManager(new FakeBridge(), Python, runner, "win32");
        var info = await manager.GetInfoAsync(Phone);
        var call = runner.Calls.Single();
        Assert.Equal(Python, call.File);
        Assert.True(call.Arguments[1].Contains("create_using_usbmux(serial=udid, autopair=False)", StringComparison.Ordinal),
            "uses existing trust only");
        Assert.True(call.Arguments[2..].SequenceEqual(["info", Udid]), string.Join(" ", call.Arguments[2..]));
        Assert.True(info is { ProductType: "iPhone8,1", OsVersion: "15.8.8", SerialNumber: "TESTSERIAL01", ModelNumber: "ML7E2" }, info.ToString());
        Assert.True(info is { ScreenWidth: 750, ScreenHeight: 1334, ScreenScale: 2.0, TotalCapacity: 16000000000 }, "screen and capacity");
        Assert.True(info is { DataAvailable: 6696353792, BatteryCycleCount: 1, BatteryHealth: 101, Source: "USB" }, "free space and health");
        Assert.True(info.Battery is { Level: 100, Charging: true, ExternalPower: true, FullyCharged: false }, "battery parsed");
        Assert.True(Math.Abs(info.BatteryTemperature!.Value - 32.4) < 0.001, "temperature in hundredths of °C");
        Assert.Equal(100, manager.GetCachedBattery(Udid.ToUpperInvariant())!.Level);

        // 读不到的字段保持 null，类型不对的值被忽略。
        var sparse = HostDeviceTools.ParseDeviceInfo(Udid, """{"DeviceName": 5, "battery": {"BatteryCurrentCapacity": 140}}""");
        Assert.True(sparse is { DeviceName: null, Battery: null, BatteryHealth: null, TotalCapacity: null }, sparse.ToString());
        Assert.Throws<InvalidDataException>(() => HostDeviceTools.ParseDeviceInfo(Udid, "[]"));
        Assert.Throws<InvalidDataException>(() => HostDeviceTools.ParseDeviceInfo(Udid, "Traceback"));
    }

    // 电量列：一次子进程读取全部在线设备；有效期内不再启动；离线设备不读；USB 读不到时改问 WDA。
    public static async Task BatteryCache()
    {
        var output = $$"""
            [{"Identifier": "{{Udid}}", "BatteryCurrentCapacity": 87, "BatteryIsCharging": false, "ExternalConnected": false, "FullyCharged": false},
             {"Identifier": "{{OtherUdid}}", "Error": "DeviceNotFoundError"}]
            """;
        var runner = new RecordingRunner(_ => RecordingRunner.Ok(output));
        var clock = new Clock();
        using var client = new HttpClient(new Handler(WdaReply));
        using var bridge = new DeviceBridge(Python, client, "win32", runner);
        var manager = new DeviceManager(bridge, Python, runner, "win32", clock);
        var wifi = new FleetDevice(2, "Wi-Fi", OtherUdid, 18101);
        var offline = new FleetDevice(3, "离线", "00008030-0012345678900003", 18102);
        DeviceSnapshot[] devices =
        [
            new(Phone, new(false), UsbConnected: true),
            new(wifi, new(true), UsbConnected: false, NetworkConnected: true),
            new(offline, new(false), UsbConnected: false, NetworkConnected: false)
        ];
        var first = await manager.GetBatteriesAsync(devices);
        Assert.Equal(1, runner.Calls.Count);
        Assert.True(runner.Calls[0].Arguments[2..].SequenceEqual(["batteries", Udid, OtherUdid]),
            string.Join(" ", runner.Calls[0].Arguments[2..]));
        Assert.True(first[1] is { Level: 87, Charging: false, Source: "USB" }, "lockdown battery");
        Assert.True(first[2] is { Level: 50, Charging: true, FullyCharged: false, Source: "WDA" }, "WDA fallback for ready device");
        Assert.True(!first.ContainsKey(3), "offline device skipped");

        clock.Now += TimeSpan.FromSeconds(30);
        await manager.GetBatteriesAsync(devices);
        Assert.Equal(1, runner.Calls.Count);
        clock.Now += TimeSpan.FromSeconds(31);
        await manager.GetBatteriesAsync(devices);
        Assert.Equal(2, runner.Calls.Count);
        await manager.GetBatteriesAsync(devices, force: true);
        Assert.Equal(3, runner.Calls.Count);

        // 工具失败时不抛出，已缓存的值保留到下一轮。
        var failing = new DeviceManager(new FakeBridge(), Python, new RecordingRunner(_ => new ProcessResult(1, "", "boom")), "win32");
        Assert.Equal(0, (await failing.GetBatteriesAsync(devices[..1])).Count);
        var parsed = HostDeviceTools.ParseBatteries("""[{"Identifier":"aa-bb-cc-dd","BatteryCurrentCapacity":5}]""");
        Assert.True(parsed["AABBCCDD"] is { Level: 5, Charging: null }, "keys are normalized UDIDs");
    }

    // 自动化在线：电量取 WDA，序列号和容量仍取 USB；USB 读不到时只用 WDA。
    public static async Task WdaInfo()
    {
        var usbFails = false;
        var runner = new RecordingRunner(_ => usbFails ? new ProcessResult(1, "", "DeviceNotFoundError: x") : RecordingRunner.Ok(InfoOutput));
        using var client = new HttpClient(new Handler(WdaReply));
        using var bridge = new DeviceBridge(Python, client, "win32", runner);
        var manager = new DeviceManager(bridge, Python, runner, "win32");
        var both = await manager.GetInfoAsync(Phone, automationReady: true);
        Assert.True(both is { Source: "USB+WDA", SerialNumber: "TESTSERIAL01", ScreenWidth: 750 }, both.ToString());
        Assert.True(both.Battery is { Level: 50, Source: "WDA" }, "live battery wins");
        usbFails = true;
        var wdaOnly = await manager.GetInfoAsync(Phone, automationReady: true);
        Assert.True(wdaOnly is { Source: "WDA", DeviceName: "Actual phone", ScreenWidth: 750, ScreenHeight: 1334, SerialNumber: null },
            wdaOnly.ToString());
        var error = await Assert.ThrowsAsync<DeviceOperationException>(() => manager.GetInfoAsync(Phone));
        Assert.True(error.Message.Contains("没有找到这台手机", StringComparison.Ordinal), error.Message);

        var full = DeviceBridge.ParseWdaBattery(JsonDocument.Parse("""{"level": 1, "state": 3}""").RootElement);
        Assert.True(full is { Level: 100, Charging: false, ExternalPower: true, FullyCharged: true }, "full battery");
        Assert.True(DeviceBridge.ParseWdaBattery(JsonDocument.Parse("""{"level": -1, "state": 0}""").RootElement) is null,
            "unknown level");
    }

    private static object? WdaReply(string path) => path switch
    {
        "/wda/device/info" => new { name = "Actual phone", model = "iPhone" },
        "/window/size" => new { width = 375, height = 667 },
        "/wda/screen" => new { scale = 2, screenSize = new { width = 375, height = 667 } },
        "/session" => new { sessionId = "session-1" },
        "/session/session-1/wda/batteryInfo" => new { level = 0.5, state = 2 },
        _ => new { }
    };

    private sealed class Handler(Func<string, object?> reply) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.True(request.RequestUri!.Host == "127.0.0.1", "WDA stays on loopback");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { value = reply(request.RequestUri.AbsolutePath) }))
            });
        }
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 22, 8, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
