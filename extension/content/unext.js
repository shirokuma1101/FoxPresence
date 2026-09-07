(() => {
  let cachedTitle = "";
  let cachedArtist = "U-NEXT";
  let cachedThumbnail = null;
  let fetchedFor = "";
  const titleCode = () => location.pathname.match(/\/(?:play|title)\/(SID\d+)/i)?.[1] || "";
  const episodeCode = () => location.pathname.match(/\/play\/SID\d+\/(ED\d+)/i)?.[1] || "";
  const clean = value => (value || "").replace(/\s*[|｜-]\s*U-NEXT.*$/iu, "").replace(/\s+/g, " ").trim();
  const useful = value => value && !/^(再生|U-NEXT|ユーネクスト)$/iu.test(value);

  function remember(title, artist, thumbnail) {
    title = clean(title); artist = clean(artist);
    if (useful(title)) cachedTitle = title;
    if (useful(artist)) cachedArtist = artist;
    if (thumbnail) cachedThumbnail = thumbnail;
    const sid = titleCode();
    if (sid && cachedTitle) try { sessionStorage.setItem(`fdp:${sid}`, JSON.stringify({ title: cachedTitle, artist: cachedArtist, thumbnail: cachedThumbnail })); } catch { }
  }

  function restore() {
    const sid = titleCode(); if (!sid) return;
    try { const item = JSON.parse(sessionStorage.getItem(`fdp:${sid}`)); if (item) remember(item.title, item.artist, item.thumbnail); } catch { }
  }

  function directText(doc, selectors) {
    for (const selector of selectors) {
      const element = doc.querySelector(selector);
      const value = element?.getAttribute("content") || element?.textContent;
      if (useful(clean(value))) return clean(value);
    }
    return "";
  }

  function findEntityData(doc, code) {
    if (!code) return "";
    const script = doc.querySelector("#__NEXT_DATA__")?.textContent;
    if (!script) return "";
    try {
      const root = JSON.parse(script); let found = "";
      const visit = (value, depth = 0) => {
        if (found || depth > 18 || !value || typeof value !== "object") return;
        if (Array.isArray(value)) { for (const child of value) visit(child, depth + 1); return; }
        const values = Object.values(value);
        if (values.some(item => item === code)) {
          for (const [key, item] of Object.entries(value)) if (typeof item === "string" && /(?:title|name|episode)/i.test(key) && useful(clean(item))) { found = clean(item); return; }
        }
        for (const child of values) visit(child, depth + 1);
      };
      visit(root); return found;
    } catch { return ""; }
  }

  function readDocument(doc) {
    const series = directText(doc, ["[data-testid='player-title']", "[data-testid='title']", "[class*='PlayerTitle']", "[class*='playerTitle']", "main h1", "h1"])
      || findEntityData(doc, titleCode());
    let episode = directText(doc, ["[data-testid='player-episode-title']", "[data-testid='episode-title']", "[class*='EpisodeTitle']", "[class*='episodeTitle']"])
      || findEntityData(doc, episodeCode());
    const episodeLink = episodeCode() && doc.querySelector(`[href*='${episodeCode()}'],[data-episode-code='${episodeCode()}']`);
    if (!episode && episodeLink) episode = clean(episodeLink.closest("li,article,[class*='episode' i]")?.textContent);
    const ogTitle = directText(doc, ["meta[property='og:title']", "meta[name='twitter:title']"]);
    const thumbnail = doc.querySelector("meta[property='og:image'],meta[name='twitter:image']")?.getAttribute("content");
    const base = series || ogTitle;
    remember(episode && base && episode !== base ? `${base} — ${episode}` : episode || base, base, thumbnail);
  }

  async function fetchMetadata(extractor) {
    const sid = titleCode(); if (!sid || fetchedFor === sid) return;
    fetchedFor = sid;
    try {
      const response = await fetch(`/title/${sid}`, { credentials: "include" });
      if (!response.ok) return;
      readDocument(new DOMParser().parseFromString(await response.text(), "text/html"));
      await FdpMessaging.send(extractor, "mediachange", true);
    } catch { }
  }

  function extract() {
    restore(); readDocument(document);
    const video = document.querySelector("video");
    if (!video) return null;
    void fetchMetadata(extract);
    return { site: "unext", title: cachedTitle || "再生中", artist: cachedArtist, album: null, url: location.href,
      thumbnailUrl: cachedThumbnail, playing: !video.paused && !video.ended, currentTime: video.currentTime, duration: video.duration };
  }

  FdpMessaging.observe(extract);
})();
