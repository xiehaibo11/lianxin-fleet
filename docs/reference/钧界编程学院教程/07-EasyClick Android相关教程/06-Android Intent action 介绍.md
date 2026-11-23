<!-- 来源：https://www.laoguicom.top/doc/266/ · 抓取状态：ok -->

# Android Intent action 介绍
`Intent`的中文意思是“意图，目的”的意思，可以理解为不同组件之间通信的“媒介”或者“信使”。

目标组件一般要通过`Intent`来声明自己的条件，一般通过组件中的`<intent-filter>`元素来过滤。

`Intent`在由以下几个部分组成：动作（`action`），数据（`data`），分类（`Category`），类型（`Type`），组件（`Component`），和扩展信息（`Extra`）。

`Intent`在寻找目标组件的时候有两种方法：
- 第一:通过组件名称直接指定；
- 第二:通过Intent Filter过滤指定


# Intent启动不同组件的方法

| 组件名称|  方法名称  |
|-----------|-------------|
|Activity|  startActivity()  startActivityForResult()  |
|Service | startService()  bindService()  |
|Broadcasts|  sendBroadcast()  sendOrderedBroadcast()  sendStickyBroadcast()|

 
# 常见的Activity Action Intent常量

| 常量名称|  常量值  |意义|
|-----------|-------------|---------|
|ACTION_MAIN | android.intent.action.MAIN |  应用程序入口|
|ACTION_VIEW | android.intent.action.VIEW | 显示数据给用户|
|ACTION_ATTACH_DATA | android.intent.action.ATTACH_DATA | 指明附加信息给其他地方的一些数据|
|ACTION_EDIT|  android.intent.action.EDIT|  显示可编辑的数据  |
|ACTION_PICK | android.intent.action.PICK|  选择数据  |
|ACTION_CHOOSER | android.intent.action.CHOOSER | 显示一个Activity选择器  |
|ACTION_GET_CONTENT | android.intent.action.GET_CONTENT | 获得内容  |
|ACTION_DIAL | android.intent.action.GET_CONTENT | 显示打电话面板  |
|ACITON_CALL | android.intent.action.DIAL | 直接打电话 |
|ACTION_SEND | android.intent.action.SEND | 直接发短信  |
|ACTION_SENDTO | android.intent.action.SENDTO | 选择发短信  |
|ACTION_ANSWER | android.intent.action.ANSWER | 应答电话  |
|ACTION_INSERT | android.intent.action.INSERT | 插入数据  |
|ACTION_DELETE | android.intent.action.DELETE  |删除数据  |
|ACTION_RUN | android.intent.action.RUN | 运行数据  |
|ACTION_SYNC | android.intent.action.SYNC | 同步数据  |
|ACTION_PICK_ACTIVITY | android.intent.action.PICK_ACTIVITY | 选择Activity  |
|ACTION_SEARCH | android.intent.action.SEARCH | 搜索  |
|ACTION_WEB_SEARCH | android.intent.action.WEB_SEARCH | Web搜索  |
|ACTION_FACTORY_TEST | android.intent.action.FACTORY_TEST | 工厂测试入口点     |


# 常见的BroadcastIntent Action常量  
| Action字符串常量|  描述  |
|-----------|-------------|
|ACTION_TIME_TICK|  系统时间每过一分钟发出的广播  |
|ACTION_TIME_CHANGED  | 系统时间通过设置发生了变化  |
|ACTION_TIMEZONE_CHANGED | 时区改变  |
|ACTION_BOOT_COMPLETED | 系统启动完毕  |
|ACTION_PACKAGE_ADDED | 新的应用程序apk包安装完毕  |
|ACTION_PACKAGE_CHANGED | 现有应用程序apk包改变  |
|ACTION_PACKAGE_REMOVED | 现有应用程序apk包被删除  |
|ACTION_UID_REMOVED | 用户id被删除     |

# 常用的 系统设置选项

1. 以com.android.settings开头的形式：

