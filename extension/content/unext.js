(() => {
  let cachedTitle = "";
  let cachedArtist = "U-NEXT";
  let cachedThumbnail = null;
  const S = {
    title: [
      "[data-testid='player-title']",
      "[data-testid='title']",
      "[class*='PlayerTitle']",
      "[class*='playerTitle']",
      "meta[property='og:title']",
      "meta[name='twitter:title']"
    ],
    episode: [
      "[data-testid='player-episode-title']",
      "[data-testid='episode-title']",
      "[class*='EpisodeTitle']",
      "[class*='episodeTitle']"
    ],
    thumbnail: ["meta[property='og:image']", "meta[name='twitter:image']"]
  };

  function value(selectors) {
    for (const selector of selectors) {
      const element = document.querySelector(selector);
      const text = element?.getAttribute("content") || element?.textContent;
      if (text?.trim()) return text.trim();
    }
    return "";
  }

  function clean(value) {
    return value.replace(/\s*[|｜-]\s*U-NEXT.*$/iu, "").trim();
  }

  function extract() {
    const video = document.querySelector("video");
    if (!video) return null;
    const series = clean(value(S.title) || document.title);
    const episode = clean(value(S.episode));
    const extractedTitle = episode && episode !== series ? `${series} — ${episode}` : series;
    if (extractedTitle && !/^U-NEXT$/iu.test(extractedTitle)) {
      cachedTitle = extractedTitle;
      cachedArtist = series || "U-NEXT";
    }
    cachedThumbnail = FdpMessaging.attr(S.thumbnail, "content") || cachedThumbnail;
    return {
      site: "unext",
      title: cachedTitle || "再生中",
      artist: cachedArtist,
      album: null,
      url: location.href,
      thumbnailUrl: cachedThumbnail,
      playing: !video.paused && !video.ended,
      currentTime: video.currentTime,
      duration: video.duration
    };
  }

  FdpMessaging.observe(extract);
})();
