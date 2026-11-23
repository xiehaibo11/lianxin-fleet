using Lianxin.Fleet.Core.Devices;

namespace Lianxin.Fleet.Core.Infrastructure;

internal sealed partial class HostDeviceTools
{
    // 设备管理的只读查询和新建文件夹：命令行工具没有紧凑的 JSON 输出（应用列表含完整 Info.plist，
    // 文件列表不带类型和大小），因此用一段脚本只取需要的字段。只使用已有的信任，不自动配对。
    private const string ManageScript = """
        import asyncio, json, posixpath, sys

        from pymobiledevice3.exceptions import PyMobileDevice3Exception
        from pymobiledevice3.lockdown import create_using_usbmux

        DEVICE_KEYS = ("DeviceName", "ProductType", "ProductVersion", "BuildVersion", "SerialNumber", "ModelNumber",
                       "HardwareModel", "RegionInfo", "ActivationState")
        BATTERY_KEYS = ("BatteryCurrentCapacity", "BatteryIsCharging", "ExternalConnected", "FullyCharged")
        DISK_KEYS = ("TotalDiskCapacity", "TotalDataCapacity", "TotalDataAvailable")
        SCREEN_KEYS = ("ScreenWidth", "ScreenHeight", "ScreenScaleFactor")
        HEALTH_KEYS = ("Temperature", "CycleCount", "DesignCapacity", "NominalChargeCapacity")
        APP_KEYS = ["CFBundleIdentifier", "CFBundleDisplayName", "CFBundleName", "CFBundleShortVersionString", "CFBundleVersion"]
        MAX_ENTRIES = 1000

        def pick(values, keys):
            values = values if isinstance(values, dict) else {}
            return {key: values[key] for key in keys if isinstance(values.get(key), (str, int, float, bool))}

        async def lockdown_for(udid):
            lockdown = await create_using_usbmux(serial=udid, autopair=False)
            if not lockdown.paired:
                await lockdown.close()
                raise SystemExit("请先在 iPhone 上信任此电脑。")
            return lockdown

        async def info(udid):
            from pymobiledevice3.services.diagnostics import DiagnosticsService
            async with await lockdown_for(udid) as lockdown:
                result = pick(lockdown.all_values, DEVICE_KEYS)
                result["battery"] = pick(await lockdown.get_value("com.apple.mobile.battery"), BATTERY_KEYS)
                result["disk"] = pick(await lockdown.get_value("com.apple.disk_usage"), DISK_KEYS)
                result["screen"] = pick(await lockdown.get_value("com.apple.mobile.iTunes"), SCREEN_KEYS)
                try:
                    async with DiagnosticsService(lockdown) as diagnostics:
                        result["health"] = pick(await diagnostics.get_battery(), HEALTH_KEYS)
                except (PyMobileDevice3Exception, OSError):
                    result["health"] = {}
            return result

        async def battery(udid):
            try:
                async with await lockdown_for(udid) as lockdown:
                    values = pick(await lockdown.get_value("com.apple.mobile.battery"), BATTERY_KEYS)
                return {"Identifier": udid, **values}
            except (PyMobileDevice3Exception, OSError, SystemExit) as error:
                return {"Identifier": udid, "Error": type(error).__name__}

        async def batteries(*udids):
            async def bounded(udid):
                try:
                    return await asyncio.wait_for(battery(udid), 10)
                except asyncio.TimeoutError:
                    return {"Identifier": udid, "Error": "Timeout"}
            return list(await asyncio.gather(*(bounded(udid) for udid in udids)))

        async def apps(udid):
            from pymobiledevice3.services.installation_proxy import InstallationProxyService
            async with await lockdown_for(udid) as lockdown:
                async with InstallationProxyService(lockdown) as proxy:
                    listed = await proxy.browse({"ApplicationType": "User"}, APP_KEYS)
            return [pick(item, APP_KEYS) for item in listed]

        async def files(udid, path):
            from pymobiledevice3.services.afc import AfcService
            entries = []
            async with await lockdown_for(udid) as lockdown:
                async with AfcService(lockdown) as afc:
                    names = sorted(await afc.listdir(path))
                    for name in names[:MAX_ENTRIES]:
                        try:
                            stat = await afc.stat(posixpath.join(path, name))
                        except PyMobileDevice3Exception:
                            continue
                        entries.append({"name": name, "dir": stat.get("st_ifmt") == "S_IFDIR",
                                        "size": stat.get("st_size", 0), "mtime": int(stat["st_mtime"].timestamp())})
            return {"entries": entries, "truncated": len(names) > MAX_ENTRIES}

        async def mkdir(udid, path):
            from pymobiledevice3.services.afc import AfcService
            async with await lockdown_for(udid) as lockdown:
                async with AfcService(lockdown) as afc:
                    await afc.makedirs(path)
            return {"created": path}

        async def stat(udid, path):
            from pymobiledevice3.exceptions import AfcFileNotFoundError
            from pymobiledevice3.services.afc import AfcService
            async with await lockdown_for(udid) as lockdown:
                async with AfcService(lockdown) as afc:
                    try:
                        item = await afc.stat(path)
                    except AfcFileNotFoundError:
                        return {"exists": False}
            return {"exists": True, "dir": item.get("st_ifmt") == "S_IFDIR", "size": item.get("st_size", 0)}

        OPERATIONS = {"info": info, "batteries": batteries, "apps": apps, "files": files, "mkdir": mkdir, "stat": stat}

        try:
            print(json.dumps(asyncio.run(OPERATIONS[sys.argv[1]](*sys.argv[2:])), ensure_ascii=False))
        except PyMobileDevice3Exception as error:
            raise SystemExit(f"{type(error).__name__}: {error}") from None
        """;

