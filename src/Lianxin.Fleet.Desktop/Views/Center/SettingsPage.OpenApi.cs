using System.Globalization;
using System.Windows;
using Lianxin.Fleet.Core.OpenApi;
using Lianxin.Fleet.Core.Persistence;

namespace Lianxin.Fleet.Desktop.Views.Center;

// 设置页「开放接口」：启用、端口、局域网模式与访问令牌的显示、复制和重新生成。
public partial class SettingsPage
{
    private bool tokenVisible, loadingApi;

    private void LoadOpenApi()
    {
        var settings = center.Settings;
        loadingApi = true;
        ApiEnabledInput.IsChecked = settings.OpenApiEnabled;
        ApiPortInput.Text = settings.OpenApiPort.ToString(CultureInfo.InvariantCulture);
        ApiLanInput.IsChecked = settings.OpenApiAllowLan;
        loadingApi = false;
        tokenVisible = false;
        ShowToken();
        ShowApiStatus();
    }

    private void ShowToken()
    {
        var token = center.LoadOpenApiToken();
        ApiTokenText.Text = token is null ? "（尚未生成，勾选「允许局域网访问」时自动生成）" : tokenVisible ? token : new string('•', 32);
        ApiShowButton.Content = tokenVisible ? "隐藏" : "显示";
    }

    private void ShowApiStatus() => ApiStatus.Text = center.OpenApiStatus + Environment.NewLine
        + "文件推送 / 拉取（fsyncPullPush）的电脑端中转目录：" + OpenApiBackend.TransferPath(center.Runtime.DataDirectory);

    // 保存时合并到设置；局域网模式没有令牌时顺带生成。
    private FleetSettings MergeOpenApi(FleetSettings settings)
    {
        if (!int.TryParse(ApiPortInput.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var port)
            || !OpenApiOptions.ValidPort(port))
            throw new ArgumentException("开放接口端口须为 1024 至 65535。");
        var lan = ApiLanInput.IsChecked == true;
        return settings with
        {
            OpenApiEnabled = ApiEnabledInput.IsChecked == true,
            OpenApiPort = port,
            OpenApiAllowLan = lan,
            OpenApiTokenProtected = lan && center.LoadOpenApiToken() is null
                ? CenterWindow.ProtectOpenApiToken(OpenApiOptions.NewToken()) : settings.OpenApiTokenProtected
        };
    }

    private static bool OpenApiChanged(FleetSettings before, FleetSettings after) =>
        before.OpenApiEnabled != after.OpenApiEnabled || before.OpenApiPort != after.OpenApiPort
        || before.OpenApiAllowLan != after.OpenApiAllowLan || before.OpenApiTokenProtected != after.OpenApiTokenProtected;

    // 第一次打开局域网模式时立即生成令牌，方便先复制给其他电脑；模式本身在点「保存设置」后生效。
    private void ApiLanChecked(object sender, RoutedEventArgs e)
    {
        if (loadingApi || center is null || center.LoadOpenApiToken() is not null)
            return;
        center.SaveSettings(center.Settings with
        {
            OpenApiTokenProtected = CenterWindow.ProtectOpenApiToken(OpenApiOptions.NewToken())
        });
        tokenVisible = true;
        ShowToken();
        SaveStatus.Text = "已生成访问令牌，点「保存设置」后局域网模式生效。";
    }

    private void ShowTokenClicked(object sender, RoutedEventArgs e)
    {
        tokenVisible = !tokenVisible;
        ShowToken();
    }

    private void CopyTokenClicked(object sender, RoutedEventArgs e) => center.Run(() =>
    {
        var token = center.LoadOpenApiToken() ?? throw new InvalidOperationException("尚未生成访问令牌。");
        Clipboard.SetText(token);
        SaveStatus.Text = "令牌已复制到剪贴板。";
        return Task.CompletedTask;
    });

    private void RegenerateTokenClicked(object sender, RoutedEventArgs e) => center.Run(async () =>
    {
        if (center.LoadOpenApiToken() is not null && MessageBox.Show(Window.GetWindow(this),
            "重新生成后旧令牌立即失效，使用旧令牌的程序需要改用新令牌。继续吗？", "重新生成令牌",
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        center.SaveSettings(center.Settings with
        {
            OpenApiTokenProtected = CenterWindow.ProtectOpenApiToken(OpenApiOptions.NewToken())
        });
        tokenVisible = true;
        ShowToken();
        if (center.Settings.OpenApiEnabled && center.Settings.OpenApiAllowLan)
            await center.ApplyOpenApiAsync();
        ShowApiStatus();
        SaveStatus.Text = "已生成新的访问令牌。";
    });
}
