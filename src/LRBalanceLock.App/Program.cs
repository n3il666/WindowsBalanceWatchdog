namespace LRBalanceLock.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var log = new LoggingService();
        Application.ThreadException += (_, e) => log.Error(e.Exception, "Unhandled UI thread exception");
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex) log.Error(ex, "Unhandled application exception");
        };
        log.Info("App start");
        var settingsService = new SettingsService(log);
        var settings = settingsService.Load();
        var devices = new AudioDeviceService();
        var balance = new BalanceLockService(devices, log, settings);
        var startup = new StartupService(log);
        settings.StartWithWindows = startup.IsEnabled();
        var startMinimized = args.Any(a => string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));
        using var form = new MainForm(settings, settingsService, devices, balance, startup, startMinimized);
        Application.Run(form);
        devices.Dispose();
        log.Info("App exit");
    }
}
