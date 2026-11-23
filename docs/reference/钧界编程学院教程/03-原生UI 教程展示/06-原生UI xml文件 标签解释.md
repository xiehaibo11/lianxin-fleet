<!-- 来源：https://www.laoguicom.top/doc/365/ · 抓取状态：ok -->

# EasyClick xml 标签解释
# 标签属性解释
```xml
 					 android:layout_height="wrap_content"
                      android:layout_width="match_parent"
                      android:orientation="vertical"
					  android:background="@drawable/xxx"
					  android:src="@drawable/xxx"
					  android:layout="@layout/xxx"
```
## `android` 声明android属性 "`:`" 后面跟的是属性名
属性名很多 可以参考公有属性和控件、布局自身私有属性
上面示例中的属性解释： 
`layout_height` 组件高度，
`layout_width`组件宽度
`orientation` 线程布局独有属性，指的是内部组件排列方向,
`background`组件背景属性
`src` ImageView 组件独有属性，这个属性会自动等比例缩放图像,
`layout` incloude 组件独有属性

# 特殊标签说明

## `android:src="@drawable/aaa" ` 图片资源转为drawable 对象
注意 @drawable/ 是android 资源引用转为 drawable 对象，一般用于 背景图片引用，前景图引用。
`android:src`属性只适用于 ImageView组件
`aaa` 为 `res`目录下的 `aaa.png` 图片资源，切记不带后缀名

## `android:background="@drawable/bbb" `图片资源转为drawable 对象
注意 @drawable/ 是android 资源引用转为 drawable 对象，一般用于 背景图片引用，前景图引用。
`android:background`属性指的是设置为组件背景图，该属性试用所有组件
`bbb` 为 `res`目录下的 `bbb.png` 图片资源，切记不带后缀名


##  android:layout="@layout/ccc" xml文件转为layout 布局 view对象
注意 @layout/ 是将后面的xml 文件转为layout 布局view对象，这个`android:layout`属性只适用于 `include`组件
`ccc` 为 `layout`目录下的 `ccc.xml`文件，切记不带后缀名


# 标签标识解释
示例
```xml
<!-- LinearLayout 这种布局声明必须有 闭合标签尾结束 -->
  <LinearLayout   android:layout_height="match_parent"
                  android:layout_width="match_parent"
                  android:orientation="vertical"
                  android:padding="20dp">
		 <!-- TextView 这种 组件声明是自闭合标签结束没有子标签无需闭合标签尾 -->
        <TextView android:layout_width="match_parent"
                  android:layout_height="wrap_content"
                  android:gravity="center"
                  android:textSize="20sp"
                  android:textColor="#FF0000"
                  android:text="111" />

    </LinearLayout>
```
## 布局类标签
 布局类标签必须有结束标签，`include`除外这个是特殊组件。
 ```xml
<LinearLayout ><!-- 标签头 -->

	<!-- 子标签  -->
	<TextView />
</LinearLayout><!-- 标签尾 -->
```
## 控件类标签
控件类标签是自闭和，没有结束尾标签。`include`组件也是自闭合标签。
```xml
<TextView /> <!-- 自闭和标签 -->
```
