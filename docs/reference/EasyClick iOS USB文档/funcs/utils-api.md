<!-- 来源：https://ieasyclick.com/iosdocs/funcs/utils-api/ -->

# 工具函数

EasyClick iOS USB 工具函数 utils：zip 解压缩、MD5、随机数、音乐播放与相册、开发者镜像等常用工具。

## 解压缩

### utils.zip 压缩

- 将多个文件压缩成一个zip文件
- @param zipFile 目标zip文件的路径
- @param passwd 目标ip文件密码，空代表无密码
- @param files 要压缩的文件或者文件夹，文件数组例如: [“/sdcard/a.txt”,“/sdcard/bb/”]
- @return `{bool}` true 代表成功 false代表失败

```javascript
function main() {
    zipFile = "/sdcard/a.zip"
    //压缩文件
    let passwd = "123";
    let files = ["D:/test.json", "D:/gifshow"]
    let re = utils.zip(zipFile, passwd, files);
    logd("压缩结果: " + re);

    let ure = utils.unzip(zipFile, passwd, "D:/test111/");
    logd("解压结果: " + ure);


    let data = utils.readFileInZip("D:/a.zip", passwd, "test.json")
    logd("读取数据结果: " + data);
}

main()
```

### utils.unzipWithEncode 解压带编码

- 解压文件
- 将zip文件解压到一个文件夹中
- 适合EC iOS 6.460+
- @param zipFile 目标zip文件的路径
- @param passwd 目标ip文件密码
- @param destDir 要解压到的目标文件夹
- @param fileNameEncode 文件名的编码，Windows压缩的写GBK，其他平台都是UTF-8
- @return `{bool}` true 代表成功 false代表失败

```javascript
function main() {
    let zipFile = "C:/a.zip"
    let ure = utils.unzipWithEncode(zipFile, "", "/sdcard/test111/", "GBK");
    logd("解压结果: " + ure);
}

main()
```

### utils.unzip 解压

- 将zip文件解压到一个文件夹中
- 适用版本(EC 5.17.0+)
- @param zipFile 目标zip文件的路径
- @param passwd 目标ip文件密码
- @param destDir 要解压到的目标文件夹
- @return `{bool}` true 代表成功 false代表失败

```javascript
function main() {
    zipFile = "C:/a.zip"
    //压缩文件
    let passwd = "123";
    let files = ["D:/test.json", "D:/gifshow"]
    let re = utils.zip(zipFile, passwd, files);
    logd("压缩结果: " + re);

    let ure = utils.unzip(zipFile, passwd, "D:/test111/");
    logd("解压结果: " + ure);


    let data = utils.readFileInZip("D:/a.zip", passwd, "test.json")
    logd("读取数据结果: " + data);
}

main()
```

### utils.readFileInZip ZIP中读取

- 从zip文件中读取数据
- 适用版本(EC 5.17.0+)
- @param zipFile zip文件的路径
- @param passwd zip文件密码
- @param filePathInZip 文件在zip中的路径，例如 a/b.txt
- @return `{string}` 解析后的字符串

```javascript
function main() {

    zipFile = "/sdcard/a.zip"
    //压缩文件
    let passwd = "123";
    let files = ["D:/test.json", "D:/gifshow"]
    let re = utils.zip(zipFile, passwd, files);
    logd("压缩结果: " + re);

    let ure = utils.unzip(zipFile, passwd, "D:/test111/");
    logd("解压结果: " + ure);


    let data = utils.readFileInZip("D:/a.zip", passwd, "test.json")
    logd("读取数据结果: " + data);
}

main()
```

### utils.getRatio 取得比例

- 取得比例，例如10参数，就是返回10%的比例，如果是true，说明随机比例正确，否则不正确
- @param ratio 浮点型 1-100
- @return true或者false

```javascript
function main() {
    var ratio = utils.getRatio(20);
    logd(ratio);
}

main();
```

### utils.getRangeInt 取得某个范围的随机值

- 取得某个范围的随机值
- @param min 最小值
- @param max 最大值
- @return 在min和max中间的值,包含最大和最小值

