# Troubleshooting

## Quick overview

| Problem | Solution |
|---|---|
| No device visible | Pair the device in Windows first, enable Bluetooth, then refresh in the app |
| "Unsupported OS" | Use at least Windows 10 version 2004 (build 19041) |
| Connection failed | Toggle Bluetooth off and on on both devices, then reconnect |
| No sound despite connection | Check the default output device in Windows sound settings |
| Media controls do not respond | Verify AVRCP support on the device and media app |

## No device in the list

Check the following:

1. Is the phone already paired with Windows?
2. Is Bluetooth enabled on the PC?
3. Does the adapter support A2DP Sink?
4. Click **Refresh Devices** in the app.

## Connection failed

Possible causes:

- The device is already connected to another audio sink
- The Bluetooth stack is stuck
- Distance or wireless interference

Try this:

1. Force a disconnect
2. Toggle Bluetooth off and on on both devices
3. Reconnect

If the target device is already connected to another audio sink, disconnect that session first.

## No sound despite connection

Check the following:

1. The default output device in Windows sound settings
2. Volume levels on the phone and the PC
3. Exclusive audio modes used by other software

Note: The stream is played through the current default output device in Windows.

## Media controls do not respond

- Make sure playback is actually active on the phone.
- Some apps or devices only support AVRCP partially.

## App does not start

- Windows 10 version 2004 or newer is required.
- If built from source, install the .NET 8 runtime or SDK.