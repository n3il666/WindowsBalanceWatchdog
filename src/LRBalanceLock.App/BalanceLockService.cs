using LRBalanceLock.App.Models;
using NAudio.CoreAudioApi;

namespace LRBalanceLock.App;

public sealed class BalanceLockService : IDisposable
{
    private readonly AudioDeviceService _devices;
    private readonly LoggingService _log;
    private readonly object _sync = new();
    private AppSettings _settings;
    private MMDevice? _watchedDevice;
    private const float Tolerance = 0.005f;

    public event Action<BalanceStatus>? StatusChanged;
    public BalanceStatus CurrentStatus { get; private set; } = new("Disabled", null, null, null, null);

    public BalanceLockService(AudioDeviceService devices, LoggingService log, AppSettings settings)
    {
        _devices = devices;
        _log = log;
        _settings = settings;
    }

    public void UpdateSettings(AppSettings settings)
    {
        lock (_sync)
        {
            _settings = settings;
            ResetWatchedDevice();
            BalanceNow();
        }
    }

    public void Start()
    {
        lock (_sync)
        {
            BalanceNow();
        }
    }

    private void BalanceNow()
    {
        try { Tick(); }
        catch (Exception ex)
        {
            _log.Error(ex, "Audio device temporarily unavailable");
            Publish(new BalanceStatus("Audio device temporarily unavailable", null, null, CurrentStatus.LastCorrection, null));
        }
    }

    private void Tick()
    {
        if (!_settings.BalanceLockEnabled)
        {
            ResetWatchedDevice();
            Publish(CurrentStatus with { State = "Disabled" });
            return;
        }

        var device = GetWatchedDevice();
        if (device is null)
        {
            Publish(new BalanceStatus("Waiting for device...", null, null, CurrentStatus.LastCorrection, _settings.SelectedDeviceFriendlyName));
            return;
        }

        var (left, right, master, channels) = AudioDeviceService.ReadLevels(device);
        if (channels < 2)
        {
            Publish(new BalanceStatus("This device does not expose separate left/right channel controls.", null, null, CurrentStatus.LastCorrection, device.FriendlyName));
            return;
        }

        var state = "Balanced";
        var lastCorrection = CurrentStatus.LastCorrection;
        if (Math.Abs(left - right) > Tolerance)
        {
            AudioDeviceService.SetLeftRightToMaster(device);
            lastCorrection = DateTimeOffset.Now;
            state = "Correcting";
            left = right = master;
            _log.Info($"Corrected {device.FriendlyName}: L/R set to {master:P0}");
        }

        Publish(new BalanceStatus(state, left, right, lastCorrection, device.FriendlyName));
    }

    private MMDevice? GetWatchedDevice()
    {
        if (_watchedDevice is not null) return _watchedDevice;

        _watchedDevice = _devices.ResolveDevice(_settings);
        if (_watchedDevice is not null)
        {
            _watchedDevice.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
        }

        return _watchedDevice;
    }

    private void OnVolumeNotification(AudioVolumeNotificationData data)
    {
        lock (_sync)
        {
            BalanceNow();
        }
    }

    private void ResetWatchedDevice()
    {
        if (_watchedDevice is null) return;
        _watchedDevice.AudioEndpointVolume.OnVolumeNotification -= OnVolumeNotification;
        _watchedDevice.Dispose();
        _watchedDevice = null;
    }

    private void Publish(BalanceStatus status)
    {
        CurrentStatus = status;
        StatusChanged?.Invoke(status);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            ResetWatchedDevice();
        }
    }
}
