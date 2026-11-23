using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Lianxin.Fleet.Core.Domain;

namespace Lianxin.Fleet.Mac.ViewModels;

public sealed class DeviceCard(DeviceSnapshot snapshot) : INotifyPropertyChanged, IDisposable
{
    private bool selected;
    private Bitmap? preview;
    private string quickText = "";
    private double zoom = 1;
    public DeviceSnapshot Snapshot { get; private set; } = snapshot;
    public FleetDevice Device => Snapshot.Device;
    public DeviceStatus Status => Snapshot.Status;
    public long Id => Device.Id;
    public string Name => Device.Name;
    public string GroupName => Device.GroupName;
    public string StateLabel => Status.Online ? "可控制" : "未连接";
    public string SelectionLabel => "选择 " + Name + " 作为批量目标";
    public IBrush StatusBrush => Status.Online ? Brushes.MediumSeaGreen : Brushes.SlateGray;
    public string Placeholder => !Status.Online ? "未连接" : PreviewError ?? "等待画面";
    public bool ShowPlaceholder => preview is null;
    public string QuickText
    {
        get => quickText;
        set
        {
            if (quickText == value)
                return;
            quickText = value;
            Changed();
        }
    }
    public string? PreviewError
    {
        get; private set;
    }
    public bool IsSelected
    {
        get => selected;
        set
        {
            if (selected == value)
                return;
            selected = value;
            Changed();
        }
    }
    public Bitmap? Preview => preview;
    // 小屏幕大小：Ctrl（⌘）+滚轮在设备墙上缩放，见 MainWindow.Shots.cs。
    public double TileWidth => Math.Round(150 * zoom);
    public double PreviewHeight => Math.Round(300 * zoom);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Update(DeviceSnapshot current)
    {
        var previous = Snapshot;
        Snapshot = current;
        if (previous.Device.Name != Name)
        {
            Changed(nameof(Name));
            Changed(nameof(SelectionLabel));
        }
        if (previous.Device.GroupName != GroupName)
            Changed(nameof(GroupName));
        if (previous.Status.Online != Status.Online)
        {
            Changed(nameof(StateLabel));
            Changed(nameof(StatusBrush));
            Changed(nameof(Placeholder));
        }
        if (!Status.Online)
            SetPreview(null);
    }

    public void SetPreview(Bitmap? bitmap, string? error = null)
    {
        var previous = preview;
        preview = bitmap;
        PreviewError = error;
        Changed(nameof(Placeholder));
        if (!ReferenceEquals(previous, bitmap))
        {
            Changed(nameof(Preview));
            Changed(nameof(ShowPlaceholder));
            previous?.Dispose();
        }
    }

    public void SetZoom(double value)
    {
        if (zoom == value)
            return;
        zoom = value;
        Changed(nameof(TileWidth));
        Changed(nameof(PreviewHeight));
    }

    public void Dispose() => SetPreview(null);

    private void Changed([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
