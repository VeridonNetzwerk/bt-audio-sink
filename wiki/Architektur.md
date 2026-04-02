# Architektur

## Ziel

BT Audio Sink trennt klar zwischen:

- **Backend-Logik** (Bluetooth, Audio, Media, Settings)
- **UI-Darstellung** (Win10/Win11 Theme)

So bleibt die Funktionalitaet identisch, waehrend das Design je nach Windows-Version wechselt.

## Technologie-Stack

- C# / .NET 8.0 mit WPF
- Windows Runtime APIs: `AudioPlaybackConnection`, `DeviceInformation`, `GlobalSystemMediaTransportControlsSessionManager`
- CommunityToolkit.Mvvm fuer MVVM-Struktur
- H.NotifyIcon.Wpf fuer System-Tray-Integration
- WiX Toolset v4 fuer den MSI-Installer

## Adaptive UI

Die App erkennt die Windows-Version zur Laufzeit und laedt das passende Theme.

| Windows Version | Theme | Merkmale |
|---|---|---|
| Windows 10 (2004+) | Win10Theme | Klassische WPF-Optik, flache Controls |
| Windows 11 | Win11Theme | Abgerundete Ecken, Mica, Fluent-Style, Segoe UI Variable |

Die View- und ViewModel-Logik bleibt gleich. Nur Styles und Ressourcen unterscheiden sich je nach Betriebssystem.

## Schichten

## 1) Platform

- `OsDetector` erkennt Build-Nummer und klassifiziert Win10/Win11.
- `NativeInterop` kapselt DWM/Registry-Aufrufe (u. a. Mica, Theme-Abfrage).

## 2) Bluetooth

- `BluetoothDeviceService`
  - DeviceWatcher fuer A2DP-faehige Geraete
  - Live-Updates bei Add/Remove/Update
- `AudioPlaybackService`
  - Kernintegration ueber `AudioPlaybackConnection`
  - Connect/Disconnect
  - StateChanged handling
  - optionales Auto-Reconnect

## 3) Media

- `MediaControlService`
  - nutzt `GlobalSystemMediaTransportControlsSessionManager`
  - liest Titel/Artist/Status
  - sendet Play/Pause/Next/Previous

## 4) Settings

- `SettingsManager`
  - JSON Persistenz
  - Run-at-Startup Registry Handling

## 5) Presentation (MVVM)

- `MainViewModel`: zentrale Orchestrierung
- `DeviceViewModel`: Zustand pro Geraet
- `MainWindow`: bindet rein auf ViewModel

## Datenfluss (vereinfacht)

1. App startet
2. OS-Detection waehlt Theme
3. DeviceWatcher findet Bluetooth-Audio-Geraete
4. Nutzer klickt Connect
5. AudioPlaybackConnection wird geoeffnet
6. Media Session wird ueber GSMTC gelesen/gesteuert
7. Einstellungen und letzte Geraete werden gespeichert

## Fehlerbehandlung

- API-Aufrufe sind durch try/catch abgesichert
- Verbindungsfehler werden pro Geraet angezeigt
- Reconnect-Versuche mit Backoff statt Endlosschleife
