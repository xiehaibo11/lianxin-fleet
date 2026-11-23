<!-- 来源：https://ieasyclick.com/iosdocs/funcs/usb-hid-event-api/ -->

# USB HID函数

EasyClick 自动化脚本 iOS免越狱 USB HID 事件函数 usbHidEvent。

- 通过 **USB 数据线** 直接模拟点击、滑动、输入等操作，**不需要**蓝牙开发板或 OTG 开发板
- 对象前缀是 `usbHidEvent`
- 适用 **EC iOS USB 10.7.0+** 中控；手机系统建议 **iOS 17+**
- 使用前请保持 USB 已连接、手机已信任本机，并已启动中控桥接
- 与 [蓝牙 BLE 函数](ble-event-api.md)、[OTG HID 函数](otg-event-api.md) 能力相近，但链路不同：USB HID 不依赖外接开发板

> **警告：与代理自动化服务互斥**
>
> **代理自动化服务**（中控「启动自动化」、脚本 `startEnv` 等）和 **USB HID**（`usbHidEvent`）是两条控机链路，**互斥，只能二选一**，不要在同一台设备、同一套脚本里混用。需要截图时请用无自动化截图函数 [`image.captureFullScreenNoAuto`](image-api.md)。

> **提示**
>
> - 首次使用建议先调用 `usbHidEvent.sessionStart()`，再 `setScreenSize`，然后做点击/输入
> - `sessionStart` 为启用 HID，**可能卸载并重新挂载较新的开发者镜像**。走 USB HID 就不要再开代理自动化服务（见上方互斥说明）
> - 坐标与投屏画面、截图像素坐标一致；分辨率变化或横竖屏切换后请重新设置屏幕尺寸
> - 返回值约定：`null` 或空字符串表示成功，其它字符串为错误信息
> - `inputText` 统一剪贴板粘贴；需要键盘打字请用 `typeText`（遇非英文会自动粘贴）
> - 粘贴英文时若出现多余空格，可在手机「设置 → 通用 → 键盘」关闭「智能标点」

## 快速示例

```javascript
function _usbOk(r) {
    return r == null || r == "";
}

function main() {
    let r = usbHidEvent.sessionStart(true);
    if (!_usbOk(r)) {
        logw("开启 USB HID 失败: " + r);
        return;
    }

    // 按本机截图/投屏实际分辨率填写
    r = usbHidEvent.setScreenSize(1170, 2532);
    if (!_usbOk(r)) {
        logw("设置屏幕尺寸失败: " + r);
        return;
    }

    r = usbHidEvent.clickPoint(200, 400);
    logd("点击: " + ( _usbOk(r) ? "成功" : r ));

    r = usbHidEvent.typeText("hello");
    logd("英文打字: " + ( _usbOk(r) ? "成功" : r ));

    r = usbHidEvent.inputText("中文测试");
    logd("中文粘贴: " + ( _usbOk(r) ? "成功" : r ));

    r = usbHidEvent.systemKey("home");
    logd("Home: " + ( _usbOk(r) ? "成功" : r ));

    usbHidEvent.sessionStop();
}

main();
```

## usbHidEvent.sessionStart 开启会话

- 开启 USB HID 会话；会话已存在时会复用
- 适配 EC iOS USB 10.7.0+版本
- @param gate 是否尝试增强兼容模式，默认 `true`（低版本系统会自动忽略）
- @returns `{string}` null 或空字符串成功，否则错误信息

> **注意：可能重挂开发者镜像**
>
> 为启用 HID，内部在必要时可能**卸载并重新挂载**较新的开发者镜像（DDI）。
>
> 代理自动化服务与 USB HID **互斥、二选一**：走 HID 就不要再调用 `startEnv` 等代理自动化。可用 `utils.checkDevImage()` 查看是否已挂载及镜像路径。详见 [工具函数 · 开发者镜像](utils-api.md#%E5%BC%80%E5%8F%91%E8%80%85%E9%95%9C%E5%83%8F)。

```javascript
function main() {
    let r = usbHidEvent.sessionStart(true);
    if (r == null || r === "") {
        logd("会话已开启");
    } else {
        logw(r);
    }
}
main();
```

## usbHidEvent.sessionStop 关闭会话

- 关闭 USB HID 会话，释放相关资源
- 适配 EC iOS USB 10.7.0+版本
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.sessionStop();
}
main();
```

## usbHidEvent.sessionRestart 重置会话

- 强制关闭并重新开启 USB HID 会话（断流、触摸失效、会话异常时用）
- 等价于 `sessionStop` 后再 `sessionStart`，比单独 `sessionStart`（可能复用旧连接）更彻底
- 适配 EC iOS USB 10.7.0+版本
- @param gate 是否尝试增强兼容模式，默认 `true`（低版本系统会自动忽略）
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    let r = usbHidEvent.sessionRestart(true);
    if (r == null || r === "") {
        logd("会话已重置");
    } else {
        logw(r);
    }
}
main();
```

## usbHidEvent.setScreenSize 设置屏幕尺寸

