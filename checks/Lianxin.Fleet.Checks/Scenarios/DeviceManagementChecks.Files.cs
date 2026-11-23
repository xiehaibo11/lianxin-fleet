using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class DeviceManagementChecks
{
    // 媒体区文件：手机路径规范化、本机路径边界、命令参数和列表解析。非法输入不会启动子进程。
    public static async Task Files()
    {
        Assert.Equal("/", DeviceManager.NormalizeDevicePath("/"));
        Assert.Equal("/DCIM/100APPLE", DeviceManager.NormalizeDevicePath("/DCIM/100APPLE/"));
        Assert.Equal("/Downloads/新建 文件夹", DeviceManager.NormalizeDevicePath("/Downloads/新建 文件夹"));
        Assert.Equal("/a/b.txt", DeviceManager.JoinDevicePath("/a", "b.txt"));
        foreach (var bad in new[] { "", "DCIM", "/../etc", "/a/./b", "/a//b", "/a\\b", "/a\0b", "/a\nb", "/" + new string('x', 1024), "/ /x" })
            Assert.Throws<ArgumentException>(() => DeviceManager.NormalizeDevicePath(bad));
        foreach (var bad in new[] { "", ".", "..", "a/b", "a\\b", " " })
            Assert.Throws<ArgumentException>(() => DeviceManager.JoinDevicePath("/", bad));

        using var folder = new TemporaryFolder();
        var local = Path.Combine(folder.Path, "照片 1.jpg");
        File.WriteAllText(local, "jpeg");
        var downloads = Directory.CreateDirectory(Path.Combine(folder.Path, "下载")).FullName;
        var listing = """
            {"entries": [{"name": "IMG_0001.JPG", "dir": false, "size": 2048, "mtime": 1794125396},
                         {"name": "100APPLE", "dir": true, "size": 96, "mtime": 1794125396},
                         {"name": "..", "dir": true, "size": 0, "mtime": 0}, {"name": "a/b", "dir": false}], "truncated": true}
            """;
        var failPull = false;
        var runner = new RecordingRunner(arguments => arguments switch
        {
            ["-c", _, "files", ..] => RecordingRunner.Ok(listing),
            ["-c", _, "stat", _, "/DCIM"] => RecordingRunner.Ok("""{"exists": true, "dir": true, "size": 96}"""),
            ["-c", _, "stat", ..] => RecordingRunner.Ok("""{"exists": false}"""),
            [_, _, _, "afc", "pull", _, _, "--", var remote, var into] => Pull(remote, into, failPull),
            _ => RecordingRunner.Ok("{}")
        });
        // 拉取：在给定的文件夹里写出文件；failPull 时只写半截再报错（连接中断）。
        static ProcessResult Pull(string remote, string into, bool fail)
        {
            File.WriteAllText(Path.Combine(into, remote[(remote.LastIndexOf('/') + 1)..]), fail ? "half" : "from phone");
            return fail ? new ProcessResult(1, "", "ConnectionTerminatedError: Connection was terminated abruptly.") : RecordingRunner.Ok();
        }
        var manager = new DeviceManager(new FakeBridge(), Python, runner, "win32");

        var files = await manager.ListFilesAsync(Phone, "/DCIM/");
        Assert.True(runner.Calls[^1].Arguments[2..].SequenceEqual(["files", Udid, "/DCIM"]), "list args");
        Assert.True(files is { Path: "/DCIM", Truncated: true } && files.Entries.Count == 2, "invalid names dropped");
        Assert.True(files.Entries[0] is { Name: "100APPLE", IsDirectory: true, Path: "/DCIM/100APPLE" }, "folders first");
        Assert.True(files.Entries[1] is { Size: 2048, IsDirectory: false } && files.Entries[1].Modified!.Value.Year == 2026, "file entry");

        await manager.CreateDirectoryAsync(Phone, "/Downloads/新建文件夹");
        Assert.True(runner.Calls[^1].Arguments[2..].SequenceEqual(["mkdir", Udid, "/Downloads/新建文件夹"]), "mkdir args");

        Assert.Equal("/Downloads/照片 1.jpg", await manager.PushFileAsync(Phone, local, "/Downloads"));
        Assert.True(runner.Calls[^1].Arguments.SequenceEqual([.. Cli, "afc", "push", "--udid", Udid, "--", local, "/Downloads/照片 1.jpg"]),
            string.Join(" ", runner.Calls[^1].Arguments));
        Assert.True(runner.Calls[^1].Timeout >= TimeSpan.FromMinutes(10), "large files may take minutes");

        Assert.Equal(Path.Combine(downloads, "IMG_0001.JPG"), await manager.PullFileAsync(Phone, "/DCIM/100APPLE/IMG_0001.JPG", downloads));
        Assert.True(runner.Calls[^1].Arguments[..^1].SequenceEqual([.. Cli, "afc", "pull", "--udid", Udid, "--", "/DCIM/100APPLE/IMG_0001.JPG"])
            && runner.Calls[^1].Arguments[^1].StartsWith(Path.Combine(downloads, ".lianxin-pull-"), StringComparison.Ordinal),
            string.Join(" ", runner.Calls[^1].Arguments));
        Assert.Equal("from phone", File.ReadAllText(Path.Combine(downloads, "IMG_0001.JPG")));
        // 拉取中途失败：临时文件夹连同半截的副本一起删掉，本机没有残留，重试可以成功。
        failPull = true;
        await Assert.ThrowsAsync<DeviceOperationException>(() => manager.PullFileAsync(Phone, "/DCIM/100APPLE/IMG_0002.JPG", downloads));
        Assert.True(!File.Exists(Path.Combine(downloads, "IMG_0002.JPG")) && Directory.GetDirectories(downloads).Length == 0, "no partial copy left");
        failPull = false;
        Assert.Equal(Path.Combine(downloads, "IMG_0002.JPG"), await manager.PullFileAsync(Phone, "/DCIM/100APPLE/IMG_0002.JPG", downloads));
        // 直接问手机某一项是否存在（上传前确认是否覆盖），不依赖列表。
        Assert.True(await manager.GetFileAsync(Phone, "/DCIM/") is { IsDirectory: true, Path: "/DCIM", Name: "DCIM" }, "stat folder");
        Assert.True(await manager.GetFileAsync(Phone, "/Downloads/none.jpg") is null && runner.Calls[^1].Arguments[2..].SequenceEqual(["stat", Udid, "/Downloads/none.jpg"]), "stat missing");

        await manager.DeleteFileAsync(Phone, "/DCIM/100APPLE/IMG_0001.JPG");
        Assert.True(runner.Calls[^1].Arguments.SequenceEqual([.. Cli, "afc", "rm", "--udid", Udid, "--", "/DCIM/100APPLE/IMG_0001.JPG"]), "rm args");

        var before = runner.Calls.Count;
        await Assert.ThrowsAsync<ArgumentException>(() => manager.DeleteFileAsync(Phone, "/"));
        await Assert.ThrowsAsync<ArgumentException>(() => manager.DeleteFileAsync(Phone, "/dcim"));
        await Assert.ThrowsAsync<ArgumentException>(() => manager.DeleteFileAsync(Phone, "/DCIM/../x"));
        // 照片库和音乐库的数据：整个子树不能删除、覆盖或新建，其中的文件也不行。
        foreach (var path in new[] { "/PhotoData/Photos.sqlite", "/PhotoData/Thumbnails/V2", "/iTunes_Control/iTunes/MediaLibrary.sqlitedb", "/ITUNES_CONTROL/x" })
            await Assert.ThrowsAsync<ArgumentException>(() => manager.DeleteFileAsync(Phone, path));
        await Assert.ThrowsAsync<ArgumentException>(() => manager.CreateDirectoryAsync(Phone, "/PhotoData/new"));
        await Assert.ThrowsAsync<ArgumentException>(() => manager.PushFileAsync(Phone, local, "/iTunes_Control/Music"));
        Assert.True(!HostDeviceTools.InProtectedTree("/PhotoDataBackup/x") && HostDeviceTools.InProtectedTree("/PhotoData"), "prefix match is per segment");
        await Assert.ThrowsAsync<ArgumentException>(() => manager.CreateDirectoryAsync(Phone, "/"));
        await Assert.ThrowsAsync<ArgumentException>(() => manager.PushFileAsync(Phone, "照片 1.jpg", "/Downloads"));
        await Assert.ThrowsAsync<FileNotFoundException>(() => manager.PushFileAsync(Phone, Path.Combine(folder.Path, "missing.jpg"), "/Downloads"));
        await Assert.ThrowsAsync<FileNotFoundException>(() => manager.PushFileAsync(Phone, downloads, "/Downloads"));
        await Assert.ThrowsAsync<ArgumentException>(() => manager.PushFileAsync(Phone, local, "Downloads"));
        await Assert.ThrowsAsync<ArgumentException>(() => manager.PullFileAsync(Phone, "/", downloads));
        await Assert.ThrowsAsync<ArgumentException>(() => manager.PullFileAsync(Phone, "/DCIM/a:b.jpg", downloads));
        await Assert.ThrowsAsync<ArgumentException>(() => manager.PullFileAsync(Phone, "/DCIM/CON.txt", downloads));
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => manager.PullFileAsync(Phone, "/DCIM/x.jpg", Path.Combine(folder.Path, "missing")));
        await Assert.ThrowsAsync<ArgumentException>(() => manager.PullFileAsync(Phone, "/DCIM/x.jpg", "relative\\folder"));
        File.WriteAllText(Path.Combine(downloads, "exists.jpg"), "keep");
        await Assert.ThrowsAsync<IOException>(() => manager.PullFileAsync(Phone, "/DCIM/exists.jpg", downloads));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(downloads, "exists.jpg")));
        Assert.Equal(before, runner.Calls.Count);

        // macOS 路径规则：/ 开头为绝对路径，文件名里的冒号在访达中无效。
        var mac = new DeviceManager(new FakeBridge(), "/usr/bin/python3", runner, "darwin");
        await Assert.ThrowsAsync<ArgumentException>(() => mac.PullFileAsync(Phone, "/DCIM/x.jpg", @"C:\Users"));
        Assert.True(HostDeviceTools.ParseFiles("/", """{"entries": []}""") is { Entries.Count: 0, Truncated: false }, "empty listing");
    }
}
