namespace Lianxin.Fleet.Core.Domain;

public enum FleetAction
{
    Tap, Swipe, Home, Text, Launch, Install,
    DoubleTap, LongPress, Lock, Unlock, VolumeUp, VolumeDown, OpenUrl, Clipboard, Terminate, AppSwitcher
}
public enum JobState
{
    Queued, Running, Succeeded, Partial, Failed, Unknown, Cancelled
}
public sealed record FleetDevice(long Id, string Name, string Udid, int WdaPort,
    string GroupName = "未分组", int Width = 390, int Height = 844, DateTimeOffset? LastSeen = null);
public sealed record DiscoveredDevice(string Udid, string Name);
public sealed record DeviceStatus(bool Online, int Width = 390, int Height = 844,
    string DeviceName = "", string? Error = null);
public sealed record DeviceSnapshot(FleetDevice Device, DeviceStatus Status);
public sealed record FleetSnapshot(IReadOnlyList<DeviceSnapshot> Devices, IReadOnlyList<FleetJob> Jobs);
public sealed record ActionParameters(double? X = null, double? Y = null,
    double? FromX = null, double? FromY = null, double? ToX = null, double? ToY = null,
    double Duration = 0.4, string? Text = null, string? BundleId = null, string? AppPath = null);
public sealed record JobTarget(long DeviceId, string Name, JobState State, string? Error = null);
public sealed record FleetJob(long Id, string RequestKey, FleetAction Action, ActionParameters Parameters,
    JobState State, DateTimeOffset CreatedAt, IReadOnlyList<JobTarget> Results, DateTimeOffset? FinishedAt = null);
