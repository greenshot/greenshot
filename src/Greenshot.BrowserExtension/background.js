/**
 * Greenshot Companion Extension - Background Service Worker
 * Manages Native Messaging lifecycle with greenshot-proxy.exe,
 * handshake/configuration sync, retries, capture ingestion, and QA tab URL tracking.
 */

const NATIVE_HOST_NAME = "org.greenshot.proxy";
const RETRY_BASE_DELAY_MS = 5000;
const RETRY_MAX_DELAY_MS = 60000;

let nativePort = null;
let isConnected = false;
let retryCount = 0;
let retryTimerId = null;

let extensionConfig = {
  track_tab_urls: true,
  capture_format: "png"
};

// Initialize configuration from storage
chrome.storage.local.get(["extensionConfig"], (res) => {
  if (res.extensionConfig) {
    extensionConfig = { ...extensionConfig, ...res.extensionConfig };
  }
});

function getBrowserName() {
  if (navigator.userAgent.includes("Edg/")) return "edge";
  if (navigator.userAgent.includes("Firefox/")) return "firefox";
  return "chrome";
}

function updateBadge() {
  if (isConnected) {
    chrome.action.setBadgeText({ text: "ON" });
    chrome.action.setBadgeBackgroundColor({ color: "#2E7D32" }); // Green
  } else {
    chrome.action.setBadgeText({ text: "" });
  }
}

/**
 * Connects to the native messaging host (greenshot-proxy.exe).
 */
function connectNativeHost() {
  if (retryTimerId) {
    clearTimeout(retryTimerId);
    retryTimerId = null;
  }

  if (nativePort) {
    try {
      nativePort.disconnect();
    } catch (e) {
      // Ignore disconnect errors on old port
    }
    nativePort = null;
  }

  try {
    nativePort = chrome.runtime.connectNative(NATIVE_HOST_NAME);

    nativePort.onMessage.addListener(onNativeMessage);
    nativePort.onDisconnect.addListener(onNativeDisconnect);

    // Send initial handshake request
    sendNativeMessage({
      source: "native_messaging",
      command: "HANDSHAKE",
      extension_version: chrome.runtime.getManifest().version,
      browser: getBrowserName()
    });
  } catch (err) {
    console.warn("Failed to connect to native messaging host:", err);
    scheduleReconnect();
  }
}

function onNativeMessage(msg) {
  if (!msg) return;

  if (msg.status === "unavailable" || msg.greenshot_running === false) {
    // Greenshot is offline
    isConnected = false;
    updateBadge();
    scheduleReconnect();
    return;
  }

  if (msg.status === "ready" && msg.greenshot_running === true) {
    // Successful handshake
    isConnected = true;
    retryCount = 0;
    updateBadge();

    if (msg.config) {
      extensionConfig = { ...extensionConfig, ...msg.config };
      chrome.storage.local.set({ extensionConfig });
    }
  }
}

function onNativeDisconnect() {
  isConnected = false;
  nativePort = null;
  updateBadge();

  const lastErr = chrome.runtime.lastError;
  if (lastErr) {
    console.info("Native messaging port disconnected:", lastErr.message);
  }

  scheduleReconnect();
}

function scheduleReconnect() {
  if (retryTimerId) return;

  // Bounded exponential backoff: 5s, 7.5s, 11s, ..., up to 60s
  const delay = Math.min(
    RETRY_MAX_DELAY_MS,
    Math.round(RETRY_BASE_DELAY_MS * Math.pow(1.5, retryCount++))
  );

  retryTimerId = setTimeout(() => {
    retryTimerId = null;
    connectNativeHost();
  }, delay);
}

function sendNativeMessage(payload) {
  if (!nativePort) {
    connectNativeHost();
    return;
  }

  try {
    nativePort.postMessage(payload);
  } catch (err) {
    console.warn("Failed to send message over native port:", err);
    isConnected = false;
    scheduleReconnect();
  }
}

/**
 * Captures the current visible tab and imports it into Greenshot.
 */
