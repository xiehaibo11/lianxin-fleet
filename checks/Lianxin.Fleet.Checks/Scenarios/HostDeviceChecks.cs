using System.Text.Json;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static class HostDeviceChecks
{
    private const string LegacyUdid = "1111111111111111111111111111111111111111";
    private const string ModernUdid = "00008030-0012345678900002";
    private const string UsbDevices = """
        [{"DeviceClass":"iPhone","Identifier":"1111111111111111111111111111111111111111","DeviceName":"Legacy phone","ProductVersion":"15.8.8"},
         {"DeviceClass":"iPhone","UniqueDeviceID":"00008030-0012345678900002","DeviceName":"Modern phone"},
         {"DeviceClass":"iPhone","Identifier":"1111111111111111111111111111111111111111","DeviceName":"Duplicate"},
         {"DeviceClass":"iPhone","UniqueDeviceID":"invalid/id"},
         {"DeviceClass":"Watch","UniqueDeviceID":"00008030-0012345678900003"}]
        """;

    public static async Task RunAsync()
    {
        foreach (var platform in new[] { "darwin", "win32" })
        {
            var calls = new List<(string File, string[] Arguments, TimeSpan Timeout)>();
            var python = platform == "darwin" ? "/Tools/Python/bin/python" : @"C:\Tools\Python\python.exe";
            var runner = new Runner((file, arguments, timeout, token) =>
            {
                token.ThrowIfCancellationRequested();
                calls.Add((file, arguments.ToArray(), timeout));
                return Task.FromResult(new ProcessResult(0, arguments.Contains("list") ? UsbDevices : "", ""));
            });
            using var bridge = new DeviceBridge(python, platform: platform, processRunner: runner);
            var devices = await bridge.DiscoverAsync();
            Assert.Equal(2, devices.Count);
            Assert.True(devices.Select(device => device.Udid).SequenceEqual([LegacyUdid, ModernUdid]));
            Assert.Equal(python, calls[0].File);
            Assert.True(calls[0].Arguments.SequenceEqual(["-m", "pymobiledevice3", "--no-color", "usbmux", "list", "--usb"]));
            var device = new FleetDevice(1, "Legacy phone", LegacyUdid, 8100);
            foreach (var extension in new[] { ".ipa", ".app" })
            {
                var path = platform == "darwin" ? "/Approved Apps/Internal & Tools" + extension : @"C:\Approved Apps\Internal & Tools" + extension;
                await bridge.ExecuteAsync(device, FleetAction.Install, new(AppPath: path));
                Assert.Equal(python, calls[^1].File);
                Assert.True(calls[^1].Arguments.SequenceEqual(["-m", "pymobiledevice3", "--no-color", "apps", "install", "--udid", LegacyUdid, path]));
                Assert.Equal(TimeSpan.FromSeconds(125), calls[^1].Timeout);
            }
        }
        await MacFallbackAsync();
        await InvalidOutputAsync();
    }

    private static async Task MacFallbackAsync()
    {
        string? outputPath = null;
        var calls = 0;
        var runner = new Runner(async (file, arguments, _, _) =>
        {
            calls++;
            if (file != "/usr/bin/xcrun")
                return new(1, "", "No module named pymobiledevice3");
            outputPath = arguments[arguments.ToList().IndexOf("--json-output") + 1];
            var result = new
            {
                info = new
                {
                    outcome = "success"
                },
                result = new
                {
                    devices = new[]
                    {
                        new { hardwareProperties = new { platform = "iOS", udid = ModernUdid }, connectionProperties = new { tunnelState = "connected" }, deviceProperties = new { name = "Modern phone" } },
                        new { hardwareProperties = new { platform = "iOS", udid = LegacyUdid }, connectionProperties = new { tunnelState = "unavailable" }, deviceProperties = new { name = "Historical phone" } }
                    }
                }
            };
            await File.WriteAllTextAsync(outputPath, JsonSerializer.Serialize(result));
            return new(0, "", "");
        });
        using var bridge = new DeviceBridge(platform: "darwin", processRunner: runner);
        var devices = await bridge.DiscoverAsync();
        Assert.Equal(2, calls);
        Assert.Equal(1, devices.Count);
        Assert.Equal(ModernUdid, devices[0].Udid);
        Assert.True(outputPath is not null && !Directory.Exists(Path.GetDirectoryName(outputPath)));
    }

    private static async Task InvalidOutputAsync()
    {
        var calls = 0;
        var runner = new Runner((_, _, _, _) =>
        {
            calls++;
            return Task.FromResult(new ProcessResult(0, "{}", ""));
        });
        using var bridge = new DeviceBridge(platform: "darwin", processRunner: runner);
        await Assert.ThrowsAsync<InvalidDataException>(() => bridge.DiscoverAsync());
        Assert.Equal(1, calls);
        var interrupted = new Runner((_, _, _, _) => throw new ProcessRunException("Installation interrupted", true, new TimeoutException()));
        using var interruptedBridge = new DeviceBridge(platform: "darwin", processRunner: interrupted);
        var error = await Assert.ThrowsAsync<DeviceOperationException>(() => interruptedBridge.ExecuteAsync(
            new(1, "Legacy phone", LegacyUdid, 8100), FleetAction.Install, new(AppPath: "/Approved Apps/Internal.ipa")));
        Assert.True(error.Uncertain);
    }

    private sealed class Runner(Func<string, IReadOnlyList<string>, TimeSpan, CancellationToken, Task<ProcessResult>> run) : IProcessRunner
    {
        public Task<ProcessResult> RunAsync(string file, IReadOnlyList<string> arguments, TimeSpan timeout,
            CancellationToken cancellationToken = default) => run(file, arguments, timeout, cancellationToken);
    }
}
