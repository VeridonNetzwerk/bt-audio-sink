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
        bool isLight = NativeInterop.IsLightTheme();
        NativeInterop.SetImmersiveDarkMode(hwnd, !isLight);

        // Extend frame for composition
        NativeInterop.ExtendFrameIntoClientArea(hwnd);

        // Enable Mica
        if (NativeInterop.TryEnableMica(hwnd))
        {
            // Make the WPF background semi-transparent so Mica shows through
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(200, 245, 245, 245));
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

    /// <summary>
    /// Forces the window to close (bypasses minimize-to-tray behavior).
    /// Called when the application is actually exiting.
    /// </summary>
    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }
}