```javascript
function main() {
    var value = utils.getRangeInt(1, 100);
    logd(value);
}

main();
```

## MD5

### utils.fileMd5 文件的MD5

- 文件的MD5
- @param file 文件路径
- @return 文件MD5字符串或者null

```javascript
function main() {
    var md5 = utils.fileMd5("D:/a.txt");
}

main();
```

### utils.dataMd5 数据计算出来的MD5

- 数据计算出来的MD5
- @param data 数据
- @return 数据MD5字符串或者null

```javascript
function main() {
    var md5 = utils.dataMd5("data");
}

main();
```

## 随机

### utils.randomInt 随机整型数据

- 随机整型数据
- @param length 位数，要随机产生多少位的整型数据
- @return 整型

```javascript
function main() {
    var r = utils.randomInt(2);
}

main();
```

### utils.randomCharNumber 取得随机的数字和字母

- 取得随机的数字和字母
- @param length 长度
- @return 字符串数字混合

```javascript
function main() {
    var r = utils.randomCharNumber(2);
}

main();
```

## 音乐

### utils.playMp3 播放mp3

- 播放mp3
- 适配EC USB 7.16.0+
- @param path 文件路径 电脑上的mp3文件
- @param volume 音量大小 0 - 100
- @param queue 是否是队列模式播放，选择否，代表立刻播放
- @param stopWhenScriptEnd 脚本结束后 就不播放
- @return boolean true代表播放成功 false代表失败

```javascript
function main() {
    //如果想把MP3内置到脚本中,将MP3文件放到res目录下
    saveResToFile("a.mp3", "d:/a.mp3")
    let r = utils.playMp3("d:/a.mp3", 100, false, false);
    sleep(10000)
    utils.stopMp3();
}

main();
```

### utils.stopMp3 停止播放mp3

- 停止播放mp3
- 适配EC USB 7.16.0+
- @return boolean true代表成功 false代表失败

```javascript
function main() {
    //如果想把MP3内置到脚本中,将MP3文件放到res目录下
    saveResToFile("a.mp3", "d:/a.mp3")
    let r = utils.playMp3("d:/a.mp3", 100, false, false);
    sleep(10000)
    utils.stopMp3();
}

main();
```

## 相册操作

### utils.requestPhotoAuthorization 请求相册权限

- 请求相册权限
- 第一次请求会有弹窗权限，请允许，或者去手机设置-拉倒最底部，找到EC app,进入勾选照片权限
- 注意: 这些都是异步的，防止卡住不能模拟点击，请忽略返回值
- 支持版本 EC 7.18.0+
- @return `{bool}` true 代表成功 false 代表失败

```javascript
function main() {
    let rx = utils.requestPhotoAuthorization()
    logd("r " + rx)
    // 接下来 调用删除的功能了
    let a = utils.deleteAllPhotos()
    logd("a " + a)

}

main();
```

### utils.deleteAllPhotos 清空相册中的图片

- 清空相册中的图片
- 调用时候会有弹窗确定，请模拟点击删除按钮
- 注意: 这些都是异步的，防止卡住不能模拟点击，请忽略返回值
- 支持版本 EC 7.18.0+
- @return `{bool}` true 代表成功 false 代表失败

```javascript
function main() {
    let rx = utils.requestPhotoAuthorization()
    logd("r " + rx)
    // 接下来 调用删除的功能了
    let a = utils.deleteAllPhotos()
    logd("a " + a)
}

main();
```

### utils.deleteAllVideos 清空相册中的视频

- 清空相册中的视频
- 调用时候会有弹窗确定，请模拟点击删除按钮
- 注意: 这些都是异步的，防止卡住不能模拟点击，请忽略返回值
- 支持版本 EC 7.18.0+
- @return `{bool}` true 代表成功 false 代表失败

```javascript
function main() {
    let rx = utils.requestPhotoAuthorization()
    logd("r " + rx)
    // 接下来 调用删除的功能了
    let a = utils.deleteAllVideos()
    logd("a " + a)
}

main();
```

