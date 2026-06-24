using System.Text.Json;
using LRBalanceLock.App.Models;

namespace LRBalanceLock.App;

public sealed class SettingsService
{
    private readonly LoggingService _log;
    private readonly string _settingsPath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public SettingsService(LoggingService log)
    {
        _log = log;
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LRBalanceLock");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return new AppSettings();
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath)) ?? new AppSettings();
            settings.Normalize();
            return settings;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Settings were corrupt; safe defaults loaded");
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        settings.Normalize();
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