    // changing：会修改手机的操作，中断时结果标为待确认。
    private async Task<string> ManageAsync(string operation, IReadOnlyList<string> arguments, bool changing,
        CancellationToken cancellationToken, TimeSpan? timeout = null)
    {
        var result = await ToolAsync(["-c", ManageScript, operation, .. arguments], changing, cancellationToken,
            timeout ?? TimeSpan.FromSeconds(30)).ConfigureAwait(false);
        return result.StandardOutput;
    }

    // 设备管理统一的子进程入口：把常见失败翻译成中文提示，其余附上工具原文。
    // 查询失败不改变手机。会改动手机的命令（推送、删除、卸载应用、卸载镜像、重启、关机）中途失败时可能已部分生效
    // （failureUncertain），只有确定没送达手机的失败（NotReached）才算确定失败。
    private async Task<ProcessResult> ToolAsync(IReadOnlyList<string> arguments, bool changing,
        CancellationToken cancellationToken, TimeSpan timeout, bool failureUncertain = false)
    {
        try
        {
            var result = await runner.RunAsync(python, arguments, timeout, cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0)
                throw new DeviceOperationException(Explain(result.StandardError),
                    failureUncertain && !NotReached(result.StandardError));
            return result;
        }
        catch (ProcessRunException error)
        {
            throw new DeviceOperationException(changing && error.Interrupted ? "操作被中断，结果待确认：" + error.Message
                : error.Message, changing && error.Interrupted, error);
        }
    }

    private Task<ProcessResult> CliAsync(IReadOnlyList<string> arguments, bool changing, CancellationToken cancellationToken,
        TimeSpan? timeout = null, bool failureUncertain = false) =>
        ToolAsync(["-m", "pymobiledevice3", "--no-color", .. arguments], changing, cancellationToken,
            timeout ?? TimeSpan.FromSeconds(30), failureUncertain);

    // 管理脚本输出「类名: 消息」；pymobiledevice3 命令行（11.57.1）只输出消息正文，例如
    // 「Device not found: usbmux has no device matching udid …」，所以两种写法都要识别。
    private static readonly string[] Missing = ["DeviceNotFoundError", "NoDeviceConnectedError", "ConnectionFailedToUsbmuxdError",
        "Device not found", "Device is not connected", "Failed to connect to usbmuxd"];
    private static readonly string[] Untrusted = ["NotPairedError", "PairingDialogResponsePendingError", "UserDeniedPairingError",
        "InvalidHostIDError", "请先在 iPhone 上信任此电脑", "Device is not paired", "has no pair record",
        "answer the Trust dialog", "refused to trust this computer", "does not recognize this computer's pairing record"];
    private static readonly string[] Locked = ["PasswordRequiredError", "protected with a passcode", "password protected"];

    internal static string Explain(string error)
    {
        var text = error.Trim();
        string? hint = text switch
        {
            _ when Has(text, "AfcFileNotFoundError", "OBJECT_NOT_FOUND", "No such file on the device", "does not exist on the device")
                => "手机上没有这个文件或文件夹。",
            _ when Has(text, Missing) => "没有找到这台手机，请检查 USB 连接和 Apple 设备服务。",
            _ when Has(text, Untrusted) => "请先在 iPhone 上信任此电脑。",
            _ when Has(text, Locked) => "请先解锁手机。",
            _ when Has(text, "NotMountedError", "image is not mounted") => "开发者镜像没有挂载。",
            _ when Has(text, "AppNotInstalledError", "app is not installed", "No app with bundle ID") => "手机上没有安装这个应用。",
            _ => null
        };
        return hint ?? "设备工具拒绝操作，请检查 USB 连接、信任状态及设备状态。" + Short(text);
    }

    // 这些失败发生在操作送达手机之前（找不到设备、没有信任、未解锁），可以确定手机状态没有改变。
    internal static bool NotReached(string error) => Has(error, Missing) || Has(error, Untrusted) || Has(error, Locked);

    private static bool Has(string text, params string[] markers) =>
        markers.Any(marker => text.Contains(marker, StringComparison.Ordinal));

    private static void CheckUdid(string udid)
    {
        if (!DeviceBridge.ValidUdid(udid))
            throw new ArgumentException("设备 UDID 无效。");
    }
}
