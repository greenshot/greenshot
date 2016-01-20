---
layout: post
status: publish
published: true
title: Happy Birthday to Greenshot + New Bugfix Release 1.1.8

tags:
- release
- '1.1'

---
<p>Hey, did you know that Greenshot just had it's seventh anniversary? To celebrate it, we have just uploaded a new release of Greenshot, version 1.1.8. Join the party :)</p>
<p>It mainly consists of bugfixes and small improvements, most notably improved clipboard support, configurable file naming for the Imgur plugin, as well as better readability when editing white or very bright text in the image editor.</p>
<p>As always, you can download the latest stable version from our <a href="/downloads/" title="Downloads">download page</a>.</p>
<p>For more details, the complete list of changes in Greenshot 1.1.8:</p>
<p><code><br />
1.1.8.24-g99facd5 Bugfix Release</p>
<p>Bugs resolved:<br />
* Bug #1578: Changed the behavior of creating filenames for Imgur to be configurable, default will be the date/time.<br />
* Bug #1580: removed device names for capture fullscreen submenu in multi-monitor setups, which sometimes delivered inconsistent results or even garbage characters. Sticking to descriptive text like "bottom left", which is more useful anyway.<br />
* Bug #1581: Ini-file reading is now done without locking the file, this should help a bit in cases where other applications have this file open.<br />
* Bug #1600: Found that Greenshot uses a wrong URL format on the clipboard, this fix might solve some issues<br />
* Bug: When capturing client windows on a DWM enabled system (Vista & Windows 7) with "auto" set, sometimes the capture had a blurred/transparent effect.</p>
<p>Features:<br />
* Feature #663: dark background for textbox input when editing bright-colored text<br />
* Feature #667: destinations with subdestinations can now be clicked to invoke the "main" subdestination</p>
<p>Languages:<br />
* Updates for Swedish translation and help, new Swedish translations for plugins and installer<br />
* Improvements for German translation and help<br />
* Bug #1608: Fixed typo in French translation<br />
</code></p>
