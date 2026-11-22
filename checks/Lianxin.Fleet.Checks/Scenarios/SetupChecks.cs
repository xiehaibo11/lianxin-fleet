using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static class SetupChecks
{
    private const string Udid = "00008030-0012345678900001";
    private const string WifiUdid = "00008030-0012345678900009";
    private const string Python = @"C:\Tools\Python\python.exe";

    public static async Task OneClickSetup()
    {
        using var folder = new TemporaryFolder();
        var approved = Directory.CreateDirectory(Path.Combine(folder.Path, "apps")).FullName;
        var package = Path.Combine(approved, "WebDriverAgent.ipa");
        File.WriteAllText(package, "signed runner");
        var developerMode = "false";
        var runner = new RecordingRunner(arguments => arguments.Contains("developer-mode-status")
            ? RecordingRunner.Ok(developerMode + "\n")
            : arguments.Contains("wifi-connections") && arguments.Count == 7
                ? RecordingRunner.Ok("{\"EnableWifiConnections\": true}")
                : RecordingRunner.Ok());
        var setup = new DeviceSetup(approved, Python, runner, "win32");
        var messages = new List<string>();
        var progress = new Progress(messages);

        // iOS 15 没有开发者模式：跳过该步，只挂载镜像并安装批准目录内的 WDA。
        var legacy = new FleetDevice(1, "6s", Udid, 8100, OsVersion: "15.8.8");
        await setup.SetupAsync(legacy, new(true, true, true, package), progress);
        Assert.True(messages.Any(message => message.Contains("跳过")), string.Join("|", messages));
        Assert.Equal(2, runner.Calls.Count);
        Assert.True(runner.Calls[0].Arguments.SequenceEqual(["-m", "pymobiledevice3", "--no-color", "mounter", "auto-mount", "--udid", Udid]), "mount args");
        Assert.True(runner.Calls[0].Timeout >= TimeSpan.FromMinutes(4), "mount may download an image");
        Assert.True(runner.Calls[1].Arguments.SequenceEqual(["-m", "pymobiledevice3", "--no-color", "apps", "install", "--udid", Udid, package]), "install args");

        // iOS 17：关闭时开启开发者模式；已开启时不再调用。
        runner.Calls.Clear();
        var modern = legacy with
        {
            OsVersion = "17.5"
        };
        await setup.SetupAsync(modern, new(true, false, false), progress);
        Assert.True(runner.Calls.Select(call => call.Arguments[3] + " " + call.Arguments[4])
            .SequenceEqual(["amfi developer-mode-status", "amfi enable-developer-mode"]), "enable when off");
        developerMode = "true";
        runner.Calls.Clear();
        await setup.SetupAsync(modern, new(true, false, false), progress);
        Assert.Equal(1, runner.Calls.Count);

        // 批准目录以外的安装包在任何设备命令之前就被拒绝。
        runner.Calls.Clear();
        var outside = Path.Combine(folder.Path, "Outside.ipa");
        File.WriteAllText(outside, "x");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => setup.SetupAsync(legacy, new(false, true, true, outside), progress));
        await Assert.ThrowsAsync<ArgumentException>(() => setup.SetupAsync(legacy, new(false, false, false), progress));
        Assert.Equal(0, runner.Calls.Count);

        Assert.True(await setup.GetWifiConnectionsAsync(legacy), "wifi state parsed");
        await setup.SetWifiConnectionsAsync(legacy, true);
        Assert.True(runner.Calls[^1].Arguments[3..].SequenceEqual(["lockdown", "wifi-connections", "--udid", Udid, "on"]), "wifi on args");
    }

    public static async Task DiscoveryDetails()
    {
        var details = HostDeviceTools.ParseDetails($$"""
            [{"Identifier":"{{Udid}}","ConnectionType":"USB","DeviceClass":"iPhone","DeviceName":"一号","ProductType":"iPhone8,1","ProductVersion":"15.8.8","UniqueChipID":1712198844482049},
             {"Identifier":"{{WifiUdid}}","ConnectionType":"Network","DeviceClass":"iPhone","DeviceName":"无线","UniqueChipID":42},
             {"Identifier":"00008030-0012345678900003","ConnectionType":"USB","DeviceClass":"Watch"},
             {"Identifier":"00008030-0012345678900004","ConnectionType":"USB","Error":"NotPairedError"},
             {"Identifier":"bad/id","ConnectionType":"USB","DeviceClass":"iPhone"}]
            """);
        Assert.Equal(2, details.Count);
        Assert.Equal("0006153C5BE90201", details[0].Ecid);
        Assert.Equal("000000000000002A", details[1].Ecid);
        Assert.Equal("Network", details[1].ConnectionType);

        // 端到端：USB 列表 + 补充信息，得到 ECID 与只走 Wi-Fi 的设备。
        var runner = new RecordingRunner(arguments => arguments.Contains("list")
            ? RecordingRunner.Ok($$"""[{"DeviceClass":"iPhone","Identifier":"{{Udid}}","DeviceName":"一号","ProductVersion":"15.8.8"}]""")
            : RecordingRunner.Ok($$"""
                [{"Identifier":"{{Udid}}","ConnectionType":"USB","DeviceClass":"iPhone","ProductType":"iPhone8,1","UniqueChipID":42},
                 {"Identifier":"{{WifiUdid}}","ConnectionType":"Network","DeviceClass":"iPhone","DeviceName":"无线"}]
                """));
        using var bridge = new DeviceBridge(Python, platform: "win32", processRunner: runner);
        var discovered = await bridge.DiscoverAsync();
        Assert.Equal(2, discovered.Count);
        Assert.True(discovered[0] is { Ecid: "000000000000002A", ProductType: "iPhone8,1", OsVersion: "15.8.8", ConnectionType: "USB" }, discovered[0].ToString());
        Assert.True(discovered[1] is { Udid: WifiUdid, ConnectionType: "Network" }, discovered[1].ToString());
        Assert.True(runner.Calls[0].Arguments.SequenceEqual(["-m", "pymobiledevice3", "--no-color", "usbmux", "list", "--usb"]), "list stays first");

        // 补充脚本失败时仍返回 USB 列表。
        var failing = new RecordingRunner(arguments => arguments.Contains("list")
            ? RecordingRunner.Ok($$"""[{"DeviceClass":"iPhone","Identifier":"{{Udid}}"}]""")
            : new ProcessResult(1, "", "Traceback"));
        using var degraded = new DeviceBridge(Python, platform: "win32", processRunner: failing);
        Assert.True((await degraded.DiscoverAsync()).Single() is { Udid: Udid, Ecid: null }, "falls back to USB list");

        // 扫描时把 ECID 等信息写入设备表，Wi-Fi 设备单独标出。
        using var store = new FleetStore(":memory:");
        var fake = new FakeBridge(discover: () => discovered);
        await using var service = new FleetService(store, fake, Path.GetTempPath());
        await service.DiscoverAsync();
        Assert.Equal("000000000000002A", store.GetDevices().Single(device => device.Udid == Udid).Ecid);
    }

    // macOS 重置 USB：固定的提权命令；用户取消授权、命令失败分别报告。
    public static async Task MacUsbReset()
    {
        var ok = new RecordingRunner(_ => RecordingRunner.Ok());
        await UsbServiceControl.RestartUsbmuxdAsync(ok);
        var call = ok.Calls.Single();
        Assert.Equal("/usr/bin/osascript", call.File);
        Assert.True(call.Arguments.SequenceEqual(["-e", "do shell script \"/usr/bin/killall usbmuxd\" with administrator privileges"]),
            string.Join(" ", call.Arguments));
        var cancelled = new RecordingRunner(_ => new ProcessResult(1, "", "execution error: User canceled. (-128)"));
        await Assert.ThrowsAsync<OperationCanceledException>(() => UsbServiceControl.RestartUsbmuxdAsync(cancelled));
        var failed = new RecordingRunner(_ => new ProcessResult(1, "", "No matching processes"));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => UsbServiceControl.RestartUsbmuxdAsync(failed));
        Assert.True(error.Message.Contains("No matching processes"), error.Message);
    }

    private sealed class Progress(List<string> messages) : IProgress<string>
    {
        public void Report(string value) => messages.Add(value);
    }
}
