<!-- 来源：https://www.laoguicom.top/doc/323/ · 抓取状态：ok -->

# JavaScript随机生成汉字

```js
//  例子
logd(getRandomChineseWord());

/**
 * @作者 Mr_老鬼 QQ:1156346325
 * @函数用途   随机生成汉字
 * @创建时间 21:54 2021-11-10
 * @参数
 * @return
 **/
function getRandomChineseWord() {
    let rsl = "";
    let _randomUniCode = Math.floor(Math.random() * (40870 - 19968) + 19968).toString(16);
    rsl = "\\u" + _randomUniCode;

    return toChineseStr(rsl)
}

/**
 * @作者 Mr_老鬼 QQ:1156346325
 * @函数用途   将 uncode 转汉字
 * @创建时间 21:54 2021-11-10
 * @参数
 * @return
 **/
function toChineseStr(unicodeStr) {
    if (unicodeStr === "") {
        return "参数为空";
    }
    unicodeStr = unicodeStr.split("\\u");
    let chineseStr = "";
    for (let i = 0; i < unicodeStr.length; i++) {
        chineseStr += String.fromCharCode(parseInt(unicodeStr[i], 16));
    }
    chineseStr =chineseStr.substr(1,chineseStr.length)
    return chineseStr;
}
```
----

我是Mr-老鬼、QQ1156346325 。交流QQ群：772810035,647082990
————————————————版权声明——————————————————
本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。