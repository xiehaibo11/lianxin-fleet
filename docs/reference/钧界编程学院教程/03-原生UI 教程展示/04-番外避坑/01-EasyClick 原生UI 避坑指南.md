<!-- 来源：https://www.laoguicom.top/doc/61/ · 抓取状态：ok -->

[TOC]

# EasyClick 原生UI 避坑指南免费版
另有更多避坑指南在VIP课程

##  属性用错
下面这两种属性 ，用错会导致控件显示不出来
```xml
		<!-- 宽度和高度自适应，根据自身大小自动调整大小  -->
		android:layout_width="wrap_content"
		android:layout_height="wrap_content"
````
```xml
		<!-- 宽度和高度充满父布局的空间  -->
		android:layout_width="match_parent"
        android:layout_height="match_parent"
````
##  权重使用不当
<font color="ff0000" >重要提示： 只适用于线性布局</font>
宽度占比为1
```xml
		android:layout_width="0dp"
        android:layout_height="wrap_content"
        android:layout_weight="1"
````
高度占比为1
```xml
		android:layout_width="wrap_content"
        android:layout_height="0dp"
        android:layout_weight="1"
````
##  UI界面卡死
请查看代码是否在主线程操作了UI控件或者布局。
解决办法 ：
1. 后台请使用ui.run()函数操作控件或布局。
2. 在UI主线程的网络操作请使用线程操作。
3. 线程里面直接操作UI导致卡死，请在线程里使用ui.run()函数操作。

##  UI操作后不生效
检查布局代码，是否设置如下属性
```xml
		android:visibility="gone"
		android:visibility="invisible"
````
排查属性是否不当。
例子：
```xml
		<!-- 子控件使用了充满属性，会导致同一布局的其他控件或布局无法显示。-->
		android:layout_width="match_parent"
        android:layout_height="match_parent"
 ````
 
 ##  代码设置UI不生效
 请排查是否有如下属性,检查是否和ui.js引用的tag是否正确。
 ```xml
			android:tag="tag"
````
```js
		let tag = ui.tag;
````
设置了控件没有的属性报错请删除，或者改为有的属性。具体参考[原生UIAndroid手册](https://shimo.im/docs/jyQ9HkG6pqgXHyvp)
设置了不当的属性，例如宽高，权重，颜色。
## UI显示混乱
请修改布局的居中属性，权重属性，宽度高度等属性调整大小适合美观即可。
查看排版是否合理。
##  UI预览或后台刷新UI报错
1. <font color="ff0000" size="5">检查tag属性是否正确！</font>
2. 检查是否使用了错误的属性。具体参考：[原生UIAndroid手册](https://shimo.im/docs/jyQ9HkG6pqgXHyvp)
3. 检查view是否存在。

## 读取UI配置为空
1. 检查是否保存配置。
2. 检查获取的tag是否正确。

##  更多避坑后续会陆续加入。。。

我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786,647082990
**-------------------------版权声明--------------------------**
 本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。