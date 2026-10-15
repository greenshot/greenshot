document.addEventListener("DOMContentLoaded", () => {
  const chkTrackUrls = document.getElementById("chkTrackUrls");
  const selFormat = document.getElementById("selFormat");
  const chkCalculateOverlap = document.getElementById("chkCalculateOverlap");
  const chkCaptureFromCurrentPosition = document.getElementById("chkCaptureFromCurrentPosition");
  const savedIndicator = document.getElementById("savedIndicator");

  function showSaved() {
    savedIndicator.style.opacity = "1";
    setTimeout(() => {
      savedIndicator.style.opacity = "0";
    }, 1500);
  }

  savedIndicator.style.opacity = "0";
  savedIndicator.style.transition = "opacity 0.3s ease";

  // Load current settings
  chrome.runtime.sendMessage({ action: "GET_STATUS" }, (response) => {
    if (response && response.config) {
      chkTrackUrls.checked = response.config.track_tab_urls !== false;
      selFormat.value = response.config.capture_format || "png";
      chkCalculateOverlap.checked = response.config.calculate_overlap !== false;
      chkCaptureFromCurrentPosition.checked = response.config.capture_from_current_position === true;
    }
  });

  function saveSettings() {
    const updated = {
      track_tab_urls: chkTrackUrls.checked,
      capture_format: selFormat.value,
      calculate_overlap: chkCalculateOverlap.checked,
      capture_from_current_position: chkCaptureFromCurrentPosition.checked
    };

    chrome.runtime.sendMessage({ action: "UPDATE_CONFIG", config: updated }, () => {
      showSaved();
    });
  }

  chkTrackUrls.addEventListener("change", saveSettings);
  selFormat.addEventListener("change", saveSettings);
  chkCalculateOverlap.addEventListener("change", saveSettings);
  chkCaptureFromCurrentPosition.addEventListener("change", saveSettings);
});
