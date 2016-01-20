---
layout: post
status: publish
published: true
title: First release candidate for Greenshot 1.1

tags:
- release candidate
- '1.1'

---
<p>It has been a few months since we have released Greenshot 1.0, finally leaving beta status. This was a huge step for us and we are happy to see that the number of downloads is still rising. Greenshot 1.0 has pushed downloads past the 2 million downloads mark last year and it is still downloaded far more than 100.000 times each month.</p>
<p>Time to take it to the next level: today we have published the first release candidate for version 1.1 - we decided to keep the interval short, so that you can can benefit from the improvements and new features as early as possible. However, the list of changes and bug fixes is still fairly impressive. Most notable, the region capture mode now comes with a small zoom window, enabling pixel exact selection of the rectangle to capture. But there is a lot more, have a look at the change log below for details.</p>
<p>As usual, the disclaimer: "release candidate" means that this version has not gone through extensive testing yet. It might be buggy, so if you do not like surprises, we suggest to stick with version 1.0 until we have ironed out all issues and publish the stable release of version 1.1.</p>
<p>And, as usal, here is the complete change log - you can download the release candidate (installer or ZIP) from our <a href="/version-history/">version history</a>:<br />
<code><br />
1.1.1 build 2550 Release Candidate 1</p>
<p>Features:<br />
* General: Added zoom when capturing<br />
* General: Better Windows 8 integration: Capture window from list now has the apps and the interactive window capture is not confused by apps or the app launcher.<br />
* General: Added Special-Folder support for the OutputPath/Filenames, now one can use the following values: MyPictures, MyMusic, MyDocuments, Personal, Desktop, ApplicationData, LocalApplicationData. Meaning one can now set the output path to e.g. ${MyPictures}<br />
* Editor: The capture is now displayed in the center of the editor, the code for this was supplied by Viktar Karpach.<br />
* Editor: Added horizontal and vertical alignment for text boxes.<br />
* Printing: Added option to force monochrome (black/white) print<br />
* Plug-in: Added Photobucket plugin</p>
<p>Languages:<br />
* Installer: Added Spanish<br />
* Installer: Added Serbian<br />
* Installer: Added Finnish<br />
* General: Fixes for many languages</p>
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
* Not reported: When having Outlook installed but not the Office plugin there was no EMail destination.</p>
<p>Known issues:<br />
* Greenshot general: a captured I-Beam cursor isn't displayed correctly on the final result.<br />
* Greenshot general: Not all hotkeys can be changed in the editor. When you want to use e.g. the pause or the Windows key, you will need to be modified the ini directly.<br />
* Greenshot general: Can't capture 256 color screens<br />
* Greenshot general: Hotkeys don't function when a UAC (elevated) process is active. This we won't change as it is a Windows security measure.<br />
* Greenshot general: Capturing apps on Windows 8 when having more than one screen still causes some issues.<br />
* Greenshot editor: Rotate only rotates the screenshot, not the added elements or cursor<br />
</code></p>
