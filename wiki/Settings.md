# Settings

## Auto-reconnect on startup

When enabled, BT Audio Sink tries to reconnect the previously connected devices when the app starts.

## Run at Windows startup

- Creates a Run entry in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- Starts the app automatically after login

## Start minimized to system tray

- Starts the app without showing the main window
- Access remains available through the tray icon

## Settings file location

Settings are stored in a JSON file in the application directory:

- `BtAudioSink.settings.json`

It contains values such as:

- AutoReconnect
- RunAtStartup
- StartMinimized
- LastConnectedDevices