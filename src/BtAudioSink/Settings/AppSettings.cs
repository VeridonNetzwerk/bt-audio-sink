namespace BtAudioSink.Settings;

/// <summary>
/// Application settings model persisted to JSON.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Whether to automatically reconnect to last connected devices on app startup.
    /// </summary>
    public bool AutoReconnect { get; set; }

    /// <summary>
    /// Whether the application should start with Windows.
    /// </summary>
    public bool RunAtStartup { get; set; }

    /// <summary>
    /// Whether the application should start minimized to the system tray.
    /// </summary>
    public bool StartMinimized { get; set; } = true;

    /// <summary>
    /// Device IDs of last connected Bluetooth devices (for auto-reconnect).
    /// </summary>
    public List<string> LastConnectedDevices { get; set; } = [];
}
