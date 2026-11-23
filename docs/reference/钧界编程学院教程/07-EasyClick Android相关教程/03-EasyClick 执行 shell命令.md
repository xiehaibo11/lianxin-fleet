<!-- 来源：https://www.laoguicom.top/doc/54/ · 抓取状态：ok -->

# EasyClick 执行 shell命令

## EasyClick 项目中执行shell命令
先看一下官方文档介绍然后在继续学习下面的文章。
官方文档：[文档](https://easyclick.gitee.io/docs/#/zh-cn/funcs/shell-api)
### EC shell 命令 不需要带“adb shell” 只需要后面的命令即可
### 代码例子如下：
```js
//执行shell 命令更改某个目录的权限为所有用户具有读写权限 需要ROOT权限
shell.sudo("chmod -R 666 /data/data/com.zf.shuashua/ ");
//执行完之后执行查看权限命令查看是否更改
//执行shell 命令查看某个目录的权限 需要ROOT权限
let sudoAl = shell.sudo("ls -al /data/data/com.zf.shuashua/ ");
logd(sudoAl);
//find . –type d –name [document] 查找目录
let sudoFind = shell.sudo("find /sdcard/. –type d -name \"newPath\" ");
logd(sudoFind);
if(sudoFind){
    //mkdir 创建目录命令
    shell.sudo("mkdir /sdcard/newPath ");
}
````
### 执行结果如下：
```java
[2021-04-11 18:09:54.831]【远程设备】total 16
drw-rw-rw-  4 u0_a43 u0_a43 4096 2021-04-10 10:30 .
drwxrwx--x 69 system system 4096 2021-04-10 14:36 ..
drw-rw-rw-  2 u0_a43 u0_a43 4096 2020-11-21 13:13 cache
drw-rw-rw-  2 u0_a43 u0_a43 4096 2020-11-21 13:13 code_cache
lrwxrwxrwx  1 root   root     35 2021-04-10 10:30 lib -> /data/app/com.zf.shuashua-1/lib/arm
[2021-04-11 18:09:56.170]【远程设备】/sdcard/./newPath
[2021-04-11 18:09:56.384]【远程设备】运行结束
````

### 关于执行结果`total 16`的解释：
下面所列文件和文件夹占用的资源为4*4kb 列出了4个文件夹所以是16kb 一个文件夹占用4kb。

关于执行结果例如：`drw-rw-rw-  4 u0_a43 u0_a43 4096 2021-04-10 10:30 .`  的解释以空格分组
（**注意**不要遗漏最后的**点**）
**第1列**：第一个字母d代表内容是目录或者文件。如果是’-‘(减号)，代表它的内容是文件。当它是l(小写l字符)，代表内容是链接文件。后面的7个字符是关于文件权限。第一个2个rw字符是文件的拥有者的权限，第二个2个rw是文件的所有组的权限，第三个2个rw是对其他人访问文件的权限。（**r**是读取**w**写入**x**执行）本例子下面一行就有x权限。
**第2列**：有多少链接指向这个文件。
**第3列**：文件/文件夹的所有者。
**第4列**：文件/文件夹的所有组。
**第5列**：文件/文件夹的以字节为单位的大小。 **目录的大小一直都是4096字节**。
**第6列**：文件最后的修改时间。
**第7列**：文件名或者目录名。

就这么多吧  ，其他的 参考[EasyClick adb shell命令大全](https://blog.csdn.net/qq_21460781/article/details/115416511)。

**-------------------------------------版权声明--------------------------------------**
 本文版权所有~Mr-老鬼 ~转载请注明原文地址
 免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。