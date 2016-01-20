---
layout: faq
status: publish
published: true
title: How can I use the Windows key as global hotkey for Greenshot?
tags: []

---
<p>There are some special keys that are handled differently by Windows and therefore cannot be inserted into the hotkey configuration panel (Settings > Hotkeys) directly. E.g. this is the case with the <kbd>Windows</kbd> or <kbd>Pause</kbd> keys. However, you can still use them with Greenshot - you just have to edit Greenshot's configuration file manually in order to do so:</p>
<ul>
<li>Exit Greenshot (important, otherwise your manual changes might be overwritten).</li>
<li>Locate and open <a href="/faq/where-does-greenshot-store-its-configuration-settings/" title="Where does Greenshot store its configuration settings?">Greenshot's configuration file</a>.</li>
<li>Find the hotkey settings within the file, they are called <code>RegionHotkey</code> (Capture region), <code>WindowHotkey</code> (Capture Window), <code>FullscreenHotkey</code> (Capture fullscreen), <code>LastregionHotkey</code> (Capture last region) and <code>IEHotkey</code> (Capture Internet Explorer) and can usually be found at the beginning of the file.</li>
<li>Change the hotkeys as desired, e.g. if you want to use <kbd>Windows</kbd> + <kbd>P</kbd> to invoke region capture, change the <code>RegionHotkey</code> entry as follows:<br><br />
<code>RegionHotkey=Win + P</code></li>
<li>Start Greenshot.</li>
</ul>
<p><strong>See also:</strong><br />
<a href="/faq/where-does-greenshot-store-its-configuration-settings/" title="Where does Greenshot store its configuration settings?">Where does Greenshot store its configuration settings?</a></p>
