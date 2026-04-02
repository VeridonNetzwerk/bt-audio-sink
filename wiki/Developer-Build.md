# Developer Build

## Requirements

- Windows 10 2004+ or Windows 11
- Visual Studio 2022 (17.8+) **or** .NET 8 SDK
- Windows 10 SDK 10.0.19041+ when using Visual Studio
- Optional for MSI packaging: WiX Toolset v4

## 1. Clone the source

```powershell
git clone https://github.com/VeridonNetzwerk/bt-audio-sink.git
cd bt-audio-sink
```

## 2. Generate the icon

```powershell
powershell -ExecutionPolicy Bypass -File scripts/generate-icon.ps1
```

Note: This step is optional. If `scripts/generate-icon.ps1` is not present in the checkout, you can skip it.

## 3. Restore

```powershell
dotnet restore src/BtAudioSink/BtAudioSink.csproj
```

## 4. Build

```powershell
dotnet build src/BtAudioSink/BtAudioSink.csproj -c Release
```

## 5. Run

```powershell
dotnet run --project src/BtAudioSink/BtAudioSink.csproj -c Release
```

## 6. Publish (self-contained)

```powershell
dotnet publish src/BtAudioSink/BtAudioSink.csproj -c Release -r win-x64 --self-contained -o publish/x64
```

## 7. Publish (framework-dependent)

```powershell
dotnet publish src/BtAudioSink/BtAudioSink.csproj -c Release -r win-x64 --no-self-contained -o publish/x64-fd
```

## 8. Build the MSI installer

```powershell
dotnet build installer/BtAudioSink.Installer.wixproj -c Release -p:Platform=x64
```

Output: `installer/bin/x64/Release/BtAudioSink.Installer.msi`

## Important notes

- The app is GUI-based (WPF). In CI, it is usually built and published, not run interactively.
- If `dotnet` is not in `PATH`, use the full executable path:

```powershell
"C:\Program Files\dotnet\dotnet.exe" build src/BtAudioSink/BtAudioSink.csproj -c Release
```