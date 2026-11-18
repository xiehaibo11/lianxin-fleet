using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Lianxin.Fleet.Mac.Views;

namespace Lianxin.Fleet.Mac;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                desktop.MainWindow = new MainWindow();
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                var dismiss = new Button { Content = "关闭", HorizontalAlignment = HorizontalAlignment.Right };
                var window = new Window
                {
                    Title = "联信设备工作站启动失败",
                    Width = 540,
                    SizeToContent = SizeToContent.Height,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new StackPanel
                    {
                        Margin = new Thickness(24),
                        Spacing = 20,
                        Children =
                        {
                            new TextBlock { Text = error.Message, TextWrapping = TextWrapping.Wrap },
                            dismiss
                        }
                    }
                };
                dismiss.Click += (_, _) => window.Close();
                desktop.MainWindow = window;
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
