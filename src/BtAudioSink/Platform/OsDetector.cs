namespace BtAudioSink.Platform;

/// <summary>
/// Detects the Windows version at runtime to enable adaptive UI theming.
/// </summary>
public static class OsDetector
{
    private static WindowsVersion? _cachedVersion;
    private static int? _cachedBuildNumber;

    public static WindowsVersion DetectedVersion => _cachedVersion ??= Detect();

    public static bool IsWindows11 => DetectedVersion == WindowsVersion.Windows11;

    public static bool IsWindows10 => DetectedVersion == WindowsVersion.Windows10;

    public static int BuildNumber
    {
        get
        {
            EnsureDetected();
            return _cachedBuildNumber ?? 0;
        }
    }

    private static void EnsureDetected()
    {
        _ = DetectedVersion;
    }

    private static WindowsVersion Detect()
    {
        var version = Environment.OSVersion.Version;
        _cachedBuildNumber = version.Build;

        if (version.Build >= 22000)
        {
            return WindowsVersion.Windows11;
        }
        else if (version.Build >= 19041)
        {
            return WindowsVersion.Windows10;
        }

        return WindowsVersion.Unsupported;
    }

    /// <summary>
    /// Windows 11 21H2+ supports Mica backdrop via DwmSetWindowAttribute with DWMWA_MICA_EFFECT.
    /// </summary>
    public static bool SupportsMicaBackdrop => BuildNumber >= 22000;

    /// <summary>
    /// Windows 11 22H2+ supports DWMWA_SYSTEMBACKDROP_TYPE for Mica/Acrylic.
    /// </summary>
    public static bool SupportsSystemBackdropType => BuildNumber >= 22621;

    /// <summary>
    /// True if the AudioPlaybackConnection API is available (Windows 10 2004+).
    /// </summary>
    public static bool SupportsAudioPlaybackConnection => BuildNumber >= 19041;
}

public enum WindowsVersion
{
    Unsupported,
    Windows10,
    Windows11
}
