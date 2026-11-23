using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Checks.Scenarios;

// 脚本 v2 语法：块结构、嵌套上限、行号报错，以及每个新命令和条件的解析结果。
internal static partial class ScriptChecks
{
    public static Task Blocks()
    {
        var steps = ScriptParser.Parse("""
            重复 3
                点击 0.1 0.2
                如果 存在 文字=继续
                    点击元素 文字=继续
                否则
                    跳出
                结束
            结束
            当 非 找到图片 完成 0.8
                上滑
                等待 1
            结束
            repeat 2
              if notexists id=done
                break
              end
            end
            停止
            """);
        Assert.Equal(4, steps.Count);
        Assert.Equal(12, ScriptParser.CountSteps(steps));
        var repeat = steps[0];
        Assert.True(repeat.Kind == ScriptStepKind.Repeat && repeat.Count == 3 && repeat.Body.Count == 2 && repeat.Line == 1, "repeat block");
        var branch = repeat.Body[1];
        Assert.True(branch.Kind == ScriptStepKind.If && branch.Body.Count == 1 && branch.ElseBody.Count == 1, "if/else bodies");
        Assert.Equal(ScriptStepKind.Break, branch.ElseBody[0].Kind);
        Assert.Equal(new ElementSelector(ElementSelectorKind.Text, "继续"), branch.Condition!.Probe.Selector);
        Assert.True(branch.Body[0].Kind == ScriptStepKind.TapProbe && branch.Body[0].Line == 4, "nested line numbers");
        var loop = steps[1];
        Assert.True(loop.Kind == ScriptStepKind.While && loop.Condition!.Negate && loop.Condition.Probe.Kind == ScriptProbeKind.Image
            && loop.Condition.Probe.Template == "完成" && loop.Condition.Probe.Similarity == 0.8 && loop.Body.Count == 2, "while 非 找到图片");
        Assert.True(steps[2].Body[0].Condition!.Negate && steps[2].Body[0].Condition!.Probe.Selector!.Kind == ElementSelectorKind.Id, "english blocks");
        Assert.Equal(ScriptStepKind.Stop, steps[3].Kind);

        var deep = string.Join('\n', Enumerable.Repeat("重复 2", ScriptParser.MaxDepth)) + "\n主屏\n"
            + string.Join('\n', Enumerable.Repeat("结束", ScriptParser.MaxDepth));
        Assert.Equal(ScriptParser.MaxDepth + 1, ScriptParser.CountSteps(ScriptParser.Parse(deep)));
        Expect("重复 2\n" + deep + "\n结束", $"第 {ScriptParser.MaxDepth + 1} 行：块最多嵌套");
        Expect("主屏\n重复 3\n点击 0.5 0.5", "第 2 行的「重复」缺少对应的「结束」");
        Expect("如果 存在 文字=a\n重复 2\n主屏\n结束", "第 1 行的「如果」缺少");
        Expect("主屏\n结束", "第 2 行：多余的「结束」");
        Expect("重复 2\n否则\n结束", "第 2 行：「否则」前面没有对应的「如果」");
        Expect("如果 存在 id=a\n否则\n否则\n结束", "第 3 行：一个「如果」只能有一个「否则」");
        Expect("如果 存在 id=a\n跳出\n结束", "第 2 行：「跳出」只能写在");
        Expect("重复 0\n结束", "重复次数须为 1 至");
        Expect("重复 两次\n结束", "第 1 行：重复次数");
        Expect("重复 2\n主屏\n结束 重复", "「结束」后面不能有其他内容");
        Expect("如果\n结束", "缺少条件");
        Expect("如果 出现 文字=a\n结束", "无法识别的条件「出现」");
        Expect("当 存在 文字=立即 登录\n结束", "值含空格时请用英文双引号");

        // docs/SCRIPT_GUIDE.md 里的示例必须能通过语法检查。
        foreach (var example in new[]
        {
            "重复 50\n    如果 存在 包含=已到底\n        停止\n    结束\n    如果 存在 文字=直播\n        上滑\n    否则\n        随机等待 3 8\n        上滑\n    结束\n结束",
            "启动 com.apple.Preferences\n等待元素 文字=通用 5\n点击元素 文字=通用\n等待元素 文字=关于本机",
            "等待图片 首页 15\n重复 3\n    点击图片 签到 0.85\n    等待 1\n    如果 比色 0.5 0.82 #C8C8C8 10\n        日志 已签到\n        停止\n    结束\n结束\n失败 签到按钮没有变灰"
        })
            ScriptParser.Parse(example);
        return Task.CompletedTask;
    }

