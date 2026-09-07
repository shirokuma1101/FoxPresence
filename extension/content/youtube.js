(() => {
  const S = {
    title: ["h1.ytd-watch-metadata yt-formatted-string", "h1.title yt-formatted-string", "meta[name='title']"],
    artist: ["ytd-watch-metadata ytd-channel-name a", "#owner-name a", "ytd-video-owner-renderer #channel-name"],
    canonical: ["link[rel='canonical']"],
    thumbnail: ["meta[property='og:image']"]
  };
  function extract() {
    const video = document.querySelector("video"); if (!video) return null;
    const canonical = FdpMessaging.attr(S.canonical, "href") || location.href;
    const id = new URL(canonical).searchParams.get("v");
    return { site: "youtube", title: FdpMessaging.text(S.title) || document.title.replace(/ - YouTube$/, ""), artist: FdpMessaging.text(S.artist), album: null, url: canonical, thumbnailUrl: id ? `https://i.ytimg.com/vi/${encodeURIComponent(id)}/hqdefault.jpg` : FdpMessaging.attr(S.thumbnail, "content"), playing: !video.paused && !video.ended, currentTime: video.currentTime, duration: video.duration };
  }
  FdpMessaging.observe(extract);
})();
