using System.Diagnostics;
using System.IO;
using System.ComponentModel;
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
    private const string StartupArgument = "--startup";
    private const string HighPriorityStartupTaskName = "BtAudioSink-Startup-HighPriority";

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
    /// Enables or disables startup. High-priority startup is managed via a scheduled task.
    /// </summary>
    public void SetRunAtStartup(bool enabled, bool highPriority)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, writable: true);
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                return;
            }

            key?.DeleteValue(StartupValueName, throwOnMissingValue: false);
            if (!RemoveHighPriorityStartupTask())
            {
                // User canceled elevation or task removal failed.
                SyncCurrentFromSystem();
                return;
            }

            if (enabled)
            {
                if (highPriority)
                {
                    if (!CreateOrUpdateHighPriorityStartupTask(exePath))
                    {
                        // User canceled UAC or task creation failed. Keep regular startup instead.
                        key?.SetValue(StartupValueName, $"\"{exePath}\" {StartupArgument}");
                        highPriority = false;
                    }
                }
                else
                {
                    key?.SetValue(StartupValueName, $"\"{exePath}\" {StartupArgument}");
                }
            }

            Current.RunAtStartup = enabled;
            Current.RunAtStartupHighPriority = enabled && highPriority;
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
            return key?.GetValue(StartupValueName) != null || HighPriorityStartupTaskExists();
        }
        catch
        {
            return false;
        }
    }

    public bool IsRunAtStartupHighPriorityEnabled()
    {
        return HighPriorityStartupTaskExists();
    }

    private void SyncCurrentFromSystem()
    {
        Current.RunAtStartup = IsRunAtStartupEnabled();
        Current.RunAtStartupHighPriority = IsRunAtStartupHighPriorityEnabled();
        Save();
    }

    private static bool CreateOrUpdateHighPriorityStartupTask(string exePath)
    {
        var taskCommand = $"\"{exePath}\" {StartupArgument}";
        return RunSchtasksElevated(
            "/Create", "/F", "/TN", HighPriorityStartupTaskName,
            "/SC", "ONLOGON", "/RL", "HIGHEST", "/TR", taskCommand) == 0;
    }

    private static bool RemoveHighPriorityStartupTask()
    {
        var queryCode = RunSchtasks("/Query", "/TN", HighPriorityStartupTaskName);
        if (queryCode != 0)
        {
            return true;
        }

        return RunSchtasksElevated("/Delete", "/F", "/TN", HighPriorityStartupTaskName) == 0;
    }

    private static bool HighPriorityStartupTaskExists()
    {
        return RunSchtasks("/Query", "/TN", HighPriorityStartupTaskName) == 0;
    }

    private static int RunSchtasks(params string[] args)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return -1;
            }

            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to run schtasks: {ex.Message}");
            return -1;
        }
    }

    private static int RunSchtasksElevated(params string[] args)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return -1;
            }

            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User canceled UAC prompt.
            return -1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to run elevated schtasks: {ex.Message}");
            return -1;
        }
    }
}
