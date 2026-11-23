<!-- 来源：https://www.laoguicom.top/doc/230/ · 抓取状态：ok -->

# 判断类型
1. toString（推荐）
```javascript
Object.prototype.toString.call(obj) === '[Object Object]'
```

2. constructor属性
constructor 属性返回对创建此对象的 Boolean 函数的引用。
```javascript
obj.constructor === Object
```

3. instanceof运算符
```javascript
obj instanceof Object
```
注：需要注意的是由于数组也是对象，因此用 arr instanceof Object 也为true。

4. typeof运算符
typeof运算符返回一个字符串，表示未经计算的操作数的类型。
```js
let obj = {}
typeof obj === Object
// 根据typeof判断对象也不太准确
表达式                       返回值
typeof undefined           'undefined'
typeof null                'object'
typeof true                'boolean'
typeof 123                 'number'
typeof "abc"               'string'
typeof function() {}       'function'
typeof {}                  'object'
typeof []                  'object'
```

我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786,647082990
**------------------------------------------------版权声明------------------------------------------------------**
 本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。