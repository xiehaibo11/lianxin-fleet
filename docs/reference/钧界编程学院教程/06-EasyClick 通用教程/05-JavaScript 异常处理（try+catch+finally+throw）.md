<!-- 来源：https://www.laoguicom.top/doc/170/ · 抓取状态：ok -->

# JavaScript 异常处理（try+catch+finally+throw）
ECMA-262 规范了 7 种错误类型，具体说明如下。其中 Error 是基类，其他 6 种错误类型是子类，都继承 Error 基类。Error 类型的主要用途是自定义错误对象。
Error：普通异常。与 throw 语句和 try/catch 语句一起使用，属性 name 可以读写异常类型，message 属性可以读写详细错误信息。
- EvalError：不正确的使用 eval() 方法时抛出。
- SyntaxError：出现语法错误时抛出。
- RangeError：数字超出合法范围时抛出、
- ReferenceError：读取不存在的变量时抛出。
- TypeError：值得类型发生错误时抛出。
- URIError：URI 编码和解码错误时抛出。

## try/catch/finally语句
`try/catch/finally `是 JavaScript 异常处理语句。语法格式如下：
```javascript
try{
    //调试代码块
}
catch(e) {
    //捕获异常，并进行异常处理的代码块
}
finally{
    //后期清理代码块
}
```
在正常情况下，JavaScript 按顺序执行 `try` 子句中的代码，如果没有异常发生，将会忽略 `catch` 子句，跳转到 `finally` 子句中继续执行。

如果在 try 子句运行时发生错误，或者使用 throw 语句主动抛出异常，则执行 catch 子句中的代码，同时传入一个参数，引用 Error 对象。

在异常处结构中，大括号不能省略。
### 示例1
下面示例先在 `try` 子句中制造一个语法错误，然后在 `catch` 子句中获取 `Error` 对象，读取错误信息，最后在 `finally` 子句中提示代码。
```javascript
try {
    1 = 1;  //非法语句
}
catch (error) {  //捕获错误
    console.log(error.name);  //访问错误类型
    console.log(error.message);  //访问错误详细信息
}
finally {  //清除处理
    console.log("1=1");  //提示代码
}

```
`catch` 和 `finally` 子句是可选的，在正常情况下应该包含 `try` 和 `catch` 子句。
```javascript
try {1 = 1;}
catch (error) { }
```

不管 try 语句是否完全执行，finally 语句最后都必须要执行，即使使用了跳转语句跳出了异常处理结构，也必须在跳出之前先执行 finally 子句。
### 示例2
下面示例在函数体内设计一个异常处理结构，为每个子句添加一个 `return` 语句。调用函数后，实际返回的是“`finally`”，而不是“`try`”，因为 `finally` 子句必须最后执行，把 `finally` 子句去掉，函数才会返回“try”。
```javascript
function test() {
    try {
        return "try";
    }catch {
        return "catch";
    }finally {
        return "finally";
    }
}
console.log(test());  //返回“finally”
```

`try/catch/finally` 语句允许嵌套使用，嵌套的层数不限，同时形成一条词法作用域链。在 try 中发生异常时，JavaScript 会停止程序的正常运行，并跳转到层级最近的 `catch` 子句（异常处理器）。如果没有找到异常处理器，则会沿着作用域链，检查上一级的 `catch` 子句，以此类推，直到找到一个异常处理器。如果在程序中没有找到任何异常处理器，将会显示错误。
### 示例3
下面代码就是一个多层嵌套的异常结构，在处理一系列的异常时，内层的 `catch` 子句通过将异常抛出，就可以将异常抛给外层的 `catch` 子句来处理。
```javascript
try {  //外层异常处理结构
    try {  //内层异常处理结构
        test();  //错误调用
    }
    catch(error) {
        if (error.name == "ReferenceError") console.log("错误参考");   //如果是异常引用，则提示这样的信息
        else throw error;  //否则再次抛出一个异常，并把错误信息向上传递
        }
}
catch (error) {  //获取内层异常处理结构中抛出的异常
    console.log("内层 try/catch 不能够处理这个错误");
}
```
## throw语句
**throw** 语句能够主动抛出异常，语法格式如下：
`throw expression;`
**expression** 是任意类型的表达式，一般为 `Error` 对象，或者 `Error` 子类示例。

当执行 `throw` 语句时，程序会立即停止执行。只有当使用 `try/catch` 语句捕获到被抛出的值时，程序才会执行。
### 示例
下面示例在循环体内设计当循环变量大于 5 时，定义并抛出一个异常。
```javascript
try {
    for (var i = 0;i < 10;i++) {
        if (i > 5) throw new Error ("循环变量的值大于 5 了");  //定义错误对象，并抛出异常
        console.log(i);
    }
}
catch (error) { }  //捕获错误，其中error就是new Error() 的实例
```
在抛出异常时，JavaScript 也会停止程序的正常执行，并跳转到最近的 `catch` 子句。如果没有找到 `catch` 子句，则会检查上一级的 `catch` 子句，以此类推，直到找到一个异常处理器为止。如果在程序中没有找到任何异常处理器，将会显示错误。

我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786,647082990
**------------------------------------------------版权声明------------------------------------------------------**
 本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。