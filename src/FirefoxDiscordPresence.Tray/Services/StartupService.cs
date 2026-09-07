using Microsoft.Win32;
namespace FirefoxDiscordPresence.Tray.Services;
public sealed class StartupService
{
    private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "FirefoxDiscordPresence";
    public bool IsEnabled() { using var key = Registry.CurrentUser.OpenSubKey(KeyPath); return key?.GetValue(ValueName) is string; }
    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
        if (enabled) key.SetValue(ValueName, $"\"{Environment.ProcessPath}\""); else key.DeleteValue(ValueName, false);
    }
}
