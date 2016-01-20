---
layout: post
status: publish
published: true
title: Greenshot 0.8.1 RC1 is available for download

tags:
- release
- release candidate

---
<p>We have just uploaded the first <strong>release candidate</strong> for the next Greenshot version, 0.8.1.<br />
Thanks to Robin's tireless efforts, many bugs of version 0.8.0.0627 have been fixed, and some new features have been added as well.<br />
We believe that the release candidate is quite stable, but please keep in mind that it is not yet thoroughly tested by the masses.</p>
<p>Enough talk for now, here is the <a href="http://sourceforge.net/projects/greenshot/files%2FGreenshot%2FGreenshot%200.8%20beta%2FGreenshot-INSTALLER-0.8.1.1312-RC1.exe/download">download link for Greenshot 0.8.1 RC 1</a>. We hope you like it :) In case of bugs, please <a href="https://sourceforge.net/tracker/?group_id=191585&atid=937972&status=1">let us know</a>.<br />
Thanks a lot, have fun!</p>
<p>P.S.: Here is the list of changes for the release candidate:</p>
<p><code><br />
Bugs resolved:<br />
* Image editor problems when exiting Greenshot.<br />
* Systray icon wasn't removed when stopping Greenshot<br />
* Installer fixes for silent installation and the selected installer language will be passed to Greenshot<br />
* Hotkeys on Windows 7 x64 weren't working, should be okay now.<br />
* Changed variable naming from %VAR% to ${VAR}, a.o to prevent early resolving on the command-line<br />
* Fixed problems with calculating the window size for Windows Vista & Windows 7.<br />
* Fixed annoying bug in editor which made the screen jump if the editor had scrollbars, got even more annoying with the new IE capture feature.<br />
* Fixed mousewheel scrolling in editor<br />
* Capture & editor performance improved<br />
* Fixed capture region selection screen losing focus<br />
* Many other minor stability fixes<br />
* At first start all available languages can be selected</p>
<p>Features added:<br />
* Changed the configuration from a proprietary binary format to a readable & modifiable "greenshot.ini".<br />
* Added the Dutch language as a third default language for all Greenshot parts (application, plugins and installer)<br />
* Added all currently available languages to the installer but only those that your windows can display are shown.<br />
* Added configurable hotkeys<br />
* Added Aero (DWM) window capture on Windows Vista and later! Either the window is captured with transparency or it is possible to replace the transparent window border with a background color, making the capture look cleaner.<br />
* Added Internet Explorer capture. Select your IE - Tab from the Greenshot context menu or use the default hotkey "Ctrl + Shift + PrintScreen" to capture the active IE page.<br />
* Added OCR Plugin, this will only work when Microsoft Office 2003 or 2007 is installed. Unfortunately there is no way to check what languages Office supports, this needs to be set manually! To set the language, go into the Greenshot configuration screen, a new "plugin" tab is available. Click on the tab, on the OCR plugin and on the configure button. This should allow you to change the language which is used to OCR your selection!<br />
* Added environment variable support for the filename and path. Now one can use e.g. "${TMP}"…<br />
* Added "experimental" Windows "Enhanced" MetaFile (=Vector graphics) support. The bitmap can be resized "without" quality loss. To use this, e.g. drag/drop a "WMF" file from the Microsoft Office "Clipart" directory on the open Greenshot editor.<br />
* Added Imgur (see: http://Imgur.com) plugin<br />
* Added plugin white/black listing, mainly needed for administrators specifying which plugins will be loaded and which not.<br />
* Added Outlook support, if Outlook is available this is used instead of MAPI: e.g. Creating HTML email with "in-body" image using the default signature for new Emails.<br />
* Added GDI capturing windows with transparency, only works if Aero (DWM) is disabled!<br />
* Added update check, if an update is detected a popup is shown asking if the user wants to download this<br />
* Added HTML as clipboard format</p>
<p>Known bugs:<br />
* When having multiple monitors the systray context menu will have options which apear on a different screen. (Issue in Microsoft Windows)<br />
* The "I" Mouse-Cursor will not be rendered correctly on the final image. (Issue in Microsoft Windows)<br />
* There might still be some minor rendering problems due to performance improvements, these will not be visible on the resulting image. We will fix them as soon as we find them.<br />
</code></p>
