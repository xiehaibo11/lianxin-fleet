namespace Lianxin.Fleet.Checks.Infrastructure;

// pymobiledevice3 11.57.1 命令行失败时 stderr 的真实样子：CLI 只记录异常的消息正文，不带异常类名。
// 格式取自本机对一个不存在的 UDID 运行 `lockdown info` 的输出（主机名、进程号已替换）；
// 其余消息正文来自 pymobiledevice3 的 exceptions.py、lockdown.py、usbmux.py 中对应异常的文字。
internal static class ToolOutput
{
    public const string NotPaired = "Device is not paired.";
    public const string Passcode = "The device is protected with a passcode. Unlock it and try again.";
    public const string NotConnected = "Device is not connected.";
    public const string Terminated = "Connection was terminated abruptly.";

    public static string NotFound(string udid) => Error("Device not found: usbmux has no device matching udid " + udid);

    public static string NoPairRecord(string udid) =>
        Error($"usbmuxd has no pair record for device {udid}. Pair it with `pymobiledevice3 lockdown pair`.");

    public static string Error(string message) =>
        $"2026-11-22 15:13:18 HOST __main__[4242] ERROR {message}\n"
        + "2026-11-22 15:13:18 HOST __main__[4242] INFO Run again with -v for the full traceback (include it when reporting an issue)\n";
}
