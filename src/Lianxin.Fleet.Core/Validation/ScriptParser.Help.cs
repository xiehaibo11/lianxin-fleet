namespace Lianxin.Fleet.Core.Validation;

// 脚本编辑器里显示的命令速查；完整说明见 docs/SCRIPT_GUIDE.md。
public static partial class ScriptParser
{
    public const string Help = """
        每行一个命令，# 开头为注释；坐标是 0–1 的相对位置（左上角 0 0，右下角 1 1），时间单位为秒。
        点击 x y            双击 x y            长按 x y [秒]
        滑动 x1 y1 x2 y2 [秒]   上滑 / 下滑 / 左滑 / 右滑   返回
        输入 文字           剪贴板 文字         打开 https://example.com
        启动 BundleID       关闭 BundleID       等待 秒       随机等待 最短 最长
        主屏  锁屏  解锁  音量加  音量减  多任务
        日志 文字（显示在设备消息列）   截图 [名称]（存到数据目录 captures）
        """;

    public const string FlowHelp = """
        流程：重复 N … 结束    如果 条件 … [否则 …] 结束    当 条件 … 结束（最多 10000 次）
              跳出（离开最近的循环）   停止（正常结束）   失败 原因（以失败结束）
        条件：存在 选择器   不存在 选择器   比色 x y #RRGGBB [容差]
              找到颜色 #RRGGBB [容差] [区域 x1 y1 x2 y2]   找到图片 名称 [相似度]   前面加「非」取反
        元素：点击元素 选择器   等待元素 选择器 [超时]   等待消失 选择器 [超时]   输入到 选择器 文字
        选择器：文字=登录  包含=登录  id=标识  类型=Button  xpath=//…  谓词="label BEGINSWITH '登'"
              值含空格时加英文双引号：文字="立即 登录"
        图色：点击颜色 #RRGGBB [容差] [区域 x1 y1 x2 y2]   点击图片 名称 [相似度]   等待图片 名称 [超时] [相似度]
              模板是数据目录 templates 下的 PNG，写名称不带扩展名；容差默认 16，相似度默认 0.9，超时默认 10 秒
        英文命令同样可用：tap swipe text launch wait home … repeat if else while end break stop fail
              tapelement waitelement waitgone inputto tapcolor tapimage waitimage randomwait log capture
        """;
}
