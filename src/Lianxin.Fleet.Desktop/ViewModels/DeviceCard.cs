using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using Lianxin.Fleet.Core.Domain;
using Lianxin.Fleet.Desktop.Interaction;

namespace Lianxin.Fleet.Desktop.ViewModels;

public sealed class DeviceCard : INotifyPropertyChanged
{
    private bool _selected;
    private ImageSource? _preview;
    private string? _previewError;
    private bool _isUsbSnapshot;
    private double _zoom = 1;
    public FleetDevice Device
    {
        get; private set;
    }
    public DeviceStatus Status
    {
        get; private set;
    }
    public long Id => Device.Id;
    public string Name => Device.Name;
    public string GroupName => Device.GroupName;
    public string SelectionLabel => "选择" + Name;
    public string QuickTextLabel => "发送到" + Name + "的文字";
    public bool? UsbConnected
    {
        get; private set;
    }
    public bool CanControl => Status.Online && UsbConnected != false;
    public bool CanPreview => UsbConnected == true || CanControl;
    public string UsbLabel => UsbConnected switch
    {
        true => "USB 已连接",
        false => "USB 未连接",
        null => "USB 未知"
    };
    public string ControlLabel => CanControl ? "WDA 可控制" : "WDA 离线";
    public string StateLabel => ControlLabel;
    public string PreviewLabel
    {
        get
        {
            if (_preview is null)
                return Placeholder;
            if (_previewError is not null || (!_isUsbSnapshot && !CanControl))
                return "保留上次画面";
            return _isUsbSnapshot ? "USB 快照" : "WDA 画面";
        }
    }
    public string? ControlError => CanControl ? null : Status.Error;
    public string? PreviewError => _previewError;
    public string Error => PreviewError ?? ControlError ?? "";
    public string Placeholder => !CanPreview ? "画面未连接" : _previewError is null ? "等待画面" : "画面读取失败";
    public Visibility PlaceholderVisibility => Preview is null ? Visibility.Visible : Visibility.Collapsed;
    public Brush StatusBrush => CanControl ? Brushes.MediumAquamarine : Brushes.LightSlateGray;
    public Brush BorderBrush => IsSelected ? Brushes.MediumPurple : new SolidColorBrush(Color.FromRgb(46, 53, 74));
    public string QuickText
    {
        get; set;
    } = "";
    public ICommand SetMasterCommand
    {
        get;
    }
    public ICommand SendTextCommand
    {
        get;
    }
    public ICommand HomeCommand
    {
        get;
    }
    public ICommand LockCommand
    {
        get;
    }
    public ICommand UnlockCommand
    {
        get;
    }
    public ICommand ScreenshotCommand
    {
        get;
    }
    // 小屏幕大小：Ctrl+滚轮在设备墙上缩放，见 MainWindow.Shots.cs。
    public double TileWidth => Math.Round(160 * _zoom);
    public double PreviewHeight => Math.Round(250 * _zoom);
    public bool IsSelected
    {
        get => _selected;
        set
        {
            if (_selected == value)
                return;
            _selected = value;
            Changed();
            Changed(nameof(BorderBrush));
        }
    }
    public ImageSource? Preview
    {
        get => _preview;
        set => UpdatePreview(value, isUsbSnapshot: false);
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    public DeviceCard(DeviceSnapshot snapshot, Action<DeviceCard> setMaster, Action<DeviceCard, FleetAction> quickAction,
        Action<DeviceCard> saveScreenshot)
    {
        Device = snapshot.Device;
        Status = snapshot.Status;
        UsbConnected = snapshot.UsbConnected;
        SetMasterCommand = new UiCommand(() => setMaster(this));
        SendTextCommand = new UiCommand(() => quickAction(this, FleetAction.Text));
        HomeCommand = new UiCommand(() => quickAction(this, FleetAction.Home));
        LockCommand = new UiCommand(() => quickAction(this, FleetAction.Lock));
        UnlockCommand = new UiCommand(() => quickAction(this, FleetAction.Unlock));
        ScreenshotCommand = new UiCommand(() => saveScreenshot(this));
    }
    public void SetZoom(double zoom)
    {
        if (_zoom == zoom)
            return;
        _zoom = zoom;
        Changed(nameof(TileWidth));
        Changed(nameof(PreviewHeight));
    }
    public void Update(DeviceSnapshot snapshot)
    {
        Device = snapshot.Device;
        Status = snapshot.Status;
        UsbConnected = snapshot.UsbConnected;
        if (!CanPreview)
        {
            _previewError = null;
            Preview = null;
        }
        foreach (var name in new[]
        {
            nameof(Name), nameof(GroupName), nameof(SelectionLabel), nameof(QuickTextLabel),
            nameof(UsbConnected), nameof(UsbLabel), nameof(CanControl), nameof(ControlLabel), nameof(StateLabel),
            nameof(StatusBrush), nameof(ControlError), nameof(CanPreview), nameof(PreviewLabel),
            nameof(Placeholder), nameof(PreviewError), nameof(Error)
        })
            Changed(name);
    }
    public void SetPreviewFrame(ImageSource image, bool isUsbSnapshot)
    {
        if (!CanPreview)
            return;
        SetPreviewError(null);
        UpdatePreview(image, isUsbSnapshot);
    }
    public void SetPreviewError(string? error)
    {
        if (_previewError == error)
            return;
        _previewError = error;
        Changed(nameof(PreviewError));
        Changed(nameof(Error));
        Changed(nameof(Placeholder));
        Changed(nameof(PreviewLabel));
    }
    private void UpdatePreview(ImageSource? image, bool isUsbSnapshot)
    {
        var visibilityChanged = (_preview is null) != (image is null);
        var sourceChanged = _isUsbSnapshot != isUsbSnapshot;
        _isUsbSnapshot = isUsbSnapshot;
        if (!ReferenceEquals(_preview, image))
        {
            _preview = image;
            Changed(nameof(Preview));
        }
        if (visibilityChanged)
            Changed(nameof(PlaceholderVisibility));
        if (visibilityChanged || sourceChanged)
            Changed(nameof(PreviewLabel));
    }
    private void Changed([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));
}
