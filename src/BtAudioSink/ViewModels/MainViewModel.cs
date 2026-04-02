using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using BtAudioSink.Bluetooth;
using BtAudioSink.Media;
using BtAudioSink.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BtAudioSink.ViewModels;

/// <summary>
/// Main ViewModel orchestrating Bluetooth device management, settings, and UI state.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly BluetoothDeviceService _deviceService;
    private readonly AudioPlaybackService _audioService;
    private readonly MediaControlService _mediaControlService;
    private readonly SettingsManager _settingsManager;
    private bool _disposed;

    /// <summary>
    /// Collection of discovered Bluetooth devices.
    /// </summary>
    public ObservableCollection<DeviceViewModel> Devices { get; } = [];

    // --- Connection state ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasConnectedDevice))]
    private string _statusText = "Ready";

    [ObservableProperty]
    private int _connectedDeviceCount;

    public bool HasConnectedDevice => ConnectedDeviceCount > 0;

    // --- Media state ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseGlyph))]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _canPlayPause;

    [ObservableProperty]
    private bool _canSkipNext;

    [ObservableProperty]
    private bool _canSkipPrevious;

    [ObservableProperty]
    private string _nowPlayingTitle = "No active media session";

    [ObservableProperty]
    private string _nowPlayingArtist = "Connect a device and start playback";

    public string PlayPauseGlyph => IsPlaying ? "\uE769" : "\uE768";

    // --- Settings ---

    [ObservableProperty]
    private bool _autoReconnect;

    [ObservableProperty]
    private bool _runAtStartup;

    [ObservableProperty]
    private bool _startMinimized;

    // --- Window state ---

    [ObservableProperty]
    private bool _isWindowVisible;

    /// <summary>
    /// Fired when the application should exit.
    /// </summary>
    public event EventHandler? ExitRequested;

    /// <summary>
    /// Fired when the main window should be shown.
    /// </summary>
    public event EventHandler? ShowWindowRequested;

    /// <summary>
    /// Fired when the main window should be hidden.
    /// </summary>
    public event EventHandler? HideWindowRequested;

    public MainViewModel(
        BluetoothDeviceService deviceService,
        AudioPlaybackService audioService,
        MediaControlService mediaControlService,
        SettingsManager settingsManager)
    {
        _deviceService = deviceService;
        _audioService = audioService;
        _mediaControlService = mediaControlService;
        _settingsManager = settingsManager;

        // Wire up events
        _deviceService.DevicesChanged += OnDevicesChanged;
        _audioService.ConnectionChanged += OnConnectionChanged;
        _mediaControlService.StateChanged += OnMediaStateChanged;
    }

    /// <summary>
    /// Initializes the ViewModel: loads settings and starts device discovery.
    /// </summary>
    public Task InitializeAsync()
    {
        return InitializeInternalAsync();
    }

    private async Task InitializeInternalAsync()
    {
        // Load settings
        _settingsManager.Load();
        AutoReconnect = _settingsManager.Current.AutoReconnect;
        RunAtStartup = _settingsManager.IsRunAtStartupEnabled();
        StartMinimized = _settingsManager.Current.StartMinimized;
        _audioService.AutoReconnect = AutoReconnect;

        // Start media session tracking for transport controls.
        await _mediaControlService.InitializeAsync();

        // Start  device discovery
        _deviceService.StartWatching();

        // Auto-reconnect to last connected devices
        if (AutoReconnect && _settingsManager.Current.LastConnectedDevices.Count > 0)
        {
            StatusText = "Reconnecting...";
            foreach (var deviceId in _settingsManager.Current.LastConnectedDevices)
            {
                _ = Task.Run(async () =>
                {
                    // Wait a moment for device discovery to find the device
                    await Task.Delay(2000);
                    await ConnectDeviceByIdAsync(deviceId);
                });
            }
        }

    }

    // --- Device management ---

    /// <summary>
    /// Rebuilds the device list from the BluetoothDeviceService.
    /// </summary>
    private void RebuildDeviceList()
    {
        DispatchToUI(() =>
        {
            // Remember existing connection states
            var connectedIds = Devices.Where(d => d.IsConnected).Select(d => d.Id).ToHashSet();
            var connectingIds = Devices.Where(d => d.IsConnecting).Select(d => d.Id).ToHashSet();

            Devices.Clear();

            foreach (var device in _deviceService.PairedDevices)
            {
                var vm = new DeviceViewModel(
                    device.Id,
                    device.Name,
                    ConnectDeviceAsync,
                    DisconnectDeviceAsync);

                vm.IsConnected = connectedIds.Contains(device.Id) || _audioService.IsConnected(device.Id);
                vm.IsConnecting = connectingIds.Contains(device.Id);

                Devices.Add(vm);
            }

            UpdateConnectionStatus();
        });
    }

    private async Task ConnectDeviceAsync(DeviceViewModel device)
    {
        device.IsConnecting = true;
        device.ErrorMessage = null;

        var (success, errorMessage) = await _audioService.ConnectAsync(device.Id);

        DispatchToUI(() =>
        {
            device.IsConnecting = false;
            device.IsConnected = success;
            device.ErrorMessage = success ? null : errorMessage;
            UpdateConnectionStatus();
        });
    }

    private async Task DisconnectDeviceAsync(DeviceViewModel device)
    {
        await _audioService.DisconnectAsync(device.Id);

        DispatchToUI(() =>
        {
            device.IsConnected = false;
            device.ErrorMessage = null;
            UpdateConnectionStatus();
        });
    }

    private async Task ConnectDeviceByIdAsync(string deviceId)
    {
        var device = Devices.FirstOrDefault(d => d.Id == deviceId);
        if (device != null)
        {
            await ConnectDeviceAsync(device);
        }
        else
        {
            // Device not yet discovered; try connecting directly
            await _audioService.ConnectAsync(deviceId);
        }
    }

    private void UpdateConnectionStatus()
    {
        ConnectedDeviceCount = Devices.Count(d => d.IsConnected);

        if (ConnectedDeviceCount > 0)
        {
            var connectedNames = Devices.Where(d => d.IsConnected).Select(d => d.Name);
            StatusText = $"Connected to {string.Join(", ", connectedNames)}";
        }
        else if (Devices.Any(d => d.IsConnecting))
        {
            StatusText = "Connecting...";
        }
        else
        {
            StatusText = Devices.Count > 0 ? $"{Devices.Count} device(s) available" : "No devices found";
        }
    }

    // --- Event handlers ---

    private void OnDevicesChanged(object? sender, EventArgs e)
    {
        RebuildDeviceList();
    }

    private void OnConnectionChanged(object? sender, DeviceConnectionChangedEventArgs e)
    {
        DispatchToUI(() =>
        {
            var device = Devices.FirstOrDefault(d => d.Id == e.DeviceId);
            if (device != null)
            {
                device.IsConnecting = false;
                device.IsConnected = e.IsConnected;
                device.ErrorMessage = e.ErrorMessage;
            }

            UpdateConnectionStatus();
            SaveConnectedDevices();
        });
    }

    private void OnMediaStateChanged(object? sender, MediaStateChangedEventArgs e)
    {
        DispatchToUI(() =>
        {
            IsPlaying = e.IsPlaying;
            CanPlayPause = e.CanPlayPause;
            CanSkipNext = e.CanSkipNext;
            CanSkipPrevious = e.CanSkipPrevious;

            NowPlayingTitle = e.HasActiveSession
                ? (string.IsNullOrWhiteSpace(e.Title) ? "Unknown Title" : e.Title)
                : "No active media session";

            NowPlayingArtist = e.HasActiveSession
                ? (string.IsNullOrWhiteSpace(e.Artist) ? "Unknown Artist" : e.Artist)
                : "Connect a device and start playback";
        });
    }

    // --- Commands ---

    [RelayCommand]
    private void RefreshDevices()
    {
        StatusText = "Scanning for devices...";
        _deviceService.RefreshDevices();
    }

    [RelayCommand]
    private void OpenBluetoothSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:bluetooth",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open Bluetooth settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PlayPauseAsync()
    {
        if (await _mediaControlService.PlayPauseAsync())
        {
            StatusText = "Media command sent: Play/Pause";
        }
    }

    [RelayCommand]
    private async Task NextTrackAsync()
    {
        if (await _mediaControlService.NextAsync())
        {
            StatusText = "Media command sent: Next";
        }
    }

    [RelayCommand]
    private async Task PreviousTrackAsync()
    {
        if (await _mediaControlService.PreviousAsync())
        {
            StatusText = "Media command sent: Previous";
        }
    }

    [RelayCommand]
    private void ShowWindow()
    {
        IsWindowVisible = true;
        ShowWindowRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void HideWindow()
    {
        IsWindowVisible = false;
        HideWindowRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ToggleWindow()
    {
        if (IsWindowVisible)
        {
            HideWindow();
        }
        else
        {
            ShowWindow();
        }
    }

    [RelayCommand]
    private void Exit()
    {
        SaveConnectedDevices();
        _settingsManager.Save();
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    // --- Settings handlers ---

    partial void OnAutoReconnectChanged(bool value)
    {
        _settingsManager.Current.AutoReconnect = value;
        _audioService.AutoReconnect = value;
        _settingsManager.Save();
    }

    partial void OnRunAtStartupChanged(bool value)
    {
        _settingsManager.SetRunAtStartup(value);
    }

    partial void OnStartMinimizedChanged(bool value)
    {
        _settingsManager.Current.StartMinimized = value;
        _settingsManager.Save();
    }

    // --- Helpers ---

    private void SaveConnectedDevices()
    {
        var connectedIds = _audioService.ConnectedDeviceIds.ToList();
        _settingsManager.SaveConnectedDevices(connectedIds);
    }

    private static void DispatchToUI(Action action)
    {
        if (Application.Current?.Dispatcher?.CheckAccess() == true)
        {
            action();
        }
        else
        {
            Application.Current?.Dispatcher?.BeginInvoke(action);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _deviceService.DevicesChanged -= OnDevicesChanged;
        _audioService.ConnectionChanged -= OnConnectionChanged;
        _mediaControlService.StateChanged -= OnMediaStateChanged;

        SaveConnectedDevices();
        _settingsManager.Save();

        _audioService.Dispose();
        _mediaControlService.Dispose();
        _deviceService.Dispose();
    }
}
