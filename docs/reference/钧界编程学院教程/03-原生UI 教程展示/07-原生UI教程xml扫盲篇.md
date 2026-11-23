<!-- 来源：https://www.laoguicom.top/doc/366/ · 抓取状态：ok -->

# EasyClick 原生UI教程xml扫盲篇
对于EasyClick 纯小白来讲原生UI教程门槛略高，特写了这篇扫盲篇。让小白更容易上手。这些都是规定死的格式 按部就班即可 没必要深入研究，如过非要研究那么去看Android开发相关资料更深入理解。
## XML 文件说明
XML 指可扩展标记语言（eXtensible Markup Language）。
资料：[xml菜鸟教程](https://www.runoob.com/xml/xml-tutorial.html)
例子
```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- 标签头-->
<a>
	<!-- 子标签头 -->
	<b>
		<c>
		</c>
	</b>
	
	<!-- 子标签尾 -->
	
	<!-- 子标签头-->
	<b1>
		<!-- 子标签头-->
		<c>
		</c>
		<!-- 子标签尾-->
	</b1>
	<!-- 子标签尾-->
	<!-- 自闭合标签 -->
	<d />
</a>
<!-- 标签尾-- >
```
## Android布局xml文件
### 国际标准格式
AndroidStudio自动创建的MainActivity布局
```xml
<?xml version="1.0" encoding="utf-8"?>  <!-- 第一行声明是个xml 文件 编码UTF-8 -->
<androidx.constraintlayout.widget.ConstraintLayout xmlns:android="http://schemas.android.com/apk/res/android"
    xmlns:app="http://schemas.android.com/apk/res-auto"
    xmlns:tools="http://schemas.android.com/tools"
    android:layout_width="match_parent"
    android:layout_height="match_parent"
    tools:context=".MainActivity">

    <include layout="@layout/layout_test" />
</androidx.constraintlayout.widget.ConstraintLayout>
````
### EasyClick 标准格式
<font color="00ffff">说明：原生UI项目里才有下图所示的资源文件。</font>
![在这里插入图片描述](https://img-blog.csdnimg.cn/20210423214113153.png)

原生UI模版自动创建的main.xml布局
```xml
<?xml version="1.0" encoding="UTF-8" ?>  <!-- 第一行声明是个xml 文件 编码UTF-8 -->

<ScrollView
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xmlns:android="http://schemas.android.com/apk/res/android"
    xsi:noNamespaceSchemaLocation="layout.xsd"
    android:layout_height="match_parent"
    android:layout_width="match_parent" >
    <LinearLayout
        android:layout_height="match_parent"
        android:layout_width="match_parent"
        android:orientation="vertical"
        android:padding="20dp"  >
        <TextView
            android:layout_width="match_parent"
            android:layout_height="wrap_content"
            android:text="表单开始,设置tag属性，用于在代码里面获取对应的值"/>


        <LinearLayout android:layout_height="wrap_content"
                      android:orientation="horizontal"
                      android:layout_width="match_parent">
            <TextView android:layout_width="wrap_content"
                      android:layout_height="wrap_content"
                      android:text="姓名: "/>

            <EditText android:layout_width="match_parent"
                      android:layout_height="wrap_content"
                      android:tag="name"
                      android:hint="请输入姓名"/>

        </LinearLayout>

        <LinearLayout android:layout_height="wrap_content"
                      android:orientation="horizontal"
                      android:layout_width="match_parent">
            <TextView android:layout_width="wrap_content"
                      android:layout_height="wrap_content"
                      android:text="年龄: "/>

            <EditText android:layout_width="match_parent"
                      android:layout_height="wrap_content"
                      android:tag="age"
                      android:hint="请输入年龄"/>

        </LinearLayout>


        <LinearLayout android:layout_height="wrap_content"
                      android:orientation="horizontal"
                      android:layout_width="match_parent">
            <TextView android:layout_width="wrap_content"
                      android:layout_height="wrap_content"
                      android:text="性别: "/>

            <Spinner android:layout_width="match_parent"
                     android:layout_height="wrap_content"
                     android:tag="sex"
                     android:text="男同学|女同学"/>

        </LinearLayout>


        <LinearLayout android:layout_height="wrap_content"
                      android:orientation="horizontal"
                      android:layout_width="match_parent">
            <TextView android:layout_width="wrap_content"
                      android:layout_height="wrap_content"
                      android:text="喜欢玩"/>

            <EditText android:layout_width="100dp"
                      android:layout_height="wrap_content"
                      android:gravity="center_horizontal"
                      android:tag="a1"
                      android:hint="什么"/>
            <TextView android:layout_width="wrap_content"
                      android:layout_height="wrap_content"
                      android:text="和"/>
            <EditText android:layout_width="100dp"
                      android:gravity="center_horizontal"
                      android:layout_height="wrap_content"
                      android:tag="a2"
                      android:hint="什么"/>
        </LinearLayout>


        <LinearLayout android:layout_height="wrap_content"
                      android:orientation="horizontal"
                      android:layout_width="match_parent">
            <TextView android:layout_width="wrap_content"
                      android:layout_height="wrap_content"
                      android:layout_gravity="center"
                      android:text="爱        好: "/>
            <LinearLayout android:layout_height="wrap_content"
                          android:orientation="vertical"
                          android:layout_width="match_parent">
                <CheckBox android:layout_width="match_parent"
                          android:layout_height="wrap_content"
                          android:tag="music"
                          android:text="听音乐"/>
                <CheckBox android:layout_width="match_parent"
                          android:layout_height="wrap_content"
                          android:tag="sing"
                          android:text="唱歌"/>
                <CheckBox android:layout_width="match_parent"
                          android:layout_height="wrap_content"
                          android:tag="dance"
                          android:text="跳舞"/>
            </LinearLayout>

        </LinearLayout>


        <LinearLayout android:layout_height="wrap_content"
                      android:orientation="horizontal"
                      android:layout_width="match_parent">
            <TextView android:layout_width="wrap_content"
                      android:layout_height="wrap_content"
                      android:layout_gravity="center"
                      android:text="所在年级: "/>
            <RadioGroup android:layout_height="wrap_content"
                        android:orientation="vertical"
                        android:layout_width="match_parent">
                <RadioButton android:layout_width="match_parent"
                             android:layout_height="wrap_content"
                             android:tag="one"
                             android:text="一年级"/>
                <RadioButton android:layout_width="match_parent"
                             android:layout_height="wrap_content"
                             android:tag="two"
                             android:text="二年级"/>
                <RadioButton android:layout_width="match_parent"
                             android:layout_height="wrap_content"
                             android:tag="three"
                             android:text="三年级"/>
            </RadioGroup>

        </LinearLayout>
        <LinearLayout android:layout_height="wrap_content"
                      android:orientation="horizontal"
                      android:gravity="center_vertical"
                      android:layout_width="match_parent">
            <TextView android:layout_width="wrap_content"
                      android:layout_height="wrap_content"
                      android:gravity="center_vertical"
                      android:text="备注: "/>

            <EditText android:layout_width="match_parent"
                      android:layout_height="200dp"
                      android:tag="mark"
                      android:minHeight="100dp"
                      android:maxLines="1000"
                      android:hint="备注"/>

        </LinearLayout>
    </LinearLayout>
</ScrollView>
````
## EasyClick 官方标准解释
**第一行是必须有的。否则Android无法解析布局文件**
这表示是一个xml 格式的文件 编码UTF-8 。
```xml
<!-- 第一行声明是个xml 文件 编码UTF-8 -->
<?xml version="1.0" encoding="UTF-8" ?>  
````
**第二行的标签**
形如下面的xml代码
```xml
<ScrollView
	    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
	    xmlns:android="http://schemas.android.com/apk/res/android"
	    xsi:noNamespaceSchemaLocation="layout.xsd"
	    android:layout_height="match_parent"
	    android:layout_width="match_parent" >
    <!-- 这里面写子布局-->
	<LinearLayout
		        android:layout_height="match_parent"
		        android:layout_width="match_parent"
		        android:orientation="vertical"
		        android:padding="20dp"  >
		        <!-- 这里面写子布局或者 子控件-->
    </LinearLayout>
</ScrollView>
````
**说明：**

 1. 第一行：声明ScrollView布局的基本布局 ，<font color="ff0000">必须有，可以换成其他布局:线性布局等</font>。
 2. 第二行：**xmlns:xsi** 解析格式 ，<font color="ff0000">必须有，**不可更改**</font>。
 3. 第三行：**xmlns:android**  表示Android布局引用资源路径，<font color="ff0000">必须有，**不可更改**</font>。
 4. 第四行：**xsi:noNamespaceSchemaLocation** 表示EasyClick 官方的解析布局的资源 ，<font color="ff0000">必须有，**不可更改**</font>。
 5. 第五行：**android:layout_height** 布局的高度 ， <font color="ff0000">必须有,按需更改</font>。
 6. 第六行：**android:layout_width** 布局的宽度   ，<font color="ff0000">必须有,按需更改</font>。
 7. **特别说明**： 子布局和子控件不需要 **第二行** 至 **第四行** 的内容。
 8. EasyClick 中使用tag 属性代替Android的 id属性。
 
AndroidXML布局规范：[谷歌官方规范](https://developer.android.google.cn/guide/topics/ui)
其他资料参考：[安卓开发常用的布局](https://blog.csdn.net/diandianxiyu_geek/article/details/78923274)


我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786,647082990
**----------版权声明-----------------**
 本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。