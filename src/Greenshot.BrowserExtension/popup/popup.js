document.addEventListener("DOMContentLoaded", () => {
  const statusIndicator = document.getElementById("statusIndicator");
  const statusText = document.getElementById("statusText");
  const btnCaptureVisible = document.getElementById("btnCaptureVisible");
  const feedbackMessage = document.getElementById("feedbackMessage");
  const qaTrackingStatus = document.getElementById("qaTrackingStatus");
  const linkOptions = document.getElementById("linkOptions");

  function showFeedback(text, isError = false) {
    feedbackMessage.textContent = text;
    feedbackMessage.className = isError ? "feedback-msg error" : "feedback-msg success";
    feedbackMessage.classList.remove("hidden");

    setTimeout(() => {
      feedbackMessage.classList.add("hidden");
    }, 3500);
  }

  function updateStatus(isConnected, config) {
    if (isConnected) {
      statusIndicator.className = "status-indicator online";
      statusText.textContent = "Ready";
      btnCaptureVisible.disabled = false;
      btnCaptureVisible.title = "Capture visible page and send to Greenshot";
    } else {
      statusIndicator.className = "status-indicator offline";
      statusText.textContent = "Offline";
      btnCaptureVisible.disabled = true;
      btnCaptureVisible.title = "Greenshot is offline. Please start Greenshot to capture.";
    }

    if (config && config.track_tab_urls) {
      qaTrackingStatus.textContent = "Active";
      qaTrackingStatus.className = "qa-badge";
    } else {
      qaTrackingStatus.textContent = "Disabled";
      qaTrackingStatus.className = "qa-badge inactive";
    }
  }

  // Request initial status from background worker
  chrome.runtime.sendMessage({ action: "GET_STATUS" }, (response) => {
    if (response) {
      updateStatus(response.isConnected, response.config);
    }
  });

  // Capture button handler
  btnCaptureVisible.addEventListener("click", () => {
    btnCaptureVisible.disabled = true;
    btnCaptureVisible.textContent = "Capturing...";

    chrome.runtime.sendMessage({ action: "CAPTURE_VISIBLE" }, (response) => {
      btnCaptureVisible.disabled = false;
      btnCaptureVisible.innerHTML = `
        <svg class="icon" viewBox="0 0 24 24" width="16" height="16">
          <path fill="currentColor" d="M4 4h3l2-2h6l2 2h3a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2m8 3a5 5 0 0 0-5 5 5 5 0 0 0 5 5 5 5 0 0 0 5-5 5 5 0 0 0 5-5 5 5 0 0 0-5-5m0 2a3 3 0 0 1 3 3 3 3 0 0 1-3 3 3 3 0 0 1-3-3 3 3 0 0 1 3-3Z"/>
        </svg>
        Capture Visible Page
      `;

      if (response && response.success) {
        showFeedback("Sent to Greenshot!");
      } else {
        showFeedback(response?.error || "Failed to capture tab.", true);
      }
    });
  });

  // Status indicator click: trigger reconnect
  statusIndicator.addEventListener("click", () => {
    statusText.textContent = "Connecting...";
    chrome.runtime.sendMessage({ action: "RECONNECT" }, () => {
      setTimeout(() => {
        chrome.runtime.sendMessage({ action: "GET_STATUS" }, (res) => {
          if (res) updateStatus(res.isConnected, res.config);
        });
      }, 800);
    });
  });

  // Options link
  linkOptions.addEventListener("click", (e) => {
    e.preventDefault();
    if (chrome.runtime.openOptionsPage) {
      chrome.runtime.openOptionsPage();
    } else {
      window.open(chrome.runtime.getURL("options/options.html"));
    }
  });
});
