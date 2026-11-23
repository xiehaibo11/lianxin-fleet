using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Infrastructure;
using Lianxin.Fleet.Core.OpenApi;

namespace Lianxin.Fleet.Checks.Scenarios;

internal static partial class OpenApiChecks
{
    // 文件接口：手机路径校验、列出 / 删除 / 新建，推送与拉取的命名规则，以及电脑端路径只能落在中转目录内。
    public static async Task Files()
    {
        await using var api = await ApiHarness.StartAsync();
        var folders = new HashSet<string>(StringComparer.Ordinal) { "/", "/Downloads", "/DCIM" };
        api.ManagerReply = arguments => arguments switch
        {
            ["-c", _, "files", _, var path] when folders.Contains(path) => RecordingRunner.Ok(path == "/"
                ? """{"entries": [{"name": "DCIM", "dir": true, "size": 96, "mtime": 1794125396}, {"name": "a.txt", "dir": false, "size": 5, "mtime": 1794125396}], "truncated": false}"""
                : """{"entries": [], "truncated": false}"""),
            ["-c", _, "files", ..] => new ProcessResult(1, "", "AfcFileNotFoundError: not a folder"),
            ["-c", _, "stat", _, var path] => RecordingRunner.Ok(folders.Contains(path) ? """{"exists": true, "dir": true, "size": 64}""" : """{"exists": false}"""),
            [_, _, _, "afc", "pull", _, _, "--", var remote, var local] => Pulled(remote, local),
            _ => RecordingRunner.Ok("{}")
        };
        static ProcessResult Pulled(string remote, string local)
        {
            File.WriteAllText(Path.Combine(local, remote[(remote.LastIndexOf('/') + 1)..]), "from phone " + remote);
            return RecordingRunner.Ok();
        }
        var calls = api.ManagerRunner.Calls;
        string[] Last() => calls[^1].Arguments;
        Dictionary<string, object?> Op(string action, string path, params (string, object?)[] more) =>
            J([("deviceId", Udid), ("action", action), ("srcPath", path), .. more]);

        var names = await api.OkAsync("/openapi/fsyncFileOpr", Op("list", "/"));
        Assert.True(names.EnumerateArray().Select(item => item.GetString()).SequenceEqual(["DCIM", "a.txt"]), names.ToString());
        var detail = (await api.OkAsync("/openapi/fsyncFileOpr", Op("list", "/", ("detail", true))))[0];
        Assert.True(detail.GetProperty("dir").GetBoolean() && detail.GetProperty("path").GetString() == "/DCIM"
            && detail.GetProperty("modified").GetInt64() == 1794125396, detail.ToString());
        Assert.True((await api.OkAsync("/openapi/fsyncFileOpr", Op("rm", "/DCIM/100APPLE/IMG_0001.JPG/")))[0].GetString() == "/DCIM/100APPLE/IMG_0001.JPG", "rm");
        Assert.True(Last().SequenceEqual([.. Cli, "afc", "rm", "--udid", Udid, "--", "/DCIM/100APPLE/IMG_0001.JPG"]), "rm args");
        await api.OkAsync("/openapi/fsyncFileOpr", Op("MKDIR", "/Downloads/新建 文件夹"));
        Assert.True(Last()[2..].SequenceEqual(["mkdir", Udid, "/Downloads/新建 文件夹"]), "mkdir args");
        Assert.Equal(OpenApiCodes.DeviceFailed, (await api.PostAsync("/openapi/fsyncFileOpr", Op("list", "/a.txt"))).Code);
        var before = calls.Count;
        // PhotoData、iTunes_Control 整个子树（照片库、音乐库的数据）都不能删除或新建。
        foreach (var (action, path) in new[] { ("list", "DCIM"), ("list", "/../etc"), ("rm", "/a/./b"), ("rm", "/DCIM"), ("rm", "/"), ("mkdir", "/"), ("move", "/a"),
            ("rm", "/PhotoData/Photos.sqlite"), ("rm", "/photodata/x"), ("mkdir", "/iTunes_Control/Music/F00") })
            Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/fsyncFileOpr", Op(action, path))).Code);
        Assert.Equal(before, calls.Count);

        // 推送：电脑文件在中转目录里（相对或绝对路径）；destPath 是完整文件路径，以 / 结尾或是已有文件夹时保留原名。
        var transfer = Directory.CreateDirectory(api.Transfer).FullName;
        var local = Path.Combine(transfer, "照片 1.png");
        File.WriteAllText(local, "png");
        Dictionary<string, object?> Move(string action, string source, string target) =>
            J(("deviceId", Udid), ("action", action), ("srcPath", source), ("destPath", target));
        Assert.Equal("/Downloads/照片 1.png", (await api.OkAsync("/openapi/fsyncPullPush", Move("push", "照片 1.png", "/Downloads/"))).GetString());
        Assert.True(Last().SequenceEqual([.. Cli, "afc", "push", "--udid", Udid, "--", local, "/Downloads/照片 1.png"]), string.Join(" ", Last()));
        Assert.Equal("/Downloads/照片 1.png", (await api.OkAsync("/openapi/fsyncPullPush", Move("push", local, "/Downloads"))).GetString());
        Assert.True(calls[^2].Arguments[2..].SequenceEqual(["stat", Udid, "/Downloads"]), "existing folder is detected with one stat");
        // 目标文件名与电脑文件不同：复制到临时文件夹改名后推送，推送后临时文件删除。
        Assert.Equal("/Downloads/b.png", (await api.OkAsync("/openapi/fsyncPullPush", Move("push", local.Replace('\\', '/'), "/Downloads/b.png"))).GetString());
        var staged = Last()[^2];
        Assert.True(Last()[^1] == "/Downloads/b.png" && Path.GetFileName(staged) == "b.png" && staged.StartsWith(transfer, StringComparison.OrdinalIgnoreCase)
            && !File.Exists(staged), staged);
        Assert.True(Directory.GetFiles(transfer, "*", SearchOption.AllDirectories).SequenceEqual([local]), "staging cleaned up");

        // 电脑端路径边界：中转目录外的绝对路径、.. 逃逸、盘符相对路径、不存在的文件、链接，都在启动子进程前拒绝。
        var outside = Path.Combine(api.Root, "secret.txt");
        File.WriteAllText(outside, "keep out");
        before = calls.Count;
        foreach (var bad in new[] { outside, "../secret.txt", "..\\secret.txt", "sub/../../secret.txt", "C:/Windows/win.ini", "/etc/hosts", "missing.png", "a\nb.png", "",
            @"\\?\" + local, "//./" + local.Replace('\\', '/') })
        {
            var reply = await api.PostAsync("/openapi/fsyncPullPush", Move("push", bad, "/Downloads/x.png"));
            Assert.True(reply.Code == OpenApiCodes.InvalidArgument, bad + ": " + reply.Text);
        }
        Assert.True((await api.PostAsync("/openapi/fsyncPullPush", Move("push", outside, "/Downloads/x.png"))).Msg.Contains(transfer), "message names the folder");
        // UNC 路径（\\电脑\共享）只做字符串判断就拒绝：真去访问会连接外部共享（并耗时数秒）。
        foreach (var action in new[] { "push", "pull" })
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var unc = await api.PostAsync("/openapi/fsyncPullPush", action == "push" ? Move("push", @"\\lianxin-check-unreachable.invalid\share\a.png", "/Downloads/")
                : Move("pull", "/DCIM/a.png", @"\\lianxin-check-unreachable.invalid\share\"));
            Assert.True(unc.Code == OpenApiCodes.InvalidArgument && timer.ElapsedMilliseconds < 1000, $"{action} UNC: {unc.Code} in {timer.ElapsedMilliseconds} ms");
        }
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/fsyncPullPush", Move("push", "照片 1.png", "/PhotoData/"))).Code);
        foreach (var bad in new[] { outside, "../evil.png", "C:/Users/Public/a.png", "a:b.png", "CON.png" })
            Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/fsyncPullPush", Move("pull", "/DCIM/a.png", bad))).Code);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/fsyncPullPush", Move("pull", "/", "x"))).Code);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/fsyncPullPush", Move("copy", "a", "b"))).Code);
        Assert.Equal(before, calls.Count);
        Assert.Equal("keep out", File.ReadAllText(outside));
        if (TryLink(Path.Combine(transfer, "escape"), api.Root))
        {
            Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/fsyncPullPush", Move("push", "escape/secret.txt", "/Downloads/"))).Code);
            Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/fsyncPullPush", Move("pull", "/DCIM/a.png", "escape/"))).Code);
            Directory.Delete(Path.Combine(transfer, "escape"));
            Assert.Equal(before, calls.Count);
        }

        // 拉取：放进已有文件夹或以 / 结尾的文件夹（自动创建）保留原名；指定文件名时先拉到临时文件夹再改名；不覆盖已有文件。
        var photos = Path.Combine(transfer, "photos", "IMG_0001.JPG");
        Assert.Equal(photos, (await api.OkAsync("/openapi/fsyncPullPush", Move("pull", "/DCIM/100APPLE/IMG_0001.JPG", "photos/"))).GetString());
        Assert.True(Last()[..^1].SequenceEqual([.. Cli, "afc", "pull", "--udid", Udid, "--", "/DCIM/100APPLE/IMG_0001.JPG"])
            && Last()[^1].StartsWith(Path.Combine(Path.GetDirectoryName(photos)!, ".lianxin-pull-"), StringComparison.Ordinal), string.Join(" ", Last()));
        Assert.True(Directory.GetDirectories(Path.GetDirectoryName(photos)!).Length == 0, "pull staging folder removed");
        Assert.Equal("from phone /DCIM/100APPLE/IMG_0001.JPG", File.ReadAllText(photos));
        var renamed = Path.Combine(transfer, "a.jpg");
        Assert.Equal(renamed, (await api.OkAsync("/openapi/fsyncPullPush", Move("pull", "/DCIM/100APPLE/IMG_0002.JPG", renamed.Replace('\\', '/')))).GetString());
        Assert.Equal("from phone /DCIM/100APPLE/IMG_0002.JPG", File.ReadAllText(renamed));
        Assert.True(!Directory.Exists(Path.Combine(transfer, ".fleet-staging")) || Directory.GetFileSystemEntries(Path.Combine(transfer, ".fleet-staging")).Length == 0,
            "pull staging cleaned up");
        before = calls.Count;
        var exists = await api.PostAsync("/openapi/fsyncPullPush", Move("pull", "/DCIM/100APPLE/IMG_0003.JPG", "a.jpg"));
        Assert.True(exists.Code == OpenApiCodes.InvalidArgument && exists.Msg.Contains("不会覆盖"), exists.Text);
        Assert.Equal(OpenApiCodes.InvalidArgument, (await api.PostAsync("/openapi/fsyncPullPush", Move("pull", "/DCIM/100APPLE/IMG_0001.JPG", "photos"))).Code);
        Assert.Equal(before, calls.Count);
        Assert.Equal("from phone /DCIM/100APPLE/IMG_0002.JPG", File.ReadAllText(renamed));

        // 没有配置中转目录时，推送和拉取明确不支持。
        await using var bare = await ApiHarness.StartAsync(backend: backend => new OpenApiBackend(backend.Store, backend.Bridge,
            backend.Service, backend.Scripts, backend.Host, backend.Setup, backend.AppDirectory)
        {
            Manager = backend.Manager
        });
        Assert.Equal(OpenApiCodes.Unsupported, (await bare.PostAsync("/openapi/fsyncPullPush", Move("push", "a.png", "/Downloads/"))).Code);
    }

    // 在中转目录里放一个指向外面的目录链接；没有创建权限时跳过链接部分。
    private static bool TryLink(string link, string target)
    {
        try
        {
            Directory.CreateSymbolicLink(link, target);
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine("  （跳过链接检查：无法创建符号链接）");
            return false;
        }
    }
}
