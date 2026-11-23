<!-- 来源：https://www.laoguicom.top/doc/233/ · 抓取状态：ok -->

# 去除^M的好办法:

指定文件名： 
```
sed -i "s/\s*$//g" filename
```
当前目录下所有文件：

```
sed -i "s/\s*$//g" `ls`
```