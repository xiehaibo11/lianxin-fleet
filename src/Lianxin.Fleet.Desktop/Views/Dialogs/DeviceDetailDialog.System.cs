using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Desktop.Views.Dialogs;

// 开发者镜像与电源标签。卸载镜像、重启和关机前先停止这台手机上的脚本和自动化。
public sealed partial class DeviceDetailDialog
{
    private static readonly Brush OkBrush = new SolidColorBrush(Color.FromRgb(0x7F, 0xD4, 0x9A));
    private TextBlock imageState = null!;

    private FrameworkElement BuildImage()
    {
        var page = new StackPanel();
        imageState = new TextBlock { Text = "开发者镜像：未查询", FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 12) };
        page.Children.Add(imageState);
        page.Children.Add(Toolbar(ActionButton("查询状态", async () => await LoadImageAsync()),
            ActionButton("挂载镜像", async () => await MountAsync()),
            ActionButton("卸载镜像…", async () => await UnmountAsync())));
        page.Children.Add(Hint("自动化（WDA）依赖开发者镜像。手机每次重启后镜像都会失效，需要重新挂载；首次挂载需要联网下载。"
            + "\niOS 16 及以下使用 Developer 镜像，iOS 17 及以上使用为本机签名的 Personalized 镜像（挂载时需要访问 Apple 服务器）。"
            + "\n卸载镜像会让这台手机上的自动化和脚本停止，适合镜像异常、需要重新挂载时使用。"));
        return page;
    }

    private Task LoadImageAsync() => RunAsync("正在查询开发者镜像…", async token =>
    {
        ShowImage(await center.Runtime.DeviceManager.GetDeveloperImageAsync(row.Device, token));
        Show("已查询开发者镜像状态。", false);
    }, readOnly: true);

    private void ShowImage(DeveloperImageStatus image)
    {
        imageState.Text = image.Mounted
            ? $"开发者镜像：✓ 已挂载（{(image.ImageType == "Personalized" ? "Personalized" : "Developer")}{(image.MountPath is null ? "" : "，" + image.MountPath)}）"
            : "开发者镜像：✗ 未挂载";
        imageState.Foreground = image.Mounted ? OkBrush : DialogKit.ErrorBrush;
    }

    private async Task MountAsync()
    {
        if (await RunAsync("正在挂载开发者镜像（首次需要联网下载，可能需要几分钟）…",
            token => center.Runtime.DeviceManager.MountDeveloperImageAsync(row.Device, token)))
        {
            loaded.Add("开发者镜像");
            await LoadImageAsync();
        }
    }

    private async Task UnmountAsync()
    {
        if (!Confirm("卸载开发者镜像", "卸载后这台手机上的脚本和自动化会停止，重新挂载后才能再启动自动化。继续吗？"))
            return;
        var changed = false;
        if (await RunAsync("正在卸载开发者镜像…", async token =>
        {
            await StopAutomationAsync();
            changed = await center.Runtime.DeviceManager.UnmountDeveloperImageAsync(row.Device, token);
        }))
        {
            await LoadImageAsync();
            Show(changed ? "已卸载开发者镜像。" : "开发者镜像本来就没有挂载。", false);
        }
    }

    private FrameworkElement BuildPower()
    {
        var page = new StackPanel();
        page.Children.Add(Hint("重启或关机前会先停止这台手机上的脚本和自动化。"
            + "\n重启后需要重新挂载开发者镜像并启动自动化；关机后需要在手机上长按电源键开机。"));
        page.Children.Add(Toolbar(ActionButton("⟳ 重启手机…", async () => await PowerAsync(restart: true)),
            ActionButton("⏻ 关机…", async () => await PowerAsync(restart: false))));
        return page;
    }

    private async Task PowerAsync(bool restart)
    {
        var verb = restart ? "重启" : "关机";
        var name = row.Alias.Length > 0 ? row.Alias : row.Name;
        if (!Confirm(verb, restart ? $"重启「{name}」？这台手机上的脚本和自动化会先停止。"
            : $"关闭「{name}」？关机后需要在手机上按电源键开机，中控无法远程开机。"))
            return;
        if (await RunAsync($"正在{verb}…", async token =>
        {
            await StopAutomationAsync();
            await (restart ? center.Runtime.DeviceManager.RestartAsync(row.Device, token)
                : center.Runtime.DeviceManager.ShutdownAsync(row.Device, token));
        }))
        {
            row.SetNote(restart ? "已发送重启指令，开机后请重新挂载开发者镜像并启动自动化。" : "已发送关机指令。");
            Show(restart ? "已发送重启指令。手机开机后请重新挂载开发者镜像并启动自动化。" : "已发送关机指令。", false);
        }
    }

    private async Task StopAutomationAsync()
    {
        center.Runtime.Scripts.Stop([row.Id]);
        await center.Runtime.Host.StopDevicesAsync([row.Device]);
    }
}
