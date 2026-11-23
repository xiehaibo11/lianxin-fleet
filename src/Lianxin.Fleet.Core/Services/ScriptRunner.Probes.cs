using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Imaging;
using Lianxin.Fleet.Core.Validation;

namespace Lianxin.Fleet.Core.Services;

// 在手机屏幕上找目标：元素经 WDA 查找；颜色和图片用一次截图在电脑端计算，命中位置换算成 0–1 相对坐标再点按。
public sealed partial class ScriptRunner
{
    public const int MaxCapturesPerRun = 500;
    public const long MaxTemplateBytes = 8 * 1024 * 1024;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(0.5);
    private readonly ConcurrentDictionary<string, (DateTime Stamp, long Length, ImageTemplate Template)> templates = new();

    // ElementId 非空表示命中元素；否则 X、Y 是命中点的相对坐标。
    private sealed record ProbeHit(string? ElementId, double X, double Y);

    private async Task<bool> TestAsync(ScriptCondition condition, RunContext run) =>
        (await LocateAsync(condition.Probe, run).ConfigureAwait(false) is not null) != condition.Negate;

    private async Task<ProbeHit?> LocateAsync(ScriptProbe probe, RunContext run)
    {
        if (probe.Kind == ScriptProbeKind.Element)
        {
            var ids = await bridge.FindElementsAsync(run.Device, probe.Selector!, run.Token).ConfigureAwait(false);
            return ids.Count > 0 ? new ProbeHit(ids[0], 0, 0) : null;
        }
        // 模板先于截图读取：文件缺失时不必再向手机要截图。
        var template = probe.Kind == ScriptProbeKind.Image ? LoadTemplate(probe.Template!) : null;
        var screen = await ScreenAsync(run).ConfigureAwait(false);
        switch (probe.Kind)
        {
            case ScriptProbeKind.ColorAt:
                var (x, y) = screen.ToPixel(probe.X, probe.Y);
                return ColorMatcher.Matches(screen, x, y, probe.Rgb, probe.Tolerance) ? new ProbeHit(null, probe.X, probe.Y) : null;
            case ScriptProbeKind.ColorSearch:
                var region = probe.Region ?? ScreenRegion.Full;
                var (left, top, right, bottom) = ColorMatcher.ToPixels(screen, region.Left, region.Top, region.Right, region.Bottom);
                return ColorMatcher.Find(screen, probe.Rgb, probe.Tolerance, left, top, right, bottom) is { } point
                    ? Hit(screen, point.X, point.Y) : null;
            default:
                // 找图在受限的后台工作线程上计算，几十台设备同时找图也不会占满线程池。
                return await TemplateMatcher.FindAsync(screen, template!, probe.Similarity, run.Token).ConfigureAwait(false) is { } match
                    ? Hit(screen, match.CenterX, match.CenterY) : null;
        }
    }

    private static ProbeHit Hit(PixelImage screen, double x, double y)
    {
        var (relativeX, relativeY) = screen.ToRelative(x, y);
        return new ProbeHit(null, relativeX, relativeY);
    }

    private async Task TapAsync(ScriptProbe probe, RunContext run)
    {
        var hit = await LocateAsync(probe, run).ConfigureAwait(false) ?? throw new InvalidOperationException(Missing(probe));
        if (hit.ElementId is { } id)
            await bridge.ClickElementAsync(run.Device, id, run.Token).ConfigureAwait(false);
        else
            await bridge.ExecuteAsync(run.Device, FleetAction.Tap, new ActionParameters(X: hit.X, Y: hit.Y), run.Token)
                .ConfigureAwait(false);
    }

    private async Task InputAsync(ScriptStep step, RunContext run)
    {
        var ids = await bridge.FindElementsAsync(run.Device, step.Probe!.Selector!, run.Token).ConfigureAwait(false);
        if (ids.Count == 0)
            throw new InvalidOperationException(Missing(step.Probe));
        await bridge.SetElementValueAsync(run.Device, ids[0], step.Text!, run.Token).ConfigureAwait(false);
    }

