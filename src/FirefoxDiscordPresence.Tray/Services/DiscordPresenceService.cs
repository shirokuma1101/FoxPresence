using DiscordRPC;
using DiscordRPC.Logging;
using FirefoxDiscordPresence.Shared.Models;
namespace FirefoxDiscordPresence.Tray.Services;
public sealed class DiscordPresenceService : IDisposable
{
    private readonly FileLogger _logger;
    private readonly object _gate = new();
    private DiscordRpcClient? _client;
    private MediaPresence? _latest;
    private string? _lastFingerprint;
    private Timestamps? _timestamps;
    public bool Connected { get; private set; }
    public event Action<bool>? ConnectionChanged;
    public DiscordPresenceService(string applicationId, FileLogger logger)
    {
        _logger = logger;
        if (string.IsNullOrWhiteSpace(applicationId)) { logger.Error("Discord Application ID is not configured"); return; }
        _client = new DiscordRpcClient(applicationId.Trim()) { Logger = new NullLogger(), SkipIdenticalPresence = true };
        _client.OnReady += (_, _) => { Connected = true; _logger.Info("Discord connection"); ConnectionChanged?.Invoke(true); lock (_gate) { if (_latest is { Playing: true }) Send(_latest); } };
        _client.OnClose += (_, _) => { Connected = false; _logger.Info("Discord disconnection"); ConnectionChanged?.Invoke(false); };
        _client.OnConnectionFailed += (_, e) => { Connected = false; _logger.Error($"Discord connection failed: {e.FailedPipe}"); ConnectionChanged?.Invoke(false); };
        try { _client.Initialize(); }
        catch (Exception ex) { logger.Error("Discord initialization failed", ex); }
    }
    public void Update(MediaPresence media)
    {
        lock (_gate)
        {
            _latest = media;
            if (!media.Playing) { Clear(); return; }
            var fingerprint = $"{media.Site}\n{media.Title}\n{media.Artist}\n{media.Album}\n{media.Url}\n{media.Playing}";
            var recalculate = media.EventType is "play" or "seeked" or "mediachange" || _lastFingerprint != fingerprint || _timestamps is null;
            if (!recalculate && media.EventType == "heartbeat") return;
            if (recalculate) _timestamps = CreateTimestamps(media);
            if (_lastFingerprint == fingerprint && !recalculate) return;
            _lastFingerprint = fingerprint;
            Send(media);
        }
    }
    private void Send(MediaPresence media)
    {
        if (_client is null) return;
        try
        {
            var presence = new RichPresence
            {
                Details = Truncate(media.Title, 128),
                State = Truncate(string.IsNullOrWhiteSpace(media.Album) ? media.Artist : $"{media.Artist} • {media.Album}", 128),
                Timestamps = _timestamps,
                Assets = new Assets { LargeImageKey = string.IsNullOrWhiteSpace(media.ThumbnailUrl) ? media.Site : media.ThumbnailUrl, LargeImageText = SiteName(media.Site) },
                Buttons = string.IsNullOrWhiteSpace(media.Url) ? null : [new DiscordRPC.Button { Label = ButtonLabel(media.Site), Url = media.Url }]
            };
            _client.SetPresence(presence); _logger.Info($"Presence update: {media.Site}");
        }
        catch (Exception ex) { _logger.Error("Presence update failed", ex); }
    }
    private static Timestamps? CreateTimestamps(MediaPresence media)
    {
        if (media.Duration <= 0) return null;
        var start = DateTime.UtcNow.AddSeconds(-media.CurrentTime);
        return new Timestamps(start, start.AddSeconds(media.Duration));
    }
    public void Clear()
    {
        lock (_gate) { try { _client?.ClearPresence(); } catch (Exception ex) { _logger.Error("Presence clear failed", ex); } _lastFingerprint = null; _timestamps = null; _logger.Info("Presence clear"); }
    }
    private static string Truncate(string value, int max) => string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Length <= max ? value : value[..(max - 1)] + "…";
    private static string SiteName(string site) => site switch { "youtube_music" => "YouTube Music", "d_anime" => "dアニメストア", "unext" => "U-NEXT", "netflix" => "Netflix", _ => "YouTube" };
    private static string ButtonLabel(string site) => site switch { "youtube_music" => "Open in YouTube Music", "d_anime" => "Watch on dアニメストア", "unext" => "Watch on U-NEXT", "netflix" => "Watch on Netflix", _ => "Watch on YouTube" };
    public void Dispose() { Clear(); _client?.Dispose(); }
}
