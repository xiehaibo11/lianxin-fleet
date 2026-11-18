using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Infrastructure;

namespace Lianxin.Fleet.Checks.Scenarios;

public static class NativeBridgeChecks
{
    public static async Task RunAsync()
    {
        var device = new FleetDevice(1, "Phone 1", "00008110-001234567890001E", 18100);
        var requests = new ConcurrentQueue<(int Port, string Path, string Body)>();
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jz5kAAAAASUVORK5CYII=");
        var actionEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseAction = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var holdAction = false;
        var failStatus = false;
        var uncertainHome = false;
        using var client = new HttpClient(new Handler(async (request, token) =>
        {
            var uri = request.RequestUri!;
            Ensure(uri.Host == "127.0.0.1" && uri.Scheme == "http", "Only loopback WDA is reachable");
            var path = uri.AbsolutePath;
            var declaredLength = request.Content?.Headers.ContentLength;
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(token);
            if (request.Content is not null)
                Ensure(declaredLength == System.Text.Encoding.UTF8.GetByteCount(body), "WDA JSON requests require Content-Length before sending");
            requests.Enqueue((uri.Port, path, body));
            if (path == "/status" && failStatus)
                throw new HttpRequestException("status socket interrupted");
            if (path == "/wda/homescreen" && uncertainHome)
                throw new HttpRequestException("action response lost");
            if (path.EndsWith("/wda/tap") && holdAction)
            {
                actionEntered.TrySetResult();
                await releaseAction.Task.WaitAsync(token);
            }
            object? value = path switch
            {
                "/status" => new { ready = true },
                "/window/size" => new { width = 390, height = 844 },
                "/wda/device/info" => new { name = "Actual phone" },
                "/session" => new { sessionId = "session-1" },
                "/screenshot" => Convert.ToBase64String(png),
                _ => null
            };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { value }))
            };
        }));
        var process = new FakeRunner();
        using var bridge = new DeviceBridge("C:\\Python\\python.exe", client, "win32", process);
        var status = await bridge.GetStatusAsync(device);
        Ensure(status.Online && status.DeviceName == "Actual phone", "Actual status name is preserved");
        Ensure(!requests.Any(r => r.Path == "/session"), "Status cannot replace a control session");
        Ensure((await bridge.GetScreenshotAsync(device)).SequenceEqual(png), "PNG data survives the bridge");
        await bridge.ExecuteAsync(device, FleetAction.Tap, new ActionParameters(X: 1, Y: 0));
        using (var tap = JsonDocument.Parse(requests.Last().Body))
            Ensure(tap.RootElement.GetProperty("x").GetInt32() == 389, "Normalized edge coordinates stay inside screen");
        Ensure(requests.Count(r => r.Path == "/session") == 1, "Session is reused");
        var beforeInvalid = requests.Count;
        await Reject(() => bridge.ExecuteAsync(device, FleetAction.Tap, new ActionParameters(X: double.NaN, Y: 0)));
        Ensure(requests.Count == beforeInvalid, "Invalid coordinates never reach a phone");
        holdAction = true;
        var action = bridge.ExecuteAsync(device, FleetAction.Tap, new ActionParameters(X: .5, Y: .5));
        await actionEntered.Task;
        failStatus = true;
        await Reject(() => bridge.GetStatusAsync(device));
        var screenshots = requests.Count(r => r.Path == "/screenshot");
        var preview = bridge.GetScreenshotAsync(device);
        await bridge.ExecuteAsync(device with
        {
            Id = 2,
            WdaPort = 18101
        }, FleetAction.Home, new ActionParameters());
        Ensure(requests.Count(r => r.Path == "/screenshot") == screenshots, "Same-phone preview waits for its action");
        releaseAction.TrySetResult();
        await Task.WhenAll(action, preview);
        Ensure(requests.Count(r => r.Path == "/session") == 1, "Read failure does not rebuild an active session");
        uncertainHome = true;
        var homes = requests.Count(r => r.Path == "/wda/homescreen");
        try
        {
            await bridge.ExecuteAsync(device, FleetAction.Home, new ActionParameters());
            throw new Exception("Expected uncertain result");
        }
        catch (DeviceOperationException error) { Ensure(error.Uncertain, "Lost action result stays uncertain"); }
        Ensure(requests.Count(r => r.Path == "/wda/homescreen") == homes + 1, "Unknown action is never retried");
        var discovered = await bridge.DiscoverAsync();
        Ensure(discovered.Count == 1, "Windows JSON discovery filters valid iPhones");
        var app = "C:\\Approved Apps\\Internal & Tools.ipa";
        await bridge.ExecuteAsync(device, FleetAction.Install, new ActionParameters(AppPath: app));
        Ensure(process.Arguments!.Last() == app && process.File == "C:\\Python\\python.exe", "Package path stays one argument");
        var plan = WdaHost.BuildPlan([device], WdaHost.DefaultRunnerBundleId, "python");
        Ensure(plan.Count == 1 && plan[0].Arguments.Contains(device.Udid), "One runner process per phone; forwarding is in-process");
        Ensure(plan[0].Arguments.Last() == "com.lianxin.fleet.webdriveragent.xctrunner", "Default selects the signed Lianxin runner");
        await using var host = new WdaHost();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Reject(() => host.StartAsync([device], "com.example.WDARunner.xctrunner", cancelled.Token));
        Ensure(!host.IsRunning, "Cancelled startup cleans host state without spawning a phone process");
        var local = await new ProcessRunner().RunAsync("dotnet", ["--version"], TimeSpan.FromSeconds(10));
        Ensure(local.ExitCode == 0 && Version.TryParse(local.StandardOutput.Trim(), out _), "Bounded process runner reads a real local tool");
        Console.WriteLine("PASS: native WDA protocol, per-device locks, Windows process arguments and host plans");
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
    private static async Task Reject(Func<Task> action)
    {
        var rejected = false;
        try
        {
            await action();
        }
        catch { rejected = true; }
        Ensure(rejected, "Operation should have been rejected");
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
    private sealed class FakeRunner : IProcessRunner
    {
        public string? File;
        public IReadOnlyList<string>? Arguments;
        public Task<ProcessResult> RunAsync(string file, IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            File = file;
            Arguments = arguments;
            var output = arguments.Contains("list") ? "[{\"DeviceClass\":\"iPhone\",\"UniqueDeviceID\":\"00008110-001234567890001E\",\"DeviceName\":\"Phone\"}]" : "";
            return Task.FromResult(new ProcessResult(0, output, ""));
        }
    }
}
