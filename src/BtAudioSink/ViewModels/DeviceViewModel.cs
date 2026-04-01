using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BtAudioSink.ViewModels;

/// <summary>
/// ViewModel representing a single Bluetooth device in the device list.
/// Exposes connection state and a toggle command for connecting/disconnecting.
/// </summary>
public sealed partial class DeviceViewModel : ObservableObject
{
    private readonly Func<DeviceViewModel, Task> _connectAction;
    private readonly Func<DeviceViewModel, Task> _disconnectAction;

    public string Id { get; }
    public string Name { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    [NotifyPropertyChangedFor(nameof(ButtonIcon))]
    private bool _isConnected;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(ButtonText))]
    [NotifyPropertyChangedFor(nameof(IsInteractive))]
    private bool _isConnecting;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Display text for current connection status.
    /// </summary>
    public string StatusText => IsConnecting ? "Connecting..." :
                                 IsConnected ? "Connected" :
                                 !string.IsNullOrEmpty(ErrorMessage) ? ErrorMessage :
                                 "Available";

    /// <summary>
    /// Text for the connect/disconnect button.
    /// </summary>
    public string ButtonText => IsConnecting ? "Cancel" :
                                 IsConnected ? "Disconnect" :
                                 "Connect";

    /// <summary>
    /// Icon glyph for the connect/disconnect button (Segoe MDL2 Assets).
    /// </summary>
    public string ButtonIcon => IsConnected ? "\uE8CD" : "\uE836"; // Disconnect : Link

    /// <summary>
    /// Whether the device can be interacted with (not currently connecting).
    /// </summary>
    public bool IsInteractive => !IsConnecting;

    /// <summary>
    /// Icon for device type display.
    /// </summary>
    public string DeviceIcon => "\uE702"; // Bluetooth icon in Segoe MDL2

    public DeviceViewModel(string id, string name, Func<DeviceViewModel, Task> connectAction, Func<DeviceViewModel, Task> disconnectAction)
    {
        Id = id;
        Name = name;
        _connectAction = connectAction;
        _disconnectAction = disconnectAction;
    }

    [RelayCommand]
    private async Task ToggleConnectionAsync()
    {
        ErrorMessage = null;

        if (IsConnected)
        {
            await _disconnectAction(this);
        }
        else
        {
            await _connectAction(this);
        }
    }
}
