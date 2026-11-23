namespace Lianxin.Fleet.Core.OpenApi;

// 需要 EasyClick 自己的硬件或手机端程序的接口：统一返回 2001 和原因，不假装成功。
internal static class UnsupportedRoutes
{
    private const string Ble = "蓝牙 BLE 接口（需要 EasyClick 蓝牙 HID 开发板和固件）。";
    private const string UsbHid = "USB HID 接口（需要 EasyClick 桥接的 USB HID 会话）。联信通过 WDA 控制手机，请改用 clickPoint、swipeToPoint、inputText 等动作接口。";
    private const string Ime = "IME 输入法接口（需要 EasyClick 自定义输入法）。写剪贴板请用 /openapi/appHelper/setPasteboard，输入文字请用 inputText。";
    private const string Album = "相册接口（需要 EasyClick 代理 IPA 写入相册）。";
    private const string Helper = "辅助助手接口（需要 EasyClick 辅助助手 App）。";
    private const string Shortcuts = "快捷指令助手接口（需要 EasyClick 快捷指令助手程序）。";

    private static readonly string[] BlePaths =
    [
        "bleSetScale", "bleClickPoint", "bleSetScreenSize", "bleOpenSerial", "bleSetSendCmdType", "bleSetWifiInfo",
        "bleResetBle", "bleCloseSerial", "bleMouseMove", "bleResetZero", "bleToggleSoftKeyboard", "bleTouchDown",
        "bleTouchMove", "bleTouchUp", "blePress", "bleDoubleClickPoint", "bleSwipeToPoint", "bleSystemKey",
        "bleKeyPress", "bleKeyPressChar", "bleMultiTouch", "bleSetStep"
    ];

    private static readonly string[] UsbHidPaths =
    [
        "usbhidSessionStart", "usbhidSessionStop", "usbhidSessionRestart", "usbhidSetScreenSize", "usbhidClickPoint",
        "usbhidDoubleClickPoint", "usbhidPress", "usbhidSwipeToPoint", "usbhidTouchDown", "usbhidTouchMove",
        "usbhidTouchUp", "usbhidMultiTouch", "usbhidMultiTouch2", "usbhidInputText", "usbhidTypeText",
        "usbhidSystemKey", "usbhidKeyPressChar", "usbhidKeyPress", "usbhidKeyUp", "usbhidVolumeUp",
        "usbhidVolumeDown", "usbhidMute", "usbhidSetClipboard", "usbhidGetClipboard"
    ];

    private static readonly string[] ImePaths =
    [
        "imeOk", "imeInput", "imePaste", "imePressDel", "imePressEnter", "imeCopyToClipboard", "imeGetText",
        "imeGetClipboard", "imeSetClipboard", "imeOpenUrl", "imeRemoveAllContent", "imeChangeKeyboard", "imeDismiss",
        "forwardImeServer", "closeForwardImeServer"
    ];

    private static readonly string[] AlbumPaths =
        ["uploadInsertVideo", "uploadInsertImage", "requestPhotoAuthorization", "deleteAllPhotos", "deleteAllVideos"];

    // setPasteboard 和 openUrl 由 ActionRoutes 通过 WDA 实现。
    private static readonly string[] HelperPaths =
        ["setParam", "getPasteboard", "uploadToAlbum", "startForwardPort", "closeForwardPort", "closeAllForwardPort"];

    public static void Register(OpenApiRouter router)
    {
        foreach (var path in BlePaths)
            router.Unsupported("/openapi/" + path, Ble);
        foreach (var path in UsbHidPaths)
            router.Unsupported("/openapi/" + path, UsbHid);
        foreach (var path in ImePaths)
            router.Unsupported("/openapi/" + path, Ime);
        foreach (var path in AlbumPaths)
            router.Unsupported("/openapi/" + path, Album);
        foreach (var path in HelperPaths)
            router.Unsupported("/openapi/appHelper/" + path, Helper);
        // 文件接口 fsyncFileOpr、fsyncPullPush 见 FileRoutes。
        // 快捷指令助手的路径不带 /openapi，部分为 GET（与文档一致）。
        router.Unsupported("/postText", Shortcuts)
            .Unsupported("/upload", Shortcuts)
            .Unsupported("/cmd", Shortcuts, "GET")
            .Unsupported("/suc", Shortcuts, "GET")
            .Unsupported("/ult", Shortcuts, "GET");
    }
}
