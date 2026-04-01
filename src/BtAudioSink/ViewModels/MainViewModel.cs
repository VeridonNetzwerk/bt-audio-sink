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
/// Main ViewModel orchestrating all application logic: Bluetooth device management,
/// media controls, settings, and UI state.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly BluetoothDeviceService _deviceService;
    private readonly AudioPlaybackService _audioService;
    private readonly MediaControlService _mediaService;
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
    [NotifyPropertyChangedFor(nameof(MediaDisplayText))]
    private string? _mediaTitle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MediaDisplayText))]
    private string? _mediaArtist;

    [ObservableProperty]
    private string? _mediaAlbumTitle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseIcon))]
    [NotifyPropertyChangedFor(nameof(PlayPauseTooltip))]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _hasMediaSession;

    /// <summary>
    /// Combined display text for current media (title - artist).
    /// </summary>
    public string? MediaDisplayText
    {
        get
        {
            if (string.IsNullOrEmpty(MediaTitle) && string.IsNullOrEmpty(MediaArtist))
            {
                return null;
            }

            if (string.IsNullOrEmpty(MediaArtist))
            {
                return MediaTitle;
            }

            if (string.IsNullOrEmpty(MediaTitle))
            {
                return MediaArtist;
            }

            return $"{MediaTitle} — {MediaArtist}";
        }
    }

    /// <summary>
    /// Play/pause button icon (Segoe MDL2 Assets).
    /// </summary>
    public string PlayPauseIcon => IsPlaying ? "\uE769" : "\uE768"; // Pause : Play

    public string PlayPauseTooltip => IsPlaying ? "Pause" : "Play";

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
        MediaControlService mediaService,
        SettingsManager settingsManager)
    {
        _deviceService = deviceService;
        _audioService = audioService;
        _mediaService = mediaService;
        _settingsManager = settingsManager;

        // Wire up events
        _deviceService.DevicesChanged += OnDevicesChanged;
        _audioService.ConnectionChanged += OnConnectionChanged;
        _mediaService.MediaPropertiesChanged += OnMediaPropertiesChanged;
        _mediaService.PlaybackStatusChanged += OnPlaybackStatusChanged;
        _mediaService.SessionAvailabilityChanged += OnSessionAvailabilityChanged;
    }

    /// <summary>
    /// Initializes the ViewModel: loads settings, starts device discovery, initializes media service.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Load settings
        _settingsManager.Load();
        AutoReconnect = _settingsManager.Current.AutoReconnect;
        RunAtStartup = _settingsManager.IsRunAtStartupEnabled();
        StartMinimized = _settingsManager.Current.StartMinimized;
        _audioService.AutoReconnect = AutoReconnect;

        // Start  device discovery
        _deviceService.StartWatching();

        // Initialize media controls
        await _mediaService.InitializeAsync();

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

    private void OnMediaPropertiesChanged(object? sender, MediaMetadataChangedEventArgs e)
    {
        DispatchToUI(() =>
        {
            MediaTitle = e.Title;
            MediaArtist = e.Artist;
            MediaAlbumTitle = e.AlbumTitle;
        });
    }

    private void OnPlaybackStatusChanged(object? sender, PlayerPlaybackStatusChangedEventArgs e)
    {
        DispatchToUI(() =>
        {
            IsPlaying = e.IsPlaying;
        });
    }

    private void OnSessionAvailabilityChanged(object? sender, bool hasSession)
    {
        DispatchToUI(() =>
        {
            HasMediaSession = hasSession;
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
    private async Task PlayPauseAsync()
    {
        await _mediaService.PlayPauseAsync();
    }

    [RelayCommand]
    private async Task NextTrackAsync()
    {
        await _mediaService.NextAsync();
    }

    [RelayCommand]
    private async Task PreviousTrackAsync()
    {
        await _mediaService.PreviousAsync();
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
        _mediaService.MediaPropertiesChanged -= OnMediaPropertiesChanged;
        _mediaService.PlaybackStatusChanged -= OnPlaybackStatusChanged;
        _mediaService.SessionAvailabilityChanged -= OnSessionAvailabilityChanged;

        SaveConnectedDevices();
        _settingsManager.Save();

        _mediaService.Dispose();
        _audioService.Dispose();
        _deviceService.Dispose();
    }
}