| SETTINGS Action 常量| 说明|
|---|---|
|android.settings.ACCESSIBILITY_SETTINGS |辅助功能|
|android.settings.ADD_ACCOUNT_SETTINGS |添加账户|
|android.settings.AIRPLANE_MODE_SETTINGS |系统设置首页|
|android.settings.APN_SETTINGS |APN设置|
|android.settings.APPLICATION_SETTINGS |应用管理|
|android.settings.BATTERY_SAVER_SETTINGS |节电助手|
|android.settings.BLUETOOTH_SETTINGS |蓝牙|
|android.settings.CAPTIONING_SETTINGS |字幕|
|android.settings.CAST_SETTINGS |无线显示|
|android.settings.DATA_ROAMING_SETTINGS |移动网络|
|android.settings.DATE_SETTINGS |日期和时间设置|
|android.settings.DEVICE_INFO_SETTINGS |关于手机|
|android.settings.DISPLAY_SETTINGS |显示设置|
|android.settings.DREAM_SETTINGS |互动屏保设置|
|android.settings.HARD_KEYBOARD_SETTINGS |实体键盘|
|android.settings.HOME_SETTINGS |应用权限,默认应用设置,特殊权限|
|android.settings.IGNORE_BATTERY_OPTIMIZATION_SETTINGS |忽略电池优化设置|
|android.settings.INPUT_METHOD_SETTINGS |可用虚拟键盘设置|
|android.settings.INPUT_METHOD_SUBTYPE_SETTINGS |安卓键盘语言设置(AOSP)|
|android.settings.INTERNAL_STORAGE_SETTINGS |内存和存储|
|android.settings.LOCALE_SETTINGS |语言偏好设置|
|android.settings.LOCATION_SOURCE_SETTINGS |定位服务设置|
|android.settings.MANAGE_ALL_APPLICATIONS_SETTINGS |所有应用|
|android.settings.MANAGE_APPLICATIONS_SETTINGS |应用管理|
|android.settings.MANAGE_DEFAULT_APPS_SETTINGS |与ACTION_HOME_SETTINGS相同|
|android.settings.action.MANAGE_OVERLAY_PERMISSION |在其他应用上层显示,悬浮窗|
|android.settings.MANAGE_UNKNOWN_APP_SOURCES |安装未知应用 安卓8.0|
|android.settings.action.MANAGE_WRITE_SETTINGS |可修改系统设置 权限|
|android.settings.MEMORY_CARD_SETTINGS |内存与存储|
|android.settings.NETWORK_OPERATOR_SETTINGS |可用网络选择|
|android.settings.NFCSHARING_SETTINGS |NFC设置|
|android.settings.NFC_SETTINGS |网络中的 更多设置|
|android.settings.ACTION_NOTIFICATION_LISTENER_SETTINGS |通知权限设置|
|android.settings.NOTIFICATION_POLICY_ACCESS_SETTINGS |勿扰权限设置|
|android.settings.ACTION_PRINT_SETTINGS |打印服务设置|
|android.settings.PRIVACY_SETTINGS |备份和重置|
|android.settings.SECURITY_SETTINGS |安全设置|
|android.settings.SHOW_REGULATORY_INFO |监管信息|
|android.settings.SOUND_SETTINGS |声音设置|
|android.settings.SYNC_SETTINGS |添加账户设置|
|android.settings.USAGE_ACCESS_SETTINGS |有权查看使用情况的应用|
|android.settings.USER_DICTIONARY_SETTINGS |个人词典|
|android.settings.VOICE_INPUT_SETTINGS |辅助应用和语音输入|
|android.settings.VPN_SETTINGS |VPN设置|
|android.settings.VR_LISTENER_SETTINGS |VR助手|
|android.settings.WEBVIEW_SETTINGS |选择webview|
|android.settings.WIFI_IP_SETTINGS |高级WLAN设置|
|android.settings.WIFI_SETTINGS |选择WIFI,连接WIFI|
|com.android.settings.Settings$DevelopmentSettingsActivity |开发者选项|

例子
```java
Intent intent = new Intent();
ComponentName cm = new ComponentName("com.android.settings","com.android.settings.Settings");
intent.setComponent(cm);
intent.setAction("android.intent.action.VIEW");
startActivity(intent);
```

