<p align="center">
  <img src="logo.jpg" alt="DualVoice Logo" width="140" style="border-radius: 28px; box-shadow: 0 10px 25px rgba(0,0,0,0.15);" />
</p>

<h1 align="center">DualVoice</h1>

<p align="center">
  <strong>Simultaneous Dual Bluetooth Audio Output & Volume Synchronizer for Windows</strong><br>
  <em>Windows üzerinde iki farklı Bluetooth kulaklığa aynı anda eş zamanlı ve senkronize ses dinletici.</em>
</p>

<p align="center">
  <a href="#license"><img src="https://img.shields.io/badge/License-MIT-green.svg" alt="License: MIT"></a>
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-blue.svg" alt="Platform: Windows">
  <img src="https://img.shields.io/badge/.NET%20Framework-4.0%2B-512BD4.svg" alt=".NET Framework 4.0+">
  <img src="https://img.shields.io/badge/Type-Portable%20Standalone%20EXE-orange.svg" alt="Portable Standalone EXE">
  <img src="https://img.shields.io/badge/Audio%20Engine-WASAPI%20Loopback-00C7B7.svg" alt="WASAPI Loopback">
</p>

---

## 📖 Overview / Genel Bakış

Windows natively routes system audio to only **one** default output device at a time. If you connect two pairs of Bluetooth headphones or earbuds, you are usually forced to pick only one of them.

**DualVoice** solves this problem cleanly and effortlessly. It captures the primary audio stream via low-latency **Windows Core Audio (WASAPI Loopback)** and mirrors it in real time to a second Bluetooth headphone or speaker.

DualVoice is distributed as a **single, portable, self-contained executable** with zero external dependencies, no virtual audio cable drivers, and no runtime installers required.

---

## ✨ Features / Özellikler

- 🎧 **Simultaneous Dual Audio (Çift Kulaklık Çıkışı):**
  Stream movies, music, games, or podcasts simultaneously to two separate Bluetooth headphones or USB audio devices.
- 🎨 **Playful & Tactile UI (Duolingo Tarzı Taktil Tasarım):**
  A clean, vibrant interface featuring tactile 3D pushable buttons, bold card boundaries, custom mascot visuals, and high-contrast readable typography.
- 🎚️ **Synchronized Volume Control (Sesleri Eşitle):**
  A dedicated 3D sync toggle keeps both headphones perfectly balanced. Changing master volume via Windows or keyboard multimedia keys adjusts both devices simultaneously.
- 🎛️ **Independent Volume Sliders (Bireysel Ses Ayarı):**
  Fine-tune the volume of each headphone independently whenever desired.
- ⚡ **Zero External Dependencies (Tek Başına Taşınabilir EXE):**
  Written directly in native C# with Windows Core Audio APIs (WASAPI). No Python, no Voicemeeter, no virtual drivers, no setup wizard needed.
- 🛡️ **Rock-Solid Background Stability (Kesintisiz Arka Plan):**
  Smooth 40ms non-blocking scalar polling ensures flawless background audio streaming without crashes when switching windows or playing fullscreen games.

---

## 🚀 Getting Started / Nasıl Kullanılır?

### Option 1: Run Pre-built Executable
1. Download **`DualVoice.exe`** from the repository or [Releases](../../releases).
2. Double-click **`DualVoice.exe`** to launch (no installation needed).
3. Click on the **1. Kulaklık** and **2. Kulaklık** `+` slot cards to choose your connected Bluetooth headphones.
4. Enjoy synchronized audio across both devices!

---

## 🛠️ Building from Source / Kaynaktan Derleme

DualVoice is built using Microsoft's built-in C# compiler (`csc.exe`), which is pre-installed on all modern Windows systems. You do not need to install Visual Studio or heavy SDKs.

### Quick Build (One-Click)
Simply run:
```cmd
build.bat
```

### Manual Build via Command Line
```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /optimize+ /target:winexe /out:DualVoice.exe /resource:logo.jpg,logo.jpg /lib:"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF" /r:PresentationFramework.dll,PresentationCore.dll,WindowsBase.dll,System.Xaml.dll DualVoice.cs
```

---

## 🔧 Architecture & How It Works

```
                        ┌────────────────────────┐
                        │   Windows Audio App    │
                        │ (Spotify, Chrome, etc) │
                        └───────────┬────────────┘
                                    │
                                    ▼
                     ┌──────────────────────────────┐
                     │    Device 1 (Primary Headset)│
                     │  Active Default WASAPI Output │
                     └──────────────┬───────────────┘
                                    │
                                    ▼ [WASAPI Loopback Capture]
                             DualVoice Engine
                       (Sample Rate / Buffer Mirror)
                                    │
                                    ▼ [WASAPI Render Client]
                     ┌──────────────────────────────┐
                     │   Device 2 (Secondary Headset)│
                     │  Synchronous Low-Latency Sink │
                     └──────────────────────────────┘
```

1. **Loopback Capture:** DualVoice initializes an `IAudioCaptureClient` in loopback mode on the primary audio endpoint.
2. **Real-time PCM Mirroring:** Audio packets are captured and rendered to the secondary endpoint's `IAudioRenderClient` with minimal buffer latency.
3. **Volume Polling:** An asynchronous UI timer polls `IAudioEndpointVolume.GetMasterVolumeLevelScalar` without hooking fragile unmanaged COM notifications, preventing focus-change access violations.

---

## 📋 System Requirements / Sistem Gereksinimleri

- **Operating System:** Windows 10 / Windows 11 (64-bit or 32-bit)
- **Framework:** .NET Framework 4.0 or higher (included by default in Windows 10/11)
- **Audio Hardware:** Any two audio output devices (Bluetooth headphones, AirPods, USB headsets, aux speakers)

---

## 📄 License

This project is licensed under the [MIT License](LICENSE). You are free to use, modify, and distribute this software.
