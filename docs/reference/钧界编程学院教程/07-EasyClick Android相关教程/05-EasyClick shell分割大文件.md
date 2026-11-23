<!-- 来源：https://www.laoguicom.top/doc/56/ · 抓取状态：ok -->

# EasyClick shell分割大文件
shell 执行Linux命令进行大文件分割
## 分割文件
```js
//  分割源文件/sdcard/renwolesshel.tar.gz  20M大小 分割后的文件前缀 /sdcard/splitPart_  以字母结尾
 let res=shell.execAgentCommand("split -b 20m /sdcard/renwolesshel.tar.gz /sdcard/splitPart_ ");
 let ls = shell.execAgentCommand("ls /sdcard/splitPart_*")
```
##  合并文件
  ```js
 let ress = shell.execAgentCommand("cat  /sdcard/splitPart_* > /sdcard/renwolesshel2.tar.gz ")
 shell.execAgentCommand("rm -rf /sdcard/splitPart_*") // 合并后删除分割的文件
```



我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786,647082990
**--------------------------版权声明-------------------**
 本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。