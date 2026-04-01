using System.Runtime.InteropServices;

namespace BtAudioSink.Platform;

/// <summary>
/// Native Win32 interop declarations for DWM, Shell, and Registry APIs.
/// </summary>
public static class NativeInterop
{
    // DWM attributes
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_MICA_EFFECT = 1029;
    public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

    // System backdrop types (Windows 11 22H2+)
    public const int DWMSBT_DISABLE = 1;
    public const int DWMSBT_MAINWINDOW = 2; // Mica
    public const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic
    public const int DWMSBT_TABBEDWINDOW = 4; // Tabbed Mica

    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int value,
        int size);

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    public static extern int DwmExtendFrameIntoClientArea(
        IntPtr hwnd,
        ref MARGINS margins);

    // Registry for theme detection
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int RegGetValueW(
        IntPtr hKey,
        string lpSubKey,
        string lpValue,
        uint dwFlags,
        IntPtr pdwType,
        ref int pvData,
        ref int pcbData);

    public static readonly IntPtr HKEY_CURRENT_USER = new(-2147483647);
    public const uint RRF_RT_REG_DWORD = 0x00000010;

    /// <summary>
    /// Returns true if the system is using a light taskbar/system theme.
    /// </summary>
    public static bool IsLightTheme()
    {
        try
        {
            int value = 0;
            int size = 4;
            int result = RegGetValueW(
                HKEY_CURRENT_USER,
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "SystemUsesLightTheme",
                RRF_RT_REG_DWORD,
                IntPtr.Zero,
                ref value,
                ref size);

            return result == 0 && value != 0;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Enables Mica backdrop on a window (Windows 11 only).
    /// </summary>
    public static bool TryEnableMica(IntPtr hwnd)
    {
        if (!OsDetector.SupportsMicaBackdrop)
        {
            return false;
        }

        // Try Windows 11 22H2+ API first
        if (OsDetector.SupportsSystemBackdropType)
        {
            int backdropType = DWMSBT_MAINWINDOW;
            int hr = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
            if (hr == 0)
            {
                return true;
            }
        }

        // Fall back to Windows 11 21H2 API
        int micaValue = 1;
        return DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref micaValue, sizeof(int)) == 0;
    }

    /// <summary>
    /// Sets the immersive dark mode attribute on a window.
    /// </summary>
    public static void SetImmersiveDarkMode(IntPtr hwnd, bool enabled)
    {
        int value = enabled ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
    }

    /// <summary>
    /// Extends the window frame into the client area for composition effects.
    /// </summary>
    public static void ExtendFrameIntoClientArea(IntPtr hwnd)
    {
        var margins = new MARGINS { Left = -1, Top = -1, Right = -1, Bottom = -1 };
        DwmExtendFrameIntoClientArea(hwnd, ref margins);
    }
}
