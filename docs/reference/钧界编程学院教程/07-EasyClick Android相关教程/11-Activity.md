<!-- 来源：https://www.laoguicom.top/doc/374/ · 抓取状态：ok -->

### 上节课内容

#### Activity



### Intent是什么

- 中文意思：意图

- 功能

  - 打开Activity

  - ```java
         //显示Intent
                    Intent intent = new Intent();
                    intent.setClassName(getPackageName(), SecondActivity.class.getName());
                    intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                    intent.putExtra("aaa", "122");
                    startActivity(intent);
    ```

    ```java
     //打开浏览器 隐式Intent
                    Intent intent = new Intent();
                    intent.setAction(Intent.ACTION_VIEW);
                    intent.setData(Uri.parse("http://baidu.com"));
                    intent.putExtra("aaa", "122");
    
                    intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
    
    
                    startActivity(intent);
    ```

    

  - 启动Service

  - 发送广播

  - 传递数据

```java
    package com.example.intentdemo;
    
    import android.os.BaseBundle;
    import android.os.Bundle;
    import android.os.Parcel;
    import android.util.Log;
    import android.widget.TextView;
    
    import androidx.annotation.Nullable;
    import androidx.appcompat.app.AppCompatActivity;
    
    import java.lang.reflect.Constructor;
    import java.lang.reflect.Field;
    
    public class SecondActivity extends AppCompatActivity {
        @Override
        protected void onCreate(@Nullable Bundle savedInstanceState) {
            super.onCreate(savedInstanceState);
            TextView textView = new TextView(this);
          //获取数据
            textView.setText("SecondActivity: " + getIntent().getStringExtra("aaa"));
            setContentView(textView);
    //        Bundle bundle = getIntent().getExtras();
    
    
    //
    //        for (String key : bundle.keySet()) {
    //            Log.i("Bundle Content", "Key=" + key + ", content=" + bundle.getString(key));
    //        }
    //        Log.e("SecondActivity", "onCreate: " + getIntent().getExtras().getString("aaa"));
    
        }
    }
    
```

  - 

- 种类

  - 显式Intent

    - 目标明确，系统只会查到找唯一的一个接收方

  - 隐式Intent

    - 目标比较模糊，系统会可能会筛选出一个或者多个接收方，如果是一个自动打开，如果是多个就让用户选择，也可以在安卓的系统设置中，设置一个默认的接收该intent的程序

      

### Service是什么

- 创建一个service，例如叫MyService，继承android.app.Service

- 在AndroidManifest.xml配置service组件

- ```xml
      <service
              android:name="com.example.intentdemo.MyService"
              android:enabled="true" />
  ```

- 使用Intent和startService方法启动Service

- ```java
  Intent intent = new Intent();
                  intent.setClassName(getPackageName(), MyService.class.getName());
                  startService(intent);
  ```

- 还有IntentService和JobService

#### 通知栏保活

- 为了让后台服务活的更久一些

- ```java
    private String createNotificationChannel(String channelID, String channelNAME, int level) {
          if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.O) {
              NotificationManager manager = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);
              NotificationChannel channel = new NotificationChannel(channelID, channelNAME, level);
              manager.createNotificationChannel(channel);
              return channelID;
          } else {
              return null;
          }
      }
  
      private void showNotification() {
          Intent intent = new Intent(this, MainActivity.class);
          intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
          PendingIntent pendingIntent = PendingIntent.getActivity(this, 0, intent, 0);
          String channelId = createNotificationChannel("my_channel_ID", "my_channel_NAME", NotificationManager.IMPORTANCE_HIGH);
          NotificationCompat.Builder notification = new NotificationCompat.Builder(this, channelId)
                  .setContentTitle("通知")
                  .setContentText("收到一条消息")
                  .setContentIntent(pendingIntent)
                  .setSmallIcon(R.mipmap.ic_launcher)
                  .setPriority(NotificationCompat.PRIORITY_HIGH)
                  .setAutoCancel(true);
          NotificationManagerCompat notificationManager = NotificationManagerCompat.from(this);
          //notificationManager.notify(100, notification.build());
          startForeground(10000, notification.build());
      }
  ```

- 



#### 生命周期图

![image-20210730215721424](images/image-20210730215721424.png)



#### onBinder进程间的通信

IPC = 进程间通信 

AIDL  

