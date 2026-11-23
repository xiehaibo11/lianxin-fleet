<!-- 来源：https://www.laoguicom.top/doc/210/ · 抓取状态：ok -->

# JavaScript调用java函数获取MP3文件时长

```js

/*
 * Copyright(c) 2021,
 *    项目名称:chajian
 *    文件名称:AudioFileVoiceTime.js
 *    创建时间:2021/4/17 下午12:03
 *    作者:laogui
 */

/**
 * 获取MP3时长
 * @param filePath  文件路径
 * @return {number}  返回毫秒数
 */

function getAudioFileVoiceTime(filePath) {
    importPackage(android.media)

    let mediaPlayerDuration = 0;
    if (filePath == null ) {
        return 0;
    }
    let  mediaPlayer = new MediaPlayer();
    try {
        mediaPlayer.setDataSource(filePath);
        mediaPlayer.prepare();
        mediaPlayerDuration = mediaPlayer.getDuration();
    } catch (e) {
        loge(e);
    }
    if (mediaPlayer != null) {
        mediaPlayer.stop();
        mediaPlayer.reset();
        mediaPlayer.release();
    }
    return mediaPlayerDuration;
}

// 示例
logd(getAudioFileVoiceTime("/sdcard/a.mp3"));
```