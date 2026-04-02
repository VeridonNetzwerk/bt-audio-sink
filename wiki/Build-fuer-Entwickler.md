# Build fuer Entwickler

## Voraussetzungen

- Windows 10 2004+ oder Windows 11
- Visual Studio 2022 (17.8+) **oder** .NET 8 SDK
- Windows 10 SDK 10.0.19041+ (bei VS-Setup)
- Optional fuer MSI-Paketierung: WiX Toolset v4

## 1. Source holen

```powershell
git clone https://github.com/VeridonNetzwerk/bt-audio-sink.git
cd bt-audio-sink
```

## 2. Icon generieren

```powershell
powershell -ExecutionPolicy Bypass -File scripts/generate-icon.ps1
```

Hinweis: Dieser Schritt ist optional. Falls `scripts/generate-icon.ps1` im Checkout nicht vorhanden ist, kann er uebersprungen werden.

## 3. Restore

```powershell
dotnet restore src/BtAudioSink/BtAudioSink.csproj
```

## 4. Build

```powershell
dotnet build src/BtAudioSink/BtAudioSink.csproj -c Release
```

## 5. Starten

```powershell
dotnet run --project src/BtAudioSink/BtAudioSink.csproj -c Release
```

## 6. Publish (Self-contained)

```powershell
dotnet publish src/BtAudioSink/BtAudioSink.csproj -c Release -r win-x64 --self-contained -o publish/x64
```

## 7. Publish (Framework-dependent)

```powershell
dotnet publish src/BtAudioSink/BtAudioSink.csproj -c Release -r win-x64 --no-self-contained -o publish/x64-fd
```

## 8. MSI Installer bauen

```powershell
dotnet build installer/BtAudioSink.Installer.wixproj -c Release -p:Platform=x64
```

Ergebnis: `installer/bin/x64/Release/BtAudioSink.Installer.msi`

## Wichtige Hinweise

- Die App ist GUI-basiert (WPF). In CI wird meist nur gebaut/publiziert, nicht interaktiv gestartet.
- Falls `dotnet` nicht im PATH ist, ueber den absoluten Pfad starten:

```powershell
"C:\Program Files\dotnet\dotnet.exe" build src/BtAudioSink/BtAudioSink.csproj -c Release
```
