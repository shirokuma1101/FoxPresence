namespace FirefoxDiscordPresence.Tray.Services;
public sealed class FileLogger
{
    private readonly object _gate = new();
    private readonly string _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FirefoxDiscordPresence", "logs");
    public void Info(string message) => Write("INFO", message, null);
    public void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);
    private void Write(string level, string message, Exception? ex)
    {
        try { lock (_gate) { Directory.CreateDirectory(_directory); File.AppendAllText(Path.Combine(_directory, $"{DateTime.Now:yyyy-MM-dd}.log"), $"{DateTimeOffset.Now:O} [{level}] {message}{(ex is null ? "" : $" | {ex}")}{Environment.NewLine}"); } }
        catch { /* Logging must never crash the tray. */ }
    }
}
