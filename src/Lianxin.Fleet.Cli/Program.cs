using System.Text.Json;
using Lianxin.Fleet.Cli.Commands;
using Lianxin.Fleet.Cli.Signing;
using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Serialization;
using Lianxin.Fleet.Core.Services;

namespace Lianxin.Fleet.Cli;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.FirstOrDefault() == "sign-ios")
                return await IosSigning.RunAsync(args.Skip(1).ToArray());
            if (args.Length == 0 || args.Contains("--help"))
            {
                Help();
                return 0;
            }
            var options = Options.Parse(args.Skip(1));
            // 旧脚本若仍带 --demo，必须报错而不是静默改为操作真机。
            if (options.Has("--demo"))
                throw new ArgumentException("演示模式已移除，CLI 只操作真实设备；请去掉 --demo。");
            var directory = Path.GetFullPath(options.Get("--data") ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), OperatingSystem.IsMacOS() ? "LianxinFleet.Mac" : "LianxinFleet"));
            Directory.CreateDirectory(directory);
            var apps = Path.GetFullPath(options.Get("--apps") ?? Environment.GetEnvironmentVariable("FLEET_APP_DIR") ?? Path.Combine(directory, "apps"));
            Directory.CreateDirectory(apps);
            using var store = new FleetStore(Path.Combine(directory, "fleet.sqlite"));
            using var bridge = new DeviceBridge(options.Get("--python"));
            await using var service = new FleetService(store, bridge, apps);
            switch (args[0])
            {
                case "status":
                    Print(await service.RefreshAsync());
                    break;
                case "scan":
                    Print(await service.DiscoverAsync());
                    break;
                case "register":
                    Print(service.RegisterDevice(options.Required("--name"), options.Required("--udid"),
                        int.Parse(options.Required("--port")), options.Get("--group") ?? "未分组"));
                    break;
                case "group":
                    service.AssignGroup(options.Ids(), options.Required("--group"));
                    break;
                case "config":
                    Console.WriteLine(service.ExportConfig(options.Get("--ids") is null ? null : options.Ids()));
                    break;
                case "jobs":
                    Print(service.GetSnapshot().Jobs);
                    break;
                case "run":
                    if (!Enum.TryParse<FleetAction>(options.Required("--action"), true, out var action))
                        throw new ArgumentException("动作仅支持 " + string.Join('/', Enum.GetNames<FleetAction>()).ToLowerInvariant() + "。");
                    var parameters = JsonSerializer.Deserialize<ActionParameters>(options.Get("--params") ?? "{}", FleetJson.Options)
                        ?? throw new ArgumentException("动作参数无效。");
                    var job = await service.SubmitAsync(options.Ids(), action, parameters,
                        options.Get("--key") ?? Guid.NewGuid().ToString());
                    while (job.State is JobState.Queued or JobState.Running)
                    {
                        await Task.Delay(100);
                        job = service.GetSnapshot().Jobs.Single(item => item.Id == job.Id);
                    }
                    Print(job);
                    return job.State == JobState.Succeeded ? 0 : 2;
                case "wda":
                    var devices = store.GetDevices().Where(d => !options.Has("--ids") || options.Ids().Contains(d.Id)).ToArray();
                    await using (var host = new WdaHost(options.Get("--python"), Console.WriteLine))
                    {
                        if (options.Has("--check-only"))
                        {
                            Print(WdaHost.BuildPlan(devices, options.Required("--bundle-id"), options.Get("--python") ?? "python"));
                            break;
                        }
                        using var stop = new CancellationTokenSource();
                        Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
                        await host.StartAsync(devices, options.Required("--bundle-id"), stop.Token);
                        try
                        {
                            await Task.Delay(Timeout.InfiniteTimeSpan, stop.Token);
                        }
                        catch (OperationCanceledException) { }
                    }
                    break;
                default:
                    throw new ArgumentException("未知命令，请使用 --help。");
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
    }

    private static void Print<T>(T value) => Console.WriteLine(JsonSerializer.Serialize(value, FleetJson.Options));
    private static void Help() => Console.WriteLine("""
        联信 Fleet · C# 原生设备工具
        status|scan|register|group|config|jobs|run|wda [选项]
        --data 目录  --python Python路径  --apps 已批准安装目录
        register --name 名称 --udid UDID --port 本地WDA端口 [--group 分组]
        group --ids 1,2 --group 分组
        run --ids 1,2 --action home [--params '{JSON}'] [--key 幂等编号]
        wda --bundle-id 实际RunnerID [--ids 1,2] [--check-only]
        sign-ios --help
        """);
}