2. 以android.provider.Settings.开头的形式：

| action 常量| 说明|
|---------|----------------|
|ACTION_ACCESSIBILITY_SETTINGS|系统的辅助功能界面|
|ACTION_ADD_ACCOUNT|创建新账户界面|
|ACTION_AIRPLANE_MODE_SETTINGS|飞行模式设置界面|
|ACTION_APN_SETTINGS|APNs设置界面|
|ACTION_APPLICATION_DETAILS_SETTINGS|根据包名跳转到系统自带的应用程序信息界面|
|ACTION_APPLICATION_DEVELOPMENT_SETTINGS|开发人员选项界面|
|ACTION_APPLICATION_SETTINGS|应用程序列表界面|
|ACTION_BLUETOOTH_SETTINGS|蓝牙设置界面|
|ACTION_DATA_ROAMING_SETTINGS|移动网络设置界面|
|ACTION_DATE_SETTINGS|日期时间设置界面|
|ACTION_DEVICE_INFO_SETTINGS|手机状态界面|
|ACTION_DISPLAY_SETTINGS|手机显示界面|
|ACTION_INPUT_METHOD_SETTINGS|语言和输入设备界面|
|ACTION_INPUT_METHOD_SUBTYPE_SETTINGS|语言选择界面（多国语言选择，API 11及以上）|
|ACTION_INTERNAL_STORAGE_SETTINGS|存储设置界面（内部存储）|
|ACTION_LOCALE_SETTINGS|语言选择界面（仅有English和中文两种选择）|
|ACTION_LOCATION_SOURCE_SETTINGS|位置服务界面（管理已安装的应用程序）|
|ACTION_MANAGE_ALL_APPLICATIONS_SETTINGS|跳转到应用程序界面（所有的）|
|ACTION_MANAGE_APPLICATIONS_SETTINGS|应用程序列表界面（已安装的）|
|ACTION_MEMORY_CARD_SETTINGS|存储设置（记忆卡存储）|
|ACTION_NETWORK_OPERATOR_SETTINGS|选择网络运营商界面|
|ACTION_NFCSHARING_SETTINGS|* 显示NFC共享设置，API 14及以上*|
|ACTION_NFC_SETTINGS|显示NFC设置，API 16及以上|
|ACTION_PRINT_SETTINGS|打印设置|
|ACTION_PRIVACY_SETTINGS|备份和重置界面|
|ACTION_QUICK_LAUNCH_SETTINGS|快速启动设置界面|
|ACTION_SEARCH_SETTINGS|搜索设置界面|
|ACTION_SECURITY_SETTINGS|安全设置界面|
|ACTION_SETTINGS|系统设置界面|
|ACTION_SOUND_SETTINGS |声音设置界面|
|ACTION_SYNC_SETTINGS|账户同步界面|
|ACTION_USER_DICTIONARY_SETTINGS|用户字典界面|
|ACTION_WIFI_IP_SETTINGS |IP设定界面|
|ACTION_WIFI_SETTINGS|Wi-Fi列表设置界面|
|ACTION_WIRELESS_SETTINGS|允许无线控制，Wi-Fi，蓝牙和移动网络的配置|

例子
```java
Intent intent = new Intent(android.provider.Settings.ACTION_SETTINGS);
startActivity(intent);
```

# Intent的Action和Data属性匹配


| Action属性|  Data属性  |说明|
|-----------|-------------|--------------|
|ACTION_VIEW|  content://contacts/people/1 | 显示id为1的联系人信息  |
|ACTION_DIAL|  content://contacts/people/1 | 将id为1的联系人电话号码显示在拨号界面中  |
|ACITON_VIEW | tel:123 | 显示电话为123的联系人信息  |
|ACTION_VIEW | http://www.google.com | 在浏览器中浏览该网站  |
|ACTION_VIEW | file://sdcard/mymusic.mp3 | 播放MP3  |
|ACTION_VIEW | geo:39.2456,116.3523 | 显示地图|


