(() => {
  const S = {
    video: ["video#video", "video"],
    title: [
      "meta[property='og:title']",
      "meta[name='twitter:title']",
      ".c-infoDetails__title",
      "[data-title]"
    ],
    series: [
      ".c-infoDetails__title",
      ".p-programDetail__title",
      "[data-work-title]"
    ],
    thumbnail: ["meta[property='og:image']", "meta[name='twitter:image']"],
    canonical: ["link[rel='canonical']"]
  };

  function metaOrText(selectors) {
    for (const selector of selectors) {
      const element = document.querySelector(selector);
      const value = element?.getAttribute("content") || element?.getAttribute("data-title") || element?.getAttribute("data-work-title") || element?.textContent;
      if (value?.trim()) return value.trim();
    }
    return "";
  }

  function cleanTitle(value) {
    return value.replace(/\s*[|｜-]\s*dアニメストア\s*$/u, "").trim();
  }

  function extract() {
    const video = S.video.map(selector => document.querySelector(selector)).find(Boolean);
    if (!video) return null;
    const pageTitle = cleanTitle(document.title);
    const metadataTitle = cleanTitle(metaOrText(S.title));
    const title = pageTitle || metadataTitle || "再生中";
    const series = metaOrText(S.series);
    return {
      site: "d_anime",
      title,
      artist: series && series !== title ? series : "dアニメストア",
      album: null,
      url: location.href || FdpMessaging.attr(S.canonical, "href"),
      thumbnailUrl: FdpMessaging.attr(S.thumbnail, "content"),
      playing: !video.paused && !video.ended,
      currentTime: video.currentTime,
      duration: video.duration
    };
  }

  FdpMessaging.observe(extract);
})();
