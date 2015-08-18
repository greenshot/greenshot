---
layout: post
status: publish
published: true
title: ! 'Greenshot 1.1 is available: Introducing the Capture Zoom'
author:
  display_name: greenshot
  login: admin
  email: greenshot-developers@lists.sourceforge.net
  url: http://getgreenshot.org/
author_login: admin
author_email: greenshot-developers@lists.sourceforge.net
author_url: http://getgreenshot.org/
wordpress_id: 960
wordpress_url: http://getgreenshot.org/?p=960
date: !binary |-
  MjAxMy0wNS0wNyAyMzoyNDozNiArMDIwMA==
date_gmt: !binary |-
  MjAxMy0wNS0wNyAyMToyNDozNiArMDIwMA==
tags:
- release
- features
- '1.1'
- zoom
comments: []
---
<p>You may have noticed that we have been busy with <a href="/2013/04/30/getting-closer-third-release-candidate-for-greenshot-1-1-indonesian-translation/">release candidates</a> in the last few weeks. Now, after three release candidates, the few issues that existend seem to be sorted out, so that Greenshot 1.1 is available for download now.</p>
<p><a href="/assets/wp-content/uploads/2013/05/greenshot-capture-zoom.png"><img src="/assets/wp-content/uploads/2013/05/greenshot-capture-zoom-300x250.png" alt="Greenshot&#039;s capture zoom feature" width="300" height="250" class="alignright size-medium wp-image-971" /></a>Greenshot 1.1 includes one of the recently most requested features: the new <strong>capture zoom</strong> is very helpful while selecting a region to capture: it displays a magnified view of the area around the cursor, making it easy to select pixel-exact regions, no matter how tiny they are. The zoom view is flexible enough to stay out of your way as you move the cursor on the screen, but in case you do not want or need it, you can either toggle it off/on easily during capture region selection by hitting the <kbd>Z</kbd> key, or disable/enable it in the settings.</p>
<p>Another nice addition is a <strong>new output file format</strong> called "Greenshot". When saving a screenshot from the image editor in Greenshot format, you will be able to re-open the file with Greenshot later and have your annotations, shapes etc. still editable.</p>
<p><strong>PortableApps </strong>users will be happy to hear that Greenshot is now released for the PortableApps platform, too.</p>
<p>But these are just a few of the new features, there are some more, as well as a lot of bug fixes, extended/improved and/or added translations.</p>
<p>Have a look at the detailed change log entry for Greenshot 1.1 below, or head over to <a href="/downloads/">our download page</a> right now to get hold of the new version. We hope you like it as much as we do :)</p>
<p><code><br />
1.1.4 build 2622 Release</p>
<p>Features added:<br />
* General: Added zoom when capturing with a option in the settings for disabling the zoom. (this can also be done via the "z" key while capturing.)<br />
* General: Better Windows 8 integration: Capture window from list now has the apps and the interactive window capture is not confused by apps or the app launcher.<br />
* General: Added Special-Folder support for the OutputPath/Filenames, now one can use the following values: MyPictures, MyMusic, MyDocuments, Personal, Desktop, ApplicationData, LocalApplicationData. Meaning one can now set the output path to e.g. ${MyPictures}<br />
* General: Greenshot now also comes as a "for PortableApps" download, this now will be released just like the other files (installer & .zip)<br />
* Editor: Added a new image format "Greenshot" which allows the user to save the current state and continue editing later. (was already in the 1.1.1.2550)<br />
* Editor: The capture is now displayed in the center of the editor, the code for this was supplied by Viktar Karpach.<br />
* Editor: Added horizontal and vertical alignment for text boxes.<br />
* Printing: Added option to force monochrome (black/white) print<br />
* Plug-in: Added Photobucket plugin</p>
<p>Bugs resolved (for bug details go to http://sourceforge.net/p/greenshot/bugs and search on the ID):<br />
* Bug #1327, #1401 & #1410 : On Windows XP Firefox/java captures are mainly black. This fix should also work with other OS versions and applications.<br />
* Bug #1340: Fixed issue with opening a screenshow from the clipboard which was created in a remote desktop<br />
* Bug #1375, #1396 & #1397: Exporting captures to Microsoft Office applications give problems when the Office application shows a dialog, this is fixed by displaying a retry dialog with info.<br />
* Bug #1375: Exported captures to Powerpoint were displayed cropped, which needed extra actions to correct.<br />
* Bug #1378: Picasa-Web uploads didn't have a filename and the filename was shown as "UNSET" in Picasa-Web.<br />
* Bug #1380: The window corners on Windows Vista & Windows 7 weren't cut correctly. While fixing this issue, we found some other small bugs which could cause small capture issues on Vista & 7 it also used more resources than needed.<br />
* Bug #1386: Resize issues with some the plugin configuration dialogs.<br />
* Bug #1390: Elements in 1.0 are drawn differently as in pre 1.0<br />
* Bug #1391: Fixed missing filename in the Editor title<br />
* Bug #1414: Pasting captures as HTML-inline in Thunderbird doesn't work when using 256-colors.<br />
* Bug #1418: Fixed a problem with the editor initialization, in some small cases this gave an error as something happend at the same time.<br />
* Bug #1426: Added some checks for some configuration values, if they were not set this caused an error<br />
* Bug #1442: If copying an image from Outlook via the clipboard the image was cropped<br />
* Bug #1443: Image exports to Microsoft Word didn't have the "Lock aspect ratio" set<br />
* Bug #1444: Colors were disappearing when "Create an 8-bit image if colors are less than 256 while having a > 8 bits image" was turned on<br />
* Bug #1462: Auto-filename generation cropping title text after period<br />
* Bug #1481: when pasting elements from one editor into another the element could end up outside the visible area<br />
* Bug #1484, #1494: External Command plug-in issues. e.g. when clicking edit in the External Command plug-in settings, than cancel, and than edit again an error occured.<br />
* Bug #1499: Stability improvements for when Greenshot tries to open the explorer.exe<br />
* Bug #1500: Error while dragging an obfuscation<br />
* Bug #1504: InvalidCastException when using the brightness-filter<br />
* Reported in forum: Fixed a problem with the OCR, it sometimes didn't work. See: http://sourceforge.net/p/greenshot/discussion/676082/thread/31a08c8c<br />
* Not reported: Flickr configuration for the Family, Friend & Public wasn't stored.<br />
* Not reported: If Greenshot is linked in a Windows startup folder, the "Start with startup" checkbox wasn't checked.<br />
* Not reported: Some shortcut keys in the editor didn't respond.<br />
* Not reported: Fixed some issues with capturing windows that were larger than the visible screen, logic should now be more reliable.<br />
* Not reported: Fixed some cases where Dragging & Dropping an image from a browser on the editor lost transparency.<br />
* Not reported: Undo while in an Auto-Crop made the editor unusable.<br />
* Not reported: When first selecting a printer, the main printer destination has been replaced by the selected one, making the Windows printer dialog unavailable for further prints<br />
* Not reported: Open last capture in explorer doesn't open the right location<br />
* Not reported: Fixed some issues where the sub-menus of the context menu moved to the next screen.<br />
* Not reported: When having Outlook installed but not the Office plugin there was no EMail destination.<br />
... and more</p>
<p>Languages:<br />
* Added Indonesian<br />
* Installer: Added Spanish<br />
* Installer: Added Serbian<br />
* Installer: Added Finnish<br />
* General: Fixes for many languages</p>
<p>Known issues:<br />
* Greenshot general: a captured I-Beam cursor isn't displayed correctly on the final result.<br />
* Greenshot general: Not all hotkeys can be changed in the editor. When you want to use e.g. the pause or the Windows key, you will need to be modified the ini directly.<br />
* Greenshot general: Can't capture 256 color screens<br />
* Greenshot general: Hotkeys don't function when a UAC (elevated) process is active. This we won't change as it is a Windows security measure.<br />
* Greenshot general: Capturing apps on Windows 8 when having more than one screen still causes some issues.<br />
* Greenshot editor: Rotate only rotates the screenshot, not the added elements or cursor<br />
</code></p>