# 常见的Category常量
|Category字符串常量 | 描述|
|------------------|-----------|
|CATEGORY_BROWSABLE | 目标Activity能通过在网页浏览器中点击链接而激活（比如，点击浏览器中的图片链接）  |
|CATEGORY_GADGET | 表示目标Activity可以被内嵌到其他Activity当中  |
|CATEGORY_HOME | 目标Activity是HOME Activity，即手机开机启动后显示的Activity，或按下HOME键后显示的Activity  |
|CATEGORY_LAUNCHER | 表示目标Activity是应用程序中最优先被执行的Activity  |
|CATEGORY_PREFERENCE | 表示目标Activity是一个偏爱设置的Activity|


# 常见的Extra常量
|Extra键值字符串常量 | 描述|
|-------------|-------------|
|EXTRA_BCC | 装有邮件密送地址的字符串数组  |
|EXTRA_CC | 装有邮件抄送地址的字符串数组  |
|EXTRA_EMAIL | 装有邮件发送地址的字符串数组  |
|EXTRA_INTENT | 使用ACTION_PICK_ACTIVITY动作时装有Intent选项的键  |
|EXTRA_KEY_EVENT | 触发该Intent的案件的KeyEvent对象  |
|EXTRA_PHONE_NUMBER | 使用拨打电话相关的Action时，电话号码字符串的键，类型为String  |
|EXTRA_SHORTCUT_ICON <br> EXTRA_SHORTCUT_ICON_RESOURCE <br> EXTRA_SHORTCUT_INTENT <br> EXTRA_SHORTCUT_NAME  <br>EXTRA_SUBJECT | 使用ACTION_CREATE_SHORTCUT在HomeActivity创建快捷方式时，对快捷方式的描述信息。<br>其中ICON和ICON_RESOURCE描述的是快捷方式的图标，类型分别为Bitmap和ShortcutIconResource。INTENT描述的是快捷方式相对应的Intent对象。NAME描述的是快捷方式的名字。  |

## 描述信息主题的键  
|Extra键值字符串常量 | 描述|
|-------------|-------------|
|EXTRA_TEXT | 使用ACTION_SEND动作时，用来描述要发送的文本信息，类型为CharSequence  |
|EXTRA_TITLE | 使用ACTION_CHOOSER动作时，描述对话框标题的键，类型为CharSequence  |
|EXTRA_UID | 使用ACTION_UID_REMOVED动作时，描述删除的用户id的键，类型为int    |


# Android.telephony包中的类  
    
|类名 | 描述|
|-------------|-------------|
|CellLocation | 表示设备位置的抽象类 |
|PhoneNumberFormattingTextWather | 监视一个TextView控件，如果有电话号码输入，则用&#124;formatNumber()方法处理电话号码  |
|PhoneNumberUtils | 包含各种处理电话号码字符串的使用工具  |
|PhoneStateListener | 监视手机中电话状态变化的监听类  |
|ServiceState | 包含电话状态和相关的服务信息  |
|TelephonyManager | 提供对手机中电话服务信息的访问|

 
## 与短信服务相关的类主要在包android.telephony.gsm中
|类名 | 描述|
|-------------|-------------|
|GsmCellLocation | 表示GSM手机的基站位置  |
|SmsManager | 管理各种短信操作  |
|SmsMessage | 表示具体的短信  |


# Intent的用法
## 用Action跳转

1. 使用Action跳转，如果有一个程序的AndroidManifest.xml中的某一个 Activity的IntentFilter段中 定义了包含了相同的Action那么这个Intent就与这个目标Action匹配。如果这个IntentFilter段中没有定义 Type,Category，那么这个 Activity就匹配了。但是如果手机中有两个以上的程序匹配，那么就会弹出一个对话可框来提示说明。
Action 的值在Android中有很多预定义，如果你想直接转到你自己定义的Intent接收者，你可以在接收者的IntentFilter 中加入一个自定义的Action值（同时要设定 Category值为"android.intent.category.DEFAULT"），在你的Intent中设定该值为Intent的 Action,就直接能跳转到你自己的Intent接收者中。因为这个Action在系统中是唯一的。

