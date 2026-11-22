using System.Globalization;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Devices;

public sealed record SetupOptions(bool DeveloperMode, bool MountImage, bool InstallRunner, string? RunnerPackage = null);

// 一键配置：开发者模式 → 开发者镜像 → 安装 WDA。每步通过 progress 报告，失败即停止该设备。
public sealed class DeviceSetup
{
    private readonly HostDeviceTools host;
    private readonly string approvedDirectory;

    public DeviceSetup(string approvedDirectory, string? pythonPath = null, IProcessRunner? processRunner = null,
        string? platform = null)
    {
        this.approvedDirectory = Path.GetFullPath(approvedDirectory);
        host = new HostDeviceTools(pythonPath, platform ?? (OperatingSystem.IsWindows() ? "win32"
            : OperatingSystem.IsMacOS() ? "darwin" : "linux"), processRunner ?? new ProcessRunner());
    }

    // 开始前统一校验，避免配置到一半才发现安装包不在批准目录。
    public SetupOptions Validate(SetupOptions options)
    {
        if (!options.DeveloperMode && !options.MountImage && !options.InstallRunner)
            throw new ArgumentException("请至少勾选一个配置步骤。");
        return options.InstallRunner
            ? options with
            {
                RunnerPackage = FleetValidation.AppPath(options.RunnerPackage, approvedDirectory)
            }
            : options with
            {
                RunnerPackage = null
            };
    }

    public async Task SetupAsync(FleetDevice device, SetupOptions options, IProgress<string> progress,
        CancellationToken cancellationToken = default)
    {
        options = Validate(options);
        Check(device);
        if (options.DeveloperMode)
        {
            if (MajorVersion(device.OsVersion) is < 16)
                progress.Report("iOS 16 以下没有开发者模式，已跳过。");
            else
            {
                progress.Report("正在检查开发者模式…");
                if (!await host.GetDeveloperModeAsync(device.Udid, cancellationToken).ConfigureAwait(false))
                {
                    progress.Report("正在开启开发者模式，手机会重启，请在手机上点「打开」…");
                    await host.EnableDeveloperModeAsync(device.Udid, cancellationToken).ConfigureAwait(false);
                }
                progress.Report("开发者模式已开启。");
            }
        }
        if (options.MountImage)
        {
            progress.Report("正在挂载开发者镜像（首次需要联网下载）…");
            await host.MountDeveloperImageAsync(device.Udid, cancellationToken).ConfigureAwait(false);
            progress.Report("开发者镜像已就绪。");
        }
        if (options.InstallRunner)
        {
            progress.Report("正在安装 WDA…");
            await host.InstallAsync(device, options.RunnerPackage, cancellationToken).ConfigureAwait(false);
            progress.Report("WDA 已安装。");
        }
    }

    public Task<bool> GetWifiConnectionsAsync(FleetDevice device, CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.GetWifiConnectionsAsync(device.Udid, cancellationToken);
    }

    // 允许本机通过 Wi-Fi 连接这台手机；需要 USB 已信任，且电脑和手机在同一局域网。
    public Task SetWifiConnectionsAsync(FleetDevice device, bool enabled, CancellationToken cancellationToken = default)
    {
        Check(device);
        return host.SetWifiConnectionsAsync(device.Udid, enabled, cancellationToken);
    }

    internal static int? MajorVersion(string? version) =>
        version?.Split('.')[0] is { } major && int.TryParse(major, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value : null;

    private static void Check(FleetDevice device)
    {
        if (!DeviceBridge.ValidUdid(device.Udid))
            throw new ArgumentException("设备 UDID 无效。");
    }
}
