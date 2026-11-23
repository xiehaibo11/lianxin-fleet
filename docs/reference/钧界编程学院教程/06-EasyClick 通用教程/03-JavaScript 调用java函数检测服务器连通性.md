<!-- 来源：https://www.laoguicom.top/doc/59/ · 抓取状态：ok -->

# JavaScript 调用java函数检测服务器连通性

```js
/**
 * ping 服务器
 * @param hostAddress 服务器地址
 * @return {boolean}
 */
function isNetworkOnline(hostAddress) {
    importPackage(java.lang);
    let runtime = Runtime.getRuntime();
    try {
        let ipProcess = runtime.exec("ping -c 3 "+hostAddress);
        let exitValue = ipProcess.waitFor();
        return (exitValue === 0);
    } catch (  e) {
        e.printStackTrace();
    }
    return false;
}

main(){
	let net = isNetworkOnline("123.132.123.111");
	logd(net)
}
```


我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786,647082990
**------------------------------------------------版权声明------------------------------------------------------**
 本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。