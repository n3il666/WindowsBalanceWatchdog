# Windows L/R Balance Lock MVP — Design Spec for Codex

## 1. Product summary

Build a native Windows desktop `.exe` that keeps a selected headset or playback device's left/right channel balance equal, without changing any other audio behavior.

The app should run as a lightweight tray utility. The user should be able to install or launch it, pick an audio output device, enable balance locking with one click, optionally start it with Windows, and then forget about it.

The app must not process, route, resample, enhance, equalize, virtualize, or otherwise modify the audio signal. It should only observe and correct Windows endpoint channel balance values for the selected playback device.

## 2. MVP deliverable

The first deliverable is a standalone Windows `.exe` with a clean UI and tray app behavior.

The MVP must include:

- A user-facing Windows executable.
- A clean main window.
- System tray icon.
- Device picker for playback devices.
- Enable/disable balance-lock toggle.
- Start-with-Windows toggle.
- Minimize-to-tray behavior.
- Persistent settings.
- Safe default behavior.
- No audio processing pipeline changes.

The MVP should be suitable for everyday use by a non-technical user.

## 3. Primary user goal

The user likes the headset's existing behavior, including spatial audio, vendor effects, surround behavior, EQ, exclusive-mode behavior, and app-specific volume behavior.

The only thing the user wants is this:

> Keep the left and right Windows balance sliders equal at all times.

The app must preserve everything else.

## 4. Non-goals

Do not build any of the following for the MVP:

- Virtual audio device.
- Audio driver.
- Equalizer.
- Spatial audio replacement.
- Audio enhancement engine.
- Mixer/router like Voicemeeter.
- Systemwide DSP.
- Per-application audio control.
- Cloud account/login/sync.
- Telemetry.
- Auto-updater.
- Installer, unless trivial. A portable `.exe` is acceptable for MVP.

## 5. Platform target

Target Windows 10 and Windows 11.

Architecture:

- x64 Windows executable.
- Prefer .NET 8 or later with WinUI 3, WPF, or WinForms.
- The UI should feel native and simple.
- Avoid Electron unless specifically chosen for speed, because this app should be lightweight.

Recommended implementation stack:

- C# / .NET 8.
- WPF or WinForms for fastest reliable MVP.
- Windows Core Audio APIs via COM interop or a maintained wrapper such as NAudio.
- Registry or Startup folder entry for start-with-Windows.

## 6. Core behavior

When balance lock is enabled, the app should periodically inspect the selected playback device's endpoint channel volumes.

For a normal stereo endpoint:

- Read left channel scalar volume.
- Read right channel scalar volume.
- If they differ beyond a tiny tolerance, set both channels to the same scalar value.

The app should not set both channels to 100% by default. That would change perceived loudness.

The app should preserve the user's current master loudness as closely as possible.

Recommended correction rule:

- Let `L` be current left channel scalar volume.
- Let `R` be current right channel scalar volume.
- Let `M` be the endpoint master scalar volume if available.
- When imbalance is detected, set both channels to `M`.
- If `M` is unavailable or unreliable, set both channels to `max(L, R)` or use a configurable strategy.

For MVP, use this default:

```text
CorrectedLeft = MasterVolumeScalar
CorrectedRight = MasterVolumeScalar
```

Rationale: the Windows balance UI usually represents per-channel endpoint volumes relative to the device level. Matching both to the current endpoint master scalar is the least surprising behavior.

Important: correction must only use endpoint volume APIs. Do not insert an audio processing layer.

## 7. Correction strategy

Default behavior should avoid actively polling on a timer. When balance lock is enabled, the app should correct balance once at app startup/app launch, whenever the selected device/settings change, and whenever Windows reports an endpoint volume change for the watched device.

The app can keep the existing polling interval setting only for backward compatibility with older settings files, but normal runtime correction should be event-driven through endpoint volume notifications rather than a periodic loop.

The correction loop should be lightweight and should not noticeably affect CPU or battery.

If the device is disconnected:

- Do not crash.
- Show device status as unavailable.
- Keep the user's selected device setting.
- Reconnect automatically when the same device returns, if possible.

If the selected device cannot be found after app start:

- Keep balance lock enabled internally.
- Show a clear status: `Waiting for device...`.
- Resume correction when available.

## 8. Device selection behavior

The main window must include a playback device picker.

Requirements:

- List active playback/render devices.
- Display friendly device names.
- Include the current Windows default output device.
- Allow user to select a specific device.
- Persist selection across app restarts.
- Provide a refresh button or automatically refresh when device list changes.

Recommended options in picker:

1. `Default playback device`.
2. Specific devices, such as `Headphones (WH-1000XM...)`, `Speakers (Realtek Audio)`, etc.

