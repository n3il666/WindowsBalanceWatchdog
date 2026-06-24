using LRBalanceLock.App.Models;
using NAudio.CoreAudioApi;

namespace LRBalanceLock.App;

public sealed class AudioDeviceService : IDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();

    public IReadOnlyList<AudioDeviceInfo> ListPlaybackDevices()
    {
        var defaultId = TryGetDefaultDevice()?.ID;
        return _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(d => new AudioDeviceInfo(d.ID, d.FriendlyName, d.ID == defaultId))
            .OrderByDescending(d => d.IsDefault)
            .ThenBy(d => d.FriendlyName)
            .ToList();
    }

    public MMDevice? ResolveDevice(AppSettings settings)
    {
        if (settings.SelectedDeviceMode == "specific" && !string.IsNullOrWhiteSpace(settings.SelectedDeviceId))
        {
            return _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .FirstOrDefault(d => d.ID == settings.SelectedDeviceId);
        }

        return TryGetDefaultDevice();
    }

    public static (float Left, float Right, float Master, int Channels) ReadLevels(MMDevice device)
    {
        var endpoint = device.AudioEndpointVolume;
        var channels = endpoint.Channels.Count;
        if (channels < 2) return (0, 0, endpoint.MasterVolumeLevelScalar, channels);
        return (endpoint.Channels[0].VolumeLevelScalar, endpoint.Channels[1].VolumeLevelScalar, endpoint.MasterVolumeLevelScalar, channels);
    }

    public static void SetLeftRightToMaster(MMDevice device)
    {
        var endpoint = device.AudioEndpointVolume;
        var master = endpoint.MasterVolumeLevelScalar;
        endpoint.Channels[0].VolumeLevelScalar = master;
        endpoint.Channels[1].VolumeLevelScalar = master;
    }

    private MMDevice? TryGetDefaultDevice()
    {
        try { return _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia); }
        catch { return null; }
    }

    public void Dispose() => _enumerator.Dispose();
}
