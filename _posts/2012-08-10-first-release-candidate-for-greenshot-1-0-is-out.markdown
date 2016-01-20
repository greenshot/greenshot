---
layout: post
status: publish
published: true
title: First release candidate for Greenshot 1.0 is out
date: !binary |-
  MjAxMi0wOC0xMCAwOTozNjowMCArMDIwMA==
date_gmt: !binary |-
  MjAxMi0wOC0xMCAwNzozNjowMCArMDIwMA==

tags:
- release candidate
- '1.0'
comments: []
---
<p>Some time ago we have <a href="/2012/04/17/working-towards-greenshot-1-0/">announced</a> our work on the next major version of Greenshot: 1.0. Today, we are taking the next step: we have just uploaded the first release candidate for the new version (1.0.1.1980). All features for 1.0 are implemented, and a friendly bunch of people have already provided helpful feedback on the latest unstable releases, so we think that the current version is quite stable and ready to be pushed out as release candidate. But of course, large-scale testing is yet to be done and you are very welcome to help out with it. Just <a href="/version-history/" target="_blank">download and install Greenshot 1.0.1.1980</a> and <a href="http://sourceforge.net/tracker/?group_id=191585&atid=937972&status=1" target="_blank">let us know</a> if you encounter any bugs or problems. (Note: as long as Greenshot 1.0 is in the release candidate phase, the big download button on our <a href="/downloads/" title="Downloads">download page</a> still points to Greenshot 0.8, always check the version history if you are looking for the latest release canidates or unstable builds.)</p>
<p>Depending on the number of issues reported, Greenshot 1.0 will hopefully be ready to be officially released as final version soon.</p>
<p>By the way: many translations are not up to date with version 1.0 yet. If you would like to help out: updating translations is is easy if you use our brand new <a href="/2012/08/10/introducing-the-brand-new-greenshot-language-editor-translators-wanted/" title="Introducing the brand new Greenshot Language Editor – translators wanted">Greenshot Language Editor</a>.</p>
<p>Here is the complete change log for Greenshot 1.0:<br />
<code><br />
Features added:<br />
* Greenshot will now run in 64 bit mode, if the OS supports it.<br />
* Added a "destinations" concept, making it possible to select all destinations from the main settings or using them inside the editor.<br />
* Added a "processor" concept, making it possible to modify the capture before it's send to a destination. Currently there is only an internal implementation which replaces the TitleFix plugin.<br />
* Added Office destinations (Word, Excel, Powerpoint, OneNote & Outlook) with dynamic resolving of open "instances".<br />
* Added Ctrl/shift logic to the editor, hard to explain (see help) but hold one of the keys down and draw..<br />
* Added a color picker in the color dialog.<br />
* Added effects: shadow, torn edges, invert, border and grayscale<br />
* Added rotate clockwise & counter clockwise<br />
* Added a preview when using the window capture from the context menu (Windows Vista and later)<br />
* Added color reduction as an option and auto detection for image with less than 256 color. When using reduction this results in smaller files.<br />
* Added direct printing to a selected printer<br />
* Added some additional logic to the IE capture, which makes it possible to capture embedded IE web-sites from other applications.<br />
* Changed multi-screen capture behaviour, assuming that capturing all screens is not a normal use-case. Now default behaviour is to capture the one with the mouse-cursor. Also the user can select which screen to capture from the context-menu.<br />
* Many languages added and updated, see our website for the whole listing!<br />
* Added some features for deploying Greenshot in companies: Fixed settings can't be changed in the settings. Settings, quicksettings and the Greenshot icon can be disabled. (See greenshot.ini)</p>
<p>Bugs resolved:<br />
* Fixed a problem with temp-files being removed before they were used, now using a delay of ~10 hours<br />
* Removed clipboard monitoring, which hopefully solves some problems with Virtual Machines<br />
* Fixed click on editor button not working if the window didn't have focus<br />
* Fixed problem with the print dialog not having focus if opened from the editor "print" button<br />
* Fixed bug #3482709 print with timestamp cropped the image<br />
* Removed the always active CaptureForm, which resulted in greenshot not "recovering" when a capture caused an exception.<br />
* Improved the auto-capture mode to honor some settings better<br />
* Synchronized the selected language to the plugins<br />
* Fixed installer issues on Windows 8, Greenshot can be used on Windows 8. Although there are still has some small issues with the Windows 8 Release Preview but these most likely are Microsoft bugs.</p>
<p>Known issues:<br />
* Greenshot general: I-Beam cursor isn't displayed correctly on the final result.<br />
* Greenshot general: Not all hotkeys can be changed in the editor. For example the pause or the Windows key need to be modified directly in the ini.<br />
* Greenshot editor: Rotate only rotates the bitmap, not the added elements or cursor<br />
* Greenshot editor: The shadow and torn edges effects don't create a transparent background yet.<br />
* Confluence Plug-in: the retrieving of the current page from firefox only works on the currently displayed Firefox tab. This is a problem since Firefox 13 and it is currently unknown if there is a fix.<br />
* OCR Plug-in: OCR is not working on 64 bit Windows, as the MODI-OCR component from Microsoft is not available in 64 bit, in this case Greenshot should be run in 32-bit or the plugins should be changed to call a 32-bit exe.</p>
<p></code></p>
