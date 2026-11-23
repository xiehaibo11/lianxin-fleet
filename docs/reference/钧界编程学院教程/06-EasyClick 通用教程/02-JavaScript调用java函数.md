<!-- 来源：https://www.laoguicom.top/doc/48/ · 抓取状态：ok -->

# JavaScript调用java函数
前言

本文中所有的代码使用 JavaScript 编写，但你也可以用其他兼容 JSR 223 的脚本语言。这些例子可作为脚本文件也可以在交互式 Shell 中一次运行一个语句的方式来运行。在 JavaScript 中访问对象的属性和方法的语法与 Java 语言相同。
>d EC 使用java数组

```
// 创建数组 大小为2 类型为byte
let byteArr = java.lang.reflect.Array.newInstance(java.lang.Byte.TYPE, 2);

```
>d EC 创建对象数组

```
  let ClassArr = java.lang.reflect.Array.newInstance(java.lang.Class.forName("java.lang.Object"), 8);
  ClassArr[0] = new java.lang.Integer(0);
  ClassArr[1] = new java.lang.Integer(0);
  ClassArr[2] = new java.lang.Integer(0);
  ClassArr[3] = new java.lang.Integer(0);
    
  ClassArr[4] = new java.lang.Short(1);
  ClassArr[5] = new java.lang.Integer(0);
  ClassArr[6] = new java.lang.Short(4);
  ClassArr[7] = new java.lang.Integer(3);
```

>d EC 创建类数组

```js
  let ClassArr = java.lang.reflect.Array.newInstance(java.lang.Class.forName("java.lang.Class"), 8);
  ClassArr[0] = java.lang.Integer.TYPE;
  ClassArr[1] = java.lang.Integer.TYPE;
  ClassArr[2] = java.lang.Integer.TYPE;
  ClassArr[3] = java.lang.Integer.TYPE;

  ClassArr[4] = java.lang.Short.TYPE;
  ClassArr[5] = java.lang.Integer.TYPE;
  ClassArr[6] = java.lang.Short.TYPE;
  ClassArr[7] = java.lang.Integer.TYPE;
```

>d EC 使用java类

```js
importPackage(java.util) // 导入需要用到的类的包路径
importClass(java.util.HashMap); // 等同于 var HashMap = java.util.HashMap
// 如果需要一个对象实现多个接口，则使用 JavaAdapter
var o = new JavaAdapter(java.awt.event.ActionListener, java.lang.Runnable, {
  run: function () {},  // 实现 Runnable
  actionPerformed: function () {}  // 实现 ActionListener
});
//当 Java 方法抛出异常，Rhino 会将其转换为 JavaScript 异常传递：
try {
    java.lang.System.getProperty(null);
} catch (e) {
    print(e.javaException);
}
// 开一个线程
var myProxy =  new java.lang.Runnable() {
 run: function() {
 logd("子线程：","运行中")
 }
};

//使用 for/in 循环能遍历 Java 类和对象的方法、属性：
importClass(java.lang.System);
for (var m in System) print(m);
// 获取静态字段
var stdout = java.lang.System.out;
// 获取实例字段
var name = f.name;  // 调用 f.getName()
//Java 和 JavaScript 一样通过 new 实例化类：
var f = new java.io.File("/tmp/test");
var out = new java.io.FileWriter(f);
f instanceof java.io.File; // Rhino 中 instanceof 可用于 Java 对象
out.write("Hello Rhino!");
out.close();
var len = f.length();
//Rhino 会按照需要自动转换原始数字、布尔值和null，Java 的 char 类型会被当做 JavaScript 的数字对待，因为 JavaScript 没有字符类型，JavaScript 字符串能自动转换为 Java 字符串，但是 Java 字符串对象不能转换回 JavaScript 字符串，为了把 Java 值转换成字符串，需要通过 String() 函数实现：
var version = String(java.lang.System.getProperty("java.version"));
```

>d EC 打印java类的属性方法

```js
importClass(java.lang.Class);

clazz = Class.forName("android.graphics.RadialGradient");

//构造方法
con = clazz.getDeclaredConstructors();
len = con.length;
log("构造方法数量 = " + len);
for (var i = 0; i < len; i++) {
  let item = con[i];
  log("构造方法 " + item);
}

//属性
field = clazz.getDeclaredFields();
len = meth.length;
log("属性数量 = " + len);
for (var i = 0; i < len; i++) {
  let item = field[i];
  item.setAccessible(true);
  log("属性 " + item);
}

//方法
meth = clazz.getDeclaredMethods();
len = meth.length;
log("方法数量 = " + len);
for (var i = 0; i < len; i++) {
  let item = meth[i];
  log("方法 " + item.getName());
}
```


我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786,647082990
**------------------------------------------------版权声明------------------------------------------------------**
 本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。