using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Core.OpenApi;

// 文件接口（advance/openapi/files）：经 USB 访问手机媒体区（/var/mobile/Media，相册 DCIM、Downloads 等），不需要自动化。
// 手机路径以 / 开头、相对媒体区根目录，由 DeviceManager 校验；电脑端路径只能位于中转目录（TransferFolder）内。
internal static class FileRoutes
{
    public static void Register(OpenApiRouter router) => router
        .Post("/openapi/fsyncFileOpr", FileOperation)
        .Post("/openapi/fsyncPullPush", PullPush);

    // action：list 列出文件夹（data 为名称数组；联信扩展 detail 为 true 时为对象数组）、rm 删除文件或文件夹、mkdir 新建文件夹。
    private static async Task<OpenApiReply> FileOperation(OpenApiCall call)
    {
        var device = call.Device();
        var manager = call.Backend.RequireManager();
        var action = call.RequiredText("action").ToLowerInvariant();
        var path = DeviceManager.NormalizeDevicePath(Raw(call, "srcPath"));
        switch (action)
        {
            case "list":
                var listing = await manager.ListFilesAsync(device, path, call.Cancellation).ConfigureAwait(false);
                var message = listing.Truncated ? $"文件夹超过 1000 项，只列出前 {listing.Entries.Count} 项。" : $"共 {listing.Entries.Count} 项。";
                return call.Flag("detail") == true
                    ? OpenApiReply.Ok(listing.Entries.Select(entry => new
                    {
                        name = entry.Name,
                        path = entry.Path,
                        dir = entry.IsDirectory,
                        size = entry.Size,
                        modified = entry.Modified?.ToUnixTimeSeconds()
                    }).ToArray(), message)
                    : OpenApiReply.Ok(listing.Entries.Select(entry => entry.Name).ToArray(), message);
            case "rm":
                await manager.DeleteFileAsync(device, path, call.Cancellation).ConfigureAwait(false);
                return OpenApiReply.Ok(new[] { path }, "已删除。");
            case "mkdir":
                await manager.CreateDirectoryAsync(device, path, call.Cancellation).ConfigureAwait(false);
                return OpenApiReply.Ok(new[] { path }, "文件夹已创建。");
            default:
                throw OpenApiCall.Invalid("action 只能是 list、rm 或 mkdir。");
        }
    }

    // push：srcPath 为中转目录里的文件，destPath 为手机上的目标路径；pull：srcPath 为手机上的文件或文件夹，destPath 为中转目录里的目标路径。
    private static async Task<OpenApiReply> PullPush(OpenApiCall call)
    {
        var device = call.Device();
        var manager = call.Backend.RequireManager();
        var action = call.RequiredText("action").ToLowerInvariant();
        if (action is not ("push" or "pull"))
            throw OpenApiCall.Invalid("action 只能是 push（电脑 → 手机）或 pull（手机 → 电脑）。");
        var source = Raw(call, "srcPath");
        var target = Raw(call, "destPath");
        var folder = TransferFolder.Open(call.Backend.TransferDirectory);
        return action == "push"
            ? await PushAsync(call, device, manager, folder, source, target).ConfigureAwait(false)
            : await PullAsync(call, device, manager, folder, source, target).ConfigureAwait(false);
    }