Preferred default selection:

```text
Default playback device
```

Device identity persistence:

- Store stable device ID where possible.
- Also store friendly name for display/debugging.
- If stored ID disappears, show friendly name with unavailable status.

## 9. Main window UI

The main window should be clean and minimal.

Suggested layout:

```text
--------------------------------------------------
L/R Balance Lock
Keep your headset's left and right balance equal.

Device
[ Default playback device                         v ]
Status: Balanced / Correcting / Waiting for device / Disabled

[ Balance Lock: ON/OFF toggle ]
[ Start with Windows: ON/OFF toggle ]
[ Minimize to tray instead of close: ON/OFF toggle ]

Last correction: Never / 10:42:31 AM
Current channels: L 72% / R 72%

[ Minimize to Tray ] [ Quit ]
--------------------------------------------------
```

UI requirements:

- One obvious primary toggle for balance locking.
- Status text must be easy to understand.
- Current L/R values should be shown when available.
- The UI must make clear that it is not changing audio effects or processing.
- No scary technical terminology in the default view.

Suggested plain-language footer:

```text
This app only keeps Windows left/right channel balance equal. It does not change spatial sound, EQ, device effects, or audio routing.
```

## 10. Tray behavior

The app must include a system tray icon.

Tray icon requirements:

- Visible while app is running.
- Tooltip should include current state, e.g. `L/R Balance Lock: On`.
- Double-click opens main window.
- Right-click context menu.

Tray context menu:

```text
L/R Balance Lock
----------------
Balance Lock: On/Off
Device: <current device name>
Open
Start with Windows: On/Off
Quit
```

Close/minimize behavior:

- Clicking window close should minimize to tray by default, not exit.
- Provide an explicit Quit command in tray and main window.
- If the user disables `Minimize to tray instead of close`, close may exit.

## 11. Start with Windows

The app must support a start-with-Windows toggle.

Acceptable implementation options:

### Option A: Registry Run key

Use current-user registry key:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

Value name:

```text
LRBalanceLock
```

Value data:

```text
"C:\path\to\LRBalanceLock.exe" --minimized
```

### Option B: Startup folder shortcut

Create/remove shortcut in:

```text
shell:startup
```

For MVP, registry Run key is acceptable.

Requirements:

- Toggle on creates startup entry.
- Toggle off removes startup entry.
- App launches minimized to tray when started with `--minimized`.
- Startup behavior must not require admin rights.

## 12. Settings persistence

Persist settings per user.

Settings to store:

```json
{
  "balanceLockEnabled": true,
  "selectedDeviceMode": "default" | "specific",
  "selectedDeviceId": "...",
  "selectedDeviceFriendlyName": "...",
  "startWithWindows": false,
  "minimizeToTrayOnClose": true,
  "pollingIntervalMs": 1000
}
```

Recommended storage location:

```text
%AppData%\LRBalanceLock\settings.json
```

Requirements:

- Create folder if absent.
- Handle corrupt settings gracefully by resetting to safe defaults.
- Do not lose settings after app update/replacement.

Safe defaults:

```json
{
  "balanceLockEnabled": false,
  "selectedDeviceMode": "default",
  "startWithWindows": false,
  "minimizeToTrayOnClose": true,
  "pollingIntervalMs": 1000
}
```

Important: first launch should not silently modify audio until the user explicitly enables balance lock.

## 13. Audio API requirements

Use Windows Core Audio endpoint APIs.

Likely APIs/interfaces:

- `IMMDeviceEnumerator`
- `IMMDevice`
- `IAudioEndpointVolume`
- `IAudioEndpointVolumeCallback`, optional for later
- `EDataFlow.eRender`
- `ERole.eMultimedia`

If using NAudio, relevant classes may include:

- `MMDeviceEnumerator`
- `MMDevice`
- `AudioEndpointVolume`

Required capabilities:

- Enumerate active render endpoints.
- Get default render endpoint.
- Read master volume scalar.
- Read channel count.
- Read per-channel volume scalar.
- Set per-channel volume scalar.

Pseudo-code:

