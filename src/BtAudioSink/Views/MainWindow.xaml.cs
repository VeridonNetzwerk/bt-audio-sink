using System.ComponentModel;
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
    private bool _forceClose;

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
        if (OsDetector.IsWindows11)
        {
            ApplyMicaBackdrop();
        }
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

    private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            DragMove();
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

}