async function captureCurrentTab() {
  const [activeTab] = await chrome.tabs.query({ active: true, currentWindow: true });
  if (!activeTab || !activeTab.id) {
    throw new Error("No active tab found.");
  }

  // Ensure native messaging is connected
  if (!isConnected || !nativePort) {
    connectNativeHost();
    throw new Error("Greenshot is offline. Please start Greenshot.");
  }

  // Capture visible area
  const dataUrl = await chrome.tabs.captureVisibleTab(activeTab.windowId, {
    format: extensionConfig.capture_format === "jpeg" ? "jpeg" : "png"
  });

  const commaIdx = dataUrl.indexOf(",");
  const base64Payload = commaIdx >= 0 ? dataUrl.slice(commaIdx + 1) : dataUrl;

  const payload = {
    source: "native_messaging",
    command: "IMPORT_CAPTURE",
    metadata: {
      title: activeTab.title || "Untitled Tab",
      url: activeTab.url || "",
      timestamp: Math.floor(Date.now() / 1000)
    },
    data: {
      mime_type: extensionConfig.capture_format === "jpeg" ? "image/jpeg" : "image/png",
      encoding: "base64",
      payload: base64Payload
    }
  };

  sendNativeMessage(payload);
  return { success: true, title: activeTab.title };
}

/**
 * QA Tab URL Tracking
 * Reports active tab URL / title changes to Greenshot to assist QA file routing.
 */
let lastReportedUrl = "";
let lastReportedTitle = "";

function reportTabChange(url, title) {
  if (!extensionConfig.track_tab_urls || !url) {
    return;
  }

  // Ignore browser internal schemes
  if (url.startsWith("chrome://") || url.startsWith("edge://") || url.startsWith("about:") || url.startsWith("chrome-extension://") || url.startsWith("moz-extension://")) {
    return;
  }

  title = title || "";

  if (url === lastReportedUrl && title === lastReportedTitle) {
    return;
  }

  lastReportedUrl = url;
  lastReportedTitle = title;

  sendNativeMessage({
    source: "native_messaging",
    command: "TAB_CHANGED",
    url: url,
    title: title
  });
}

chrome.tabs.onUpdated.addListener((tabId, changeInfo, tab) => {
  if (!tab.active) {
    return;
  }

  // 1. Navigation started: URL changed.
  // During initial navigation / loading, tab.title may still be the previous document's title.
  // Send the new URL immediately with an empty title to clear stale context in Greenshot.
  if (changeInfo.url) {
    reportTabChange(changeInfo.url, "");
  }

  // 2. Title resolved or changed dynamically (e.g. SPAs, client-side routing, document.title set)
  if (changeInfo.title) {
    reportTabChange(tab.url, changeInfo.title);
  }

  // 3. Page load completed: ensure final URL and title are recorded
  if (changeInfo.status === "complete" && tab.url) {
    reportTabChange(tab.url, tab.title);
  }
});

chrome.tabs.onActivated.addListener(async (activeInfo) => {
  try {
    const tab = await chrome.tabs.get(activeInfo.tabId);
    if (tab && tab.url) {
      reportTabChange(tab.url, tab.title);
    }
  } catch (e) {
    // Tab might have closed
  }
});

// Handle messages from Popup or Options UI
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
  if (request.action === "GET_STATUS") {
    sendResponse({
      isConnected,
      config: extensionConfig
    });
    return true;
  }

  if (request.action === "CAPTURE_VISIBLE") {
    captureCurrentTab()
      .then((res) => sendResponse(res))
      .catch((err) => sendResponse({ success: false, error: err.message }));
    return true;
  }

  if (request.action === "RECONNECT") {
    retryCount = 0;
    connectNativeHost();
    sendResponse({ status: "connecting" });
    return true;
  }

  if (request.action === "UPDATE_CONFIG") {
    extensionConfig = { ...extensionConfig, ...request.config };
    chrome.storage.local.set({ extensionConfig });
    sendResponse({ success: true, config: extensionConfig });
    return true;
  }
});

// Connect on worker startup
connectNativeHost();
