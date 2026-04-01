# Einstellungen

## Auto-reconnect on startup

Wenn aktiviert, versucht BT Audio Sink beim Start die zuletzt verbundenen Geraete automatisch wieder zu verbinden.

## Run at Windows startup

- Legt einen Run-Eintrag in `HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Run` an.
- App startet beim Login automatisch.

## Start minimized to system tray

- App startet ohne sichtbares Hauptfenster.
- Zugriff ueber Tray-Icon.

## Speicherort der App-Einstellungen

Die Einstellungen werden in einer JSON-Datei im App-Verzeichnis gespeichert:

- `BtAudioSink.settings.json`

Enthaelt u. a.:

- AutoReconnect
- RunAtStartup
- StartMinimized
- LastConnectedDevices