## agent文件操作

### utils.uploadAgentFile 发送文件到agent

- 发送文件到agent runner存储目录
- @param filePath 本地文件路径
- @param fileName 保存的文件名称
- @return `{string}` 发送完成后，远程文件路径

```javascript
function main() {
  let recFilePath = utils.uploadAgentFile("/Volumes/dev/ch_PP-OCRv5_rec_mobile_infer.onnx", "rec.txt")
  logd("自定义 recFilePath " + recFilePath)
}

main();
```

### utils.uploadToAutoImage 上传图片到agent

- 上传图片到远程agent中，并且转换为autoimage对象
- 这个对象是在手机中，只能使用imageAgent模块，或者ocrAgent、yoloAgent三个模块操作
- @param filePath 文件路径
- @return `{AutoImage|null}`

```javascript
function main() {
  let r = utils.uploadToAutoImage("/Volumes/aa.png")
  logd("AutoImage  " + r)
}

main();
```

### utils.deleteAgentFile 删除代理ipa内部文件

- 删除代理ipa内部存储的文件
- @param filePath 文件路径
- @return `{boolean|*}` true 代表成功

```javascript
function main() {
  let recFilePath = utils.uploadAgentFile("/Volumes/dev/ch_PP-OCRv5_rec_mobile_infer.onnx", "rec.txt")
  let res = utils.deleteAgentFile(recFilePath)
  logd("删除结果  " + res)
}

main();
```

## 开发者镜像

用于查询 / 挂载 / 卸载 iOS **开发者镜像（DDI）**。无需先启动自动化。  
适配 **EC iOS USB 10.8.0+**。也可直接调用全局函数 `mountDevImage` / `unmountDevImage` / `checkDevImage`。  
第三方 HTTP 调用见 [OpenAPI · 开发者镜像](../advance/openapi/dev-image.md)。

与 [USB HID · sessionStart](usb-hid-event-api.md#usbhideventsessionstart-%E5%BC%80%E5%90%AF%E4%BC%9A%E8%AF%9D) 相关：`sessionStart` 可能为 HID 重挂较新镜像，导致随后 `startEnv` 失败；此时可先卸载再加载镜像后重启自动化。

### utils.mountDevImage 加载开发者镜像

- 加载（挂载/刷入）开发者镜像。无需启动自动化
- 手机重启后镜像会失效，需重新加载
- 适配 EC iOS USB 10.8.0+版本
- @return `{boolean}` true 成功 false 失败

```javascript
function main() {
    let ok = utils.mountDevImage();
    logd("加载开发者镜像: " + ok);
}
main();
```

### utils.unmountDevImage 卸载开发者镜像

- 卸载开发者镜像。无需启动自动化
- 卸载后依赖镜像的自动化能力不可用，需重新挂载
- 适配 EC iOS USB 10.8.0+版本
- @return `{boolean}` true 成功 false 失败

```javascript
function main() {
    let ok = utils.unmountDevImage();
    logd("卸载开发者镜像: " + ok);
}
main();
```

### utils.checkDevImage 查看镜像挂载状态

- 查看开发者镜像是否已挂载。无需启动自动化
- 适配 EC iOS USB 10.8.0+版本
- @return `{string}` JSON 字符串，例如 `{"mounted":true,"result":true,"path":"/path/to/DeveloperDiskImage.dmg"}`；未挂载时 `mounted`/`result` 为 `false`，`path` 为空

```javascript
function main() {
    let s = utils.checkDevImage();
    logd("镜像状态: " + s);
    let info = JSON.parse(s);
    logd("已挂载: " + info.mounted + ", 路径: " + info.path);
}
main();
```

## 其他

### utils.getPCIps 获取本电脑的IP

- 获取本电脑的IP，多个会用,逗号隔开
- @return `{null|string}` ip地址字符串

```javascript
function main() {
    let ips = utils.getPCIps();
    if (_isEmpty(ips)) {
        logd("未找到本电脑的IP")
        return false;
    }
    logd("本电脑的IP为: {}", ips)
}

main();
```

