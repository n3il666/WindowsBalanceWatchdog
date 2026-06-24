<div align="center">
  <img src="assets/logo.svg" alt="L/R Balance Lock logo" width="128" height="128" />
  <h1>L/R Balance Lock</h1>
  <p><strong>A tiny Windows tray utility that keeps left and right playback balance centered.</strong></p>

  <p>
    <img alt="Platform: Windows" src="https://img.shields.io/badge/platform-Windows-2F6BFF" />
    <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8.0-6F42C1" />
    <img alt="License" src="https://img.shields.io/badge/license-MIT-0B8F6A" />
  </p>
</div>

---

## Why this exists

Some Windows audio devices occasionally drift into an uneven left/right endpoint balance. **L/R Balance Lock** is a small, local-first MVP that watches the selected playback endpoint and brings the first two channel sliders back together when you ask it to. Instead of polling on a timer, it corrects once when the app starts or settings change, then reacts to Windows endpoint volume-change notifications.

It does not process your audio stream, install drivers, create virtual devices, collect telemetry, or require administrator rights.

## Features

- 🎧 Lists active Windows playback devices.
- ⚖️ Locks the first two endpoint balance channels together at startup, app launch, device changes, and volume changes.
- 🖱️ Runs quietly in the system tray with quick toggles.
- 🚀 Optional per-user **Start with Windows** support.
- 🔒 Local settings and logs only; no telemetry or network access.
- 🧰 Safe defaults: balance locking is off on first launch.

## Non-goals

L/R Balance Lock intentionally avoids anything that would make a tiny utility risky or invasive:

- No audio DSP, EQ, enhancement, resampling, recording, telemetry, or network access.
- No virtual audio devices, drivers, APOs, or Windows services.
- No admin rights required.
- No changes to spatial sound, device effects, exclusive mode, app mixer values, default devices, sample rate, bit depth, microphone settings, or Bluetooth profiles.

## Quick start

1. Download or build `LRBalanceLock.exe` on Windows.
2. Double-click the executable.
3. If Windows SmartScreen warns about an unknown publisher, choose **More info** → **Run anyway** only if you trust the build source.
4. Choose **Default playback device** or a specific playback device.
5. Enable **Balance Lock**.
6. Close the window to keep the app running in the tray.

> Balance locking is disabled by default on first launch, so no audio setting is modified until you enable it.

## Using the tray app

The tray icon lets you:

- Reopen the main window.
- Toggle balance locking.
- Toggle start with Windows.
- See the selected device.
- Quit the app fully.

Closing the main window minimizes to the tray when **Minimize to tray instead of close** is enabled.

## Start with Windows

The **Start with Windows** toggle writes this current-user registry Run entry:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\LRBalanceLock
```

The entry launches the app with `--minimized` and does not require administrator rights.

## Settings and logs

| Item | Location |
| --- | --- |
| Settings | `%AppData%\LRBalanceLock\settings.json` |
| Logs | `%AppData%\LRBalanceLock\logs\app.log` |

Corrupt settings are ignored and safe defaults are loaded.

## Build from source

### Prerequisites

- Windows 10 or Windows 11.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- PowerShell.

Confirm the SDK is installed:

```powershell
dotnet --version
```

### Clone

```powershell
git clone <repository-url>
cd WindowsBalanceWatchdog
```

### Restore, build, and test

```powershell
dotnet restore
dotnet build LRBalanceLock.sln -c Release
dotnet test LRBalanceLock.sln -c Release
```

### Publish the release executable

The release build is intentionally self-contained so users can run `LRBalanceLock.exe` without installing the .NET Desktop Runtime first. Because this is a Windows Forms app, self-contained publishing must carry the .NET Windows desktop runtime with the app; that overhead cannot be reduced to the size of a native-only utility without rewriting the UI/runtime stack.

To keep the standalone executable as small and non-scary as practical, the project defaults to a compressed single-file publish, bundles native libraries for runtime self-extraction, disables ReadyToRun expansion, omits debug symbols, and avoids trimming because Windows Forms is not reliably trim-safe.

```powershell
dotnet publish src/LRBalanceLock.App/LRBalanceLock.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:EnableCompressionInSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:PublishReadyToRun=false `
  -o .\dist\LRBalanceLock-win-x64
```

The self-contained executable will be written to:

```powershell
.\dist\LRBalanceLock-win-x64\LRBalanceLock.exe
```

This configuration is intended to produce a release that can be distributed as just `LRBalanceLock.exe`. If the publish folder still contains additional files after publishing, treat that as a sign that one of the current dependencies or SDK targets could not be embedded safely; verify the executable from a clean folder before deleting any generated sidecar files.

For comparison during development only, a framework-dependent publish is much smaller but requires the target PC to already have the .NET 8 Desktop Runtime:

```powershell
dotnet publish src/LRBalanceLock.App/LRBalanceLock.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  /p:PublishSingleFile=false `
  -o .\dist\LRBalanceLock-win-x64-framework-dependent
```

## Manual release smoke test

Before publishing a release build:

1. Launch `LRBalanceLock.exe` and confirm the window opens fully without resizing.
2. Confirm the taskbar/window/tray icon is visible.
3. Leave **Balance Lock** off and choose a playback device.
4. Turn **Balance Lock** on.
5. Change the Windows left/right balance for that playback device and confirm the app restores equal values.
6. Close the window and confirm the tray icon remains available.
7. Reopen from the tray icon and then choose **Quit**.

## Privacy and safety

L/R Balance Lock is local-only. It stores settings and logs under your Windows user profile and does not send data anywhere.

## License

MIT. See [LICENSE](LICENSE).
