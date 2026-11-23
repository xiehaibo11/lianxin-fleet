<!-- 来源：https://www.laoguicom.top/doc/324/ · 抓取状态：ok -->

# JSON 语法
  1.对象表示为键值对
  2.数据由逗号分割
  3.花括号保存对象
  4.方括号保存数组
# JSON 数据示例
```js
let str = {
    "name" : "wgq",
    "age" : "18",
    "arg" : [
        { "teacher" : "ce" ,"firstname" : "wang"},
        { "teacher2" : "ce" ,"firstname2" : "wang"}
    ]
}
```
取值
```js
    let name = str["name"]
    logd(name);
    let teacher = str["arg"][0]["teacher"]
    let firstname = str["arg"][0]["firstname"]
    logd("teacher:"+teacher+"  firstname:"+firstname);
```
输出结果
```java
wgq
teacher:ce  firstname:wang
```

# JSON 对象和字符串 互转
`JSON.stringify()` 方法将一个JSON对象转换为一个JSON字符串
`JSON.parse()` 方法将一个JSON字符串转为JSON对象

# JSON 增删改
例子如下
```js
    let jsonObj = {};
    for (let i = 0; i < 10; i++) {
        let key = "key"+i;
        let arr =[] ;
        for (let j = 0; j < 3; j++) {
            arr.push(random(1,10));
        }
        jsonObj[key] =arr;// 新增 键值对 键为key  值为arr数组 值可以为 数字，字符串，null ，数组
    }

    logd(JSON.stringify(jsonObj));
    //  改值
    jsonObj["key2"] = "修改的值";
    logd(JSON.stringify(jsonObj));
    // 添加
    jsonObj["add"] = "add value";
    logd(JSON.stringify(jsonObj));
    // 删除
    delete jsonObj["key1"];
    logd(JSON.stringify(jsonObj));
```
输出结果如下
```java
[2021-11-15 22:15:43.961]【远程设备】{"key0":[10,4,2],"key1":[4,4,10],"key2":[5,5,2],"key3":[9,4,7],"key4":[6,3,7],"key5":[2,8,8],"key6":[3,4,10],"key7":[9,3,1],"key8":[5,8,9],"key9":[4,6,9]}
[2021-11-15 22:15:43.965]【远程设备】{"key0":[10,4,2],"key1":[4,4,10],"key2":"修改的值","key3":[9,4,7],"key4":[6,3,7],"key5":[2,8,8],"key6":[3,4,10],"key7":[9,3,1],"key8":[5,8,9],"key9":[4,6,9]}
[2021-11-15 22:15:43.966]【远程设备】{"key0":[10,4,2],"key1":[4,4,10],"key2":"修改的值","key3":[9,4,7],"key4":[6,3,7],"key5":[2,8,8],"key6":[3,4,10],"key7":[9,3,1],"key8":[5,8,9],"key9":[4,6,9],"add":"add value"}
[2021-11-15 22:15:43.971]【远程设备】{"key0":[10,4,2],"key2":"修改的值","key3":[9,4,7],"key4":[6,3,7],"key5":[2,8,8],"key6":[3,4,10],"key7":[9,3,1],"key8":[5,8,9],"key9":[4,6,9],"add":"add value"}
```

我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786，647082990，772810035
————————————————版权声明——————————————————
本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。