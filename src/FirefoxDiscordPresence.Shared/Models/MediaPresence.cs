using System.Text.Json.Serialization;
namespace FirefoxDiscordPresence.Shared.Models;
public sealed record MediaPresence
{
    [JsonPropertyName("tabId")] public int TabId { get; init; }
    [JsonPropertyName("site")] public string Site { get; init; } = "";
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("artist")] public string Artist { get; init; } = "";
    [JsonPropertyName("album")] public string? Album { get; init; }
    [JsonPropertyName("url")] public string Url { get; init; } = "";
    [JsonPropertyName("thumbnailUrl")] public string? ThumbnailUrl { get; init; }
    [JsonPropertyName("playing")] public bool Playing { get; init; }
    [JsonPropertyName("currentTime")] public double CurrentTime { get; init; }
    [JsonPropertyName("duration")] public double Duration { get; init; }
    [JsonPropertyName("updatedAt")] public DateTimeOffset UpdatedAt { get; init; }
    [JsonPropertyName("eventType")] public string EventType { get; init; } = "update";
    public MediaPresence Normalize()
    {
        var duration = double.IsFinite(Duration) && Duration > 0 ? Duration : 0;
        var current = double.IsFinite(CurrentTime) ? Math.Clamp(CurrentTime, 0, duration > 0 ? duration : double.MaxValue) : 0;
        return this with { Site = Site is "youtube" or "youtube_music" or "d_anime" ? Site : "", Title = Title.Trim(), Artist = Artist.Trim(), Album = string.IsNullOrWhiteSpace(Album) ? null : Album.Trim(), Url = ValidHttps(Url), ThumbnailUrl = ValidHttps(ThumbnailUrl), CurrentTime = current, Duration = duration, UpdatedAt = UpdatedAt == default ? DateTimeOffset.UtcNow : UpdatedAt };
    }
    private static string ValidHttps(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps ? uri.ToString() : "";
}
