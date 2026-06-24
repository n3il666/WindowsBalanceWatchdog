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

## Run

Build or publish the app on Windows, then run `LRBalanceLock.exe`. Balance lock is off by default on first launch so audio settings are not modified until you enable it.

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

## Build from source

Requires the .NET 8 SDK on Windows.

```powershell
dotnet restore
dotnet build -c Release
```

Publish a portable self-contained Windows x64 build:

```powershell
dotnet publish src/LRBalanceLock.App/LRBalanceLock.App.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o dist
```

Run tests:

```powershell
dotnet test
```
