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
            var siteName = SiteName(media.Site);
            var details = DisplayDetails(media);
            var state = DisplayState(media);
            var hasArtwork = !string.IsNullOrWhiteSpace(media.ThumbnailUrl);
            var serviceIcon = ServiceIconUrl(media.Site);
            var presence = new RichPresence
            {
                Type = media.Site == "youtube_music" ? ActivityType.Listening : ActivityType.Watching,
                StatusDisplay = StatusDisplayType.Details,
                Details = Truncate(details, 128),
                DetailsUrl = media.Url,
                State = Truncate(state, 128),
                StateUrl = media.Url,
                Timestamps = _timestamps,
                Assets = new Assets
                {
                    LargeImageKey = hasArtwork ? media.ThumbnailUrl : serviceIcon,
                    LargeImageText = Truncate(media.Title, 128),
                    LargeImageUrl = media.Url,
                    SmallImageKey = hasArtwork ? serviceIcon : null,
                    SmallImageText = hasArtwork ? $"{(media.Site == "youtube_music" ? "Listening" : "Watching")} on {siteName}" : null,
                    SmallImageUrl = hasArtwork ? media.Url : null
                },
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
    private static string DisplayDetails(MediaPresence media)
    {
        if (!string.IsNullOrWhiteSpace(media.Artist))
        {
            var prefix = media.Artist + " — ";
            if (media.Title.StartsWith(prefix, StringComparison.Ordinal)) return media.Title[prefix.Length..];
        }
        return media.Title;
    }
    private static string DisplayState(MediaPresence media)
    {
        var creator = string.IsNullOrWhiteSpace(media.Artist) ? SiteName(media.Site) : media.Artist;
        return string.IsNullOrWhiteSpace(media.Album) ? creator : $"{creator} • {media.Album}";
    }
    private static string SiteName(string site) => site switch { "youtube_music" => "YouTube Music", "d_anime" => "dアニメストア", "unext" => "U-NEXT", "netflix" => "Netflix", _ => "YouTube" };
    private static string ServiceIconUrl(string site) => site switch
    {
        "youtube_music" => "https://music.youtube.com/img/favicon_144.png",
        "d_anime" => "https://animestore.docomo.ne.jp/favicon-highres.png?1",
        "unext" => "https://video.unext.jp/android-icon.png",
        "netflix" => "https://assets.nflxext.com/us/ffe/siteui/common/icons/nficon2016.png",
        _ => "https://www.youtube.com/img/favicon_144.png"
    };
    private static string ButtonLabel(string site) => site switch { "youtube_music" => "Open in YouTube Music", "d_anime" => "Watch on dアニメストア", "unext" => "Watch on U-NEXT", "netflix" => "Watch on Netflix", _ => "Watch on YouTube" };
    public void Dispose() { Clear(); _client?.Dispose(); }
}
