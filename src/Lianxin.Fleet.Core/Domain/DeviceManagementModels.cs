namespace Lianxin.Fleet.Core.Domain;

// 电量 0–100；读不到的项为 null。Source 为 "USB"（锁屏服务）或 "WDA"。
public sealed record BatteryInfo(int? Level, bool? Charging, bool? ExternalPower = null, bool? FullyCharged = null,
    string Source = "USB");

// 设备详情：USB 锁屏服务提供序列号、容量等静态信息，自动化启动时电量与屏幕优先取 WDA。
// 容量单位为字节，电池温度为摄氏度，屏幕为物理像素（WDA 为逻辑点 × Scale）。
public sealed record DeviceInfo(string Udid, string? DeviceName = null, string? ProductType = null, string? OsVersion = null,
    string? BuildVersion = null, string? SerialNumber = null, string? ModelNumber = null, string? HardwareModel = null,
    string? RegionInfo = null, string? ActivationState = null, int? ScreenWidth = null, int? ScreenHeight = null,
    double? ScreenScale = null, long? TotalCapacity = null, long? DataCapacity = null, long? DataAvailable = null,
    BatteryInfo? Battery = null, double? BatteryTemperature = null, int? BatteryCycleCount = null,
    int? BatteryHealth = null, string Source = "USB");

// WDA 侧的设备信息；屏幕为逻辑点。
public sealed record WdaDeviceInfo(string? Name, string? Model, int ScreenWidth, int ScreenHeight, double? Scale,
    BatteryInfo? Battery);

public sealed record DeviceApp(string BundleId, string Name, string? Version, string? Build);

// 媒体区（/var/mobile/Media）里的一项；Path 以 / 开头，相对媒体区根目录。
public sealed record DeviceFileEntry(string Path, string Name, bool IsDirectory, long Size, DateTimeOffset? Modified);

public sealed record DeviceFileListing(string Path, IReadOnlyList<DeviceFileEntry> Entries, bool Truncated);

// ImageType 为 Developer（iOS 16 及以下）或 Personalized（iOS 17 及以上）。
public sealed record DeveloperImageStatus(bool Mounted, string? ImageType = null, string? MountPath = null);

// 批量操作每台设备的结果；Uncertain 表示中断时操作可能已在手机上生效。
public sealed record DeviceBatchResult(long DeviceId, string Name, bool Succeeded, bool Uncertain = false,
    string? Error = null);
