# LR Balance Lock

LR Balance Lock is a tiny Windows tray app that keeps the selected playback device's left and right endpoint balance equal. It does **not** process audio, install drivers, change spatial sound, alter EQ, or route audio through a virtual device.

## What it does

- Lists active Windows playback devices.
- Lets you choose the default playback device or a specific device.
- Polls the selected endpoint and keeps the first two channel balance sliders equal.
- Uses the current endpoint master volume scalar when correcting imbalance.
- Runs in the system tray with a menu for opening, toggling balance lock, toggling startup, and quitting.
- Stores settings and logs locally per user.

## What it does not do

- No audio DSP, EQ, enhancement, resampling, routing, recording, telemetry, or network access.
- No virtual audio devices, drivers, APOs, or Windows services.
- No admin rights required.
- No changes to spatial sound, device effects, exclusive mode, app volume mixer values, default devices, sample rate, bit depth, microphone settings, or Bluetooth profiles.

## Quick start from a release build

1. Download or build `LRBalanceLock.exe` on Windows.
2. Double-click `LRBalanceLock.exe`.
3. If Windows SmartScreen warns about an unknown publisher, choose **More info** and then **Run anyway** only if you trust the build source.
4. Balance lock is off by default on first launch, so audio settings are not modified until you enable it.

## Use

1. Choose `Default playback device` or a specific playback device.
2. Turn on **Balance Lock**.
3. Close the window to keep the app running in the tray.
4. Use the tray icon to reopen, toggle lock, toggle start with Windows, or quit.

## Start with Windows

The **Start with Windows** toggle writes a current-user registry Run entry:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\LRBalanceLock
```

The entry launches the app with `--minimized` and does not require administrator rights.

## Settings and logs

- Settings: `%AppData%\LRBalanceLock\settings.json`
- Logs: `%AppData%\LRBalanceLock\logs\app.log`

Corrupt settings are ignored and safe defaults are loaded.

## Build and try locally on Windows

These steps are intended for a local Windows checkout after the PR is merged.

### Prerequisites

- Windows 10 or Windows 11.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- PowerShell.

Confirm the SDK is installed:

```powershell
dotnet --version
```

### Clone or update the repository

If you do not already have the repository locally:

```powershell
git clone <repository-url>
cd WindowsBalanceWatchdog
```

If you already have it locally after merging the PR:

```powershell
git checkout main
git pull
```

### Restore, build, and test

```powershell
dotnet restore
dotnet build LRBalanceLock.sln -c Release
dotnet test LRBalanceLock.sln -c Release
```

### Publish a self-contained `.exe`

This creates a Windows x64 build that can run on a machine without installing the .NET runtime separately:

```powershell
dotnet publish src/LRBalanceLock.App/LRBalanceLock.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\dist\LRBalanceLock-win-x64
```

The executable will be here:

```powershell
.\dist\LRBalanceLock-win-x64\LRBalanceLock.exe
```

### Try the published app

```powershell
.\dist\LRBalanceLock-win-x64\LRBalanceLock.exe
```

When testing for the first time:

1. Leave **Balance Lock** off and confirm the window opens.
2. Choose **Default playback device** or a specific playback device.
3. Turn on **Balance Lock**.
4. Change the Windows left/right balance for that playback device and confirm the app brings the first two channels back together.
5. Use the tray icon to reopen the window or quit the app.
