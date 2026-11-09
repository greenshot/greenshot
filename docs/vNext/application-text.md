# Text from the applications

When Greenshot captures the screen, it also reads the text the applications show in the capture through UI Automation
(Dapplo.Windows.Automation `UiAutomationText.ReadVisibleTextAsync`). That text is exact, and a link brings its target (the web
address of a link in a browser, `mailto:` and `tel:` links). OCR only adds text where the applications gave none: text in images,
remote desktops (Citrix, Remote Desktop, Horizon, Hyper-V, VNC only show pixels here), applications without UI Automation text.

- Read in the background right after the capture of the screen (`Greenshot.Base/Capturing/ApplicationText.cs`), at most 2 seconds
  for all windows together. The OCR waits for it, so it knows where its lines are needed.
- The lines are kept with the capture like OCR lines (`IOcrLineFeature.Source` is `UiAutomation`, `OcrWordInfo.LinkTarget` the link),
  so the text selection (T), the OCR step and destination, obfuscation and Smart text all use them.
- Only what is visible in the captured pixels: a word counts when its middle isn't covered by another window, password fields and
  text scrolled away aren't read. Neither the text nor link targets are logged, only counts and times.
- Not for AI tool captures: windows excluded from AI tools are only blacked out in the pixels.
- Smart text takes links with a target: a web address, an e-mail address (`mailto:`) or a phone number (`tel:`) under its visible
  text, e.g. a link "Download" gives the address it opens.
- Expert setting `ReadApplicationText` (Core, default true): false uses only OCR, e.g. to find out where a wrong text comes from.