```csharp
while (appRunning)
{
    if (!settings.BalanceLockEnabled)
    {
        await Delay();
        continue;
    }

    var device = ResolveSelectedDevice(settings);
    if (device == null)
    {
        UpdateStatus("Waiting for device...");
        await Delay();
        continue;
    }

    var endpoint = device.AudioEndpointVolume;
    int channels = endpoint.Channels.Count;

    if (channels < 2)
    {
        UpdateStatus("Device has fewer than 2 channels");
        await Delay();
        continue;
    }

    float left = endpoint.Channels[0].VolumeLevelScalar;
    float right = endpoint.Channels[1].VolumeLevelScalar;
    float master = endpoint.MasterVolumeLevelScalar;

    if (Math.Abs(left - right) > 0.005f)
    {
        endpoint.Channels[0].VolumeLevelScalar = master;
        endpoint.Channels[1].VolumeLevelScalar = master;
        RecordCorrection(left, right, master);
    }

    UpdateStatus("Balanced");
    await Delay(settings.PollingIntervalMs);
}
```

Tolerance:

```text
0.005 scalar, approximately 0.5 percentage points
```

## 14. Channel handling

MVP only needs to enforce equality on the first two channels.

For stereo devices:

- Channel 0 = left.
- Channel 1 = right.

For multichannel devices:

- Only equalize channels 0 and 1.
- Do not modify center, subwoofer, surround, rear, or height channels.

Do not attempt to downmix, upmix, or reinterpret channels.

## 15. Audio behavior preservation requirements

The app must not change:

- Master volume except as indirectly represented by equalizing channels to current master scalar.
- Spatial sound setting.
- Audio enhancements setting.
- Exclusive mode setting.
- Default device.
- Default communications device.
- Sample rate.
- Bit depth.
- Speaker configuration.
- App volume mixer values.
- Vendor headset settings.
- Bluetooth profile.
- Microphone settings.
- Communications ducking setting.

The app must not install:

- Audio drivers.
- APOs.
- Virtual devices.
- Services requiring admin.

## 16. Error handling

Handle these cases gracefully:

- No playback devices.
- Selected device removed.
- Device access denied.
- Endpoint does not expose channel controls.
- Channel count less than 2.
- COM exception during read/write.
- App launched before audio service is ready.
- Corrupt settings file.
- Startup registry write failure.

User-facing error examples:

```text
Waiting for device...
This device does not expose separate left/right channel controls.
Could not change startup setting. Try running normally from your user account.
Audio device temporarily unavailable.
```

Do not show raw COM HRESULTs in the main UI. Put them in logs.

## 17. Logging

MVP should include lightweight local logging.

Log file location:

```text
%AppData%\LRBalanceLock\logs\app.log
```

Log events:

- App start/exit.
- Selected device changes.
- Balance lock enabled/disabled.
- Device unavailable/available.
- Corrections applied.
- Exceptions.
- Startup setting changes.

Log rotation:

- Keep simple for MVP.
- Cap log file around 1 to 5 MB.
- Truncate or rotate when exceeded.

No telemetry or network requests.

## 18. Accessibility and usability

Requirements:

- Keyboard navigable controls.
- Clear labels for screen readers where easy to implement.
- High-DPI aware.
- Works in light and dark Windows themes if framework supports it easily.
- Does not require admin rights.
- Does not require command-line use.

## 19. Security and privacy

Requirements:

- No network access.
- No telemetry.
- No collection of audio content.
- No microphone access.
- No recording.
- No admin requirement.
- Settings and logs remain local.

## 20. Packaging

MVP packaging options, in priority order:

1. Single portable `.exe` folder distribution.
2. Self-contained .NET publish folder.
3. Optional installer later.

For Codex implementation, produce:

```text
/dist/LRBalanceLock.exe
/README.md
/LICENSE, if applicable
```

If .NET self-contained publish is used, include all required runtime files in the dist folder.

Preferred publish command example:

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

If single-file publish causes issues with tray icon/resources, a folder-based release is acceptable.

## 21. Suggested project structure

```text
LRBalanceLock/
  src/
    LRBalanceLock.App/
      Program.cs
      MainWindow.xaml or MainForm.cs
      TrayIconService.cs
      AudioDeviceService.cs
      BalanceLockService.cs
      StartupService.cs
      SettingsService.cs
      LoggingService.cs
      Models/
        AppSettings.cs
        AudioDeviceInfo.cs
        BalanceStatus.cs
  tests/
    LRBalanceLock.Tests/
      SettingsServiceTests.cs
      DeviceSelectionTests.cs
      BalanceCorrectionLogicTests.cs
  README.md
```

## 22. Core services

### AudioDeviceService

Responsibilities:

- Enumerate playback devices.
- Resolve default playback device.
- Resolve stored specific device by ID.
- Return current channel volumes.
- Set first two channel volumes.

### BalanceLockService

Responsibilities:

- Subscribe to endpoint volume-change notifications for the watched device.
- Apply balance correction.
- Track current status.
- Expose events/status updates to UI.
- Avoid concurrent correction loops.

### SettingsService

Responsibilities:

