using System.Runtime.InteropServices;

namespace BtAudioSink.Bluetooth;

/// <summary>
/// Sends media-key based transport commands that Windows routes to the active
/// Bluetooth audio source (AVRCP) when an AudioPlaybackConnection is active.
/// </summary>
public sealed class AvrcpCommandService
{
    private const uint InputKeyboard = 1;
    private const uint KeyeventfKeyup = 0x0002;

    private const ushort VkMediaNextTrack = 0xB0;
    private const ushort VkMediaPrevTrack = 0xB1;
    private const ushort VkMediaPlayPause = 0xB3;

    private readonly AudioPlaybackService _audioPlaybackService;

    public AvrcpCommandService(AudioPlaybackService audioPlaybackService)
    {
        _audioPlaybackService = audioPlaybackService;
    }

    public bool TryPlayPause() => TrySendMediaVirtualKey(VkMediaPlayPause);

    public bool TryNext() => TrySendMediaVirtualKey(VkMediaNextTrack);

    public bool TryPrevious() => TrySendMediaVirtualKey(VkMediaPrevTrack);

    private bool TrySendMediaVirtualKey(ushort virtualKey)
    {
        // Only send commands while at least one Bluetooth source is connected.
        if (_audioPlaybackService.ConnectedDeviceIds.Count == 0)
        {
            return false;
        }

        var inputs = new INPUT[]
        {
            new()
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKey,
                        dwFlags = 0
                    }
                }
            },
            new()
            {
                type = InputKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKey,
                        dwFlags = KeyeventfKeyup
                    }
                }
            }
        };

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        return sent == inputs.Length;
    }

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
        public nuint dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}
