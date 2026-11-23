<!-- 来源：https://www.laoguicom.top/doc/263/ · 抓取状态：ok -->

```js
/**
 * 桌面启动APP
 * Mr、老鬼 QQ：1156346325
 * @param pkgName
 */
function lunchAPP(pkgName) {
    try {
        importPackage(android.content);
        importPackage(android.content.pm);
        importPackage(android.app);
        let pm = context.getPackageManager();
        let intent = pm.getLaunchIntentForPackage(pkgName);
        logd(intent);
        // 如果intent为空，就没有安装要跳转的app
        if (intent != null) {
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK
                | Intent.FLAG_ACTIVITY_RESET_TASK_IF_NEEDED);
			//intent.addFlags(Intent.FLAG_ACTIVITY_CLEAR_TOP);// 加上这个代表重启APP
            context.startActivity(intent)
            toast("正在启动》》" + pkgName);
        }
    } catch (e) {
        loge(e);
    }
}
// 例子 启动UC浏览器 填APP 包名即可
lunchAPP("com.UCMobile")

```


我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786，647082990，772810035
**------------------------------------------------版权声明------------------------------------------------------**
 本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。