    // 每 0.5 秒查一次（查询本身耗时计入间隔），直到出现/消失或超时。单次查询失败（WDA 短暂无响应、截图损坏）
    // 不立即结束，继续查到超时；超时时最后一次仍是失败就报告那次的错误。
    private async Task WaitForAsync(ScriptStep step, RunContext run, bool present)
    {
        var started = Stopwatch.GetTimestamp();
        var timeout = TimeSpan.FromSeconds(step.TimeoutSeconds);
        // 模板文件的问题不会自己好，先读一次，有错立即结束，不重试到超时。
        if (step.Probe!.Kind == ScriptProbeKind.Image)
            LoadTemplate(step.Probe.Template!);
        while (true)
        {
            var attempt = Stopwatch.GetTimestamp();
            Exception? failure = null;
            try
            {
                if ((await LocateAsync(step.Probe!, run).ConfigureAwait(false) is not null) == present)
                    return;
            }
            catch (Exception error) when (error is DeviceOperationException or InvalidDataException && !run.Token.IsCancellationRequested)
            {
                failure = error;
            }
            var waited = step.TimeoutSeconds.ToString("0.##", CultureInfo.InvariantCulture);
            if (Stopwatch.GetElapsedTime(started) >= timeout)
                throw failure is null
                    ? new TimeoutException($"等待 {waited} 秒后{(present ? "仍未出现" : "仍未消失")}。")
                    : new DeviceOperationException($"等待 {waited} 秒后仍不确定，最后一次查询失败：{failure.Message}", false, failure);
            var rest = PollInterval - Stopwatch.GetElapsedTime(attempt);
            if (rest > TimeSpan.Zero)
                await Task.Delay(rest, run.Token).ConfigureAwait(false);
        }
    }

    private static string Missing(ScriptProbe probe) => probe.Kind switch
    {
        ScriptProbeKind.Element => "屏幕上没有找到该元素。",
        ScriptProbeKind.Image => $"屏幕上没有找到图片「{probe.Template}」（相似度 {probe.Similarity.ToString("0.##", CultureInfo.InvariantCulture)}）。",
        _ => $"屏幕上没有找到颜色 {ColorMatcher.Format(probe.Rgb)}。"
    };

    private async Task<PixelImage> ScreenAsync(RunContext run)
    {
        var png = await bridge.GetScreenshotAsync(run.Device, run.Token).ConfigureAwait(false);
        try
        {
            return PngDecoder.Decode(png);
        }
        catch (InvalidDataException error)
        {
            throw new InvalidDataException("截图无法解析：" + error.Message, error);
        }
    }

    private async Task CaptureAsync(ScriptStep step, RunContext run)
    {
        var folder = CaptureDirectory ?? throw new InvalidOperationException("未设置数据目录，无法保存截图。");
        if (++run.Captures > MaxCapturesPerRun)
            throw new InvalidOperationException($"一次运行最多保存 {MaxCapturesPerRun} 张截图。");
        var png = await bridge.GetScreenshotAsync(run.Device, run.Token).ConfigureAwait(false);
        if (!PngDecoder.IsPng(png))
            throw new InvalidDataException("截图不是 PNG 图片。");
        Directory.CreateDirectory(folder);
        // 名称后附设备编号和时间，多台手机同时运行同一脚本也不会互相覆盖。
        var prefix = step.Text is null ? "" : ScriptParser.TemplateName(step.Text) + "_";
        var file = Path.Combine(folder, $"{prefix}{run.Device.Id}_{DateTime.Now:yyyyMMdd-HHmmss-fff}.png");
        await File.WriteAllBytesAsync(file, png, run.Token).ConfigureAwait(false);
    }

    // 模板按文件修改时间和大小缓存，替换图片后下次使用即生效。
    private ImageTemplate LoadTemplate(string name)
    {
        var folder = TemplateDirectory ?? throw new InvalidOperationException("未设置数据目录，无法使用图片模板。");
        name = ScriptParser.TemplateName(name);
        var path = Path.Combine(folder, name + ".png");
        var info = new FileInfo(path);
        if (!info.Exists)
            throw new FileNotFoundException($"找不到模板图片「{name}」，请把 {name}.png 放到 {folder}。");
        if (info.Length > MaxTemplateBytes)
            throw new InvalidDataException($"模板图片「{name}」超过 8 MB。");
        if (templates.TryGetValue(path, out var cached) && cached.Stamp == info.LastWriteTimeUtc && cached.Length == info.Length)
            return cached.Template;
        ImageTemplate template;
        try
        {
            template = ImageTemplate.From(PngDecoder.Decode(File.ReadAllBytes(path), (long)ImageTemplate.MaxSide * ImageTemplate.MaxSide));
        }
        catch (Exception error) when (error is ArgumentException or InvalidDataException)
        {
            throw new InvalidDataException($"模板图片「{name}」：{error.Message}", error);
        }
        templates[path] = (info.LastWriteTimeUtc, info.Length, template);
        return template;
    }
}
