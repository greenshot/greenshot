---
layout: post
status: publish
published: true
title: Greenshot 1.1.6 is ready for download
tags:
- windows
- release
- bugfix
- '1.1'
---
<p><a href="http://getgreenshot.org/2013/10/16/current-development-status-future-plans/" title="Current Development Status and Plans for the Future">Not so long ago we have announced</a> that Greenshot 1.1.6 is near, today it is here :)</p>
<p>It includes a few new features/improvements but is mainly a bugfix release, fixing some issues that have been reported for the previous version, especially for those who were having problems with Greenshot "forgetting" their settings now and then and for two problems that occurring when exporting screenshots to Microsoft Office.</p>
<p>The new version is eagerly waiting for you to download it, so don't keep it waiting and get it from our <a href="/downloads/">download page</a>.</p>
<p>Here is the complete changelog for Greenshot 1.1.6 - enjoy :)<br />
<code><br />
1.1.6 build 2779 Bugfix Release</p>
<p>Bugs resolved (for bug details go to http://sourceforge.net/p/greenshot/bugs and search on the ID):<br />
* Bug #1515: Changed the settings GUI to clearly show that the interactive Window capture mode doesn't use the windows capture mode settings.<br />
* Bug #1517: export to Microsoft Word always goes to the last active Word instance.<br />
* Bug #1525/#1486: Greenshot looses configuration settings. (At least we hope this is resolved)<br />
* Bug #1528: export to Microsoft Excel isn't stored in file, which results in a "red cross" when opening on a different or MUCH later on the same computer.<br />
* Bug #1544: EntryPointNotFoundException when using higlight area or blur<br />
* Bug #1546: Exception in the editor when using multiple destination, among which the editor, and a picker (e.g. Word) is shown.<br />
* Not reported: Canceling Imgur authorization or upload caused an NullPointerReference</p>
<p>Features:<br />
* Added EXIF orientation support when copying images from the clipboard<br />
* Feature #596: Added commandline option "/inidirectory <directory>" to specify the location of the greenshot.ini, this can e.g. be used for multi-profiles...<br />
* Removed reading the greenshot.ini if it was changed manually outside of Greenshot while it is running, this should increase stability. People should now exit Greenshot before modifying this file manually.</p>
<p>Improvements:<br />
* Printouts are now rotated counter-clockwise instead of clockwise, for most people this should be preferable (#1552)<br />
</code></p>
