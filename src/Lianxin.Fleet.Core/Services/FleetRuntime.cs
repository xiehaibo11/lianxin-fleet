using Lianxin.Fleet.Core.Devices;
using Lianxin.Fleet.Core.Persistence;

namespace Lianxin.Fleet.Core.Services;

// 一个工作站进程内共享的全部服务。界面窗口只使用它，不自行创建数据库或设备通道。
public sealed class FleetRuntime : IAsyncDisposable
{
    private readonly DeviceBridge bridge;
    private bool disposed;

    public FleetRuntime(string dataDirectory, string? appDirectory = null, string? pythonPath = null)
    {
        DataDirectory = Path.GetFullPath(dataDirectory);
        Directory.CreateDirectory(DataDirectory);
        AppDirectory = Path.GetFullPath(appDirectory ?? Path.Combine(DataDirectory, "apps"));
        Directory.CreateDirectory(AppDirectory);
        PythonPath = string.IsNullOrWhiteSpace(pythonPath) ? null : pythonPath;
        Store = new FleetStore(Path.Combine(DataDirectory, "fleet.sqlite"));
        bridge = new DeviceBridge(PythonPath);
        try
        {
            Service = new FleetService(Store, bridge, AppDirectory);
            Host = new WdaHost(PythonPath, message => HostLog?.Invoke(message));
            Scripts = new ScriptRunner(Store, bridge);
            Schedules = new ScheduleService(Store, Scripts);
            Setup = new DeviceSetup(AppDirectory, PythonPath);
        }
        catch
        {
            bridge.Dispose();
            Store.Dispose();
            throw;
        }
    }

    public string DataDirectory
    {
        get;
    }
    public string AppDirectory
    {
        get;
    }
    public string? PythonPath
    {
        get;
    }
    public FleetStore Store
    {
        get;
    }
    public IDeviceBridge Bridge => bridge;
    public FleetService Service
    {
        get;
    }
    public WdaHost Host
    {
        get;
    }
    public ScriptRunner Scripts
    {
        get;
    }
    public ScheduleService Schedules
    {
        get;
    }
    public DeviceSetup Setup
    {
        get;
    }
    public string SettingsPath => Path.Combine(DataDirectory, "settings.json");

    // WDA 子进程输出，可能来自任意线程。
    public event Action<string>? HostLog;

    // 关闭顺序：先停调度和脚本，再停 WDA 通道和任务队列，最后释放设备连接和数据库。
    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;
        disposed = true;
        var errors = new List<Exception>();
        async Task Step(Func<ValueTask> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        await Step(Schedules.DisposeAsync);
        await Step(Scripts.DisposeAsync);
        await Step(Host.DisposeAsync);
        await Step(Service.DisposeAsync);
        await Step(() =>
        {
            bridge.Dispose();
            return ValueTask.CompletedTask;
        });
        await Step(() =>
        {
            Store.Dispose();
            return ValueTask.CompletedTask;
        });
        if (errors.Count > 0)
            throw new AggregateException("关闭工作站时发生错误。", errors);
    }
}
