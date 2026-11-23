<!-- 来源：https://www.laoguicom.top/doc/375/ · 抓取状态：ok -->

### 环境的安装

- AndroidStudio 4.x，我目前用的是4.1 的版本
- 官方网站：https://www.android.com/ ，国内可以到https://www.androiddevtools.cn/地址进行下载
- 官方下载地址： https://developer.android.com/studio，
- https://r4---sn-i3b7knlk.gvt1.com/edgedl/android/studio/install/4.2.2.0/android-studio-ide-202.7486908-windows.exe?cms_redirect=yes&mh=2R&mip=45.133.238.74&mm=28&mn=sn-i3b7knlk&ms=nvh&mt=1626872786&mv=u&mvi=4&pl=24&rmhost=r2---sn-i3b7knlk.gvt1.com&shardbypass=yes&smhost=r2---sn-i3b7knld.gvt1.com

- JDK 1.8安装
- Gradle 6.X，
- Android SDK 安装，包含adb命令这些

推荐：https://www.bilibili.com/video/BV1D54y1j7gW，看第一节课的环境安装，讲的比较详细！！！



### 新建一个Android的工程

- 打开Android Studio，选择创建一个新的工程

<img src="images/image-20210721211629705.png" alt="image-20210721211629705" style="zoom:50%;" />



​		<img src="images/image-20210721211737067.png" alt="image-20210721211737067" style="zoom:50%;" />





- Gradle依赖的Jar无法下载

  - 概念1 Maven ，Maven 包的管理系统 ，下载jar包和依赖

  - Gradle也是一个包的管理系统，下载jar包和依赖，比Maven强大，可以自定义脚本来处理编译时候的代码

  - dl.google.com无法访问和更新jar的情况

    - 在根目录的build.gradle文件中找到google()这个字样，替换成为国内的源

    - 换成这样的代码

    - ```groovy
       maven {
                  url 'https://maven.aliyun.com/repository/google'
              }
      ```

    - 重新加载一次工程

    - <img src="images/image-20210721212906331.png" alt="image-20210721212906331" style="zoom:50%;" />

  





### 工程结构

-  src：目录

  - main

    - java： java的源码包

    - res：资源文件目录，放图片，布局xml，音视频文件等等

      - drawable开头的 用来存储图片，最开始的android存放图片的模式,使用方式 R.drawable.xxx
      - mipmap开头的 也可以用来存储图片，5.0以后的新的存放结构, R.mipmap.xxx
      - layout用于存放布局xml文件
      - values：用存放字符串xml文件，风格xml，颜色xml
      - R文件的生成和应用

    - AndroidManifest.xml：整个Android组件的核心配置文件

      - Activity配置，name: 代码类路径

      - label 代表名称

        - intent-filter

          - action : 代表要接受的动作

          - category: intent过滤器的一个属性

          - 默认的入口

          - ```xml
               <intent-filter>
                            <action android:name="android.intent.action.MAIN" />
            
                            <category android:name="android.intent.category.LAUNCHER" />
                        </intent-filter>
            ```

      - service配置

      - receiver配置

      - provider配置

      - uses-permission 权限配置

- test：做测试用例源码







### Activity 是什么

- 可以理解为一个窗口，内部用于附加视图，比如按钮，文字等等
- 无论你做app还是做游戏，至少得有一个Activity

### AppCompatActivity

- support-v4的兼容包 ，support-v的兼容包中出现的AppCompatActivity
- 包含了toolbar和actionbar，以及默认的主题风格，更加丰富
- 新建的Activitity建议继承AppCompatActivity



### Activity如何进行跳转

```java
Intent intent = new Intent();
intent.setClassName(getApplicationContext(), MainActivity3.class.getName());
intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
getApplicationContext().startActivity(intent);
```















