using FirefoxDiscordPresence.Shared.Models;
namespace FirefoxDiscordPresence.Tests;
public sealed class MediaPresenceTests
{
    [Fact]
    public void Normalize_RejectsNonFiniteTimesAndUnsafeUrls()
    {
        var actual = new MediaPresence { Site = "youtube", Url = "javascript:alert(1)", ThumbnailUrl = "http://example.test/a.jpg", CurrentTime = double.NaN, Duration = double.PositiveInfinity }.Normalize();
        Assert.Equal(0, actual.CurrentTime); Assert.Equal(0, actual.Duration); Assert.Empty(actual.Url); Assert.Equal("", actual.ThumbnailUrl);
    }
    [Fact]
    public void Normalize_ClampsCurrentTimeToDuration()
    {
        var actual = new MediaPresence { Site = "youtube_music", CurrentTime = 200, Duration = 100 }.Normalize();
        Assert.Equal(100, actual.CurrentTime);
    }
    [Fact]
    public void Normalize_AcceptsDAnimeSite()
    {
        var actual = new MediaPresence { Site = "d_anime", Title = " 第3話 " }.Normalize();
        Assert.Equal("d_anime", actual.Site); Assert.Equal("第3話", actual.Title);
    }
    [Theory]
    [InlineData("unext")]
    [InlineData("netflix")]
    public void Normalize_AcceptsStreamingSites(string site)
    {
        Assert.Equal(site, new MediaPresence { Site = site }.Normalize().Site);
    }
}
