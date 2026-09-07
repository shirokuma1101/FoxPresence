namespace FirefoxDiscordPresence.Tray.Configuration;
public sealed class AppSettings
{
    public bool PresenceEnabled { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public string DiscordApplicationId { get; set; } = "";
    public int StaleTimeoutSeconds { get; set; } = 45;
}
