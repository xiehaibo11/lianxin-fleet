<!-- 来源：https://www.laoguicom.top/doc/52/ · 抓取状态：ok -->

# adb shell之am、pm、dpm命令大全

## 调用 Activity 管理器 (am)
在 adb shell 中，您可以使用 Activity 管理器 (am) 工具发出命令以执行各种系统操作，如启动 Activity、强行停止进程、广播 intent、修改设备屏幕属性，等等。在 shell 中，相应的语法为：

## am command
您也可以直接从 adb 发出 Activity 管理器命令，无需进入远程 shell。例如：

```shell
adb shell am start -a android.intent.action.VIEW
```
表 2. 可用的 Activity 管理器命令


| 命令 | 说明 |
|:---: |:---:  |
| start [options] intent | 启动由 intent 指定的 Activity。  请参阅 intent 参数的规范。  具体选项包括：  -D：启用调试功能。 -W：等待启动完成。 --start-profiler file：启动性能剖析器并将结果发送至 file。 -P file：类似于 --start-profiler，但当应用进入空闲状态时剖析停止。 -R count：重复启动 Activity count 次。在每次重复前，将完成顶层 Activity。 -S：在启动 Activity 前，强行停止目标应用。 --opengl-trace：启用 OpenGL 函数的跟踪。 --user user_id&#124;current：指定要作为哪个用户运行；如果未指定，则作为当前用户运行。|
| startservice [options] intent | 启动由 intent 指定的 Service。  请参阅 intent 参数的规范。  具体选项包括：  --user user_id &#124; current：指定要作为哪个用户运行；如果未指定，则作为当前用户运行。 |
| force-stop package | 强行停止与 package（应用的软件包名称）关联的所有进程。 |
| kill [options] package | 终止与 package（应用的软件包名称）关联的所有进程。此命令仅终止可安全终止且不会影响用户体验的进程。  具体选项包括：  --user user_id&#124;all &#124; current：指定要终止哪个用户的进程；如果未指定，则终止所有用户的进程。 |
| kill-all | 终止所有后台进程。 |
| broadcast [options] intent | 发出广播 intent。  请参阅 intent 参数的规范。  具体选项包括：  [--user user_id &#124; all&#124; current]：指定要发送给哪个用户；如果未指定，则发送给所有用户。 |
| instrument [options] component | 使用 Instrumentation 实例启动监控。通常情况下，目标 component 采用 test_package/runner_class 格式。  具体选项包括：  -r：输出原始结果（否则，对 report_key_streamresult 进行解码）。与 [-e perf true] 结合使用可生成性能测量的原始输出。 -e name value：将参数 name 设为 value。 对于测试运行程序，通用格式为 -e testrunner_flag value[,value...]。 -p file：将剖析数据写入 file。 -w：等待插桩完成后再返回。测试运行程序需要使用此选项。 --no-window-animation：运行时关闭窗口动画。 --user user_id &#124; current：指定以哪个用户身份运行插桩；如果未指定，则以当前用户身份运行。 |
| profile start process file | 启动 process 的性能剖析器，将结果写入 file。 |
| profile stop process | 停止 process 的性能剖析器。 |
| dumpheap [options] process file | 转储 process 的堆，写入 file。  具体选项包括：  --user [user_id &#124; current]：提供进程名称时，指定要转储的进程的用户；如果未指定，则使用当前用户。 -n：转储原生堆，而非托管堆。 |
| set-debug-app [options] package | 设置要调试的应用 package。  具体选项包括：  -w：应用启动时等待调试程序。 --persistent：保留此值。 |
| clear-debug-app | 清除之前使用 set-debug-app 设置的待调试软件包。 |
| monitor [options] | 开始监控崩溃或 ANR。  具体选项包括：  --gdb：在崩溃/ANR 时，在给定的端口上启动 gdbserv。 |
| screen-compat {on&#124; off} package | 控制 package 的屏幕兼容性模式。 |
| display-size [reset&#124; widthxheight] | 替换设备显示尺寸。此命令支持使用大屏设备模仿小屏幕分辨率（反之亦然），对于在不同尺寸的屏幕上测试应用非常有用。示例：  am display-size 1280x800 |
| display-density dpi | 替换设备显示密度。此命令支持使用低密度屏幕在高密度屏幕环境上进行测试（反之亦然），对于在不同密度的屏幕上测试应用非常有用。示例：  am display-density 480 |
| to-uri intent | 以 URI 的形式输出给定的 intent 规范。请参阅 intent 参数的规范。 |
| to-intent-uri intent | 以 intent: URI 的形式输出给定的 intent 规范。请参阅 intent 参数的规范。 |




## intent 参数的规范
对于采用 intent 参数的 Activity 管理器命令，您可以使用以下选项指定 intent：
全部显示
### -a action
指定 intent 操作，如 android.intent.action.VIEW。只能声明一次。
### -d data_uri
指定 intent 数据 URI，如` content://contacts/people/1`。只能声明一次。
### -t mime_type
指定 intent MIME 类型，如 image/png。只能声明一次。
### -c category
指定 intent 类别，如` android.intent.category.APP_CONTACTS。`
### -n component
指定带有软件包名称前缀的组件名称以创建显式 intent，如 `com.example.app/.ExampleActivity`。
### -f flags
向 `setFlags() `支持的 intent 添加标记。
### --esn extra_key
添加一个空 extra。URI intent 不支持此选项。
### -e | --es extra_key extra_string_value
以键值对的形式添加字符串数据。
### --ez extra_key extra_boolean_value
以键值对的形式添加布尔值数据。
### --ei extra_key extra_int_value
以键值对的形式添加整数型数据。
### --el extra_key extra_long_value
以键值对的形式添加长整型数据。
### --ef extra_key extra_float_value
以键值对的形式添加浮点型数据。
### --eu extra_key extra_uri_value
以键值对的形式添加 URI 数据。
### --ecn extra_key extra_component_name_value
添加组件名称，该名称作为 ComponentName 对象进行转换和传递。
### --eia extra_key extra_int_value[,extra_int_value...]
添加整数数组。
### --ela extra_key extra_long_value[,extra_long_value...]
添加长整数数组。
### --efa extra_key extra_float_value[,extra_float_value...]
添加浮点数数组。
### --grant-read-uri-permission
添加 FLAG_GRANT_READ_URI_PERMISSION 标记。
### --grant-write-uri-permission
添加 FLAG_GRANT_WRITE_URI_PERMISSION 标记。
### --debug-log-resolution
添加 FLAG_DEBUG_LOG_RESOLUTION 标记。
### --exclude-stopped-packages
添加 FLAG_EXCLUDE_STOPPED_PACKAGES 标记。
### --include-stopped-packages
添加 FLAG_INCLUDE_STOPPED_PACKAGES 标记。
### --activity-brought-to-front
添加 FLAG_ACTIVITY_BROUGHT_TO_FRONT 标记。
### --activity-clear-top
添加 FLAG_ACTIVITY_CLEAR_TOP 标记。
### --activity-clear-when-task-reset
添加 FLAG_ACTIVITY_CLEAR_WHEN_TASK_RESET 标记。
### --activity-exclude-from-recents
添加 FLAG_ACTIVITY_EXCLUDE_FROM_RECENTS 标记。
### --activity-launched-from-history
添加 FLAG_ACTIVITY_LAUNCHED_FROM_HISTORY 标记。
### --activity-multiple-task
添加 FLAG_ACTIVITY_MULTIPLE_TASK 标记。
### --activity-no-animation
添加 FLAG_ACTIVITY_NO_ANIMATION 标记。
### --activity-no-history
添加 FLAG_ACTIVITY_NO_HISTORY 标记。
### --activity-no-user-action
添加 FLAG_ACTIVITY_NO_USER_ACTION 标记。
### --activity-previous-is-top
添加 FLAG_ACTIVITY_PREVIOUS_IS_TOP 标记。
### --activity-reorder-to-front
添加 FLAG_ACTIVITY_REORDER_TO_FRONT 标记。
### --activity-reset-task-if-needed
添加 FLAG_ACTIVITY_RESET_TASK_IF_NEEDED 标记。
### --activity-single-top
添加 FLAG_ACTIVITY_SINGLE_TOP 标记。
### --activity-clear-task
添加 FLAG_ACTIVITY_CLEAR_TASK 标记。
### --activity-task-on-home
添加 FLAG_ACTIVITY_TASK_ON_HOME 标记。
### --receiver-registered-only
添加 FLAG_RECEIVER_REGISTERED_ONLY 标记。
### --receiver-replace-pending
添加 FLAG_RECEIVER_REPLACE_PENDING 标记。
### --selector
需要使用 -d 和 -t 选项设置 intent 数据和类型。
### URI component package
如果不受上述任一选项的限制，您可以直接指定 URI、软件包名称和组件名称。当某个参数不受限制时，如果该参数包含“:”（冒号），那么该工具会假定参数是 URI；如果该参数包含“/”（正斜线），那么该工具会假定参数是组件名称；如果并非上述两种情况，那么该工具会假定参数是软件包名称。
## 调用软件包管理器 (pm)
在 adb shell 中，您可以使用软件包管理器 (pm) 工具发出命令，以对设备上安装的应用软件包执行操作和查询。在 shell 中，相应的语法为：

### pm command
您也可以直接从 adb 发出软件包管理器命令，无需进入远程 shell。例如：

```shell
adb shell pm uninstall com.example.MyApp
```
表 3. 可用的软件包管理器命令。


| 命令   | 说明    |
|:----------------------------:| :------------------------------------------------:|
| list packages [options] filter | 输出所有软件包，或者，仅输出软件包名称包含 filter 中的文本的软件包。  具体选项：  -f：查看它们的关联文件。 -d：进行过滤以仅显示已停用的软件包。 -e：进行过滤以仅显示已启用的软件包。 -s：进行过滤以仅显示系统软件包。 -3：进行过滤以仅显示第三方软件包。 -i：查看软件包的安装程序。 -u：也包括已卸载的软件包。 --user user_id：要查询的用户空间。 |
| list permission-groups | 输出所有已知的权限组。 |
| list permissions [options] group | 输出所有已知的权限，或者，仅输出 group 中的权限。  具体选项：  -g：按组进行整理。 -f：输出所有信息。 -s：简短摘要。 -d：仅列出危险权限。 -u：仅列出用户将看到的权限。 |
| list instrumentation [options] | 列出所有测试软件包。  具体选项：  -f：列出测试软件包的 APK 文件。 target_package：仅列出此应用的测试软件包。 |
| list features | 输出系统的所有功能。 |
| list libraries   | 输出当前设备支持的所有库。 |
| list users | 输出系统中的所有用户。 |
| path package  | 输出给定 package 的 APK 的路径。 |
| install [options] path  | 将软件包（通过 path 指定）安装到系统。  具体选项：  -r：重新安装现有应用，并保留其数据。 -t：允许安装测试 APK。仅当您运行或调试了应用或者使用了 Android Studio 的 Build > Build APK 命令时，Gradle 才会生成测试 APK。如果是使用开发者预览版 SDK（如果 targetSdkVersion 是字母，而非数字）构建的 APK，那么安装测试 APK 时必须在 install 命令中包含 -t 选项。 -i installer_package_name：指定安装程序软件包名称。 --install-location location：使用以下某个值设置安装位置： 0：使用默认安装位置。 1：在内部设备存储上安装。 2：在外部介质上安装。 -f：在内部系统内存上安装软件包。 -d：允许版本代码降级。 -g：授予应用清单中列出的所有权限。 --fastdeploy：通过仅更新已更改的 APK 部分来快速更新安装的软件包。 --incremental：仅安装 APK 中启动应用所需的部分，同时在后台流式传输剩余数据。如要使用此功能，您必须为 APK 签名，创建一个 APK 签名方案 v4 文件，并将此文件放在 APK 所在的目录中。只有部分设备支持此功能。此选项会强制 adb 使用该功能，如果该功能不受支持，则会失败（并提供有关失败原因的详细信息）。附加 --wait 选项，可等到 APK 完全安装完毕后再授予对 APK 的访问权限。--no-incremental 可阻止 adb 使用此功能。 |
| uninstall [options] package  | 从系统中移除软件包。  具体选项：  -k：移除软件包后保留数据和缓存目录。  |
| clear package   | 删除与软件包关联的所有数据。 |
| enable package_or_component    | 启用给定的软件包或组件（写为“package/class”）。 |
| disable package_or_component    | 停用给定的软件包或组件（写为“package/class”）。  |
| disable-user [options] package_or_component       | 具体选项：  --user user_id：要停用的用户。 |
| grant package_name permission     | 向应用授予权限。在搭载 Android 6.0（API 级别 23）及更高版本的设备上，该权限可以是应用清单中声明的任何权限。在搭载 Android 5.1（API 级别 22）及更低版本的设备上，该权限必须是应用定义的可选权限。 |
| revoke package_name permission  | 从应用撤消权限。在搭载 Android 6.0（API 级别 23）及更高版本的设备上，该权限可以是应用清单中声明的任何权限。在搭载 Android 5.1（API 级别 22）及更低版本的设备上，该权限必须是应用定义的可选权限。 |
| set-install-location location   | 更改默认安装位置。位置值如下：  0：自动：让系统决定最合适的位置。 1：内部：在内部设备存储上安装。 2：外部：在外部介质上安装。  注意：此命令仅用于调试目的；使用此命令可能会导致应用中断和其他意外行为。   |
| get-install-location | 返回当前安装位置。返回值如下：  0 [auto]：让系统决定最合适的位置 1 [internal]：在内部设备存储上安装 2 [external]：在外部介质上安装 |
| set-permission-enforced permission [true &#124; false] | 指定是否应强制执行指定权限。 |
| trim-caches desired_free_space  | 减少缓存文件以达到给定的可用空间。 |
| create-user user_name | 创建具有给定 user_name 的新用户，从而输出该用户的新用户标识符。 |
| remove-user user_id | 移除具有给定 user_id 的用户，从而删除与该用户关联的所有数据。 |
| get-max-users  | 输出设备支持的最大用户数。 |


## 调用设备政策管理器 (dpm)
为便于您开发和测试设备管理（或其他企业）应用，您可以向设备政策管理器 (dpm) 工具发出命令。使用该工具可控制活动管理应用，或更改设备上的政策状态数据。在 shell 中，语法如下：

### dpm command
您也可以直接从 adb 发出设备政策管理器命令，无需进入远程 shell：

```shell
adb shell dpm command
```
表 4. 可用的设备政策管理器命令


|      命令                  |              说明                 |
|:------------------------------------------------:|:--------------------------------------------------------------------------------------:|
| set-active-admin [options] component    | 将 component 设为活动管理。  具体选项包括：  --user user_id：指定目标用户。您也可以传递 --user current 以选择当前用户。                                                         |
| set-profile-owner [options] component   | 将 component 设为活动管理，并将其软件包设为现有用户的个人资料所有者。  具体选项包括：  --user user_id：指定目标用户。您也可以传递 --user current 以选择当前用户。 --name name：指定简单易懂的组织名称。           |
| set-device-owner [options] component    | 将 component 设为活动管理，并将其软件包设为设备所有者。  具体选项包括：  --user user_id：指定目标用户。您也可以传递 --user current 以选择当前用户。 --name name：指定简单易懂的组织名称。                  |
| remove-active-admin [options] component | 停用活动管理。应用必须在清单中声明 android:testOnly。此命令还会移除设备所有者和个人资料所有者。  具体选项包括：  --user user_id：指定目标用户。您也可以传递 --user current 以选择当前用户。                    |
| clear-freeze-period-record              | 清除设备之前设置的系统 OTA 更新冻结期记录。在开发管理冻结期的应用时，这有助于避免设备存在调度方面的限制。请参阅管理系统更新。在搭载 Android 9.0（API 级别 28）及更高版本的设备上受支持。                                   |
| force-network-logs                      | 强制系统让任何现有网络日志随时可供 DPC 检索。如果有可用的连接或 DNS 日志，DPC 会收到 onNetworkLogsAvailable() 回调。请参阅网络活动日志。此命令有调用频率限制。在搭载 Android 9.0（API 级别 28）及更高版本的设备上受支持。 |
| force-security-logs                     | 强制系统向 DPC 提供任何现有安全日志。如果有可用的日志，DPC 会收到 onSecurityLogsAvailable() 回调。请参阅记录企业设备活动。此命令有调用频率限制。在搭载 Android 9.0（API 级别 28）及更高版本的设备上受支持。          |

## 截取屏幕截图
### screencap 命令是一个用于对设备显示屏截取屏幕截图的 shell 实用程序。在 shell 中，
语法如下：

```shell
screencap filename
```
如需从命令行使用 screencap，请输入以下命令：

```shell
adb shell screencap /sdcard/screen.png
```
以下屏幕截图会话示例展示了如何使用 adb shell 截取屏幕截图，以及如何使用 pull 命令从设备下载屏幕截图文件：


```shell
$ adb shell
shell@ $ screencap /sdcard/screen.png
shell@ $ exit
$ adb pull /sdcard/screen.png
```
## 录制视频
### screenrecord 命令是一个用于录制设备（搭载 Android 4.4（API 级别 19）及更高版本）显示屏的 shell 实用程序。
该实用程序将屏幕 Activity 录制为 MPEG-4 文件。您可以使用此文件创建宣传视频或培训视频，或将其用于调试或测试。
在 shell 中，使用以下语法：
```shell
screenrecord [options] filename
```
如需从命令行使用 screenrecord，请输入以下命令：

```shell
adb shell screenrecord /sdcard/demo.mp4
```
按 Ctrl + C 键（在 Mac 上，按 Command + C 键）可停止屏幕录制；如果不手动停止，到三分钟或 --time-limit 设置的时间限制时，录制将会自动停止。
如需开始录制设备屏幕，请运行 screenrecord 命令以录制视频。然后，运行 pull 命令以将视频从设备下载到主机。下面是一个录制会话示例：
```shell
$ adb shell
shell@ $ screenrecord --verbose /sdcard/demo.mp4
(press Control + C to stop)
shell@ $ exit
$ adb pull /sdcard/demo.mp4
```
screenrecord 实用程序能以您要求的任何支持的分辨率和比特率进行录制，同时保持设备显示屏的宽高比。默认情况下，该实用程序以本机显示分辨率和屏幕方向进行录制，时长不超过三分钟。
screenrecord 实用程序的局限性：
- 音频不与视频文件一起录制。
- 无法在搭载 Wear OS 的设备上录制视频。
- 某些设备可能无法以它们的本机显示分辨率进行录制。如果在录制屏幕时出现问题，请尝试使用较低的屏幕分辨率。
- 不支持在录制时旋转屏幕。如果在录制期间屏幕发生了旋转，则部分屏幕内容在录制时将被切断。

表 5. screenrecord 选项


| 选项                  | 说明                                                                                                                                       |
|:---------------------:|:------------------------------------------------------------------------------------------------------------------------------------------:|
| --help              | 显示命令语法和选项                                                                                                                                |
| --size widthxheight | 设置视频大小：1280x720。默认值为设备的本机显示屏分辨率（如果支持）；如果不支持，则为 1280x720。为获得最佳效果，请使用设备的 Advanced Video Coding (AVC) 编码器支持的大小。                             |
| --bit-rate rate     | 设置视频的视频比特率（以 MB/秒为单位）。默认值为 4Mbps。您可以增加比特率以提升视频品质，但这样做会导致视频文件变大。下面的示例将录制比特率设为 6Mbps：     screenrecord --bit-rate 6000000 /sdcard/demo.mp4 |
| --time-limit time   | 设置最大录制时长（以秒为单位）。默认值和最大值均为 180（3 分钟）。                                                                                                     |
| --rotate            | 将输出旋转 90 度。此功能处于实验阶段。                                                                                                                    |
| --verbose           | 在命令行屏幕显示日志信息。如果您不设置此选项，则该实用程序在运行时不会显示任何信息。                                                                                               |

## 读取应用的 ART 配置文件
从 Android 7.0（API 级别 24）开始，Android Runtime (ART) 会收集已安装应用的执行配置文件，这些配置文件用于优化应用性能。您可能需要检查收集的配置文件，以了解在应用启动期间，系统频繁执行了哪些方法和使用了哪些类。
要生成文本格式的配置文件信息，请使用以下命令：

```shell
adb shell cmd package dump-profiles package
```
要检索生成的文件，请使用：

```shell
adb pull /data/misc/profman/package.txt
```
## 重置测试设备
如果您在多个测试设备上测试应用，则在两次测试之间重置设备可能很有用，例如，可以移除用户数据并重置测试环境。您可以使用 testharness adb shell 命令对搭载 Android 10（API 级别 29）或更高版本的测试设备执行恢复出厂设置，如下所示。

```shell
adb shell cmd testharness enable
```
使用 testharness 恢复设备时，设备会自动将允许通过当前工作站调试设备的 RSA 密钥备份在一个持久性位置。也就是说，在重置设备后，工作站可以继续调试设备并向设备发出 adb 命令，而无需手动注册新密钥。
此外，为了帮助您更轻松且更安全地继续测试您的应用，使用 testharness 恢复设备还会更改以下设备设置：
- 设备会设置某些系统设置，以便不会出现初始设备设置向导。也就是说，设备会进入一种状态，供您快速安装、调试和测试您的应用。
- 设置：
 - 停用锁定屏幕
 - 停用紧急提醒
 - 停用帐户自动同步
 - 停用自动系统更新
- 其他：
 - 停用预装的安全应用

如果您的应用需要检测并适应 testharness 命令的默认设置，您可以使用 `ActivityManager.isRunningInUserTestHarness()`。
## sqlite
sqlite3 可启动用于检查 sqlite 数据库的 sqlite 命令行程序。它包含用于输出表格内容的 .dump 以及用于输出现有表格的 SQL CREATE 语句的 .schema 等命令。您也可以从命令行执行 SQLite 命令，如下所示。

```shell
$ adb -s emulator-5554 shell
$ sqlite3 /data/data/com.example.app/databases/rssitems.db
SQLite version 3.3.12
Enter ".help" for instructions
```
 
 
我是Mr-老鬼、QQ1156346325 。交流QQ群：620028786,647082990
**------------------------------------------------版权声明------------------------------------------------------**
 本文版权所有~Mr-老鬼 ~转载请注明原文地址
免责声明：本文所有的教程仅限交流学习使用不得用于违法用途，造成的法律后果本人不承担责任。