using FirefoxDiscordPresence.Shared.Models;
using FirefoxDiscordPresence.Tray.Configuration;
using FirefoxDiscordPresence.Tray.Services;
namespace FirefoxDiscordPresence.Tray;
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly SettingsStore _store;
    private readonly FileLogger _logger;
    private readonly StartupService _startup = new();
    private readonly NamedPipeServer _pipe;
    private readonly DiscordPresenceService _discord;
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _status = new("Status: Waiting for Firefox") { Enabled = false };
    private readonly ToolStripMenuItem _current = new("Current: None") { Enabled = false };
    private readonly ToolStripMenuItem _enabled;
    private readonly ToolStripMenuItem _autoStart;
    private readonly System.Windows.Forms.Timer _staleTimer = new() { Interval = 5_000 };
    private readonly SynchronizationContext _ui;
    private MediaPresence? _lastMedia;
    private DateTimeOffset _lastReceived;
    private bool _firefoxConnected;

    public TrayApplicationContext(AppSettings settings, SettingsStore store, FileLogger logger)
    {
        if (SynchronizationContext.Current is null) SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        _ui = SynchronizationContext.Current!;
        _settings = settings; _store = store; _logger = logger;
        _enabled = new ToolStripMenuItem("Enable Presence") { Checked = settings.PresenceEnabled, CheckOnClick = true };
        _autoStart = new ToolStripMenuItem("Start with Windows") { Checked = _startup.IsEnabled(), CheckOnClick = true };
        var menu = new ContextMenuStrip();
        menu.Items.AddRange([new ToolStripMenuItem("Firefox Discord Presence") { Enabled = false }, new ToolStripSeparator(), _status, _current, new ToolStripSeparator(), _enabled, _autoStart, new ToolStripSeparator(), new ToolStripMenuItem("Exit", null, (_, _) => ExitThread())]);
        _icon = new NotifyIcon { Icon = SystemIcons.Application, Text = "Firefox Discord Presence", ContextMenuStrip = menu, Visible = true };
        _discord = new DiscordPresenceService(settings.DiscordApplicationId, logger);
        _pipe = new NamedPipeServer(logger);
        _pipe.MessageReceived += media => _ui.Post(_ => Receive(media), null);
        _pipe.ConnectionChanged += connected => _ui.Post(_ => { _firefoxConnected = connected || DateTimeOffset.UtcNow - _lastReceived < TimeSpan.FromSeconds(10); UpdateStatus(); }, null);
        _discord.ConnectionChanged += _ => _ui.Post(_ => UpdateStatus(), null);
        _enabled.CheckedChanged += (_, _) => { _settings.PresenceEnabled = _enabled.Checked; _store.Save(_settings); if (!_enabled.Checked) _discord.Clear(); else if (_lastMedia is { Playing: true }) _discord.Update(_lastMedia); };
        _autoStart.CheckedChanged += (_, _) => { try { _startup.SetEnabled(_autoStart.Checked); _settings.StartWithWindows = _autoStart.Checked; _store.Save(_settings); } catch (Exception ex) { logger.Error("Startup setting failed", ex); _autoStart.Checked = _startup.IsEnabled(); } };
        _staleTimer.Tick += (_, _) => CheckStale(); _staleTimer.Start(); _pipe.Start(); UpdateStatus();
    }
    private void Receive(MediaPresence media)
    {
        _lastMedia = media; _lastReceived = DateTimeOffset.UtcNow; _firefoxConnected = true;
        _current.Text = $"Current: {(media.Playing ? media.Title : "None")}";
        if (_settings.PresenceEnabled) _discord.Update(media); UpdateStatus();
    }
    private void CheckStale()
    {
        if (_lastMedia is { Playing: true } && DateTimeOffset.UtcNow - _lastReceived > TimeSpan.FromSeconds(Math.Max(30, _settings.StaleTimeoutSeconds))) { _lastMedia = null; _firefoxConnected = false; _current.Text = "Current: None"; _discord.Clear(); UpdateStatus(); }
    }
    private void UpdateStatus() => _status.Text = $"Status: {(_discord.Connected ? "Discord connected" : "Discord disconnected")}, {(_firefoxConnected ? "Firefox active" : "waiting for Firefox")}";
    protected override void ExitThreadCore()
    {
        _staleTimer.Stop(); _discord.Dispose(); _pipe.DisposeAsync().AsTask().GetAwaiter().GetResult(); _icon.Visible = false; _icon.Dispose(); base.ExitThreadCore();
    }
}
