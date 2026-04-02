using System.Runtime.InteropServices;

namespace BtAudioSink.Platform;

/// <summary>
/// Native Win32 interop declarations for DWM, Shell, and Registry APIs.
/// </summary>
public static class NativeInterop
{
    public const byte VK_MEDIA_NEXT_TRACK = 0xB0;
    public const byte VK_MEDIA_PREV_TRACK = 0xB1;
    public const byte VK_MEDIA_STOP = 0xB2;
    public const byte VK_MEDIA_PLAY_PAUSE = 0xB3;

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

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
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

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
    /// Returns true if the selected Windows theme scope is light.
    /// </summary>
    /// <param name="useAppsTheme">
    /// true = AppsUseLightTheme (recommended for app UI),
    /// false = SystemUsesLightTheme (taskbar/system surfaces).
    /// </param>
    public static bool IsLightTheme(bool useAppsTheme = true)
    {
        try
        {
            int value = 0;
            int size = 4;
            int result = RegGetValueW(
                HKEY_CURRENT_USER,
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                useAppsTheme ? "AppsUseLightTheme" : "SystemUsesLightTheme",
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

    /// <summary>
    /// Sends a multimedia key (play/pause/next/previous) to the system.
    /// This provides an AVRCP-like fallback path when GSMTC has no active session.
    /// </summary>
    public static void SendMediaKey(byte virtualKey)
    {
        var inputs = new INPUT[]
        {
            new()
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKey,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            },
            new()
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKey,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            }
        };

        _ = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
