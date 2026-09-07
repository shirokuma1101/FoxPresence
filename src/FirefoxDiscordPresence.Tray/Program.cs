using FirefoxDiscordPresence.Tray;
using FirefoxDiscordPresence.Tray.Configuration;
using FirefoxDiscordPresence.Tray.Services;

ApplicationConfiguration.Initialize();
using var mutex = new Mutex(true, "Local\\FirefoxDiscordPresence.Tray", out var firstInstance);
if (!firstInstance) return;
var logger = new FileLogger();
try
{
    logger.Info("Application start");
    var settingsStore = new SettingsStore(logger);
    var settings = settingsStore.Load();
    Application.Run(new TrayApplicationContext(settings, settingsStore, logger));
}
catch (Exception ex) { logger.Error("Fatal application error", ex); }
finally { logger.Info("Application stop"); }
