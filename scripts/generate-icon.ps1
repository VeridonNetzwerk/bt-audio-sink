<#
.SYNOPSIS
    Generates the application icon (app.ico) for BT Audio Sink.

.DESCRIPTION
    Creates a simple Bluetooth-themed icon using System.Drawing.
    The icon features a blue circle with a white Bluetooth symbol.
    Run this script once during initial project setup.

.EXAMPLE
    .\generate-icon.ps1
#>

Add-Type -AssemblyName System.Drawing

$outputPath = Join-Path $PSScriptRoot "..\src\BtAudioSink\Assets\app.ico"
$outputDir = Split-Path $outputPath

if (-not (Test-Path $outputDir)) {
    New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
}

$size = 32
$bmp = New-Object System.Drawing.Bitmap $size, $size
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.Clear([System.Drawing.Color]::Transparent)

# Draw blue circle background
$brushBg = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 0, 120, 215))
$g.FillEllipse($brushBg, 1, 1, 30, 30)

# Draw white Bluetooth symbol
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 2.0)
$pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

# Bluetooth rune geometry (centered at 16,16)
# Vertical line
$g.DrawLine($pen, 16, 7, 16, 25)
# Upper right arrow
$g.DrawLine($pen, 16, 7, 22, 13)
# Upper right to lower left diagonal
$g.DrawLine($pen, 22, 13, 10, 21)
# Lower right arrow
$g.DrawLine($pen, 16, 25, 22, 19)
# Lower right to upper left diagonal
$g.DrawLine($pen, 22, 19, 10, 11)

$g.Dispose()
$pen.Dispose()
$brushBg.Dispose()

# Convert bitmap to icon and save
$hIcon = $bmp.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)

$stream = [System.IO.File]::Create($outputPath)
$icon.Save($stream)
$stream.Close()
$stream.Dispose()
$icon.Dispose()

# Clean up GDI handle
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class IconHelper {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
"@
[void][IconHelper]::DestroyIcon($hIcon)
$bmp.Dispose()

Write-Host "Icon generated successfully at: $outputPath" -ForegroundColor Green
