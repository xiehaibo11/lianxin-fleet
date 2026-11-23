using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class DeviceManagementChecks
{
    // 应用列表只取需要的字段；卸载参数、系统应用拒绝与中断待确认。镜像状态、按镜像类型卸载与电源命令。
    public static async Task AppsImagesAndPower()
    {
        var listing = """
            [{"CFBundleIdentifier": "com.lianxin.fleet.webdriveragent.xctrunner", "CFBundleDisplayName": "WebDriverAgentRunner-Runner", "CFBundleName": "WebDriverAgentRunner-Runner", "CFBundleShortVersionString": "1.0", "CFBundleVersion": "1"},
             {"CFBundleIdentifier": "com.ownbook.notes", "CFBundleDisplayName": "爱思全能版", "CFBundleName": "爱思全能版", "CFBundleShortVersionString": "2.2.0", "CFBundleVersion": "3"},
             {"CFBundleName": "no bundle id"}, {"CFBundleIdentifier": "com.example.plain", "CFBundleName": "Plain"}]
            """;
        var images = MounterOutput;
        var runner = new RecordingRunner(arguments => arguments switch
        {
            ["-c", _, "apps", ..] => RecordingRunner.Ok(listing),
            [_, _, _, "mounter", "list", ..] => RecordingRunner.Ok(images),
            _ => RecordingRunner.Ok()
        });
        var manager = new DeviceManager(new FakeBridge(), Python, runner, "win32");
        var apps = await manager.GetAppsAsync(Phone);
        Assert.True(runner.Calls[0].Arguments[2..].SequenceEqual(["apps", Udid]), "apps args");
        Assert.True(runner.Calls[0].Arguments[1].Contains("\"ApplicationType\": \"User\"", StringComparison.Ordinal), "user apps only");
        Assert.Equal(3, apps.Count);
        Assert.True(apps[0] is { BundleId: "com.example.plain", Name: "Plain", Version: null }, apps[0].ToString());
        Assert.True(apps.Any(app => app is { BundleId: "com.ownbook.notes", Name: "爱思全能版", Version: "2.2.0", Build: "3" }), "display name");

        await manager.UninstallAppAsync(Phone, "com.ownbook.notes");
        Assert.True(runner.Calls[^1].Arguments.SequenceEqual([.. Cli, "apps", "uninstall", "--udid", Udid, "--", "com.ownbook.notes"]),
            string.Join(" ", runner.Calls[^1].Arguments));
        Assert.Equal(TimeSpan.FromSeconds(125), runner.Calls[^1].Timeout);
        var before = runner.Calls.Count;
        await Assert.ThrowsAsync<ArgumentException>(() => manager.UninstallAppAsync(Phone, "com.apple.mobilesafari"));
        await Assert.ThrowsAsync<ArgumentException>(() => manager.UninstallAppAsync(Phone, "bad bundle"));
        await Assert.ThrowsAsync<ArgumentException>(() => manager.UninstallAppAsync(Phone with { Udid = "bad/udid" }, "com.a.b"));
        Assert.Equal(before, runner.Calls.Count);

        Assert.True(await manager.GetDeveloperImageAsync(Phone) is { Mounted: true, ImageType: "Developer", MountPath: "/Developer" },
            "real mounter list output");
        Assert.True(await manager.UnmountDeveloperImageAsync(Phone), "unmounted");
        Assert.True(runner.Calls[^1].Arguments.SequenceEqual([.. Cli, "mounter", "umount-developer", "--udid", Udid]), "classic unmount");
        images = """[{"DiskImageType": "Personalized", "IsMounted": true, "MountPath": "/System/Developer"}]""";
        await manager.UnmountDeveloperImageAsync(Phone);
        Assert.True(runner.Calls[^1].Arguments.SequenceEqual([.. Cli, "mounter", "umount-personalized", "--udid", Udid]), "iOS 17 unmount");
        images = """[{"DiskImageType": "Developer", "IsMounted": false}, {"DiskImageType": "Cryptex", "IsMounted": true}]""";
        before = runner.Calls.Count;
        Assert.True(!await manager.UnmountDeveloperImageAsync(Phone), "nothing mounted");
        Assert.Equal(before + 1, runner.Calls.Count);
        await manager.MountDeveloperImageAsync(Phone);
        Assert.True(runner.Calls[^1].Arguments.SequenceEqual([.. Cli, "mounter", "auto-mount", "--udid", Udid]), "auto-mount");

        await manager.RestartAsync(Phone);
        Assert.True(runner.Calls[^1].Arguments.SequenceEqual([.. Cli, "diagnostics", "restart", "--udid", Udid]), "restart");
        await manager.ShutdownAsync(Phone);
        Assert.True(runner.Calls[^1].Arguments.SequenceEqual([.. Cli, "diagnostics", "shutdown", "--udid", Udid]), "shutdown");

        // 连接断开可能发生在请求送达之后：重启失败标为待确认；找不到设备则确定没有执行。
        var dropped = new DeviceManager(new FakeBridge(), Python,
            new RecordingRunner(_ => new ProcessResult(1, "", "ConnectionAbortedError: reset")), "win32");
        Assert.True((await Assert.ThrowsAsync<DeviceOperationException>(() => dropped.RestartAsync(Phone))).Uncertain, "dropped");
        var absent = new DeviceManager(new FakeBridge(), Python,
            new RecordingRunner(_ => new ProcessResult(1, "", ToolOutput.NotFound(Udid))), "win32");
        var error = await Assert.ThrowsAsync<DeviceOperationException>(() => absent.ShutdownAsync(Phone));
        Assert.True(!error.Uncertain && error.Message.StartsWith("没有找到这台手机", StringComparison.Ordinal), error.Message);
        Assert.True(!(await Assert.ThrowsAsync<DeviceOperationException>(() => absent.UninstallAppAsync(Phone, "com.a.b"))).Uncertain,
            "reported failure is certain");
        var interrupted = new DeviceManager(new FakeBridge(), Python, new RecordingRunner(_ =>
            throw new ProcessRunException("设备工具执行中断", true, new TimeoutException())), "win32");
        Assert.True((await Assert.ThrowsAsync<DeviceOperationException>(() => interrupted.UninstallAppAsync(Phone, "com.a.b"))).Uncertain,
            "interrupted uninstall");
        Assert.True(!(await Assert.ThrowsAsync<DeviceOperationException>(() => interrupted.GetAppsAsync(Phone))).Uncertain,
            "interrupted query changes nothing");
    }

    // 批量：并发上限、按输入顺序返回、待确认与失败分开报告。
    public static async Task Batch()
    {
        var devices = Enumerable.Range(1, 5).Select(id => Phone with { Id = id, Name = "P" + id }).ToArray();
        var active = 0;
        var peak = 0;
        var reports = new List<DeviceBatchResult>();
        var results = await DeviceManager.RunBatchAsync(devices, async (device, token) =>
        {
            peak = Math.Max(peak, Interlocked.Increment(ref active));
            await Task.Delay(30, token);
            Interlocked.Decrement(ref active);
            if (device.Id == 2)
                throw new DeviceOperationException("中断", uncertain: true);
            if (device.Id == 4)
                throw new InvalidOperationException("失败");
        }, new SyncProgress(reports), maxParallel: 2);
        Assert.True(peak <= 2, "parallel limit " + peak);
        Assert.True(results.Select(result => result.DeviceId).SequenceEqual([1, 2, 3, 4, 5]), "input order");
        Assert.True(results[1] is { Succeeded: false, Uncertain: true } && results[3] is { Succeeded: false, Uncertain: false, Error: "失败" },
            "uncertain vs failed");
        Assert.Equal(3, results.Count(result => result.Succeeded));
        Assert.Equal(5, reports.Count);

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var skipped = await DeviceManager.RunBatchAsync(devices[..1], (_, _) => Task.CompletedTask, cancellationToken: cancelled.Token);
        Assert.True(skipped[0] is { Succeeded: false, Uncertain: false }, "never started");
        Assert.True(HostDeviceTools.Explain("AfcFileNotFoundError: x") == "手机上没有这个文件或文件夹。", "chinese hint");
    }

    private sealed class SyncProgress(List<DeviceBatchResult> reports) : IProgress<DeviceBatchResult>
    {
        public void Report(DeviceBatchResult value)
        {
            lock (reports)
                reports.Add(value);
        }
    }
}
