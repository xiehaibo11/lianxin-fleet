using System.Windows;
using Lianxin.Fleet.Desktop.Views.Main;

namespace Lianxin.Fleet.Desktop;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "联信启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
