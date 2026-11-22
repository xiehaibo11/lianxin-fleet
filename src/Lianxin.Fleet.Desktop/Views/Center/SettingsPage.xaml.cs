using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Lianxin.Fleet.Core.Validation;
using Lianxin.Fleet.Desktop.Interaction;
using Microsoft.Win32;

namespace Lianxin.Fleet.Desktop.Views.Center;

public partial class SettingsPage : UserControl
{
    private CenterWindow center = null!;

    public SettingsPage()
    {
        InitializeComponent();
    }

    public void Attach(CenterWindow host)
    {
        center = host;
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible && !center.IsClosing)
                Load();
        };
        Load();
    }

    private void Load()
    {
        var settings = center.Settings;
        PythonInput.Text = settings.PythonPath ?? "";
        PythonStatus.Text = "当前使用：" + (center.Runtime.PythonPath ?? Environment.GetEnvironmentVariable("PYTHON_PATH") ?? "python（系统 PATH）");
        BundleInput.Text = center.LoadRunnerBundleId();
        PackageInput.Text = settings.RunnerPackage ?? "";
        AppsLabel.Text = center.Runtime.AppDirectory;
        DataLabel.Text = center.Runtime.DataDirectory;
        ModelInput.Text = settings.AgentModel;
        StepsInput.Text = settings.AgentMaxSteps.ToString(CultureInfo.InvariantCulture);
        KeyInput.Clear();
        KeyStatus.Text = settings.AgentKeyProtected is null ? "尚未保存密钥。" : SecretProtector.Unprotect(settings.AgentKeyProtected) is null
            ? "已保存的密钥无法在当前 Windows 用户下解密，请重新填写。" : "已保存密钥（已加密）。";
    }

    private void SaveClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        var python = PythonInput.Text.Trim();
        if (python.Length > 0 && !File.Exists(python))
            throw new ArgumentException("Python 路径不存在。");
        var model = ModelInput.Text.Trim();
        if (model.Length is 0 or > 80 || model.Any(char.IsWhiteSpace))
            throw new ArgumentException("模型名称无效。");
        if (!int.TryParse(StepsInput.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var steps) || steps is < 1 or > 200)
            throw new ArgumentException("最多轮数须为 1 至 200。");
        var package = PackageInput.Text.Trim();
        if (package.Length > 0)
            package = FleetValidation.AppPath(package, center.Runtime.AppDirectory);
        center.SaveRunnerBundleId(BundleInput.Text.Trim());
        var key = KeyInput.Password.Trim();
        var restart = python != (center.Settings.PythonPath ?? "");
        center.SaveSettings(center.Settings with
        {
            PythonPath = python.Length == 0 ? null : python,
            RunnerPackage = package.Length == 0 ? null : package,
            AgentModel = model,
            AgentMaxSteps = steps,
            AgentKeyProtected = key.Length > 0 ? SecretProtector.Protect(key) : center.Settings.AgentKeyProtected
        });
        Load();
        SaveStatus.Text = "已保存。" + (restart ? "Python 路径在重启中控后生效。" : "");
        return Task.CompletedTask;
    });

    private void ClearKeyClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        center.SaveSettings(center.Settings with
        {
            AgentKeyProtected = null
        });
        Load();
        SaveStatus.Text = "已清除保存的密钥。";
        return Task.CompletedTask;
    });

    private void BrowsePythonClicked(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Filter = "python.exe|python.exe", CheckFileExists = true };
        if (picker.ShowDialog(Window.GetWindow(this)) == true)
            PythonInput.Text = picker.FileName;
    }

    private void BrowsePackageClicked(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Filter = "已签名 WDA 安装包|*.ipa", InitialDirectory = center.Runtime.AppDirectory, CheckFileExists = true };
        if (picker.ShowDialog(Window.GetWindow(this)) == true)
            PackageInput.Text = picker.FileName;
    }

    private void OpenAppsClicked(object sender, RoutedEventArgs e) => center.OpenApprovedFolder();
}
