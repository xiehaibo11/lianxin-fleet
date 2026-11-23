<!-- 来源：https://www.laoguicom.top/doc/318/ · 抓取状态：ok -->

# JavaScript 数组随机取值


```js

/**
 * @作者 Mr_老鬼 QQ:1156346325
 * @函数用途   随机生成字符串
 * @创建时间 15:37 2021-10-31
 * @参数  arr 要取随机值的数组 count 随机值个数
 * @return   数组随机值
 **/
function getRandomArrayElements(arr, count) {
    let shuffled = arr.slice(0), i = arr.length, min = i - count, temp, index;
    while (i-- > min) {
        index = Math.floor((i + 1) * Math.random());
        temp = shuffled[index];
        shuffled[index] = shuffled[i];
        shuffled[i] = temp;
    }
    return shuffled.slice(min);
}
var items = ['1','2','4','5','6','7','8','9','10'];
// 例子
getRandomArrayElements(items,5);
```

------------

我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786，647082990，772810035
————————————————版权声明——————————————————
本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。