    // destPath 以 / 结尾或是手机上已有的文件夹时放进该文件夹并保留原文件名，否则就是完整的目标文件路径；同名文件覆盖。
    private static async Task<OpenApiReply> PushAsync(OpenApiCall call, FleetDevice device, DeviceManager manager,
        TransferFolder folder, string source, string target)
    {
        var local = folder.ExistingFile(source, "srcPath（电脑文件）");
        var name = Path.GetFileName(local);
        var wanted = DeviceManager.NormalizeDevicePath(target);
        string directory, fileName;
        if (target.EndsWith('/') || wanted == "/" || await IsDeviceFolderAsync(manager, device, wanted, call.Cancellation).ConfigureAwait(false))
            (directory, fileName) = (wanted, name);
        else
            (directory, fileName) = (Parent(wanted), wanted[(wanted.LastIndexOf('/') + 1)..]);
        string pushed;
        if (fileName == name)
            pushed = await manager.PushFileAsync(device, local, directory, call.Cancellation).ConfigureAwait(false);
        else
        {
            if (!TransferFolder.ValidName(fileName))
                throw OpenApiCall.Invalid($"「{OpenApiBackend.Short(fileName)}」不能作为电脑上的临时文件名，请让 destPath 的文件名与电脑文件相同，或以 / 结尾。");
            using var staging = folder.CreateStaging();
            var copy = Path.Combine(staging.Path, fileName);
            File.Copy(local, copy);
            pushed = await manager.PushFileAsync(device, copy, directory, call.Cancellation).ConfigureAwait(false);
        }
        return OpenApiReply.Ok(pushed, "已推送到手机：" + pushed);
    }

    // destPath 是中转目录里已有的文件夹（或以 / 结尾）时放进其中并保留手机上的名称，否则就是完整的目标路径；缺少的上级文件夹自动创建。
    // 电脑上已有同名文件或文件夹时拒绝，不覆盖。
    private static async Task<OpenApiReply> PullAsync(OpenApiCall call, FleetDevice device, DeviceManager manager,
        TransferFolder folder, string source, string target)
    {
        var devicePath = DeviceManager.NormalizeDevicePath(source);
        if (devicePath == "/")
            throw OpenApiCall.Invalid("不能拉取整个媒体区，请指定其中的文件或文件夹。");
        var name = devicePath[(devicePath.LastIndexOf('/') + 1)..];
        var destination = folder.Resolve(target, "destPath（电脑路径）");
        var intoFolder = target.EndsWith('/') || target.EndsWith('\\') || Directory.Exists(destination);
        var parent = Path.TrimEndingDirectorySeparator(intoFolder ? destination : Path.GetDirectoryName(destination)!);
        var finalName = intoFolder ? name : Path.GetFileName(destination);
        if (!TransferFolder.ValidName(finalName))
            throw OpenApiCall.Invalid($"「{OpenApiBackend.Short(finalName)}」不能作为电脑上的文件名，请在 destPath 里换一个名称。");
        Directory.CreateDirectory(parent);
        var final = folder.Resolve(Path.Combine(parent, finalName), "destPath（电脑路径）");
        if (File.Exists(final) || Directory.Exists(final))
            throw OpenApiCall.Invalid($"中转目录里已有「{folder.Display(final)}」，不会覆盖，请换一个 destPath 或先移走它。");
        if (finalName == name)
            return Pulled(folder, await manager.PullFileAsync(device, devicePath, parent, call.Cancellation).ConfigureAwait(false));
        using var staging = folder.CreateStaging();
        var pulled = await manager.PullFileAsync(device, devicePath, staging.Path, call.Cancellation).ConfigureAwait(false);
        if (Directory.Exists(pulled))
            Directory.Move(pulled, final);
        else
            File.Move(pulled, final, overwrite: false);
        return Pulled(folder, final);
    }

    private static OpenApiReply Pulled(TransferFolder folder, string local) =>
        OpenApiReply.Ok(local, "已拉取到电脑：" + folder.Display(local));

    // 直接查询手机上这一项：是已有的文件夹时放进去，是文件或不存在时当作目标文件路径。
    private static async Task<bool> IsDeviceFolderAsync(DeviceManager manager, FleetDevice device, string path,
        CancellationToken cancellationToken) =>
        (await manager.GetFileAsync(device, path, cancellationToken).ConfigureAwait(false))?.IsDirectory == true;

    private static string Parent(string path)
    {
        var slash = path.LastIndexOf('/');
        return slash <= 0 ? "/" : path[..slash];
    }

    // 路径保留首尾空格（手机上的文件名可以以空格结尾），只拒绝缺失和空字符串。
    private static string Raw(OpenApiCall call, string name) =>
        call.Text(name) is { Length: > 0 } value ? value : throw OpenApiCall.Invalid($"缺少参数 {name}。");
}
