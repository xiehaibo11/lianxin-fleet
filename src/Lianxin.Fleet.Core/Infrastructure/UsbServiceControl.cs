using System.ComponentModel;
using System.Diagnostics;

namespace Lianxin.Fleet.Core.Infrastructure;

// Apple USB 设备服务控制（Windows 为 Apple Mobile Device Service，macOS 为 usbmuxd）。
// 重启服务会断开所有手机的 USB 连接，调用方应先停止通道和脚本。
public static class UsbServiceControl
{
    public const string AppleServiceName = "Apple Mobile Device Service";
    public const string BonjourServiceName = "Bonjour Service";
    public static string ServiceLabel => OperatingSystem.IsMacOS() ? "usbmuxd" : AppleServiceName;

    // 通过 UAC（Windows）或管理员密码框（macOS）提权重启服务；用户拒绝授权时抛出 OperationCanceledException。
    public static async Task RestartAppleServiceAsync(CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsMacOS())
        {
            await RestartUsbmuxdAsync(new ProcessRunner(), cancellationToken).ConfigureAwait(false);
            return;
        }
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("重置 USB 只支持 Windows 和 macOS。");
        var info = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = true,
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden,
            // 命令是固定文本，不拼接任何外部输入。
            Arguments = $"-NoProfile -NonInteractive -Command \"Restart-Service -Name '{AppleServiceName}' -Force -ErrorAction Stop\""
        };
        Process? process;
        try
        {
            process = Process.Start(info);
        }
        catch (Win32Exception error) when (error.NativeErrorCode == 1223)
        {
            throw new OperationCanceledException("已取消管理员授权，USB 服务未重启。", error);
        }
        using (process)
        {
            if (process is null)
                throw new InvalidOperationException("无法启动服务重启命令。");
            await process.WaitForExitAsync(cancellationToken).WaitAsync(TimeSpan.FromMinutes(1), cancellationToken)
                .ConfigureAwait(false);
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"{AppleServiceName} 重启失败（退出码 {process.ExitCode}），请在「服务」中手动重启。");
        }
    }

    // macOS 的 usbmuxd 由 launchd 托管，结束进程后会被自动拉起。命令是固定文本，不拼接任何外部输入。
    public static async Task RestartUsbmuxdAsync(IProcessRunner runner, CancellationToken cancellationToken = default)
    {
        ProcessResult result;
        try
        {
            result = await runner.RunAsync("/usr/bin/osascript",
                ["-e", "do shell script \"/usr/bin/killall usbmuxd\" with administrator privileges"], TimeSpan.FromMinutes(2),
                cancellationToken).ConfigureAwait(false);
        }
        catch (ProcessRunException error)
        {
            throw new InvalidOperationException("无法请求管理员授权重启 usbmuxd。", error);
        }
        // -128：用户在授权框点了取消。
        if (result.ExitCode != 0 && result.StandardError.Contains("-128", StringComparison.Ordinal))
            throw new OperationCanceledException("已取消管理员授权，USB 服务未重启。");
        if (result.ExitCode != 0)
            throw new InvalidOperationException("usbmuxd 重启失败：" + result.StandardError.Trim());
    }

    // Windows 的 Apple 设备服务依赖 Bonjour 发现 Wi-Fi 手机；没有安装时只能用 USB。
    public static async Task<bool?> IsServiceInstalledAsync(string name, IProcessRunner? runner = null,
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
            return null;
        try
        {
            var result = await (runner ?? new ProcessRunner()).RunAsync("sc.exe", ["query", name], TimeSpan.FromSeconds(10),
                cancellationToken).ConfigureAwait(false);
            // 1060：指定的服务未安装。
            return result.ExitCode == 0 ? true : result.ExitCode == 1060 ? false : null;
        }
        catch (ProcessRunException)
        {
            return null;
        }
    }
}
