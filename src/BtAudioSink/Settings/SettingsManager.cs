using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace BtAudioSink.Settings;

/// <summary>
/// Manages loading and saving of application settings from a JSON file.
/// Also handles the "Run at startup" registry entry.
/// </summary>
public sealed class SettingsManager
{
    private const string SettingsFileName = "BtAudioSink.settings.json";
    private const string StartupRegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "BtAudioSink";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsFilePath;

    public AppSettings Current { get; private set; } = new();

    public SettingsManager()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BtAudioSink");

        Directory.CreateDirectory(appDataDir);
        _settingsFilePath = Path.Combine(appDataDir, SettingsFileName);
    }

    /// <summary>
    /// Loads settings from the JSON file. Returns default settings on failure.
    /// </summary>
    public void Load()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                Current = new AppSettings();
                return;
            }

            var json = File.ReadAllText(_settingsFilePath);
            Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load settings: {ex.Message}");
            Current = new AppSettings();
        }
    }

    /// <summary>
    /// Persists current settings to the JSON file.
    /// </summary>
    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Current, JsonOptions);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the list of currently connected device IDs for auto-reconnect.
    /// </summary>
    public void SaveConnectedDevices(IEnumerable<string> deviceIds)
    {
        Current.LastConnectedDevices = deviceIds.ToList();
        Save();
    }

    /// <summary>
    /// Enables or disables the "Run at startup" registry entry.
    /// </summary>
    public void SetRunAtStartup(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: true);
            if (key == null)
            {
                return;
            }

            if (enabled)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(StartupValueName, $"\"{exePath}\"");
                }
            }
            else
            {
                key.DeleteValue(StartupValueName, throwOnMissingValue: false);
            }

            Current.RunAtStartup = enabled;
            Save();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to set run at startup: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks whether the "Run at startup" registry entry currently exists.
    /// </summary>
    public bool IsRunAtStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: false);
            return key?.GetValue(StartupValueName) != null;
        }
        catch
        {
            return false;
        }
    }
}
