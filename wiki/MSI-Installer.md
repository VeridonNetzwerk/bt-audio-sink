# MSI Installer (WiX)

## Goal

The MSI setup installs BT Audio Sink cleanly on Windows systems and supports upgrade and uninstall scenarios.

## Technology

- WiX Toolset v4
- Project: `installer/BtAudioSink.Installer.wixproj`
- Main definition: `installer/Package.wxs`

## What the installer does

- Installs to `Program Files\\BT Audio Sink`
- Creates a Start Menu shortcut
- Optional desktop shortcut
- Upgrades existing versions through `MajorUpgrade`
- Supports uninstall

## Building the MSI

```powershell
# if not installed yet
dotnet tool install --global wix

# build the MSI
dotnet build installer/BtAudioSink.Installer.wixproj -c Release
```

## Output

The MSI is generated here:

- `installer/bin/Release/`

## Best practices for production use

- Digitally sign the EXE and MSI
- Keep versioning consistent
- Never change the `UpgradeCode`
- Before every release, test clean install, upgrade, and uninstall
