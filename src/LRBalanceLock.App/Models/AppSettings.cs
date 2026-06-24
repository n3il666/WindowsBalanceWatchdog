namespace LRBalanceLock.App.Models;

public sealed class AppSettings
{
    public bool BalanceLockEnabled { get; set; } = false;
    public string SelectedDeviceMode { get; set; } = "default";
    public string? SelectedDeviceId { get; set; }
    public string? SelectedDeviceFriendlyName { get; set; }
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public int PollingIntervalMs { get; set; } = 1000;

    public void Normalize()
    {
        if (SelectedDeviceMode is not "default" and not "specific") SelectedDeviceMode = "default";
        PollingIntervalMs = Math.Clamp(PollingIntervalMs, 250, 5000);
    }
}
