using System.Diagnostics;
using System.Globalization;
using Lianxin.Fleet.Checks.Infrastructure;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Imaging;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;

namespace Lianxin.Fleet.Checks.Scenarios;

// 模板找图的正确性与耗时，以及脚本里的图色命令（假截图，不连真机）。
internal static partial class ImagingChecks
{
    public static Task Templates()
    {
        var screen = Screen(750, 1334, 42);
        var template = ImageTemplate.From(screen.Crop(412, 903, 64, 64));
        var cold = Stopwatch.StartNew();
        var found = TemplateMatcher.Find(screen, template, 0.9);
        cold.Stop();
        var warm = Stopwatch.StartNew();
        found = TemplateMatcher.Find(screen, template, 0.9);
        warm.Stop();
        Assert.True(found is { X: 412, Y: 903, Score: > 0.99 }, $"64×64 template: {found}");
        Assert.True(cold.ElapsedMilliseconds < 1000 && warm.ElapsedMilliseconds < 1000,
            $"750×1334 search took {cold.ElapsedMilliseconds} ms cold, {warm.ElapsedMilliseconds} ms warm");

        foreach (var (x, y, w, h) in new[] { (5, 7, 16, 16), (530, 1200, 200, 120), (300, 40, 33, 90) })
        {
            var match = TemplateMatcher.Find(screen, ImageTemplate.From(screen.Crop(x, y, w, h)), 0.9);
            Assert.True(match is { } m && m.X == x && m.Y == y && m.Width == w, $"{w}×{h} template at {x},{y}: {match}");
        }

        // 亮度整体偏移仍能找到（归一化相关）；原位置被覆盖后找到新位置。
        var shifted = screen.Crop(0, 0, screen.Width, screen.Height);
        Blit(shifted, screen.Crop(412, 903, 64, 64), 100, 200, 25);
        Fill(shifted, 412, 903, 64, 64, 200);
        var moved = TemplateMatcher.Find(shifted, template, 0.9);
        Assert.True(moved is { X: 100, Y: 200, Score: > 0.95 }, $"brightened copy: {moved}");
        var absent = ImageTemplate.From(Screen(750, 1334, 7).Crop(300, 300, 64, 64));
        Assert.Equal(null, TemplateMatcher.Find(screen, absent, 0.9));

        Assert.Throws<ArgumentException>(() => ImageTemplate.From(new PixelImage(20, 20, Enumerable.Repeat((byte)128, 1600).ToArray())));
        Assert.Throws<ArgumentException>(() => ImageTemplate.From(screen.Crop(0, 0, 3, 30)));
        Assert.Throws<ArgumentException>(() => TemplateMatcher.Find(screen.Crop(0, 0, 50, 50), template, 0.9));

        var large = Screen(1170, 2532, 5);
        var big = ImageTemplate.From(large.Crop(700, 1800, 96, 96));
        var timer = Stopwatch.StartNew();
        var hit = TemplateMatcher.Find(large, big, 0.9);
        timer.Stop();
        Assert.True(hit is { X: 700, Y: 1800 } && timer.ElapsedMilliseconds < 1500, $"1170×2532: {hit} in {timer.ElapsedMilliseconds} ms");
        Console.WriteLine($"     找图耗时：750×1334 首次 {cold.ElapsedMilliseconds} ms、再次 {warm.ElapsedMilliseconds} ms；1170×2532 {timer.ElapsedMilliseconds} ms");
        return Task.CompletedTask;
    }

