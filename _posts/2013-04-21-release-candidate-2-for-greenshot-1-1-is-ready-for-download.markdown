---
layout: post
status: publish
published: true
title: ! 'Release candidate #2 for Greenshot 1.1 is ready for download'
tags:
- windows
- release candidate
- '1.1'

---
<p>Just a short notice that we have published the second release candidate for version 1.1 of Greenshot today. It does not only fix bugs reported with the previous release candidate, but also has two additional features:</p>
<ul>
<li>It is now possible to disable the new zoom window feature for region capture in the settings dialog.</li>
<li>There is a new image format (.greenshot) for saving an edited screenshot, allowing to re-open the file later with annotations etc still being editable.</li>
</ul>
<p>The complete change log can be found below, the latest release candidate is downloadable from our <a href="/version-history/">version history page</a>. Check it out and if you encounter an error, please <a href="http://sourceforge.net/p/greenshot/bugs/">report the bug</a>. Thanks a lot :)</p>
<p><code><br />
1.1.2 build 2572 Release Candidate 2</p>
<p>Features added since 1.1.1:<br />
* Editor: Added a new image format "Greenshot" which allows the user to save the current state and continue editing later. (was already in the 1.1.1.2550)<br />
* Capture: Added an option to the settings for disabling the zoom. (this can also be done via the "z" key while capturing.)</p>
<p>Bugs resolved (for bug details go to http://sourceforge.net/p/greenshot/bugs and search on the ID):<br />
* Bug #1484, #1494: External Command plug-in issues. e.g. when clicking edit in the External Command plug-in settings, than cancel, and than edit again an error occured.<br />
* Bug #1499: Stability improvements for when Greenshot tries to open the explorer.exe<br />
* Bug #1500: Error while dragging an obfuscation<br />
* Fixed some additional unreported issues<br />
</code></p>
