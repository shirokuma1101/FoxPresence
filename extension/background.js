const HOST = "com.shiro1103.firefox_discord_presence";
const tabs = new Map();
let sequence = 0;
let port = null;
let selectedTabId = null;

function nativePort() {
  if (port) return port;
  try {
    port = browser.runtime.connectNative(HOST);
    port.onDisconnect.addListener(() => { port = null; });
    return port;
  } catch (error) { console.warn("Native host unavailable", error); return null; }
}

function selectPresence() {
  return [...tabs.values()].filter(x => x.media.playing).sort((a, b) => b.playSequence - a.playSequence)[0] ?? null;
}

function publish(fallback, triggerTabId) {
  const selected = selectPresence();
  const previousSelected = selectedTabId;
  selectedTabId = selected?.media.tabId ?? null;
  if (selected && triggerTabId !== selectedTabId && previousSelected === selectedTabId) return;
  let message = selected?.media ?? (fallback ? { ...fallback, playing: false, eventType: "pause" } : null);
  if (!message) return;
  if (selected && previousSelected !== selectedTabId && triggerTabId !== selectedTabId) {
    const elapsed = Math.max(0, (Date.now() - Date.parse(message.updatedAt)) / 1000);
    message = { ...message, currentTime: Math.min(message.duration || Infinity, message.currentTime + elapsed), eventType: "mediachange" };
  }
  message = { ...message, updatedAt: new Date().toISOString() };
  try { nativePort()?.postMessage(message); } catch { port = null; }
}

browser.runtime.onMessage.addListener((request, sender) => {
  if (request?.type !== "media-presence" || sender.tab?.id == null) return;
  const tabId = sender.tab.id;
  const previous = tabs.get(tabId);
  const started = request.media.playing && !previous?.media.playing;
  const entry = { media: { ...request.media, tabId }, playSequence: started ? ++sequence : previous?.playSequence ?? 0 };
  tabs.set(tabId, entry);
  publish(entry.media, tabId);
});

browser.tabs.onRemoved.addListener(tabId => {
  const removed = tabs.get(tabId)?.media;
  tabs.delete(tabId);
  if (removed) publish(removed, tabId);
});
