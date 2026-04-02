<div align="center">

# 📶 BT Audio Sink

**Bluetooth A2DP Audio Sink for Windows — Stream audio from your smartphone to your PC and control playback from either device.**

BT Audio Sink turns your Windows PC into a Bluetooth audio receiver (A2DP Sink). Pair your phone, connect through the app, and your phone's audio plays through your PC speakers. Media controls (play, pause, next, previous) work bidirectionally between both devices.

<p>
  <a href="https://github.com/VeridonNetzwerk/bt-audio-sink/blob/main/LICENSE">
    <img src="https://img.shields.io/github/license/VeridonNetzwerk/bt-audio-sink?style=flat-square" alt="License: MIT">
  </a>
  <a href="https://github.com/VeridonNetzwerk/bt-audio-sink/issues">
    <img src="https://img.shields.io/github/issues/VeridonNetzwerk/bt-audio-sink?style=flat-square" alt="Open Issues">
  </a>
  <a href="https://github.com/VeridonNetzwerk/bt-audio-sink/stargazers">
    <img src="https://img.shields.io/github/stars/VeridonNetzwerk/bt-audio-sink?style=flat-square" alt="Stars">
  </a>
  <img src="https://img.shields.io/badge/Windows%2010-2004%2B-blue" alt="Windows 10">
  <img src="https://img.shields.io/badge/Windows%2011-Supported-blue" alt="Windows 11">
  <img src="https://img.shields.io/badge/.NET-8.0-purple" alt=".NET 8">
</p>

</div>

---

## 🛠️ Requirements

| Component | Version | Notes |
|-----------|---------|-------|
| OS | Windows 10/11 | (On Windows 10 build 19041 or later) |
| .NET Runtime | .NET 8.0 Desktop Runtime | included in self-contained builds |
| Bluetooth | - | Bluetooth adapter with A2DP Sink support |

> **Note**: Your Bluetooth adapter must support A2DP Sink role. Most modern built-in and USB Bluetooth adapters support this. The feature was added to Windows in version 2004.

---

## 🚀 Quick Start

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

---

## 🙏 Attribution

This project builds on the work of:

| Project | Author |
|---------|--------|
| [AudioPlaybackConnector](https://github.com/ysc3839/AudioPlaybackConnector) | ysc3839 |
| [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) | HavenDV |

---

## 📖 Wiki

Full documentation is available in the **[GitHub Wiki](https://github.com/VeridonNetzwerk/bt-audio-sink/wiki)**:

| Page | Description |
|------|-------------|
| [Home](https://github.com/VeridonNetzwerk/bt-audio-sink/wiki/Home) | Overview and navigation |
| [Installation](https://github.com/VeridonNetzwerk/bt-audio-sink/wiki/Installation) | Step-by-step first-time setup |
| [Bedienung](https://github.com/VeridonNetzwerk/bt-audio-sink/wiki/Bedienung) | App usage and controls |
| [Architektur](https://github.com/VeridonNetzwerk/bt-audio-sink/wiki/Architektur) | Architecture and component overview |
| [Fehlerbehebung](https://github.com/VeridonNetzwerk/bt-audio-sink/wiki/Fehlerbehebung) | Common errors and fixes |
| [FAQ](https://github.com/VeridonNetzwerk/bt-audio-sink/wiki/FAQ) | Frequently asked questions |

---

## 🐛 Reporting Issues

Found a bug? Open an [**Issue**](https://github.com/VeridonNetzwerk/bt-audio-sink/issues/new/choose) and include:

- What you expected vs. what actually happened
- Which device you tried to connect
- Your Windows version and Bluetooth adapter
- Any relevant app output or screenshots

An issue template is pre-filled at `.github/ISSUE_TEMPLATE/bug_report.md`.

---

## 💖 Support

If you like this project, consider Donating: 

<a href="https://www.paypal.com/donate/?hosted_button_id=972P9WTWE7RBU">
  <img src="https://img.shields.io/badge/Donate-PayPal-0070ba?style=for-the-badge&logo=paypal&logoColor=white" alt="Donate via PayPal">
</a>

---

## 🤖 Built With AI

Parts of this project were created and refined with the assistance of AI tools.

---

<div align="center">
  <sub>MIT License · © 2026 VeridonNetzwerk</sub>
</div>