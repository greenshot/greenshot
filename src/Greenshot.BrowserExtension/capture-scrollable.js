/**
 * Greenshot Companion Extension - scrollable page capture.
 * Captures the document as viewport tiles and transfers a capture-list to Greenshot.
 */

globalThis.GreenshotScrollableCapture = (() => {
  const CAPTURE_LIST_MIME_TYPE = "application/vnd.greenshot.capturelist";
  const MAX_CAPTURE_MESSAGE_BYTES = 20 * 1024 * 1024;
  const MAX_TILES = 4096;
  let activeCapture = null;

  function delay(milliseconds) {
    return new Promise((resolve) => setTimeout(resolve, milliseconds));
  }

  function makePositions(startScroll, maxScroll, viewportSize, overlap) {
    const start = Math.max(0, Math.min(startScroll, maxScroll));
    const positions = [start];
    let position = start;
    const stride = Math.max(1, viewportSize - overlap);

    while (position < maxScroll && positions.length <= MAX_TILES) {
      const next = Math.min(position + stride, maxScroll);
      if (next <= position) {
        break;
      }
      positions.push(next);
      position = next;
    }

    return positions;
  }

  function chooseOverlap(viewportSize, overlayExtent) {
    const maximum = Math.floor(viewportSize * 0.4);
    if (maximum < 1) {
      return 0;
    }

    const minimumUsefulOverlap = Math.ceil(viewportSize * 0.1);
    const overlayOverlap = Math.ceil(overlayExtent + 16);
    return Math.min(maximum, Math.max(minimumUsefulOverlap, overlayOverlap));
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
  async function performScrollableCapture({ isConnected, nativePort, sendNativeMessage, getBrowserName, connectNativeHost, calculateOverlap, captureFromCurrentPosition }, captureState) {
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

    const pageInfo = await executeInPage(activeTab.id, (inspectOverlays) => {
      const info = {
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
        scrollY: window.scrollY,
        maxTopOverlayHeight: 0,
        maxLeftOverlayWidth: 0
      };

      if (inspectOverlays) {
        for (const element of document.querySelectorAll("*")) {
          const style = window.getComputedStyle(element);
          if (style.position !== "fixed" && style.position !== "sticky") {
            continue;
          }

          const rect = element.getBoundingClientRect();
          if (rect.width <= 0 || rect.height <= 0) {
            continue;
          }

          const topInset = Number.parseFloat(style.top);
          if (style.position === "fixed" && rect.top < window.innerHeight / 2 && rect.width >= rect.height) {
            info.maxTopOverlayHeight = Math.max(info.maxTopOverlayHeight, Math.max(0, rect.top) + rect.height);
          } else if (style.position === "sticky" && Number.isFinite(topInset)) {
            info.maxTopOverlayHeight = Math.max(info.maxTopOverlayHeight, Math.max(0, topInset) + rect.height);
          }

          const leftInset = Number.parseFloat(style.left);
          if (style.position === "fixed" && rect.left < window.innerWidth / 2 && rect.height > rect.width) {
            info.maxLeftOverlayWidth = Math.max(info.maxLeftOverlayWidth, Math.max(0, rect.left) + rect.width);
          } else if (style.position === "sticky" && Number.isFinite(leftInset)) {
            info.maxLeftOverlayWidth = Math.max(info.maxLeftOverlayWidth, Math.max(0, leftInset) + rect.width);
          }
        }
      }

      return info;
    }, [calculateOverlap]);

    if (!pageInfo.viewportWidth || !pageInfo.viewportHeight || !pageInfo.devicePixelRatio) {
      throw new Error("The browser returned invalid page dimensions.");
    }

    const maxX = Math.max(0, pageInfo.documentWidth - pageInfo.viewportWidth);
    const maxY = Math.max(0, pageInfo.documentHeight - pageInfo.viewportHeight);
    const captureStartX = captureFromCurrentPosition ? Math.min(pageInfo.scrollX, maxX) : 0;
    const captureStartY = captureFromCurrentPosition ? Math.min(pageInfo.scrollY, maxY) : 0;
    const captureWidth = pageInfo.documentWidth - captureStartX;
    const captureHeight = pageInfo.documentHeight - captureStartY;
    const overlapX = calculateOverlap ? chooseOverlap(pageInfo.viewportWidth, pageInfo.maxLeftOverlayWidth) : 0;
    const overlapY = calculateOverlap ? chooseOverlap(pageInfo.viewportHeight, pageInfo.maxTopOverlayHeight) : 0;
    const positionsX = makePositions(captureStartX, maxX, pageInfo.viewportWidth, overlapX);
    const positionsY = makePositions(captureStartY, maxY, pageInfo.viewportHeight, overlapY);
    const screenshots = [];
    let totalImageBase64Characters = 0;
    let stoppedBySafetyLimit = false;
    const totalTiles = positionsX.length * positionsY.length;

    try {
      captureLoop:
      for (const y of positionsY) {
        for (const x of positionsX) {
          if (captureState.stopRequested && screenshots.length > 0) {
            break captureLoop;
          }
          if (screenshots.length >= MAX_TILES) {
            stoppedBySafetyLimit = true;
            break captureLoop;
          }

          const projectedWidth = Math.ceil(Math.min(captureWidth, x - captureStartX + pageInfo.viewportWidth) * pageInfo.devicePixelRatio);
          const projectedHeight = Math.ceil(Math.min(captureHeight, y - captureStartY + pageInfo.viewportHeight) * pageInfo.devicePixelRatio);
          if (projectedWidth > 32767 || projectedHeight > 32767 || projectedWidth * projectedHeight > 100000000) {
            if (screenshots.length === 0) {
              throw new Error("The resulting page image would exceed Greenshot's image dimension limits.");
            }
            stoppedBySafetyLimit = true;
            break captureLoop;
          }

          const position = await executeInPage(activeTab.id, ({ x, y, inspectOverlays }) => {
            window.scrollTo({ left: x, top: y, behavior: "instant" });
            return new Promise((resolve) => {
              // Let the page paint after scrolling without changing its content or styles.
              setTimeout(() => resolve({
                scrollX: window.scrollX,
                scrollY: window.scrollY,
                viewportWidth: window.innerWidth,
                viewportHeight: window.innerHeight,
                overlays: inspectOverlays ? Array.from(document.querySelectorAll("*"), (element) => {
                  const style = window.getComputedStyle(element);
                  if (style.position !== "fixed" && style.position !== "sticky") {
                    return null;
                  }

                  const rect = element.getBoundingClientRect();
                  if (rect.width <= 0 || rect.height <= 0 || rect.right <= 0 || rect.bottom <= 0 ||
                      rect.left >= window.innerWidth || rect.top >= window.innerHeight) {
                    return null;
                  }

                  const topInset = Number.parseFloat(style.top);
                  const leftInset = Number.parseFloat(style.left);
                  const pinnedTop = style.position === "fixed"
                    ? rect.top < window.innerHeight / 2 && rect.width >= rect.height
                    : Number.isFinite(topInset) && window.scrollY > 0 && rect.top <= topInset + 1;
                  const pinnedLeft = style.position === "fixed"
                    ? rect.left < window.innerWidth / 2 && rect.height > rect.width
                    : Number.isFinite(leftInset) && window.scrollX > 0 && rect.left <= leftInset + 1;

                  if (pinnedTop) {
                    return { edge: "top", x: rect.left, y: rect.top, width: rect.width, height: rect.height };
                  }
                  if (pinnedLeft) {
                    return { edge: "left", x: rect.left, y: rect.top, width: rect.width, height: rect.height };
                  }
                  return null;
                }).filter(Boolean) : []
              }), 250);
            });
          }, [{ x, y, inspectOverlays: calculateOverlap }]);

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
            if (screenshots.length === 0) {
              throw new Error("The scrollable-page capture exceeds the 20 MiB transfer limit.");
            }
            stoppedBySafetyLimit = true;
            break captureLoop;
          }

          screenshots.push({
            index: screenshots.length + 1,
            x: position.scrollX - captureStartX,
            y: position.scrollY - captureStartY,
            viewportWidth: position.viewportWidth,
            viewportHeight: position.viewportHeight,
            overlays: position.overlays,
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

    if (screenshots.length === 0) {
      throw new Error("No page area was captured.");
    }

    let message;
    let stoppedEarly = false;
    do {
      const lastScreenshot = screenshots[screenshots.length - 1];
      const capturedWidth = Math.min(captureWidth, lastScreenshot.x + lastScreenshot.viewportWidth);
      const capturedHeight = Math.min(captureHeight, lastScreenshot.y + lastScreenshot.viewportHeight);
      stoppedEarly = screenshots.length < totalTiles;
      const captureList = {
        version: 1,
        coordinateUnit: "css-pixels",
        devicePixelRatio: pageInfo.devicePixelRatio,
        captureStartX,
        captureStartY,
        partial: stoppedEarly,
        stopReason: stoppedEarly && stoppedBySafetyLimit ? "size-or-safety-limit" : undefined,
        overlapEnabled: calculateOverlap,
        overlapX,
        overlapY,
        documentWidth: capturedWidth,
        documentHeight: capturedHeight,
        viewportWidth: pageInfo.viewportWidth,
        viewportHeight: pageInfo.viewportHeight,
        screenshots
      };
      const captureListBase64 = encodeUtf8Base64(JSON.stringify(captureList));
      message = {
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
      if (messageSize <= MAX_CAPTURE_MESSAGE_BYTES) {
        break;
      }
      if (screenshots.length === 1) {
        throw new Error("The scrollable-page capture exceeds the 20 MiB transfer limit.");
      }
      screenshots.pop();
      stoppedBySafetyLimit = true;
    } while (screenshots.length > 0);

    if (!sendNativeMessage(message)) {
      throw new Error("The connection to Greenshot was lost before the capture could be sent.");
    }

    return { success: true, title: activeTab.title, screenshotCount: screenshots.length, stoppedEarly };
  }

  async function captureScrollablePage(options) {
    if (activeCapture) {
      throw new Error("A scrollable-page capture is already running.");
    }

    const captureState = { stopRequested: false };
    activeCapture = captureState;
    try {
      return await performScrollableCapture(options, captureState);
    } finally {
      if (activeCapture === captureState) {
        activeCapture = null;
      }
    }
  }

  function requestStop() {
    if (!activeCapture) {
      return false;
    }
    activeCapture.stopRequested = true;
    return true;
  }

  return { captureScrollablePage, requestStop };
})();