    public static async Task Commands()
    {
        using var folder = new TemporaryFolder();
        using var store = new FleetStore(":memory:");
        var device = store.RegisterDevice("Phone", "00008030-0012345678900001", 8101);
        var bridge = new ScriptBridge();
        await using var runner = new ScriptRunner(store, bridge, folder.Path);
        Assert.True(Directory.Exists(runner.TemplateDirectory) && runner.CaptureDirectory == Path.Combine(folder.Path, "captures"), "folders");
        var screen = Screen(300, 500, 3);
        Fill(screen, 250, 400, 1, 1, 0, 0xFF00FF);
        Fill(screen, 20, 20, 1, 1, 0, 0xFF00FF);
        var templatePath = Path.Combine(runner.TemplateDirectory!, "按钮.png");
        File.WriteAllBytes(templatePath, EncodePng(screen.Crop(60, 90, 40, 40)));
        File.WriteAllBytes(Path.Combine(runner.TemplateDirectory!, "别的.png"), EncodePng(Screen(300, 500, 99).Crop(10, 10, 40, 40)));
        bridge.Screenshot = () => EncodePng(screen, alpha: false);
        string F(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        string Tap((double X, double Y) point) => string.Create(CultureInfo.InvariantCulture, $"tap {point.X:0.###} {point.Y:0.###}");
        var (sx, sy) = screen.ToRelative(150, 250);

        var run = await Run(store, runner, device.Id, $"""
            点击图片 按钮
            如果 找到图片 按钮 0.95
              日志 找到了
            否则
              失败 没找到
            结束
            如果 比色 {F(sx)} {F(sy)} {ColorMatcher.Format(screen.Rgb(150, 250))} 0
              点击 0.1 0.1
            结束
            如果 非 比色 {F(sx)} {F(sy)} #010203 0
              点击 0.2 0.2
            结束
            点击颜色 #FF00FF 0 区域 0.5 0.5 1 1
            截图 首页
            等待图片 按钮 2
            """);
        Assert.True(run.State == ScriptRunState.Succeeded, run.Message ?? "");
        var taps = bridge.Calls.Where(call => call.StartsWith("tap", StringComparison.Ordinal)).ToArray();
        Assert.True(taps.SequenceEqual([Tap(screen.ToRelative(79.5, 109.5)), "tap 0.1 0.1", "tap 0.2 0.2", Tap(screen.ToRelative(250, 400))]),
            string.Join(" | ", taps));
        var captures = Directory.GetFiles(runner.CaptureDirectory!);
        Assert.True(captures.Length == 1 && Path.GetFileName(captures[0]).StartsWith($"首页_{device.Id}_", StringComparison.Ordinal)
            && PngDecoder.Decode(File.ReadAllBytes(captures[0])).Width == 300, "capture saved as PNG");

        Assert.True((await Run(store, runner, device.Id, "点击图片 不存在")).Message!.Contains("找不到模板图片「不存在」"), "missing template");
        Assert.True((await Run(store, runner, device.Id, "点击图片 别的")).Message!.Contains("没有找到图片「别的」"), "template not on screen");
        Assert.True((await Run(store, runner, device.Id, "等待图片 别的 1")).Message!.Contains("等待 1 秒后仍未出现"), "wait image timeout");
        Assert.True((await Run(store, runner, device.Id, "点击颜色 #FF00FF 0 区域 0 0.5 0.5 1")).Message!.Contains("没有找到颜色 #FF00FF"), "colour outside region");

        // 替换模板文件后立即使用新内容。
        File.Copy(Path.Combine(runner.TemplateDirectory!, "别的.png"), templatePath, true);
        File.SetLastWriteTimeUtc(templatePath, DateTime.UtcNow.AddMinutes(1));
        Assert.Equal(ScriptRunState.Failed, (await Run(store, runner, device.Id, "如果 找到图片 按钮\n结束\n点击图片 按钮")).State);

        bridge.Screenshot = () => [1, 2, 3];
        Assert.True((await Run(store, runner, device.Id, "如果 比色 0.5 0.5 #000000\n结束")).Message!.Contains("截图无法解析"), "bad screenshot");

        await using var bare = new ScriptRunner(store, bridge);
        Assert.True(bare.TemplateDirectory is null && (await Run(store, bare, device.Id, "截图")).Message!.Contains("未设置数据目录"), "no data dir");
    }

    private static async Task<ScriptRun> Run(FleetStore store, ScriptRunner runner, long deviceId, string source)
    {
        var script = store.SaveScript(null, "图色 " + Guid.NewGuid().ToString("N")[..8], source, 1);
        runner.Start(script.Id, [deviceId], "手动");
        await ScriptChecks.WaitFor(() => runner.RunningCount == 0);
        return store.GetLatestScriptRuns()[deviceId];
    }

    // 类似 App 界面的合成截图：渐变背景、色块卡片、文字状小点和轻微噪声。
    internal static PixelImage Screen(int width, int height, int seed)
    {
        var random = new Random(seed);
        var rgba = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var level = 235 - y * 40 / height;
                Set(rgba, width, x, y, level, level, level + 8);
            }
        for (var k = 0; k < width * height / 12_000; k++)
        {
            int w = random.Next(20, 220), h = random.Next(12, 90), left = random.Next(width - 20), top = random.Next(height - 12);
            int r = random.Next(256), g = random.Next(256), b = random.Next(256);
            for (var y = top; y < Math.Min(height, top + h); y++)
                for (var x = left; x < Math.Min(width, left + w); x++)
                    Set(rgba, width, x, y, r, g, b);
        }
        for (var k = 0; k < width * height / 250; k++)
        {
            int left = random.Next(width - 4), top = random.Next(height - 5), shade = random.Next(0, 90), bits = random.Next();
            for (var i = 0; i < 12; i++)
                if ((bits >> i & 1) != 0)
                    Set(rgba, width, left + i % 3, top + i / 3, shade, shade, shade);
        }
        for (var i = 0; i < rgba.Length; i += 4)
            for (var channel = 0; channel < 3; channel++)
                rgba[i + channel] = (byte)Math.Clamp(rgba[i + channel] + random.Next(-2, 3), 0, 255);
        return new PixelImage(width, height, rgba);
    }

    private static void Set(byte[] rgba, int width, int x, int y, int r, int g, int b)
    {
        var offset = (y * width + x) * 4;
        (rgba[offset], rgba[offset + 1], rgba[offset + 2], rgba[offset + 3]) = ((byte)Math.Clamp(r, 0, 255), (byte)Math.Clamp(g, 0, 255),
            (byte)Math.Clamp(b, 0, 255), (byte)255);
    }

    private static void Blit(PixelImage target, PixelImage source, int left, int top, int brighten)
    {
        for (var y = 0; y < source.Height; y++)
            for (var x = 0; x < source.Width; x++)
            {
                var rgb = source.Rgb(x, y);
                Set(target.Rgba, target.Width, left + x, top + y, (rgb >> 16 & 0xFF) + brighten, (rgb >> 8 & 0xFF) + brighten, (rgb & 0xFF) + brighten);
            }
    }

    private static void Fill(PixelImage target, int left, int top, int width, int height, int level, int? rgb = null)
    {
        for (var y = top; y < top + height; y++)
            for (var x = left; x < left + width; x++)
                Set(target.Rgba, target.Width, x, y, rgb is { } c ? c >> 16 & 0xFF : level, rgb is { } d ? d >> 8 & 0xFF : level,
                    rgb is { } e ? e & 0xFF : level);
    }
}
