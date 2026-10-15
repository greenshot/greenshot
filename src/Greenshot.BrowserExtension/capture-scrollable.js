/**
 * Greenshot Companion Extension - scrollable page capture.
 * Captures the document as viewport tiles and transfers a capture-list to Greenshot.
 */

globalThis.GreenshotScrollableCapture = (() => {
  const CAPTURE_LIST_MIME_TYPE = "application/vnd.greenshot.capturelist";
  const MAX_CAPTURE_MESSAGE_BYTES = 20 * 1024 * 1024;
  const MAX_TILES = 4096;

  function delay(milliseconds) {
    return new Promise((resolve) => setTimeout(resolve, milliseconds));
  }

  function makePositions(maxScroll, viewportSize) {
    const positions = [0];
    let position = 0;

    while (position < maxScroll) {
      const next = Math.min(position + viewportSize, maxScroll);
      if (next <= position) {
        break;
      }
      positions.push(next);
      position = next;
    }

    return positions;
  }

  function encodeUtf8Base64(text) {
    const bytes = new TextEncoder().encode(text);
    let binary = "";
    const chunkSize = 0x8000;

    for (let offset = 0; offset < bytes.length; offset += chunkSize) {
      const chunk = bytes.subarray(offset, Math.min(offset + chunkSize, bytes.length));
      binary += String.fromCharCode(...chunk);
    }

    return btoa(binary);
  }

  async function executeInPage(tabId, func, args = []) {
    const results = await chrome.scripting.executeScript({
      target: { tabId },
      func,
      args
    });
    if (!results || !results.length) {
      throw new Error("Could not read page dimensions from the active tab.");
    }
    return results[0].result;
  }

  /** Captures the full scrollable document as PNG tiles and sends one capture-list request. */
  async function captureScrollablePage({ isConnected, nativePort, sendNativeMessage, getBrowserName, connectNativeHost }) {
    const [activeTab] = await chrome.tabs.query({ active: true, currentWindow: true });
    if (!activeTab || !activeTab.id) {
      throw new Error("No active tab found.");
    }

    const activeUrl = activeTab.url || "";
    if (/^(about|chrome|chrome-untrusted|chrome-search|edge|brave|opera|devtools|view-source|moz-extension|chrome-extension|data):/i.test(activeUrl)) {
      throw new Error("Scrollable capture is not available on browser-internal pages. Open a regular website and try again.");
    }

    if (!isConnected || !nativePort) {
      connectNativeHost();
      throw new Error("Greenshot is offline. Please start Greenshot.");
    }

    const pageInfo = await executeInPage(activeTab.id, () => ({
      viewportWidth: window.innerWidth,
      viewportHeight: window.innerHeight,
      documentWidth: Math.max(
        document.documentElement?.scrollWidth ?? 0,
        document.body?.scrollWidth ?? 0,
        window.innerWidth
      ),
      documentHeight: Math.max(
        document.documentElement?.scrollHeight ?? 0,
        document.body?.scrollHeight ?? 0,
        window.innerHeight
      ),
      devicePixelRatio: window.devicePixelRatio,
      scrollX: window.scrollX,
      scrollY: window.scrollY
    }));

    if (!pageInfo.viewportWidth || !pageInfo.viewportHeight || !pageInfo.devicePixelRatio) {
      throw new Error("The browser returned invalid page dimensions.");
    }

    const maxX = Math.max(0, pageInfo.documentWidth - pageInfo.viewportWidth);
    const maxY = Math.max(0, pageInfo.documentHeight - pageInfo.viewportHeight);
    const outputWidth = Math.ceil(pageInfo.documentWidth * pageInfo.devicePixelRatio);
    const outputHeight = Math.ceil(pageInfo.documentHeight * pageInfo.devicePixelRatio);
    if (outputWidth > 32767 || outputHeight > 32767 || outputWidth * outputHeight > 100000000) {
      throw new Error("The resulting page image would exceed Greenshot's image dimension limits.");
    }

    const positionsX = makePositions(maxX, pageInfo.viewportWidth);
    const positionsY = makePositions(maxY, pageInfo.viewportHeight);
    if (positionsX.length * positionsY.length > MAX_TILES) {
      throw new Error("The page requires too many screenshots to capture safely.");
    }

    const screenshots = [];
    let totalImageBase64Characters = 0;

    try {
      for (const y of positionsY) {
        for (const x of positionsX) {
          const position = await executeInPage(activeTab.id, ({ x, y }) => {
            window.scrollTo({ left: x, top: y, behavior: "instant" });
            return new Promise((resolve) => {
              // Let the page paint after scrolling without changing its content or styles.
              setTimeout(() => resolve({
                scrollX: window.scrollX,
                scrollY: window.scrollY,
                viewportWidth: window.innerWidth,
                viewportHeight: window.innerHeight
              }), 250);
            });
          }, [{ x, y }]);

          const dataUrl = await chrome.tabs.captureVisibleTab(activeTab.windowId, { format: "png" });
          const commaIndex = dataUrl.indexOf(",");
          if (commaIndex < 0) {
            throw new Error("The browser returned an invalid screenshot.");
          }

          const imageBase64 = dataUrl.slice(commaIndex + 1);
          totalImageBase64Characters += imageBase64.length;
          // The capture-list itself is base64-encoded for Native Messaging. This lower-bound check
          // avoids capturing the rest of a page once the 20 MiB wire limit cannot possibly be met.
          if (totalImageBase64Characters > MAX_CAPTURE_MESSAGE_BYTES * 0.75) {
            throw new Error("The scrollable-page capture exceeds the 20 MiB transfer limit.");
          }

          screenshots.push({
            index: screenshots.length + 1,
            x: position.scrollX,
            y: position.scrollY,
            viewportWidth: position.viewportWidth,
            viewportHeight: position.viewportHeight,
            imageFormat: "png",
            imageBase64
          });

          // captureVisibleTab is rate limited by browsers; keep calls below two per second.
          await delay(300);
        }
      }
    } finally {
      // Restore the user's previous scroll position even if a tile capture failed.
      try {
        await executeInPage(activeTab.id, ({ x, y }) => {
          window.scrollTo({ left: x, top: y, behavior: "instant" });
        }, [{ x: pageInfo.scrollX, y: pageInfo.scrollY }]);
      } catch (err) {
        console.debug("Could not restore the tab's original scroll position:", err);
      }
    }

    const captureList = {
      version: 1,
      coordinateUnit: "css-pixels",
      devicePixelRatio: pageInfo.devicePixelRatio,
      documentWidth: pageInfo.documentWidth,
      documentHeight: pageInfo.documentHeight,
      viewportWidth: pageInfo.viewportWidth,
      viewportHeight: pageInfo.viewportHeight,
      screenshots
    };
    const captureListBase64 = encodeUtf8Base64(JSON.stringify(captureList));
    const message = {
      source: "native_messaging",
      command: "IMPORT_CAPTURE",
      browser: getBrowserName(),
      metadata: {
        title: activeTab.title || "Untitled Tab",
        url: activeTab.url || "",
        timestamp: Math.floor(Date.now() / 1000)
      },
      data: {
        mime_type: CAPTURE_LIST_MIME_TYPE,
        encoding: "base64",
        payload: captureListBase64
      }
    };

    const messageSize = new TextEncoder().encode(JSON.stringify(message)).byteLength;
    if (messageSize > MAX_CAPTURE_MESSAGE_BYTES) {
      throw new Error("The scrollable-page capture exceeds the 20 MiB transfer limit.");
    }

    if (!sendNativeMessage(message)) {
      throw new Error("The connection to Greenshot was lost before the capture could be sent.");
    }

    return { success: true, title: activeTab.title, screenshotCount: screenshots.length };
  }

  return { captureScrollablePage };
})();
