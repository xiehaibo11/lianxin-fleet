<!-- 来源：https://www.laoguicom.top/doc/373/ · 抓取状态：ok -->

### Intent教学培训

1、Intent = 意图

2、你想要做什么？？



### Intent做什么？ 

1、打开新的Activitiy

2、启动Service

3、BrocastReceiver广播

4、Intent传递数据到目标



### Intent类型

我要做菜  ====== 目标意图

1、显式类型

​	我需要盐 -- 召唤出一个

第一个Activity -> MainActivity  

点击go按钮 跳转到第二个activity -> SecondActivity

startService () -> XXService

bindService() -> XXService



2、隐式类型

​	我要做菜 ----- 召唤出很多可以做出菜品的指令应答方

通过模糊匹配的方式

分别有action,category,data三个节点进行模糊查找

snssdk284://xxxx?xxx=1 隐式的Intent

### 

### 为什么打不开App

1、系统限制，从后台程序打开三方程序

2、am命令也打不开，activity的exported属性为false



### 抓取Intent（主要课程）

1、frida 抓Intent，配和root使用，Genymotion 自带root

2、安装frida https://www.cnblogs.com/hankleo/p/12834723.html

3、编译frida的python代码

​	代码逻辑：

- 加载js文件
- 挂载到目标进程
- 启动，是pycharm直接启动Python的代码

```python
# coding: utf-8
import os
import sys
import frida
import sys

reload(sys)
sys.setdefaultencoding('utf8')
data = open("catchintent.js").read()
print data
jscode = data

hdata = open("rpc.js").read()
print hdata

hook_code = hdata
os.system("adb forward tcp:27042 tcp:27042")
os.system("adb forward tcp:27043 tcp:27043")


def message(message, data):
    if message["type"] == 'send':
        print(message['payload'])
    else:
        print(message)


process = frida.get_remote_device().attach('com.example.intentdemo')
script = process.create_script(jscode)
script.on("message", message)
script.load()
sys.stdin.read()

```





4、js文件的代码

- frida语法结构，Java.perform

```javascript
Java.perform(function () {
  	//查找Activity类进行挂载
    var activity = Java.use('android.app.Activity');
  //对activity中的startActivity函数进行重载
    activity.startActivity.overload('android.content.Intent').implementation = function (intent) {
        send(intent);
       //对intent获取数据
        send("startActivity name ---> " + intent.getComponent())
        send("startActivity data ---> " + intent.getData())
        send("startActivity getAction ---> " + intent.getAction())
        send("startActivity getExtras ---> " + intent.getExtras())
      // 让被挂载的代码继续执行
        this.startActivity(intent)
    }
		
  // 对activity的onCreate函数进行重载
    activity.onCreate.overload('android.os.Bundle').implementation = function (bundle) {
        send(bundle);
        let intent = this.getIntent();
        send(intent)
        send("onCreate name ---> " + intent.getComponent())
        send("onCreate data ---> " + intent.getData())
        send("onCreate getAction ---> " + intent.getAction())
        send("onCreate getExtras ---> " + intent.getExtras())
      // 有可能是跨进程过来的，所有先获取数据的key集合
      // 然后循环从bundle中进行获取数据即可
        var bundle = intent.getExtras()
        var set = bundle.keySet().toArray();
        for (let i = 0; i < set.length; i++) {
            send("key=" + set[i] + "  value=" + bundle.get(set[i]))
        }
        //继续执行
        this.onCreate(bundle)
    }
});



```

