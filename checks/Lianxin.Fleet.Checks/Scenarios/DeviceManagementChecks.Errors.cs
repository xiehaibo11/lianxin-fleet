using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class DeviceManagementChecks
{
    // pymobiledevice3 命令行的真实失败输出：找不到设备、没有信任、未解锁说明操作没送达手机（确定失败，给中文提示）；
    // 推送、删除、卸载应用、卸载镜像、重启中途断开时可能已部分生效（待确认）；只读查询失败永远是确定失败。
    public static async Task ToolErrors()
    {
        using var folder = new TemporaryFolder();
        var local = Path.Combine(folder.Path, "a.txt");
        File.WriteAllText(local, "a");
        var stderr = "";
        var runner = new RecordingRunner(arguments => arguments.Contains("list") && arguments.Contains("mounter")
            ? RecordingRunner.Ok("""[{"DiskImageType": "Developer", "IsMounted": true, "MountPath": "/Developer"}]""")
            : new ProcessResult(1, "", stderr));
        var manager = new DeviceManager(new FakeBridge(), Python, runner, "win32");
        var changing = new (string Name, Func<Task> Run)[]
        {
            ("push", () => manager.PushFileAsync(Phone, local, "/Downloads")),
            ("rm", () => manager.DeleteFileAsync(Phone, "/Downloads/a.txt")),
            ("uninstall", () => manager.UninstallAppAsync(Phone, "com.example.notes")),
            ("umount", () => manager.UnmountDeveloperImageAsync(Phone)),
            ("restart", () => manager.RestartAsync(Phone))
        };
        foreach (var (text, hint) in new[]
        {
            (ToolOutput.NotFound(Udid), "没有找到这台手机"),
            (ToolOutput.Error(ToolOutput.NotConnected), "没有找到这台手机"),
            (ToolOutput.Error(ToolOutput.NotPaired), "请先在 iPhone 上信任此电脑"),
            (ToolOutput.NoPairRecord(Udid), "请先在 iPhone 上信任此电脑"),
            (ToolOutput.Error(ToolOutput.Passcode), "请先解锁手机")
        })
        {
            stderr = text;
            Assert.True(HostDeviceTools.NotReached(text), "not reached: " + text);
            foreach (var (name, run) in changing)
            {
                var error = await Assert.ThrowsAsync<DeviceOperationException>(run);
                Assert.True(!error.Uncertain && error.Message.StartsWith(hint, StringComparison.Ordinal), $"{name}: {error.Message}");
            }
        }
        stderr = ToolOutput.Error(ToolOutput.Terminated);
        foreach (var (name, run) in changing)
            Assert.True((await Assert.ThrowsAsync<DeviceOperationException>(run)).Uncertain, name + " dropped mid-way is uncertain");
        Assert.True(!(await Assert.ThrowsAsync<DeviceOperationException>(() => manager.ListFilesAsync(Phone, "/"))).Uncertain,
            "a failed query changed nothing");
        Assert.True(!(await Assert.ThrowsAsync<DeviceOperationException>(() => manager.GetAppsAsync(Phone))).Uncertain, "apps query");
        Assert.Equal("手机上没有这个文件或文件夹。", HostDeviceTools.Explain(ToolOutput.Error("No such file on the device: /x")));
        Assert.Equal("手机上没有安装这个应用。", HostDeviceTools.Explain(ToolOutput.Error("No app with bundle ID com.a.b is installed.")));
        Assert.Equal("开发者镜像没有挂载。", HostDeviceTools.Explain(ToolOutput.Error("The image is not mounted.")));
    }
}
