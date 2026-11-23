<!-- 来源：https://www.laoguicom.top/doc/269/ · 抓取状态：ok -->

# EasyClick Intent 打开应用详情页
## 打开应用详情页
```js
	try {
		importClass(android.content.Intent);
		importClass(android.net.Uri)
		//let pkgName = getRunningPkg().toString();
		let intent = new Intent();
		intent.setAction("android.settings.APPLICATION_DETAILS_SETTINGS");
		let uri = Uri.parse("package:" + pkgName);// 填包名
		intent.setData(uri);
		intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        context.startActivity(intent);
    } catch (e) {
        loge(e)
    }
```

## 应用详情页强行停止

```js
/**
 * 关闭应用
 */

function closeAPP(pkgName) {
	try {
		importClass(android.content.Intent);
		importClass(android.net.Uri)
		//let pkgName = getRunningPkg().toString();
		let intent = new Intent();
		intent.setAction("android.settings.APPLICATION_DETAILS_SETTINGS");
		let uri = Uri.parse("package:" + pkgName);
		intent.setData(uri);
		intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);

        context.startActivity(intent);
    } catch (e) {
        loge(e)
    }
    let is_sure = textMatch(".*强.*|.*停.*|.*结.*|.*行.*");
    logi(is_sure + "");
    sleep(1000);
    let issure = is_sure.getOneNodeInfo(1000);
    if (issure !== null) {
        var resl = clickRandom(is_sure);
        sleep(1000)
        logi(resl + "");
        click(textMatch(".*确.*|.*定.*"));
		// 如果找不到确定
		removeNodeFlag(0)
        let nodeClose = textMatch("取消").getOneNodeInfo(0);
        logd(nodeClose);
        let mnnn = nodeClose.nextSiblings()[0]
        mnnn.click()
        toast("应用" + pkgName + "已被关闭");
        sleep(2000);
        back();
        sleep(2000);
    } else {
        loge(pkgName + "应用不能被正常关闭或不在后台运行,关闭应用失败");
        back();
    }

}
```

我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786，647082990，772810035
**------------------------------------------------版权声明------------------------------------------------------**
 本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。