- Load/save JSON settings.
- Provide safe defaults.
- Recover from corrupt settings.

### StartupService

Responsibilities:

- Read current startup state.
- Enable/disable start-with-Windows.
- Handle `--minimized` launch flag.

### TrayIconService

Responsibilities:

- Create tray icon.
- Build context menu.
- Open main window.
- Toggle balance lock.
- Quit app.

## 23. Acceptance criteria

The MVP is acceptable when all of the following are true:

1. User can run a Windows `.exe` without admin rights.
2. App opens a clean UI.
3. User can select `Default playback device` or a specific playback device.
4. User can enable balance lock with one toggle/click.
5. When L/R balance is manually changed in Windows, the app restores equality automatically.
6. The app does not disable or alter spatial sound.
7. The app does not change audio enhancements.
8. The app does not change default playback device.
9. The app does not install a virtual audio device or driver.
10. App continues running in the tray when main window is closed.
11. Tray menu can enable/disable balance lock.
12. Tray menu can reopen the window.
13. Tray menu can quit the app.
14. Start-with-Windows toggle works for the current user.
15. Settings persist after restart.
16. If the selected headset disconnects and reconnects, the app resumes correction.
17. If the selected device is missing, the app shows a non-crashing waiting state.
18. CPU usage is negligible during idle operation.
19. No network connections are made.
20. Logs are written locally for troubleshooting.

## 24. Manual test plan

### Test 1: Basic launch

- Launch `.exe`.
- Confirm main window appears.
- Confirm tray icon appears.
- Confirm no audio settings change before enabling lock.

Expected result:

- App is idle.
- Balance lock is off by default on first launch.

### Test 2: Enable lock on default device

- Select `Default playback device`.
- Enable balance lock.
- Manually open Windows sound balance settings.
- Change L/R to uneven values.

Expected result:

- Within about 1 second, L/R return to equal values.

### Test 3: Preserve spatial audio

- Enable Windows spatial sound or headset vendor spatial mode.
- Enable balance lock.
- Confirm spatial setting remains enabled.
- Play spatial audio test content.

Expected result:

- Spatial setting remains unchanged.
- App only equalizes L/R endpoint balance.

### Test 4: Tray behavior

- Close main window.
- Confirm app remains in tray.
- Reopen via tray double-click or menu.
- Toggle lock from tray menu.
- Quit from tray menu.

Expected result:

- All tray actions work.

### Test 5: Start with Windows

- Enable start-with-Windows.
- Confirm registry Run key or startup shortcut exists.
- Restart Windows or sign out/in.

Expected result:

- App starts minimized to tray.
- Previous settings are restored.

### Test 6: Device disconnect/reconnect

- Select a Bluetooth or USB headset.
- Enable balance lock.
- Disconnect headset.
- Reconnect headset.

Expected result:

- App shows waiting state while disconnected.
- App resumes correction after reconnect.

### Test 7: Missing channel controls

- Select a device that does not expose stereo channel controls, if available.

Expected result:

- App shows a friendly unsupported-device message.
- App does not crash.

## 25. README requirements

The repository should include a README explaining:

- What the app does.
- What it does not do.
- How to run it.
- How to enable balance lock.
- How to enable start with Windows.
- How to quit from tray.
- Where settings/logs live.
- How to build from source.

Suggested README summary:

```text
LR Balance Lock is a tiny Windows tray app that keeps the selected playback device's left and right endpoint balance equal. It does not process audio, install drivers, change spatial sound, alter EQ, or route audio through a virtual device.
```

## 26. Future enhancements, not MVP

Possible later improvements:

- Event-driven correction using `IAudioEndpointVolumeCallback`.
- Toast notification when repeated external changes are detected.
- Advanced setting for correction strategy: master, max, average, left-to-right, right-to-left.
- Hidden debug page.
- Signed installer.
- MSIX packaging.
- Auto-update.
- Multi-device profiles.
- Per-device correction intervals.
- Localization.

## 27. Implementation priority order

Build in this order:

1. Minimal app shell with main window and tray icon.
2. Settings load/save.
3. Audio device enumeration and picker.
4. Read/display L/R channel values.
5. Balance correction loop.
6. Enable/disable toggle.
7. Start-with-Windows toggle.
8. Device disconnect/reconnect handling.
9. Logging.
10. Packaging/publish.
11. Manual test pass.

## 28. Critical implementation notes

The success of this app depends on restraint.

Do not “fix” audio by changing the audio path. Do not add DSP. Do not install a virtual device. Do not disable enhancements. Do not change spatial sound. Do not change Bluetooth profiles.

The app should behave like a small guardrail around one Windows setting: left/right endpoint channel balance.

