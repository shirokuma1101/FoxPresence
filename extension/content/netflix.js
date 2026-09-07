(() => {
  let cachedTitle = "";
  let cachedArtist = "Netflix";
  let cachedThumbnail = null;
  const S = {
    series: [
      "[data-uia='video-title'] h4",
      ".watch-video--player-view .video-title h4",
      ".video-title h4"
    ],
    movie: [
      "[data-uia='video-title']",
      ".watch-video--player-view .video-title",
      ".video-title"
    ],
    episode: [
      "[data-uia='video-title'] span",
      ".watch-video--player-view .video-title span",
      ".video-title span"
    ],
    thumbnail: ["meta[property='og:image']", "meta[name='twitter:image']"]
  };

  function firstText(selectors) {
    for (const selector of selectors) {
      const text = document.querySelector(selector)?.textContent?.trim();
      if (text) return text;
    }
    return "";
  }

  function episodeText() {
    for (const selector of S.episode) {
      const values = [...document.querySelectorAll(selector)].map(element => element.textContent?.trim()).filter(Boolean);
      const unique = [...new Set(values)];
      if (unique.length) return unique.join(" ");
    }
    return "";
  }

  function clean(value) {
    return value.replace(/\s*[|｜-]\s*Netflix\s*$/iu, "").trim();
  }

  function extract() {
    const video = document.querySelector("video");
    if (!video) return null;
    const series = clean(firstText(S.series));
    const episode = clean(episodeText());
    const fallback = clean(firstText(S.movie) || document.title);
    const extractedTitle = series && episode ? `${series} — ${episode}` : series || fallback;
    if (extractedTitle && !/^Netflix$/iu.test(extractedTitle)) {
      cachedTitle = extractedTitle;
      cachedArtist = series && episode ? series : "Netflix";
    }
    cachedThumbnail = FdpMessaging.attr(S.thumbnail, "content") || cachedThumbnail;
    return {
      site: "netflix",
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
