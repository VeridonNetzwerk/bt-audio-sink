# Architecture

## Goal

BT Audio Sink keeps a clear separation between:

- **Backend logic** (Bluetooth, audio, media, settings)
- **UI presentation** (Windows 10 / Windows 11 themes)

This keeps the functionality identical while adapting the visual design to the Windows version.

## Technology stack

- C# / .NET 8.0 with WPF
- Windows Runtime APIs: `AudioPlaybackConnection`, `DeviceInformation`
- CommunityToolkit.Mvvm for MVVM structure
- H.NotifyIcon.Wpf for system tray integration
- WiX Toolset v4 for the MSI installer

## Adaptive UI

The app detects the Windows version at runtime and loads the matching theme.

| Windows version | Theme | Characteristics |
|---|---|---|
| Windows 10 (2004+) | Win10Theme | Classic WPF appearance, flatter controls |
| Windows 11 | Win11Theme | Rounded corners, Mica, Fluent styling, Segoe UI Variable |

The view and view model logic stay the same. Only styles and resources differ between operating systems.

## Layers

## 1) Platform

- `OsDetector` detects the build number and classifies Windows 10 vs Windows 11.
- `NativeInterop` wraps DWM and registry calls such as Mica and theme detection.

## 2) Bluetooth

- `BluetoothDeviceService`
  - DeviceWatcher for A2DP-capable devices
  - Live updates for add/remove/update events
- `AudioPlaybackService`
  - Core integration through `AudioPlaybackConnection`
  - Connect / disconnect
  - State change handling
  - Optional auto reconnect

## 3) Settings

- `SettingsManager`
  - JSON persistence
  - Run-at-startup registry handling

## 4) Presentation (MVVM)

- `MainViewModel`: central orchestration
- `DeviceViewModel`: per-device state
- `MainWindow`: binds directly to the view model

## Data flow (simplified)

1. The app starts.
2. OS detection selects the theme.
3. DeviceWatcher discovers Bluetooth audio devices.
4. The user clicks Connect.
5. `AudioPlaybackConnection` is opened.
6. Settings and recently connected devices are saved.

## Error handling

- API calls are protected with try/catch blocks.
- Connection errors are shown per device.
- Reconnect attempts use backoff instead of a tight retry loop.