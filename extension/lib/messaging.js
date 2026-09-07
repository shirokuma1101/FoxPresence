globalThis.FdpMessaging = (() => {
  let lastSignature = "";
  function finite(value) { return Number.isFinite(value) ? value : 0; }
  async function send(extractor, eventType = "update", force = false) {
    try {
      const media = extractor();
      if (!media) return;
      media.currentTime = finite(media.currentTime);
      media.duration = finite(media.duration);
      media.updatedAt = new Date().toISOString();
      media.eventType = eventType;
      const signature = JSON.stringify([media.site, media.title, media.artist, media.album, media.url, media.playing]);
      if (!force && signature === lastSignature) return;
      lastSignature = signature;
      await browser.runtime.sendMessage({ type: "media-presence", media });
    } catch (error) { console.debug("Presence extraction skipped", error); }
  }
  function observe(extractor) {
    let media;
    const listeners = new Map();
    const events = ["play", "pause", "playing", "waiting", "ended", "seeked", "loadedmetadata", "durationchange"];
    const bind = () => {
      const next = document.querySelector("video");
      if (next === media) return;
      if (media) for (const [name, handler] of listeners) media.removeEventListener(name, handler);
      listeners.clear(); media = next;
      if (media) for (const name of events) { const handler = () => send(extractor, name === "playing" ? "play" : name, true); listeners.set(name, handler); media.addEventListener(name, handler, { passive: true }); }
      send(extractor, "mediachange", true);
    };
    let debounce;
    const changed = () => { clearTimeout(debounce); debounce = setTimeout(() => { bind(); send(extractor, "mediachange"); }, 300); };
    new MutationObserver(changed).observe(document.documentElement, { childList: true, subtree: true });
    addEventListener("popstate", changed); addEventListener("yt-navigate-finish", changed); addEventListener("yt-page-data-updated", changed);
    bind(); setInterval(bind, 7500); setInterval(() => send(extractor, "heartbeat", true), 30000);
  }
  function text(selectors) { for (const selector of selectors) { const value = document.querySelector(selector)?.textContent?.trim(); if (value) return value; } return ""; }
  function attr(selectors, name) { for (const selector of selectors) { const value = document.querySelector(selector)?.getAttribute(name); if (value) return value; } return null; }
  return { observe, send, text, attr };
})();
