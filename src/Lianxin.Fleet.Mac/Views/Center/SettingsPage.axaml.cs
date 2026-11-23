using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Lianxin.Fleet.Core.Validation;
using Lianxin.Fleet.Mac.Interaction;

namespace Lianxin.Fleet.Mac.Views.Center;

public sealed partial class SettingsPage : UserControl
{
    private CenterWindow center = null!;

    public SettingsPage()
    {
        InitializeComponent();
    }

    public void Attach(CenterWindow host)
    {
        center = host;
        center.OpenApiChanged += ShowApiStatus;
        Load();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty && IsVisible && center is not null && !center.IsClosing)
            Load();
    }

    private void Load()
    {
        var settings = center.Settings;
        PythonInput.Text = settings.PythonPath ?? "";
        PythonStatus.Text = "当前使用：" + (center.Runtime.PythonPath ?? Environment.GetEnvironmentVariable("PYTHON_PATH") ?? "python3（系统 PATH）");
        BundleInput.Text = center.LoadRunnerBundleId();
        PackageInput.Text = settings.RunnerPackage ?? "";
        AppsLabel.Text = center.Runtime.AppDirectory;
        DataLabel.Text = center.Runtime.DataDirectory;
        // 预置三个模型；设置文件里是别的模型名时临时加一项，保存时原样保留。
        var model = ModelInput.Items.OfType<ComboBoxItem>().FirstOrDefault(item => (string?)item.Content == settings.AgentModel);
        if (model is null)
        {
            model = new ComboBoxItem { Content = settings.AgentModel };
            ModelInput.Items.Add(model);
        }
        ModelInput.SelectedItem = model;
        StepsInput.Text = settings.AgentMaxSteps.ToString(CultureInfo.InvariantCulture);
        KeyInput.Text = "";
        KeyStatus.Text = AgentKeyStore.Load(center.Runtime.DataDirectory) is null ? "尚未保存密钥。" : "已保存密钥（仅当前用户可读）。";
        LoadOpenApi();
    }

    private void SaveClicked(object? sender, RoutedEventArgs e) => center.Run(async () =>
    {
        var python = PythonInput.Text?.Trim() ?? "";
        if (python.Length > 0 && !File.Exists(python))
            throw new ArgumentException("Python 路径不存在。");
        var model = (ModelInput.SelectedItem as ComboBoxItem)?.Content as string ?? "";
        if (model.Length is 0 or > 80 || model.Any(char.IsWhiteSpace))
            throw new ArgumentException("模型名称无效。");
        if (!int.TryParse(StepsInput.Text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var steps) || steps is < 1 or > 200)
            throw new ArgumentException("最多轮数须为 1 至 200。");
        var package = PackageInput.Text?.Trim() ?? "";
        if (package.Length > 0)
            package = FleetValidation.AppPath(package, center.Runtime.AppDirectory);
        var previous = center.Settings;
        var updated = MergeOpenApi(previous, out var tokenCreated);
        center.SaveRunnerBundleId(BundleInput.Text?.Trim() ?? "");
        var key = KeyInput.Text?.Trim() ?? "";
        if (key.Length > 0)
            AgentKeyStore.Save(center.Runtime.DataDirectory, key);
        var restart = python != (previous.PythonPath ?? "");
        center.SaveSettings(updated with
        {
            PythonPath = python.Length == 0 ? null : python,
            RunnerPackage = package.Length == 0 ? null : package,
            AgentModel = model,
            AgentMaxSteps = steps
        });
        if (tokenCreated || OpenApiChanged(previous, center.Settings))
            await center.ApplyOpenApiAsync();
        Load();
        SaveStatus.Text = "已保存。" + (restart ? "Python 路径在重启中控后生效。" : "");
    });

    private void ClearKeyClicked(object? sender, RoutedEventArgs e) => center.Run(() =>
    {
        AgentKeyStore.Delete(center.Runtime.DataDirectory);
        Load();
        SaveStatus.Text = "已清除保存的密钥。";
        return Task.CompletedTask;
    });

    private async Task<string?> PickAsync(string title, FilePickerFileType? type, string? folder)
    {
        var storage = TopLevel.GetTopLevel(this)!.StorageProvider;
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            SuggestedStartLocation = folder is null ? null : await storage.TryGetFolderFromPathAsync(folder),
            FileTypeFilter = type is null ? null : [type]
        });
        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    private async void BrowsePythonClicked(object? sender, RoutedEventArgs e)
    {
        if (await PickAsync("选择安装了 pymobiledevice3 的 Python", null, null) is { } path)
            PythonInput.Text = path;
    }

    private async void BrowsePackageClicked(object? sender, RoutedEventArgs e)
    {
        if (await PickAsync("选择已签名的 WDA 安装包", new FilePickerFileType("已签名 WDA 安装包") { Patterns = ["*.ipa"] },
            center.Runtime.AppDirectory) is { } path)
            PackageInput.Text = path;
    }

    private void OpenAppsClicked(object? sender, RoutedEventArgs e) => center.OpenApprovedFolder();
}