- 设置屏幕像素宽高，用于坐标换算；请与截图/投屏分辨率一致
- 适配 EC iOS USB 10.7.0+版本
- @param w 宽度（像素）
- @param h 高度（像素）
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    // 竖屏示例
    usbHidEvent.setScreenSize(1170, 2532);
    // 横屏时宽高对调，坐标仍用横屏截图像素
    // usbHidEvent.setScreenSize(2532, 1170);
}
main();
```

## usbHidEvent.touchDown 按下坐标

- 适配 EC iOS USB 10.7.0+版本
- @param x X 坐标
- @param y Y 坐标
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.touchDown(100, 200);
}
main();
```

## usbHidEvent.touchMove 移动坐标

- 适配 EC iOS USB 10.7.0+版本
- @param x X 坐标
- @param y Y 坐标
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.touchMove(150, 260);
}
main();
```

## usbHidEvent.touchUp 抬起坐标

- 适配 EC iOS USB 10.7.0+版本
- @param x X 坐标
- @param y Y 坐标
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.touchUp(150, 260);
}
main();
```

## usbHidEvent.clickPoint 点击坐标

- 适配 EC iOS USB 10.7.0+版本
- @param x X 坐标
- @param y Y 坐标
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    let r = usbHidEvent.clickPoint(300, 400);
    logd(r == null || r === "" ? "点击成功" : r);
}
main();
```

## usbHidEvent.press 长按坐标

- 适配 EC iOS USB 10.7.0+版本
- @param x X 坐标
- @param y Y 坐标
- @param delay 按住毫秒数
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.press(300, 500, 800);
}
main();
```

## usbHidEvent.doubleClickPoint 双击坐标

- 适配 EC iOS USB 10.7.0+版本
- @param x X 坐标
- @param y Y 坐标
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.doubleClickPoint(200, 400);
}
main();
```

## usbHidEvent.swipeToPoint 滑动

- 适配 EC iOS USB 10.7.0+版本
- @param startX 起点 X
- @param startY 起点 Y
- @param endX 终点 X
- @param endY 终点 Y
- @param duration 持续毫秒
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.swipeToPoint(100, 600, 300, 200, 500);
}
main();
```

## usbHidEvent.multiTouch 多点触摸（轨迹）

- 轨迹点格式对齐 `bleEvent.multiTouch`
- `action`：`0` 按下，`1` 抬起，`2` 移动；`delay` 为该点延迟毫秒
- 按顺序回放**单指**轨迹，适合滑动/手势脚本
- 适配 EC iOS USB 10.7.0+版本
- @param touch1 触点数组
- @param timeout 总超时毫秒
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    let touch1 = [
        {"action": 0, "x": 100, "y": 500, "delay": 20},
        {"action": 2, "x": 100, "y": 300, "delay": 30},
        {"action": 1, "x": 100, "y": 300, "delay": 20}
    ];
    usbHidEvent.multiTouch(touch1, 10000);
}
main();
```

## usbHidEvent.multiTouch2 真多指触摸

- 最多 **5 指并行**；同毫秒时刻的点会合成一帧多触点 HID report（双指缩放、三指滑动等）
- 点格式与 `multiTouch` 相同：`action` `0` 按下 / `1` 抬起 / `2` 移动，`delay` 为该点前等待的毫秒
- 不用的指传 `null`；缩放建议密采样中间点（步数多、步进小），并先打开「照片」全屏再观察效果
- 建议先用 [`image.captureFullScreenNoAuto`](image-api.md) 取宽高，再 `setScreenSize`
- 适配 EC iOS USB 10.8.0+ / Bridge 多指 USB HID
- @param finger1 第1指触点数组；不用传 `null`
- @param finger2 第2指；不用传 `null`
- @param finger3 第3指；不用传 `null`
- @param finger4 第4指；不用传 `null`
- @param finger5 第5指；不用传 `null`
- @param timeout 总超时毫秒
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function _usbOk(r) {
    return r == null || r == "";
}

/** 无自动化截图，用截图像素设置 USB HID 屏幕尺寸 */
function captureAndSetScreenSize() {
    let img = image.captureFullScreenNoAuto();
    if (!img) {
        image.resetCaptureScreenNoAutoEnv();
        sleep(1000);
        img = image.captureFullScreenNoAuto();
    }
    if (!img) {
        return null;
    }
    let w = img.getWidth();
    let h = img.getHeight();
    img.recycle();
    if (w <= 0 || h <= 0) {
        return null;
    }
    let r = usbHidEvent.setScreenSize(w, h);
    if (!_usbOk(r)) {
        return null;
    }
    return {w: w, h: h};
}

/** 单指 pinch 轨迹：相对 cx 从 startOffset 平滑移到 endOffset */
function buildPinchFinger(cx, cy, startOffset, endOffset, steps, stepDelayMs) {
    let pts = [];
    let x0 = cx + startOffset;
    pts.push({"action": 0, "x": x0, "y": cy, "delay": 10});
    pts.push({"action": 2, "x": x0, "y": cy, "delay": 60});
    for (let i = 1; i <= steps; i++) {
        let t = i / steps;
        let ox = Math.floor(startOffset + (endOffset - startOffset) * t);
        pts.push({"action": 2, "x": cx + ox, "y": cy, "delay": stepDelayMs});
    }
    pts.push({"action": 1, "x": cx + endOffset, "y": cy, "delay": 40});
    return pts;
}

function doPinch(cx, cy, startHalf, endHalf, steps, stepDelayMs, timeout) {
    let left = buildPinchFinger(cx, cy, -startHalf, -endHalf, steps, stepDelayMs);
    let right = buildPinchFinger(cx, cy, startHalf, endHalf, steps, stepDelayMs);
    return usbHidEvent.multiTouch2(left, right, null, null, null, timeout);
}

function main() {
    // 建议先打开「照片」全屏看一张图，再跑，才能看清平滑缩放
    let r = usbHidEvent.sessionStart(true);
    if (!_usbOk(r)) {
        logw("sessionStart fail: " + r);
        return;
    }

    let size = captureAndSetScreenSize();
    if (!size) {
        usbHidEvent.sessionStop();
        return;
    }
    let cx = Math.floor(size.w / 2);
    let cy = Math.floor(size.h / 2);
    let steps = 24;
    let stepDelay = 20;
    let timeout = 15000;

    // 双指外扩（放大）
    r = doPinch(cx, cy, 90, 340, steps, stepDelay, timeout);
    logd("pinch out: " + (_usbOk(r) ? "ok" : r));
    sleep(3000);

    // 双指内收（缩小）
    r = doPinch(cx, cy, 340, 90, steps, stepDelay, timeout);
    logd("pinch in: " + (_usbOk(r) ? "ok" : r));

    usbHidEvent.sessionStop();
}

main();
```

