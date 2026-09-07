(() => {
  let cachedTitle = "";
  let cachedArtist = "U-NEXT";
  let cachedThumbnail = null;
  let fetchedFor = "";
  const titleCode = () => location.pathname.match(/\/(?:play|title)\/(SID\d+)/i)?.[1] || "";
  const episodeCode = () => location.pathname.match(/\/play\/SID\d+\/(ED\d+)/i)?.[1] || "";
  const clean = value => (value || "").replace(/\s*[|｜-]\s*U-NEXT.*$/iu, "").replace(/\s+/g, " ").trim();
  const useful = value => value && value.length <= 300
    && !/^(?:再生|U-NEXT|ユーネクスト|(?:SID|ED)\d+)$/iu.test(value)
    && !/^(?:洋画|邦画|海外ドラマ|国内ドラマ|韓流・アジア|アニメ|キッズ|TV番組・エンタメ|報道・スペシャル|音楽・ライブ|舞台・演劇)$/u.test(value)
    && !/^https?:\/\//iu.test(value);

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
      const value = element?.getAttribute("content")
        || element?.getAttribute("data-title-name")
        || element?.getAttribute("data-program-title")
        || element?.getAttribute("data-series-title")
        || element?.getAttribute("data-episode-title")
        || element?.getAttribute("data-subtitle")
        || element?.textContent;
      if (useful(clean(value))) return clean(value);
    }
    return "";
  }

  function propertyPriority(key, entityType) {
    const normalized = key.replace(/[_-]/g, "").toLowerCase();
    if (/(?:id|code|url|href|path)$/.test(normalized)) return -1;
    const priorities = entityType === "episode"
      ? ["episodetitle", "subtitle", "subtitlename", "episodename"]
      : ["titlename", "seriestitle", "programtitle", "contenttitle", "worktitle"];
    const index = priorities.indexOf(normalized);
    return index < 0 ? -1 : priorities.length - index;
  }

  function findEntityData(doc, code, entityType) {
    if (!code) return "";
    const scripts = [...doc.querySelectorAll("script")].map(element => element.textContent || "").filter(text => text.includes(code));
    for (const script of scripts) try {
      const root = JSON.parse(script); let found = ""; let bestPriority = -1;
      const visit = (value, depth = 0) => {
        if (depth > 18 || !value || typeof value !== "object") return;
        if (Array.isArray(value)) { for (const child of value) visit(child, depth + 1); return; }
        const values = Object.values(value);
        if (values.some(item => item === code)) {
          for (const [key, item] of Object.entries(value)) {
            const priority = propertyPriority(key, entityType);
            if (priority > bestPriority && typeof item === "string" && useful(clean(item))) {
              found = clean(item); bestPriority = priority;
            }
          }
        }
        for (const child of values) visit(child, depth + 1);
      };
      visit(root);
      if (found) return found;
    } catch { }
    return findSerializedProperty(scripts.join("\n"), code, entityType);
  }

  function findSerializedProperty(source, code, entityType) {
    const codeIndex = source.indexOf(code);
    if (codeIndex < 0) return "";
    const nearby = source.slice(Math.max(0, codeIndex - 4000), codeIndex + 4000);
    const expression = /["']([A-Za-z][\w-]*)["']\s*:\s*"((?:\\.|[^"\\])*)"/g;
    let match; let best = ""; let bestPriority = -1;
    while ((match = expression.exec(nearby))) {
      const priority = propertyPriority(match[1], entityType);
      if (priority <= bestPriority) continue;
      try {
        const value = clean(JSON.parse(`"${match[2]}"`));
        if (useful(value)) { best = value; bestPriority = priority; }
      } catch { }
    }
    return best;
  }

  function readDocument(doc) {
    const series = directText(doc, ["[class*='styles__Title-sc-']", "[data-title-name]", "[data-program-title]", "[data-series-title]", "[data-testid='player-title']", "[data-testid='title']", "[class*='PlayerTitle']", "[class*='playerTitle']", "main h1", "h1"])
      || findEntityData(doc, titleCode(), "title");
    let episode = directText(doc, ["[class*='styles__SubTitle-sc-']", "[data-episode-title]", "[data-subtitle]", "[data-testid='player-episode-title']", "[data-testid='episode-title']", "[class*='EpisodeTitle']", "[class*='episodeTitle']"])
      || findEntityData(doc, episodeCode(), "episode");
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
