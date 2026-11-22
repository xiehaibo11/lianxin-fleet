using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Lianxin.Fleet.Core.Persistence;
using Lianxin.Fleet.Core.Services;
using Lianxin.Fleet.Mac.Views.Center;

namespace Lianxin.Fleet.Mac;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 关闭中控即退出；集控投屏窗口由中控负责关闭。
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            FleetRuntime? runtime = null;
            try
            {
                // FLEET_DATA_DIR 用于隔离测试或多工作站；macOS 上 .NET 不按 HOME 解析 LocalApplicationData。
                var dataDirectory = Path.GetFullPath(Environment.GetEnvironmentVariable("FLEET_DATA_DIR")
                    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LianxinFleet.Mac"));
                Directory.CreateDirectory(dataDirectory);
                var settings = FleetSettings.Load(Path.Combine(dataDirectory, "settings.json"));
                runtime = new FleetRuntime(dataDirectory, Environment.GetEnvironmentVariable("FLEET_APP_DIR"), settings.PythonPath);
                desktop.MainWindow = new CenterWindow(runtime);
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                if (runtime is not null)
                    _ = runtime.DisposeAsync().AsTask().ContinueWith(task => Console.Error.WriteLine(task.Exception),
                        TaskContinuationOptions.OnlyOnFaulted);
                desktop.MainWindow = StartupFailure(error.Message);
            }
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static Window StartupFailure(string message)
    {
        var dismiss = new Button { Content = "关闭", HorizontalAlignment = HorizontalAlignment.Right };
        var window = new Window
        {
            Title = "联信中控启动失败",
            Width = 540,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new StackPanel
            {
                Margin = new Thickness(24),
                Spacing = 20,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    dismiss
                }
            }
        };
        dismiss.Click += (_, _) => window.Close();
        return window;
    }
}
