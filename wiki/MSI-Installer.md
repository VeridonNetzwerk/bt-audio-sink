# MSI-Installer (WiX)

## Ziel

Das MSI-Setup installiert BT Audio Sink sauber auf Windows-Systemen und unterstuetzt Upgrade/Uninstall.

## Technologie

- WiX Toolset v4
- Projekt: `installer/BtAudioSink.Installer.wixproj`
- Hauptdefinition: `installer/Package.wxs`

## Was der Installer macht

- Installation nach `Program Files\\BT Audio Sink`
- Startmenue-Verknuepfung
- Optionale Desktop-Verknuepfung
- Upgrade bestehender Versionen (MajorUpgrade)
- Uninstall-Unterstuetzung

## Build des MSI

```powershell
# falls noch nicht installiert
dotnet tool install --global wix

# MSI bauen
dotnet build installer/BtAudioSink.Installer.wixproj -c Release
```

## Ausgabe

Die MSI liegt danach unter:

- `installer/bin/Release/`

## Best Practices fuer Produktivbetrieb

- EXE und MSI digital signieren
- Versionierung strikt beibehalten
- UpgradeCode nie aendern
- Vor Release immer frische Install + Upgrade + Uninstall testen
