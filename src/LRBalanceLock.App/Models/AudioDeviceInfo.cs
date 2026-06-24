namespace LRBalanceLock.App.Models;

public sealed record AudioDeviceInfo(string Id, string FriendlyName, bool IsDefault)
{
    public override string ToString() => IsDefault ? $"Default playback device ({FriendlyName})" : FriendlyName;
}