2. data/type，你可以用Uri 来做为data,比如Uri uri = Uri.parse(http://www.google.com);
Intent i = new Intent(Intent.ACTION_VIEW,uri);手机的Intent分发过程中，会根据http://www.google.com 的scheme判断出数据类型type 。手机的Brower则能匹配它，在Brower的Manifest.xml中的IntenFilter中 首先有ACTION_VIEW Action,也能处理http:的type。

3.  至于分类Category，一般不要去在Intent中设置它，如果你写Intent的接收者，就在Manifest.xml的Activity的 IntentFilter中包含android.category.DEFAULT,这样所有不设置 Category（Intent.addCategory(String c);）的Intent都会与这个Category匹配。
4. extras（附 加信息），是其它所有附加信息的集合。使用extras可以为组件提供扩展信息，比如，如果要执行“发送电子邮件”这个动 作，可以将电子邮件的标题、正文等保存在extras里，传给电子邮件发送组件。

## EasyClick Intent 例子
```js
importClass(android.content.Intent);
importClass(android.net.Uri)
var intent = new Intent();
intent.setAction("android.settings.APPLICATION_DETAILS_SETTINGS");
intent.setData(Uri.parse("package:com.gibb.easyclick"))
intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
try {
context.startActivity(intent);
} catch (e) {
loge(e)
}
```

## 用类名跳转
Intent负责对应用中一次操作的动作、动作涉及数据、附加数据进行描 述，Android则根据此Intent的描述， 负责找到对应的组件，将 Intent传递给调用的组件，并完成组件的调用。Intent在这里起着实现调用者与被调用者之间的解耦作用。Intent传递过程中，要找 到目标消费者（另一个Activity,IntentReceiver或Service），也就是Intent的响 应者。
```java
Intent intent = new Intent();
intent.setClass(context, targetActivy.class);
//或者直接用 Intent intent = new Intent(context, targetActivity.class);
startActivity(intent);
```
不过注意用类名跳转，需要在`AndroidManifest.xml`中申明`activity`
```xml
<activity android:name="targetActivity"></activity>
```


# Intent 常用用法


1. 从google搜索内容
```java
Intent intent = new Intent();
intent.setAction(Intent.ACTION_WEB_SEARCH);
intent.putExtra(SearchManager.QUERY,"searchString")
startActivity(intent);
```

2. 浏览网页
```java
Uri uri = Uri.parse("http://www.google.com");
Intent it  = new Intent(Intent.ACTION_VIEW,uri);
startActivity(it);
```

3. 显示地图
```java
Uri uri = Uri.parse("geo:38.899533,-77.036476");
Intent it = new Intent(Intent.Action_VIEW,uri);
startActivity(it);
```

4. 路径规划
```java
Uri uri = Uri.parse("http://maps.google.com/maps?f=dsaddr=startLat%20startLng&daddr=endLat%20endLng&hl=en");
Intent it = new Intent(Intent.ACTION_VIEW,URI);
startActivity(it);
```

5. 拨打电话
```java
Uri uri = Uri.parse("tel:xxxxxx");
Intent it = new Intent(Intent.ACTION_DIAL, uri);   
startActivity(it);
```

6. 调用发短信的程序 
```java
Intent it = new Intent(Intent.ACTION_VIEW);    
it.putExtra("sms_body", "The SMS text");    
it.setType("vnd.android-dir/mms-sms");    
startActivity(it);
```

7. 发送短信
```java
Uri uri = Uri.parse("smsto:0800000123");    
Intent it = new Intent(Intent.ACTION_SENDTO, uri);    
it.putExtra("sms_body", "The SMS text");    
startActivity(it);
String body="this is sms demo";
Intent mmsintent = new Intent(Intent.ACTION_SENDTO, Uri.fromParts("smsto", number, null));
mmsintent.putExtra(Messaging.KEY_ACTION_SENDTO_MESSAGE_BODY, body);
mmsintent.putExtra(Messaging.KEY_ACTION_SENDTO_COMPOSE_MODE, true);
mmsintent.putExtra(Messaging.KEY_ACTION_SENDTO_EXIT_ON_SENT, true);
startActivity(mmsintent);
```

8. 发送彩信
```java
Uri uri = Uri.parse("content://media/external/images/media/23");    
Intent it = new Intent(Intent.ACTION_SEND);    
it.putExtra("sms_body", "some text");    
it.putExtra(Intent.EXTRA_STREAM, uri);    
it.setType("image/png");    
startActivity(it);
StringBuilder sb = new StringBuilder();
sb.append("file://");
sb.append(fd.getAbsoluteFile());
Intent intent = new Intent(Intent.ACTION_SENDTO, Uri.fromParts("mmsto", number, null));
// Below extra datas are all optional.
intent.putExtra(Messaging.KEY_ACTION_SENDTO_MESSAGE_SUBJECT, subject);
intent.putExtra(Messaging.KEY_ACTION_SENDTO_MESSAGE_BODY, body);
intent.putExtra(Messaging.KEY_ACTION_SENDTO_CONTENT_URI, sb.toString());
intent.putExtra(Messaging.KEY_ACTION_SENDTO_COMPOSE_MODE, composeMode);
intent.putExtra(Messaging.KEY_ACTION_SENDTO_EXIT_ON_SENT, exitOnSent);
startActivity(intent);
```

9. 发送Email 
```java
Uri uri = Uri.parse("mailto:xxx@abc.com");
Intent it = new Intent(Intent.ACTION_SENDTO, uri);
startActivity(it);
Intent it = new Intent(Intent.ACTION_SEND);
it.putExtra(Intent.EXTRA_EMAIL, "me@abc.com");
it.putExtra(Intent.EXTRA_TEXT, "The email body text");
it.setType("text/plain");
startActivity(Intent.createChooser(it, "Choose Email Client"));
Intent it=new Intent(Intent.ACTION_SEND);
String[] tos={"me@abc.com"};
String[] ccs={"you@abc.com"};
it.putExtra(Intent.EXTRA_EMAIL, tos);
it.putExtra(Intent.EXTRA_CC, ccs);
it.putExtra(Intent.EXTRA_TEXT, "The email body text");
it.putExtra(Intent.EXTRA_SUBJECT, "The email subject text");
it.setType("message/rfc822");
startActivity(Intent.createChooser(it, "Choose Email Client"));    
Intent it = new Intent(Intent.ACTION_SEND);    
it.putExtra(Intent.EXTRA_SUBJECT, "The email subject text");    
it.putExtra(Intent.EXTRA_STREAM, "file:///sdcard/mysong.mp3");    
sendIntent.setType("audio/mp3");    
startActivity(Intent.createChooser(it, "Choose Email Client"));
```

10. 播放多媒体
```java
Intent it = new Intent(Intent.ACTION_VIEW);
Uri uri = Uri.parse("file:///sdcard/song.mp3");
it.setDataAndType(uri, "audio/mp3");
startActivity(it);
Uri uri = Uri.withAppendedPath(MediaStore.Audio.Media.INTERNAL_CONTENT_URI, "1");
Intent it = new Intent(Intent.ACTION_VIEW, uri);
startActivity(it);
```

11. uninstall apk
```java
Uri uri = Uri.fromParts("package", strPackageName, null);    
Intent it = new Intent(Intent.ACTION_DELETE, uri);    
startActivity(it);
```

12. install apk
```java
Uri installUri = Uri.fromParts("package", "xxx", null);
returnIt = new Intent(Intent.ACTION_PACKAGE_ADDED, installUri);
```

13. 打开照相机
```java
<1>Intent i = new Intent(Intent.ACTION_CAMERA_BUTTON, null);
    this.sendBroadcast(i);
<2>long dateTaken = System.currentTimeMillis();
    String name = createName(dateTaken) + ".jpg";
    fileName = folder + name;
    ContentValues values = new ContentValues();
    values.put(Images.Media.TITLE, fileName);
    values.put("_data", fileName);
    values.put(Images.Media.PICASA_ID, fileName);
    values.put(Images.Media.DISPLAY_NAME, fileName);
    values.put(Images.Media.DESCRIPTION, fileName);
    values.put(Images.ImageColumns.BUCKET_DISPLAY_NAME, fileName);
    Uri photoUri = getContentResolver().insert(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, values);
    Intent inttPhoto = new Intent(MediaStore.ACTION_IMAGE_CAPTURE);
    inttPhoto.putExtra(MediaStore.EXTRA_OUTPUT, photoUri);
    startActivityForResult(inttPhoto, 10);
```

14. 从gallery选取图片
```java
Intent i = new Intent();
i.setType("image/*");
i.setAction(Intent.ACTION_GET_CONTENT);
startActivityForResult(i, 11);
```

15. 打开录音机
```java
Intent mi = new Intent(Media.RECORD_SOUND_ACTION);
startActivity(mi);
```

16. 显示应用详细列表
```java
Uri uri = Uri.parse("market://details?id=app_id");
Intent it = new Intent(Intent.ACTION_VIEW, uri);
startActivity(it);
//where app_id is the application ID, find the ID
//by clicking on your application on Market home
//page, and notice the ID from the address bar
Uri uri = Uri.parse("market://details?id=<packagename>");
```



17. 寻找应用
```java
Uri uri = Uri.parse("market://search?q=pname:pkg_name");         
Intent it = new Intent(Intent.ACTION_VIEW, uri);         
startActivity(it);
//where pkg_name is the full package path for an application       
```

18. 打开联系人列表
```java
    <1>            
    Intent i = new Intent();
    i.setAction(Intent.ACTION_GET_CONTENT);
    i.setType("vnd.android.cursor.item/phone");
    startActivityForResult(i, REQUEST_TEXT);

    <2>
    Uri uri = Uri.parse("content://contacts/people");
    Intent it = new Intent(Intent.ACTION_PICK, uri);
    startActivityForResult(it, REQUEST_TEXT);
```

19. 打开另一程序
```java
Intent i = new Intent();
ComponentName cn = new ComponentName("com.yellowbook.android2",
        "com.yellowbook.android2.AndroidSearch");
i.setComponent(cn);
i.setAction("android.intent.action.MAIN");
startActivityForResult(i, RESULT_OK);
```

20. 调用系统编辑添加联系人（高版本SDK有效）：
```java
Intent it = newIntent(Intent.ACTION_INSERT_OR_EDIT);
it.setType("vnd.android.cursor.item/contact");
//it.setType(Contacts.CONTENT_ITEM_TYPE);
it.putExtra("name","myName");
it.putExtra(android.provider.Contacts.Intents.Insert.COMPANY,  "organization");
it.putExtra(android.provider.Contacts.Intents.Insert.EMAIL,"email");
it.putExtra(android.provider.Contacts.Intents.Insert.PHONE,"homePhone");
it.putExtra(android.provider.Contacts.Intents.Insert.SECONDARY_PHONE,"mobilePhone");
it.putExtra(  android.provider.Contacts.Intents.Insert.TERTIARY_PHONE,"workPhone");
it.putExtra(android.provider.Contacts.Intents.Insert.JOB_TITLE,"title");
startActivity(it);
```

21. 调用系统编辑添加联系人（全有效）：
```java
Intent intent = newIntent(Intent.ACTION_INSERT_OR_EDIT);
intent.setType(People.CONTENT_ITEM_TYPE);
intent.putExtra(Contacts.Intents.Insert.NAME, "My Name");
intent.putExtra(Contacts.Intents.Insert.PHONE, "+1234567890");
intent.putExtra(Contacts.Intents.Insert.PHONE_TYPE,Contacts.PhonesColumns.TYPE_MOBILE);
intent.putExtra(Contacts.Intents.Insert.EMAIL, "com@com.com");
intent.putExtra(Contacts.Intents.Insert.EMAIL_TYPE,                    Contacts.ContactMethodsColumns.TYPE_WORK);
startActivity(intent);
```

我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786,647082990
**------------------------------------------------版权声明------------------------------------------------------**
 本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。