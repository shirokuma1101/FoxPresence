using System.Text.Json;
using FirefoxDiscordPresence.Tray.Configuration;
namespace FirefoxDiscordPresence.Tray.Services;
public sealed class SettingsStore(FileLogger logger)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public string PathName { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FirefoxDiscordPresence", "settings.json");
    public AppSettings Load()
    {
        try { return File.Exists(PathName) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(PathName), Options) ?? new() : CreateDefault(); }
        catch (Exception ex) { logger.Error("Settings load failed", ex); return new(); }
    }
    private AppSettings CreateDefault() { var settings = new AppSettings(); Save(settings); return settings; }
    public void Save(AppSettings settings)
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(PathName)!); File.WriteAllText(PathName, JsonSerializer.Serialize(settings, Options)); }
        catch (Exception ex) { logger.Error("Settings save failed", ex); }
    }
}
