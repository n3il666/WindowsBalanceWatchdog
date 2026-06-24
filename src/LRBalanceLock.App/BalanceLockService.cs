using LRBalanceLock.App.Models;

namespace LRBalanceLock.App;

public sealed class BalanceLockService : IDisposable
{
    private readonly AudioDeviceService _devices;
    private readonly LoggingService _log;
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private AppSettings _settings;
    private const float Tolerance = 0.005f;

    public event Action<BalanceStatus>? StatusChanged;
    public BalanceStatus CurrentStatus { get; private set; } = new("Disabled", null, null, null, null);

    public BalanceLockService(AudioDeviceService devices, LoggingService log, AppSettings settings)
    {
        _devices = devices;
        _log = log;
        _settings = settings;
    }

    public void UpdateSettings(AppSettings settings) => _settings = settings;

    public void Start()
    {
        lock (_sync)
        {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => LoopAsync(_cts.Token));
        }
    }

    private async Task LoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try { Tick(); }
            catch (Exception ex)
            {
                _log.Error(ex, "Audio device temporarily unavailable");
                Publish(new BalanceStatus("Audio device temporarily unavailable", null, null, CurrentStatus.LastCorrection, null));
            }
            await Task.Delay(Math.Clamp(_settings.PollingIntervalMs, 250, 5000), token).ContinueWith(_ => { });
        }
    }

    private void Tick()
    {
        if (!_settings.BalanceLockEnabled)
        {
            Publish(CurrentStatus with { State = "Disabled" });
            return;
        }

        using var device = _devices.ResolveDevice(_settings);
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

    private void Publish(BalanceStatus status)
    {
        CurrentStatus = status;
        StatusChanged?.Invoke(status);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
