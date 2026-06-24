namespace LRBalanceLock.App;

public sealed class LoggingService
{
    private readonly string _logPath;
    private const long MaxBytes = 2 * 1024 * 1024;

    public LoggingService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LRBalanceLock", "logs");
        Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, "app.log");
    }

    public void Info(string message) => Write("INFO", message);
    public void Error(Exception ex, string message) => Write("ERROR", $"{message}: {ex}");

    private void Write(string level, string message)
    {
        try
        {
            RotateIfNeeded();
            File.AppendAllText(_logPath, $"{DateTimeOffset.Now:O} [{level}] {message}{Environment.NewLine}");
        }
        catch { }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(_logPath) || new FileInfo(_logPath).Length <= MaxBytes) return;
        File.WriteAllText(_logPath, $"{DateTimeOffset.Now:O} [INFO] Log truncated after reaching {MaxBytes} bytes.{Environment.NewLine}");
    }
}
