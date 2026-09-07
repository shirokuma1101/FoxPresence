(() => {
  const S = {
    title: ["ytmusic-player-bar .title.ytmusic-player-bar", "ytmusic-player-bar .content-info-wrapper .title"],
    byline: ["ytmusic-player-bar .byline.ytmusic-player-bar", "ytmusic-player-bar .subtitle"],
    artist: ["ytmusic-player-bar .byline a:first-of-type", "ytmusic-player-bar .subtitle a:first-of-type"],
    image: ["ytmusic-player-bar img.image", "ytmusic-player-bar .thumbnail img"], canonical: ["link[rel='canonical']"]
  };
  function extract() {
    const video = document.querySelector("video"); if (!video) return null;
    const byline = FdpMessaging.text(S.byline); const artist = FdpMessaging.text(S.artist) || byline.split(" • ")[0];
    const parts = byline.split(" • ").map(x => x.trim()).filter(Boolean); const album = parts.length >= 3 ? parts[1] : null;
    return { site: "youtube_music", title: FdpMessaging.text(S.title), artist, album, url: FdpMessaging.attr(S.canonical, "href") || location.href, thumbnailUrl: FdpMessaging.attr(S.image, "src"), playing: !video.paused && !video.ended, currentTime: video.currentTime, duration: video.duration };
  }
  FdpMessaging.observe(extract);
})();
