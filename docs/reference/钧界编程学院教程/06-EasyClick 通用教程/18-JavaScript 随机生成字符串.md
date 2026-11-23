<!-- 来源：https://www.laoguicom.top/doc/339/ · 抓取状态：ok -->

# JavaScript随机生成字符串
```js
/**
 * @作者 Mr_老鬼 QQ:1156346325
 * @函数用途   随机生成字符串
 * @创建时间 15:37 2021-10-31
 * @参数 要生成的字符串位数
 * @return  生成的随机字符串
 **/
function randomString(num) {
    let str = '0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ';
    let result = '';

    while (result === "") {
        for (let i = num; i > 0; --i) {
            result += str[Math.floor(Math.random() * str.length)];
        }
    }
    return result;
}
```
-----
我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786，647082990，772810035
————————————————版权声明——————————————————
本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。