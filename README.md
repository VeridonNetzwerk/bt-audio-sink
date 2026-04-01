# BT Audio Sink

**Bluetooth A2DP Audio Sink for Windows** — Stream audio from your smartphone to your PC and control playback from either device.

![Windows 10](https://img.shields.io/badge/Windows%2010-2004%2B-blue)
![Windows 11](https://img.shields.io/badge/Windows%2011-Supported-blue)
![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/License-MIT-green)

## Overview

BT Audio Sink turns your Windows PC into a Bluetooth audio receiver (A2DP Sink). Pair your phone, connect through the app, and your phone's audio plays through your PC speakers. Media controls (play, pause, next, previous) work bidirectionally between both devices.

### Key Features

- **A2DP Sink**: Receive Bluetooth audio from phones and tablets
- **Bidirectional Media Controls**: Play, pause, skip tracks from either device via SMTC/AVRCP
- **Adaptive UI**: Automatically matches Windows 10 or Windows 11 design language
- **System Tray**: Runs quietly in the notification area
- **Auto-Reconnect**: Automatically reconnects to previously connected devices
- **Multi-Device**: Connect multiple Bluetooth audio sources simultaneously

## Requirements

| Requirement | Details |
|---|---|
| **OS** | Windows 10 version 2004 (build 19041) or later |
| **Bluetooth** | Bluetooth adapter with A2DP Sink support |
| **.NET Runtime** | .NET 8.0 Desktop Runtime (included in self-contained builds) |

> **Note**: Your Bluetooth adapter must support A2DP Sink role. Most modern built-in and USB Bluetooth adapters support this. The feature was added to Windows in version 2004.

## Installation

### Option A: MSI Installer
Download the latest MSI installer from [Releases](../../releases) and run it. The installer:
- Installs to `Program Files\BT Audio Sink`
- Creates a Start Menu shortcut
- Supports clean uninstallation

### Option B: Portable
Download the ZIP archive from [Releases](../../releases), extract to any folder, and run `BtAudioSink.exe`.

## Usage

1. **Pair your Bluetooth device** in Windows Settings → Bluetooth & devices
2. **Launch BT Audio Sink** — the app starts in the system tray
3. **Click the tray icon** to open the device picker
4. **Select your device** and click **Connect**
5. **Play music** on your phone — audio streams to your PC speakers
6. **Control playback** using the media controls in the app or on your phone

### System Tray

- **Left-click**: Show/hide the main window
- **Right-click**: Context menu (Bluetooth Settings, Refresh, Exit)

### Settings

| Setting | Description |
|---|---|
| Auto-reconnect on startup | Reconnects to previously connected devices when the app starts |
| Run at Windows startup | Launches the app automatically when you log in |
| Start minimized to tray | Starts the app hidden in the system tray |

## Architecture

```
src/BtAudioSink/
├── App.xaml(.cs)              # Application entry point, theme loading, tray icon
├── Bluetooth/
│   ├── AudioPlaybackService   # AudioPlaybackConnection management (A2DP Sink)
│   ├── BluetoothDeviceInfo     # Device data model
│   └── BluetoothDeviceService  # Device discovery via DeviceWatcher
├── Media/
│   └── MediaControlService     # GSMTC integration for bidirectional media controls
├── Platform/
│   ├── NativeInterop           # Win32 P/Invoke (DWM, theme detection)
│   └── OsDetector              # Windows version detection for adaptive UI
├── Settings/
│   ├── AppSettings             # Settings model
│   └── SettingsManager         # JSON persistence and registry (startup)
├── ViewModels/
│   ├── DeviceViewModel         # Per-device UI state and commands
│   └── MainViewModel           # Main application ViewModel
├── Views/
│   └── MainWindow              # WPF main window with adaptive layout
├── Themes/
│   ├── Win10Theme.xaml          # Windows 10 visual styles
│   └── Win11Theme.xaml          # Windows 11 Fluent styles (rounded corners, Mica)
├── Converters/
│   └── Converters.cs           # WPF value converters
└── Assets/
    └── app.ico                 # Application icon
```

### Technology

- **C# / .NET 8.0** with WPF
- **Windows Runtime APIs**: `AudioPlaybackConnection`, `DeviceInformation`, `GlobalSystemMediaTransportControlsSessionManager`
- **CommunityToolkit.Mvvm**: MVVM framework
- **H.NotifyIcon.Wpf**: System tray integration
- **WiX Toolset v4**: MSI installer

### Adaptive UI

The app detects the Windows version at runtime and loads the appropriate theme:

| Windows Version | Theme | Features |
|---|---|---|
| Windows 10 (2004+) | Win10Theme | Standard WPF styling, flat controls |
| Windows 11 | Win11Theme | Rounded corners, Mica backdrop, Fluent Design, Segoe UI Variable font |

The UI structure is shared — only the visual styles differ. No code duplication.

## Building from Source

### Prerequisites

1. **Visual Studio 2022** (17.8+) with:
   - .NET desktop development workload
   - Windows 10 SDK (10.0.19041.0 or later)

2. **Or** .NET 8 SDK:
   ```
   winget install Microsoft.DotNet.SDK.8
   ```

3. **(Optional)** WiX Toolset v4 .NET tool for building the MSI:
   ```
   dotnet tool install --global wix
   ```

### Build Steps

```powershell
# 1. Clone the repository
git clone https://github.com/your-username/bt-audio-sink.git
cd bt-audio-sink

# 2. Generate the application icon
powershell -ExecutionPolicy Bypass -File scripts/generate-icon.ps1

# 3. Restore and build
dotnet build src/BtAudioSink/BtAudioSink.csproj -c Release

# 4. Run
dotnet run --project src/BtAudioSink/BtAudioSink.csproj -c Release
```

### Publishing

#### Self-Contained (Recommended for distribution)
```powershell
dotnet publish src/BtAudioSink/BtAudioSink.csproj -c Release -r win-x64 --self-contained -o publish/x64
```

#### Framework-Dependent (Smaller, requires .NET 8 runtime)
```powershell
dotnet publish src/BtAudioSink/BtAudioSink.csproj -c Release -r win-x64 --no-self-contained -o publish/x64-fd
```

### Building the MSI Installer

```powershell
# Install WiX v4 .NET tool
dotnet tool install --global wix

# Build the installer (builds the app first, then packages it)
dotnet build installer/BtAudioSink.Installer.wixproj -c Release
```

The MSI file will be output to `installer/bin/Release/`.

## Troubleshooting

| Issue | Solution |
|---|---|
| No devices appear | Ensure the device is paired in Windows Bluetooth Settings first |
| "Unsupported OS" error | Upgrade to Windows 10 version 2004 or later |
| Connection fails | Restart Bluetooth on both devices; ensure the device isn't connected to another sink |
| No audio output | Check Windows Sound settings; the BT audio stream uses the default output device |
| Media controls don't work | Ensure the connected device supports AVRCP (most modern phones do) |

## Acknowledgments

Inspired by [AudioPlaybackConnector](https://github.com/ysc3839/AudioPlaybackConnector) by ysc3839.
The `AudioPlaybackConnection` API was added to Windows 10 version 2004 by Microsoft.

## License

This project is licensed under the [MIT License](LICENSE).
