using Microsoft.Win32;

namespace LRBalanceLock.App;

public sealed class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LRBalanceLock";
    private readonly LoggingService _log;

    public StartupService(LoggingService log) => _log = log;

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        return key?.GetValue(ValueName) is string value && value.Contains(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true) ?? Registry.CurrentUser.CreateSubKey(RunKey, true);
            if (enabled) key.SetValue(ValueName, $"\"{Application.ExecutablePath}\" --minimized");
            else key.DeleteValue(ValueName, false);
            _log.Info($"Start with Windows set to {enabled}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Could not change startup setting");
            throw;
        }
    }
}