## usbHidEvent.systemKey 系统按键

- 适配 EC iOS USB 10.7.0+版本
- @param key `home` | `recents` | `lock`
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.systemKey("home");
    sleep(500);
    usbHidEvent.systemKey("recents");
}
main();
```

## usbHidEvent.inputText 输入文字（粘贴）

- 统一走剪贴板粘贴，中英文一致
- 适配 EC iOS USB 10.7.0+版本
- @param text 文本
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.inputText("hello");
    usbHidEvent.inputText("你好");
}
main();
```

## usbHidEvent.typeText 打字输入

- 可打印英文走键盘逐键；含中文、emoji 等自动改为剪贴板粘贴
- 适配 EC iOS USB 10.7.0+版本
- @param text 文本
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.typeText("hello");   // 键盘打字
    usbHidEvent.typeText("你好");     // 自动粘贴
}
main();
```

## usbHidEvent.keyPressChar 字符按键 / 组合键

- 适配 EC iOS USB 10.7.0+版本
- @param prefix 组合键，可为空：`alt` | `ctrl` | `gui` | `shift` | `r_ctrl` | `r_shift`
- @param code 字符或键名，例如 `a` / `enter` / `BS`
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.keyPressChar("", "a");
    // 粘贴：gui + v（对应 Command/Win + V）
    usbHidEvent.keyPressChar("gui", "v");
}
main();
```

## usbHidEvent.keyPress 按下单个键

- 适配 EC iOS USB 10.7.0+版本
- @param key 键名，例如 `a` / `enter` / `shift`
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.keyPress("a");
    usbHidEvent.keyUp();
}
main();
```

## usbHidEvent.keyUp 抬起全部按键

- 适配 EC iOS USB 10.7.0+版本
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.keyUp();
}
main();
```

## usbHidEvent.volumeUp 音量+

- 适配 EC iOS USB 10.7.0+版本
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.volumeUp();
}
main();
```

## usbHidEvent.volumeDown 音量-

- 适配 EC iOS USB 10.7.0+版本
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.volumeDown();
}
main();
```

## usbHidEvent.mute 静音

- 适配 EC iOS USB 10.7.0+版本
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.mute();
}
main();
```

## usbHidEvent.setClipboard 设置剪贴板

- 只写入设备剪贴板，不会粘贴到输入框
- 适配 EC iOS USB 10.7.0+版本
- @param text 文本
- @returns `{string}` null 或空字符串成功，否则错误信息

```javascript
function main() {
    usbHidEvent.setClipboard("要粘贴的内容");
    usbHidEvent.keyPressChar("gui", "v");
}
main();
```

## usbHidEvent.getClipboard 读取剪贴板

- 读取设备当前剪贴板文本
- 适配 EC iOS USB 10.7.0+版本
- @returns `{string}` 剪贴板文本；无内容或失败时返回空字符串
- **不稳定（已知限制）**：经 CoreDevice 直读，`setClipboard` 写入后再读一般可用；手机端长按复制的内容在部分 iOS（如 26.x）上常超时或读不到，甚至可能楔住设备剪贴板服务（需重启手机）。当前不要依赖本函数读取人手复制内容；后续计划改用快捷指令等使用方式绕过，暂未实现

```javascript
function main() {
    usbHidEvent.setClipboard("hello-clip");
    let t = usbHidEvent.getClipboard();
    logd("clipboard: " + t);
}
main();
```

