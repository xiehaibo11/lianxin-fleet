using System.IO;
using System.Windows;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Desktop.Views.Center;

namespace Lianxin.Fleet.Desktop;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        FleetRuntime? runtime = null;
        try
        {
            // FLEET_DATA_DIR 用于隔离测试或多工作站；FLEET_APP_DIR 覆盖应用批准目录。
            var dataDirectory = Environment.GetEnvironmentVariable("FLEET_DATA_DIR")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LianxinFleet");
            Directory.CreateDirectory(dataDirectory);
            var settings = FleetSettings.Load(Path.Combine(dataDirectory, "settings.json"));
            runtime = new FleetRuntime(dataDirectory, Environment.GetEnvironmentVariable("FLEET_APP_DIR"), settings.PythonPath);
            var window = new CenterWindow(runtime);
            MainWindow = window;
            window.Show();
        }
        catch (Exception error)
        {
            if (runtime is not null)
                await runtime.DisposeAsync();
            MessageBox.Show(error.Message, "联信启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
