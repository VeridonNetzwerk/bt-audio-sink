using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Media;
using Microsoft.Win32;
using BtAudioSink.Bluetooth;
using BtAudioSink.Platform;
using BtAudioSink.Settings;
using BtAudioSink.ViewModels;
using BtAudioSink.Views;
using H.NotifyIcon;
using System.Linq;

namespace BtAudioSink;

/// <summary>
/// Application entry point. Manages single-instance enforcement, theme loading,
/// tray icon setup, and service initialization.
/// </summary>
public partial class App : Application
{
    private const string StartupArgument = "--startup";

    private Mutex? _singleInstanceMutex;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private MainViewModel? _viewModel;
    private ResourceDictionary? _activeThemeDictionary;

    // Services
    private BluetoothDeviceService? _deviceService;
    private AudioPlaybackService? _audioService;
    private SettingsManager? _settingsManager;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Enforce single instance
        _singleInstanceMutex = new Mutex(true, "BtAudioSink_SingleInstance_Mutex", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "BT Audio Sink is already running.\nCheck the system tray notification area.",
                "BT Audio Sink",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // Check OS compatibility
        if (!OsDetector.SupportsAudioPlaybackConnection)
        {
            MessageBox.Show(
                "BT Audio Sink requires Windows 10 version 2004 (build 19041) or later.\n\n" +
                "Your current Windows build: " + Environment.OSVersion.Version.Build,
                "Unsupported Operating System",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        // Verify AudioPlaybackConnection API availability at runtime
        try
        {
            bool apiPresent = global::Windows.Foundation.Metadata.ApiInformation.IsTypePresent(
                "Windows.Media.Audio.AudioPlaybackConnection");
            if (!apiPresent)
            {
                MessageBox.Show(
                    "The AudioPlaybackConnection API is not available on this system.\n" +
                    "Please ensure you are running Windows 10 version 2004 or later.",
                    "API Not Available",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"API check failed: {ex.Message}");
            // Continue anyway; the API might still work
        }

        // Load theme based on detected OS version
        LoadTheme();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        // Initialize services
        _settingsManager = new SettingsManager();
        _settingsManager.Load();

        bool launchedFromStartup = e.Args.Any(arg =>
            string.Equals(arg, StartupArgument, StringComparison.OrdinalIgnoreCase));

        if (launchedFromStartup && _settingsManager.Current.RunAtStartupHighPriority)
        {
            TrySetCurrentProcessHighPriority();
        }

        _deviceService = new BluetoothDeviceService();
        _audioService = new AudioPlaybackService();

        // Create ViewModel
        _viewModel = new MainViewModel(_deviceService, _audioService, _settingsManager);
        _viewModel.ExitRequested += OnExitRequested;
        _viewModel.ShowWindowRequested += OnShowWindowRequested;
        _viewModel.HideWindowRequested += OnHideWindowRequested;

        // Create main window
        _mainWindow = new MainWindow { DataContext = _viewModel };

        // Set up system tray icon
        SetupTrayIcon();

        // Initialize ViewModel (loads settings, starts discovery, etc.)
        await _viewModel.InitializeAsync();

        // Start minimized only when launched via startup entry.
        bool startMinimized = launchedFromStartup && _settingsManager.Current.StartMinimized;
        if (!startMinimized)
        {
            _mainWindow.Show();
            _viewModel.IsWindowVisible = true;
        }
    }

    private static void TrySetCurrentProcessHighPriority()
    {
        try
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to set high priority: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the appropriate theme ResourceDictionary based on the detected Windows version.
    /// </summary>
    private void LoadTheme()
    {
        ApplyModeTheme();
    }

    private void ApplyModeTheme()
    {
        bool isLight = NativeInterop.IsLightTheme(useAppsTheme: true);
        string themeUri = OsDetector.IsWindows11
            ? (isLight ? "Themes/Win11Theme.xaml" : "Themes/Win11DarkTheme.xaml")
            : (isLight ? "Themes/Win10Theme.xaml" : "Themes/Win10DarkTheme.xaml");

        if (_activeThemeDictionary != null)
        {
            Resources.MergedDictionaries.Remove(_activeThemeDictionary);
            _activeThemeDictionary = null;
        }

        _activeThemeDictionary = new ResourceDictionary
        {
            Source = new Uri(themeUri, UriKind.Relative)
        };

        Resources.MergedDictionaries.Clear();
        Resources.MergedDictionaries.Add(_activeThemeDictionary);

        if (_mainWindow != null)
        {
            _mainWindow.ApplySystemThemeMode(isLight);
        }

        Debug.WriteLine($"Loaded theme: {themeUri} (Build {OsDetector.BuildNumber})");
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General ||
            e.Category == UserPreferenceCategory.Color ||
            e.Category == UserPreferenceCategory.VisualStyle)
        {
            Dispatcher.Invoke(ApplyModeTheme);
        }
    }

    /// <summary>
    /// Creates and configures the system tray icon with context menu.
    /// </summary>
    private void SetupTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "BT Audio Sink",
            ContextMenu = CreateTrayContextMenu(),
        };

        // Load icon from disk first (most reliable for tray), then fall back.
        try
        {
            var fileIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(fileIconPath))
            {
                _trayIcon.Icon = new System.Drawing.Icon(fileIconPath);
            }
            else
            {
                var iconUri = new Uri("pack://application:,,,/Assets/app.ico");
                var resourceInfo = GetResourceStream(iconUri);
                if (resourceInfo?.Stream != null)
                {
                    _trayIcon.Icon = new System.Drawing.Icon(resourceInfo.Stream);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load tray icon: {ex.Message}");
            // Use system default icon as fallback
            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        // Left-click toggles window visibility
        _trayIcon.TrayLeftMouseUp += (_, _) => _viewModel?.ToggleWindowCommand.Execute(null);

        // Ensure the icon is created immediately in notification area.
        _trayIcon.ForceCreate();
    }

    /// <summary>
    /// Creates the tray icon's right-click context menu.
    /// </summary>
    private ContextMenu CreateTrayContextMenu()
    {
        var menu = new ContextMenu();

        // Show/Hide window
        var showItem = new MenuItem
        {
            Header = "Show Window",
            FontWeight = FontWeights.SemiBold
        };
        showItem.Click += (_, _) => _viewModel?.ShowWindowCommand.Execute(null);

        // Separator
        var sep1 = new Separator();

        // Bluetooth Settings
        var btSettingsItem = new MenuItem
        {
            Header = "Bluetooth Settings",
            Icon = new TextBlock
            {
                Text = "\uE713",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 14
            }
        };
        btSettingsItem.Click += (_, _) => _viewModel?.OpenBluetoothSettingsCommand.Execute(null);

        // Refresh devices
        var refreshItem = new MenuItem
        {
            Header = "Refresh Devices",
            Icon = new TextBlock
            {
                Text = "\uE72C",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 14
            }
        };
        refreshItem.Click += (_, _) => _viewModel?.RefreshDevicesCommand.Execute(null);

        var sep2 = new Separator();

        // Exit
        var exitItem = new MenuItem
        {
            Header = "Exit",
            Icon = new TextBlock
            {
                Text = "\uE8BB",
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 14
            }
        };
        exitItem.Click += (_, _) => _viewModel?.ExitCommand.Execute(null);

        menu.Items.Add(showItem);
        menu.Items.Add(sep1);
        menu.Items.Add(btSettingsItem);
        menu.Items.Add(refreshItem);
        menu.Items.Add(sep2);
        menu.Items.Add(exitItem);

        return menu;
    }

    private void OnShowWindowRequested(object? sender, EventArgs e)
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.Activate();

            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
        }
    }

    private void OnHideWindowRequested(object? sender, EventArgs e)
    {
        _mainWindow?.Hide();
    }

    private void OnExitRequested(object? sender, EventArgs e)
    {
        PerformShutdown();
    }

    /// <summary>
    /// Performs a clean shutdown: disposes services, removes tray icon, and exits.
    /// </summary>
    private void PerformShutdown()
    {
        _viewModel?.Dispose();
        _trayIcon?.Dispose();
        _mainWindow?.ForceClose();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _trayIcon?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
