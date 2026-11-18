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
    public string StateLabel => Status.Online ? "可控制" : "未连接";
    public string Error => _previewError ?? Status.Error ?? "";
    public string Placeholder => !Status.Online ? "未连接\n请启动 WDA 控制通道" : _previewError ?? "等待画面";
    public Visibility PlaceholderVisibility => Preview is null ? Visibility.Visible : Visibility.Collapsed;
    public Brush StatusBrush => Status.Online ? Brushes.MediumAquamarine : Brushes.SlateGray;
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
        set
        {
            if (ReferenceEquals(_preview, value))
                return;
            _preview = value;
            Changed();
            Changed(nameof(PlaceholderVisibility));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;

    public DeviceCard(DeviceSnapshot snapshot, Action<DeviceCard> setMaster, Action<DeviceCard, FleetAction> quickAction)
    {
        Device = snapshot.Device;
        Status = snapshot.Status;
        SetMasterCommand = new UiCommand(() => setMaster(this));
        SendTextCommand = new UiCommand(() => quickAction(this, FleetAction.Text));
        HomeCommand = new UiCommand(() => quickAction(this, FleetAction.Home));
    }
    public void Update(DeviceSnapshot snapshot)
    {
        Device = snapshot.Device;
        Status = snapshot.Status;
        foreach (var name in new[] { nameof(Name), nameof(GroupName), nameof(StateLabel), nameof(StatusBrush), nameof(Placeholder), nameof(Error), nameof(QuickTextLabel) })
            Changed(name);
        if (!Status.Online)
            Preview = null;
    }
    public void SetPreviewError(string? error)
    {
        if (_previewError == error)
            return;
        _previewError = error;
        Changed(nameof(Error));
        Changed(nameof(Placeholder));
    }
    private void Changed([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new(propertyName));
}