    public static Task Commands()
    {
        var steps = ScriptParser.Parse("""
            点击元素 文字="立即 登录"
            等待元素 包含=欢迎 5
            等待消失 类型=activityIndicator
            输入到 id=user.name 张 三
            点击元素 xpath=//XCUIElementTypeButton[@name='OK']
            点击元素 谓词="label BEGINSWITH '设' AND visible == 1"
            点击元素 文字="引号\"与\\斜杠"
            点击颜色 #FF8800
            点击颜色 0x00ff00 30 区域 0 0.5 1 1
            点击图片 登录按钮.png 0.85
            等待图片 首页
            等待图片 首页 20 0.95
            随机等待 1 2.5
            日志 正在刷视频 第 1 个
            截图
            截图 首页
            失败
            失败 账号被封
            如果 比色 0.5 0.5 #FFFFFF 8
            结束
            if hascolor #123456 region 0.1 0.1 0.2 0.2
            end
            tapelement text=OK
            waitelement type=Button
            waitgone contains=Loading 1
            inputto text=搜索 hello
            tapcolor #000000
            tapimage icon
            waitimage icon 3
            randomwait 0.05 0.05
            log hi
            capture shot
            stop
            fail done
            """);
        Assert.Equal(32, steps.Count);
        ElementSelector Selector(int index) => steps[index].Probe!.Selector!;
        Assert.Equal(new ElementSelector(ElementSelectorKind.Text, "立即 登录"), Selector(0));
        Assert.True(steps[1].Kind == ScriptStepKind.WaitProbe && steps[1].TimeoutSeconds == 5 && Selector(1).Kind == ElementSelectorKind.Contains, "wait element");
        Assert.True(steps[2].Kind == ScriptStepKind.WaitProbeGone && steps[2].TimeoutSeconds == ScriptParser.DefaultTimeout
            && Selector(2).Value == "XCUIElementTypeActivityIndicator", "wait gone + type prefix");
        Assert.True(steps[3].Kind == ScriptStepKind.InputToElement && Selector(3).Value == "user.name" && steps[3].Text == "张 三", "input to");
        Assert.Equal("//XCUIElementTypeButton[@name='OK']", Selector(4).Value);
        Assert.Equal("label BEGINSWITH '设' AND visible == 1", Selector(5).Value);
        Assert.Equal("引号\"与\\斜杠", Selector(6).Value);
        var color = steps[7].Probe!;
        Assert.True(color.Kind == ScriptProbeKind.ColorSearch && color.Rgb == 0xFF8800 && color.Tolerance == ScriptParser.DefaultTolerance
            && color.Region == ScreenRegion.Full, "tap color defaults");
        Assert.True(steps[8].Probe!.Rgb == 0x00FF00 && steps[8].Probe!.Tolerance == 30 && steps[8].Probe!.Region == new ScreenRegion(0, .5, 1, 1), "tap color region");
        Assert.True(steps[9].Probe!.Template == "登录按钮" && steps[9].Probe!.Similarity == 0.85, "tap image");
        Assert.True(steps[10].TimeoutSeconds == ScriptParser.DefaultTimeout && steps[10].Probe!.Similarity == ScriptParser.DefaultSimilarity, "wait image defaults");
        Assert.True(steps[11].TimeoutSeconds == 20 && steps[11].Probe!.Similarity == 0.95, "wait image options");
        Assert.True(steps[12].Kind == ScriptStepKind.RandomWait && steps[12].WaitSeconds == 1 && steps[12].MaxWaitSeconds == 2.5, "random wait");
        Assert.Equal("正在刷视频 第 1 个", steps[13].Text);
        Assert.True(steps[14].Kind == ScriptStepKind.Capture && steps[14].Text is null && steps[15].Text == "首页", "capture");
        Assert.True(steps[16].Kind == ScriptStepKind.Fail && steps[16].Text is null && steps[17].Text == "账号被封", "fail");
        var compare = steps[18].Condition!.Probe;
        Assert.True(compare.Kind == ScriptProbeKind.ColorAt && compare.X == .5 && compare.Rgb == 0xFFFFFF && compare.Tolerance == 8, "比色");
        Assert.Equal(new ScreenRegion(.1, .1, .2, .2), steps[19].Condition!.Probe.Region);
        Assert.True(steps.Skip(20).Take(10).All(step => step.Kind is ScriptStepKind.TapProbe or ScriptStepKind.WaitProbe
            or ScriptStepKind.WaitProbeGone or ScriptStepKind.InputToElement or ScriptStepKind.RandomWait or ScriptStepKind.Log
            or ScriptStepKind.Capture), "english aliases");
        Assert.True(steps[30].Kind == ScriptStepKind.Stop && steps[31].Text == "done", "english stop/fail");

        Expect("点击元素 登录", "选择器格式为 键=值");
        Expect("点击元素 名字=登录", "选择器格式为");
        Expect("点击元素 文字=", "选择器的值不能为空");
        Expect("点击元素 文字=\"没闭合", "双引号没有闭合");
        Expect("点击元素 类型=Button1", "元素类型只能是英文字母");
        Expect("点击元素 id=" + new string('a', ScriptParser.MaxSelectorLength + 1), "最多 500 个字符");
        Expect("等待元素 文字=a 0.1", "超时时间须为");
        Expect("输入到 文字=a", "缺少要输入的文字");
        Expect("点击颜色 #FF88", "不是有效颜色");
        Expect("点击颜色 red", "不是有效颜色");
        Expect("点击颜色 #FF8800 300", "容差须为 0 至 255");
        Expect("点击颜色 #FF8800 区域 0 0 1", "区域格式为");
        Expect("点击颜色 #FF8800 区域 0.5 0 0.4 1", "左上角须在右下角");
        Expect("点击图片 ../secret", "图片名称只能包含");
        Expect("点击图片 a/b", "图片名称只能包含");
        Expect("点击图片 a\\b", "图片名称只能包含");
        Expect("点击图片 按钮 0.3", "相似度须为 0.5 至 1");
        Expect("等待图片 按钮 5 0.9 1", "格式为：等待图片");
        Expect("随机等待 3 1", "最短时间不能大于最长时间");
        Expect("随机等待 1", "参数个数应为 2");
        Expect("日志", "缺少内容");
        Expect("截图 a b", "截图名称不能包含空格");
        Expect("截图 ..", "图片名称只能包含");
        Expect("停止 现在", "「停止」后面不能有其他内容");
        Expect("如果 比色 0.5 2 #FFFFFF\n结束", "屏幕坐标须在 0 到 1 之间");

        // 选择器写回脚本后能原样读回，包括空格、引号和反斜杠。
        foreach (var selector in new[]
        {
            new ElementSelector(ElementSelectorKind.Text, "立即 登录"),
            new ElementSelector(ElementSelectorKind.Id, "\"quoted\" \\ path"),
            new ElementSelector(ElementSelectorKind.Type, "XCUIElementTypeStaticText"),
            new ElementSelector(ElementSelectorKind.Predicate, "name == 'a b'")
        })
        {
            var text = ScriptParser.FormatSelector(selector);
            Assert.Equal(selector, ScriptParser.Parse("点击元素 " + text)[0].Probe!.Selector);
        }
        Assert.Equal("类型=Button", ScriptParser.FormatSelector(new ElementSelector(ElementSelectorKind.Type, "XCUIElementTypeButton")));
        return Task.CompletedTask;
    }
}
