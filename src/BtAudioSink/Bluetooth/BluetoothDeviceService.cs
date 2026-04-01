using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using Windows.Devices.Enumeration;
using Windows.Media.Audio;

namespace BtAudioSink.Bluetooth;

/// <summary>
/// Discovers and monitors paired Bluetooth devices that support A2DP audio playback.
/// Uses DeviceWatcher to dynamically track device additions, removals, and updates.
/// </summary>
public sealed class BluetoothDeviceService : IDisposable
{
    private DeviceWatcher? _watcher;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Collection of discovered paired devices. Updated on the UI thread.
    /// </summary>
    public ObservableCollection<BluetoothDeviceInfo> PairedDevices { get; } = [];

    /// <summary>
    /// Fired when the device list changes (add/remove/update).
    /// </summary>
    public event EventHandler? DevicesChanged;

    /// <summary>
    /// Starts watching for paired Bluetooth audio devices.
    /// </summary>
    public void StartWatching()
    {
        StopWatching();

        try
        {
            var selector = AudioPlaybackConnection.GetDeviceSelector();
            _watcher = DeviceInformation.CreateWatcher(selector);

            _watcher.Added += OnDeviceAdded;
            _watcher.Removed += OnDeviceRemoved;
            _watcher.Updated += OnDeviceUpdated;
            _watcher.EnumerationCompleted += OnEnumerationCompleted;
            _watcher.Stopped += OnWatcherStopped;

            _watcher.Start();
            Debug.WriteLine("BluetoothDeviceService: Started watching for A2DP devices.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"BluetoothDeviceService: Failed to start watcher - {ex.Message}");
        }
    }

    /// <summary>
    /// Stops the device watcher.
    /// </summary>
    public void StopWatching()
    {
        if (_watcher != null)
        {
            try
            {
                if (_watcher.Status == DeviceWatcherStatus.Started ||
                    _watcher.Status == DeviceWatcherStatus.EnumerationCompleted)
                {
                    _watcher.Stop();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BluetoothDeviceService: Error stopping watcher - {ex.Message}");
            }

            _watcher.Added -= OnDeviceAdded;
            _watcher.Removed -= OnDeviceRemoved;
            _watcher.Updated -= OnDeviceUpdated;
            _watcher.EnumerationCompleted -= OnEnumerationCompleted;
            _watcher.Stopped -= OnWatcherStopped;
            _watcher = null;
        }
    }

    /// <summary>
    /// Refreshes the device list by restarting the watcher.
    /// </summary>
    public void RefreshDevices()
    {
        lock (_lock)
        {
            DispatchToUI(() => PairedDevices.Clear());
        }

        StartWatching();
    }

    private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation device)
    {
        var info = new BluetoothDeviceInfo
        {
            Id = device.Id,
            Name = string.IsNullOrWhiteSpace(device.Name) ? "Unknown Device" : device.Name,
            IsPaired = device.Pairing?.IsPaired ?? false
        };

        Debug.WriteLine($"BluetoothDeviceService: Device added - {info.Name} ({info.Id})");

        lock (_lock)
        {
            DispatchToUI(() =>
            {
                if (!PairedDevices.Any(d => d.Id == info.Id))
                {
                    PairedDevices.Add(info);
                    DevicesChanged?.Invoke(this, EventArgs.Empty);
                }
            });
        }
    }

    private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        Debug.WriteLine($"BluetoothDeviceService: Device removed - {update.Id}");

        lock (_lock)
        {
            DispatchToUI(() =>
            {
                var device = PairedDevices.FirstOrDefault(d => d.Id == update.Id);
                if (device != null)
                {
                    PairedDevices.Remove(device);
                    DevicesChanged?.Invoke(this, EventArgs.Empty);
                }
            });
        }
    }

    private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
    {
        Debug.WriteLine($"BluetoothDeviceService: Device updated - {update.Id}");
        // Device properties updated; connection status is managed by AudioPlaybackService
    }

    private void OnEnumerationCompleted(DeviceWatcher sender, object args)
    {
        Debug.WriteLine($"BluetoothDeviceService: Enumeration completed. Found {PairedDevices.Count} device(s).");
    }

    private void OnWatcherStopped(DeviceWatcher sender, object args)
    {
        Debug.WriteLine("BluetoothDeviceService: Watcher stopped.");
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
        StopWatching();
    }
}
