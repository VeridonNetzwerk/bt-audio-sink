using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using BtAudioSink.Platform;
using BtAudioSink.ViewModels;

namespace BtAudioSink.Views;

/// <summary>
/// Code-behind for the main window. Handles window lifecycle, Mica backdrop on Win11,
/// and minimizing to tray instead of closing.
/// </summary>
public partial class MainWindow : Window
{
    private const int WmHotkey = 0x0312;
    private const uint ModNorepeat = 0x4000;
    private const uint VkMediaNextTrack = 0xB0;
    private const uint VkMediaPrevTrack = 0xB1;
    private const uint VkMediaPlayPause = 0xB3;

    private const int HotkeyIdPlayPause = 1;
    private const int HotkeyIdNext = 2;
    private const int HotkeyIdPrevious = 3;

    private bool _forceClose;
    private HwndSource? _hwndSource;

    public MainWindow()
    {
        InitializeComponent();

        // Apply window style from current theme
        var windowStyle = TryFindResource("AppWindowStyle") as Style;
        if (windowStyle != null)
        {
            Style = windowStyle;
        }

        SourceInitialized += OnSourceInitialized;
    }

    /// <summary>
    /// Called when the native window handle is created.
    /// Applies Windows 11 Mica backdrop if supported.
    /// </summary>
    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        _hwndSource?.AddHook(WndProc);
        RegisterMediaHotkeys();

        if (OsDetector.IsWindows11)
        {
            ApplyMicaBackdrop();
        }
    }

    private void RegisterMediaHotkeys()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        RegisterSingleHotkey(hwnd, HotkeyIdPlayPause, VkMediaPlayPause);
        RegisterSingleHotkey(hwnd, HotkeyIdNext, VkMediaNextTrack);
        RegisterSingleHotkey(hwnd, HotkeyIdPrevious, VkMediaPrevTrack);
    }

    private static void RegisterSingleHotkey(IntPtr hwnd, int id, uint virtualKey)
    {
        if (!RegisterHotKey(hwnd, id, ModNorepeat, virtualKey))
        {
            Debug.WriteLine($"RegisterHotKey failed for id={id}, vk=0x{virtualKey:X2}, error={Marshal.GetLastWin32Error()}");
        }
    }

    private void UnregisterMediaHotkeys()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        UnregisterHotKey(hwnd, HotkeyIdPlayPause);
        UnregisterHotKey(hwnd, HotkeyIdNext);
        UnregisterHotKey(hwnd, HotkeyIdPrevious);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey || DataContext is not MainViewModel vm)
        {
            return IntPtr.Zero;
        }

        switch (wParam.ToInt32())
        {
            case HotkeyIdPlayPause:
                vm.PlayPauseCommand.Execute(null);
                handled = true;
                break;

            case HotkeyIdNext:
                vm.NextTrackCommand.Execute(null);
                handled = true;
                break;

            case HotkeyIdPrevious:
                vm.PreviousTrackCommand.Execute(null);
                handled = true;
                break;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Applies the Mica backdrop effect on Windows 11.
    /// </summary>
    private void ApplyMicaBackdrop()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        // Set dark mode based on system theme
        bool isLight = NativeInterop.IsLightTheme(useAppsTheme: true);
        NativeInterop.SetImmersiveDarkMode(hwnd, !isLight);

        // Extend frame for composition
        NativeInterop.ExtendFrameIntoClientArea(hwnd);

        // Enable Mica
        if (NativeInterop.TryEnableMica(hwnd))
        {
            // Keep window background in sync with current app mode when Mica is active.
            if (isLight)
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(200, 232, 232, 232));
            }
            else
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(200, 31, 31, 31));
            }
        }
    }

    /// <summary>
    /// Applies dark/light mode DWM attributes when Windows app theme changes.
    /// </summary>
    public void ApplySystemThemeMode(bool isLight)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        if (OsDetector.IsWindows11)
        {
            NativeInterop.SetImmersiveDarkMode(hwnd, !isLight);
            NativeInterop.ExtendFrameIntoClientArea(hwnd);

            if (!isLight)
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(200, 31, 31, 31));
            }
            else
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(200, 232, 232, 232));
            }
        }
    }

    /// <summary>
    /// Override closing to minimize to tray instead of exiting.
    /// The app exits only when Exit is chosen from the tray menu.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_forceClose)
        {
            e.Cancel = true;
            Hide();

            if (DataContext is MainViewModel vm)
            {
                vm.IsWindowVisible = false;
            }

            return;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        UnregisterMediaHotkeys();

        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }

        base.OnClosed(e);
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);

        // Re-apply mode attributes when restored from tray to avoid stale glass/background colors.
        ApplySystemThemeMode(NativeInterop.IsLightTheme(useAppsTheme: true));
    }

    /// <summary>
    /// Forces the window to close (bypasses minimize-to-tray behavior).
    /// Called when the application is actually exiting.
    /// </summary>
    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
