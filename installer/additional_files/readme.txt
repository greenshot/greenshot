Greenshot: A screenshot tool optimized for productivity.
Save a screenshot or a part of the screen to a file within a second. Supports several image formats.
Apply text and shapes to the screenshot. Offers capture of window, region or full screenshot.


CHANGE LOG:

All details to our tickets can be found here: https://greenshot.atlassian.net

# Greenshot 1.x.xxx

Bugs fixed:

Features added:

# Greenshot 1.3.296

Bugs fixed
* Fix Administrative installation via user interface [#546](https://github.com/greenshot/greenshot/issues/546) [#611](https://github.com/greenshot/greenshot/issues/611) [#598](https://github.com/greenshot/greenshot/issues/598)

Features added:
* Installer: Allow Choice between All-Users (Administrative) and Current-User Installation [#625](https://github.com/greenshot/greenshot/pull/625)

# Greenshot 1.3.292

Bugs fixed:
* Fix Administrative installation via command line using /ALLUSERS [#601](https://github.com/greenshot/greenshot/issues/601) [#619](https://github.com/greenshot/greenshot/issues/619) 

# Greenshot 1.3.290

Note: the version information for the first 1.3 release is outdated/incomplete. Due to the long timespan and large amount of changes between 1.2 and 1.3 we lost track. Sorry.

Greenshot 1.3 is the first Greenshot which targets .NET 4.7.2 which just by doing to solves some general issues in the area of Internet Explorer capturing, TLS communication and some other minor issues.

We've also added some new Windows 10 functions, like notifications, OCR and sharing with Windows 10 apps.

## Supported platforms:
As always Greenshot comes "as-is" with no guarantee whatsoever, we are not paid enough to make this possible.
Due to the amount of work involved in testing Greenshot on different platforms, and needing hardware to do so, we cannot support all of them. What does this mean?

* Windows 10 "current" - We develop & test Greenshot on the latest available version of Windows 10, which means you can pretty much assume it works on there and if not we can work on it.
* Windows 10 "previous" - We don't have the hardware to have all supported Windows versions side-by-side, we try to keep the compatibility high. If there are very Windows specific bugs, we can look at it. In general we add new Windows features with a check for the version, preventing issues with older versions.
* Windows 10 insiders - If someone is on an insider, and wants to use Greenshot, there is no guarantee. But please do report issues, and make sure you mention the build you are on. As insiders are the next "current", we will try to look at this.
* Windows 7 - this is no longer supported by Microsoft, and we will not buy extended support licenses for older Windows, we rather motivate people to upgrade to a normal supported version. It should still work on Windows 7, but we can't guarantee this and functionality will be degraded compared to that on Windows 10.
* Windows 8 - Greenshot 1.2 mostly worked on it, thus we assume 1.3 will do so too, but we cannot support this.
* Windows Server version XYZ- We do not have the hardware nor licenses to test on this, do so at your own risk. You can report issues, but we cannot support specific Windows Server issues, unless someone wants to pay for the time, hardware & licenses.
* Linux via Wine or CrossOver - Although we made some small changes to Greenshot to get basics working, it's not supported and to be honest it's a lot of work to get it running, and the result is very disappointed.
* MacOS - We have a version of Greenshot for MacOS in the appstore [here](https://apps.apple.com/us/app/greenshot/id1103915944).

## Support for other software?
We have many request, mainly from companies in form of "we use an eMail client from this company, and have a tool which does this cool thing, can you support this?" This depends, if it's something which benefits the masses, we can look at it. An example is PowerToys, where we [interact with the maintainers](https://github.com/microsoft/PowerToys/wiki/Application-Compatibility) to see how we can make Greenshot work together and not have PowerToys break us. We can't support all, and we cannot buy licenses & install every product.

## Let us see the changes:

### New features
* The editor now has a zoom, completely build by our awesome community member @KillyMXI PR #201 
* Windows 10 has out of the box OCR support, this can now be used from Greenshot. We added functionality which is on par with that we provided via an old Microsoft component called "MODI". To reduce our maintenance, we removed the old functionality. This is the first step with Windows 10 OCR, we have some great plans with this.
* Greenshot can now use the Windows 10 app sharing, share screenshots with app which support bitmaps.
* Greenshot now can use the Windows 10 notifications system, which looks better. We will add more user friendly functionality for this later.
* [FEATURE-1110](https://greenshot.atlassian.net/browse/FEATURE-1110): Making it possible to use a hotkey to open the clipboard, for details look at the issue.

### General improvements:
* Improved the about and error details with better version information, and OS name.
* Upgraded the .NET Framework dependency to 4.7.2, any lower version would cause a lot of additional work and make the installer even bigger.
* Used more recent versions of software components, which make it easier to find bugs.
* DPI improvements, so people can use Greenshot with high DPI screens.
* Made the selection boxes (grippers) bigger and reize with the DPI settings
* Update check should have less impact with us and for the user
* Added Italian to the installer, a lof of PR's from @bovirus e.g. PR #224, #230
* Added support to generate random characters in the filename PR #216
* Changed the cloud services Box, DropBox and Imgur to use the default browser in combination with getgreenshot.org instead of the embedded, solving incompatibility issues.

### Bug fixes

#### Functionality fixes:
* [BUG-2693](https://greenshot.atlassian.net/browse/BUG-2693), [BUG-2693](https://greenshot.atlassian.net/browse/BUG-2693), [BUG-2743](https://greenshot.atlassian.net/browse/BUG-2743): Greenshot doesn't recoginize a MAPI client
* [BUG-2535](https://greenshot.atlassian.net/browse/BUG-2535): Greenshot selects invisible windows fix was suggested by Raymond Chen [here](https://devblogs.microsoft.com/oldnewthing/20200302-00/?p=103507)
* [BUG-2544](https://greenshot.atlassian.net/browse/BUG-2544): Enabled TLS1.1 & 1.2 to fix jira connectivity and do not use the JIRA session support as this was deprecated.
* [BUG-2529](https://greenshot.atlassian.net/browse/BUG-2529): This should most likely fix the excessive update checks.
* [BUG-1919](https://greenshot.atlassian.net/browse/BUG-1919): Screenshot works once on Internet Explorer
* [BUG-1943](https://greenshot.atlassian.net/browse/BUG-1943): Picassa no longer supported by Google - Picassa plugin update to Google Photos
* [BUG-2127](https://greenshot.atlassian.net/browse/BUG-2127): Incorrect link image to picasa
* [BUG-2170](https://greenshot.atlassian.net/browse/BUG-2170): Editor buttons are too small on high-resolution PC
* [BUG-2300](https://greenshot.atlassian.net/browse/BUG-2300): User need to do a lot of CTRL + Z to undo the ellipse/rectangle modification
* [BUG-2736](https://greenshot.atlassian.net/browse/BUG-2736): Cropping a capture where a speech bubble is shown doesn't move the bubble tail
* PR #157 -- Fixing textbox issue with polish letter "ą"
* Bug #124, some minor issues with the EmailDestination
* Bug #149: If using a full screen capture of the current monitor, not the default, the mouse cursor is not visible.
* Bug #279 External commands where duplicated in the editor and dynamic destination picker
* Bug #283 Fixed an issue with drawing Unicode icons in the editor

#### Stability fixes:
* [BUG-2644](https://greenshot.atlassian.net/browse/BUG-2644): Fixed a NPRE when exporting to Powerpoint when it's not running yet.
* [BUG-2542](https://greenshot.atlassian.net/browse/BUG-2542): Fix for shutdown issue (ImgurPlugin)
* [BUG-2249](https://greenshot.atlassian.net/browse/BUG-2249): Error after switching between colors
* [BUG-2303](https://greenshot.atlassian.net/browse/BUG-2303): Greenshot 1.2.10 Build 6 (64 bit) - Dotnet 4.0 framework crash.
* [BUG-2307](https://greenshot.atlassian.net/browse/BUG-2307): Using the pencil tool, changing colors, undo, pencil tool again, produces error dump.
* [BUG-2309](https://greenshot.atlassian.net/browse/BUG-2309): Exception while editing screen shot
* [BUG-2403](https://greenshot.atlassian.net/browse/BUG-2403): error message came up immediately after a restart of Windows.
* [BUG-2435](https://greenshot.atlassian.net/browse/BUG-2435): Crash
* [BUG-2463](https://greenshot.atlassian.net/browse/BUG-2463): Crash on moving freehand drawing
* [BUG-2484](https://greenshot.atlassian.net/browse/BUG-2484): Unexpected error pop-up after DRIVER VERIFIER IOMANAGER VIOLATION Blue Screen of Death
* [BUG-2486](https://greenshot.atlassian.net/browse/BUG-2486): program window goes white with red x through it
* [BUG-2552](https://greenshot.atlassian.net/browse/BUG-2552): Crashed when drawing new colored line after a ctrl+z undo.
* [BUG-2557](https://greenshot.atlassian.net/browse/BUG-2557): sudden shut down due to error , Error
* [BUG-2567](https://greenshot.atlassian.net/browse/BUG-2567): Automatic error, tells me to report a bug
* [BUG-2577](https://greenshot.atlassian.net/browse/BUG-2577): Erreur inattendue
* [BUG-2592](https://greenshot.atlassian.net/browse/BUG-2592): Crash when copying to clipboard
* [BUG-2615](https://greenshot.atlassian.net/browse/BUG-2615): Greenshot crashed while changing line color
* [BUG-2642](https://greenshot.atlassian.net/browse/BUG-2642): Greenshot Image Editor crashed when I clicked "undo"
* [BUG-2654](https://greenshot.atlassian.net/browse/BUG-2654): GS crashing when logging off from a Windows account right after logging-on
* [BUG-2669](https://greenshot.atlassian.net/browse/BUG-2669): Exception on startup of Greenshot
* [BUG-2684](https://greenshot.atlassian.net/browse/BUG-2684): Freehand tool causes crash after color change & undo
* [BUG-2698](https://greenshot.atlassian.net/browse/BUG-2698): Imgur: Object reference not set to an instance of an object.
* [BUG-2699](https://greenshot.atlassian.net/browse/BUG-2699): Crash on deleting a line path
* [BUG-2700](https://greenshot.atlassian.net/browse/BUG-2700): Imgur: Continued Error
* [BUG-2703](https://greenshot.atlassian.net/browse/BUG-2703): Crash dump: "Message: The notification platform is unavailable."
* [BUG-2707](https://greenshot.atlassian.net/browse/BUG-2707): Moving or editing object (usually cursor object) occasionally crashes Greenshot
* #246,[BUG-2448](https://greenshot.atlassian.net/browse/BUG-2448), [BUG-2593](https://greenshot.atlassian.net/browse/BUG-2593), [BUG-2427](https://greenshot.atlassian.net/browse/BUG-2427), [BUG-2444](https://greenshot.atlassian.net/browse/BUG-2444), [BUG-2272](https://greenshot.atlassian.net/browse/BUG-2272): Inconsistent scale behavior when scaling objects with shift modifier


Greenshot 1.2.10 RELEASE

Bugs fixed:
* [BUG-2235] - Imgur authentication issues due to imgur api change
* [BUG-2227] - NullReferenceException when accessing the Imgur History
* [BUG-2225] - NullReferenceException when changing the color of text
* [BUG-2219] - Korean is spelled incorrectly in the installer
* [BUG-2213] - NullReferenceException in the Freehand tool
* [BUG-2203] - ArgumentNullException in the Freehand tool
* [BUG-2141] - Imgur authentication issues due to old embedded IE
* [BUG-2172] - NullReferenceException when using the speech bubble
* [BUG-2246] - Login issues with the Atlassian Jira Cloud

Features:
* [FEATURE-1064] - Added CTRL+Backspace & CTRL+A to the text tool
 

Greenshot 1.2.9.129-569de71 RELEASE

Bugs fixed:
* [BUG-2051] - Scroll-lock button not usable as hotkey
* [BUG-2056] - Cannot export to external command Paint.NET if .greenshot format is used
* [BUG-2081] - Canvas resize (Ctrl + / Ctrl -) only works via numpad keys
* [BUG-2093] - Shadow effects not rendering correctly on Windows 10
* [BUG-2095] - 'Save as' doesn't remember last saved directory (after restart)
* [BUG-2097] - Window border is not captured on Windows 10
* [BUG-2102] - InvalidOperationException when selecting a color
* [BUG-2104] - Speechbubble can't be used after copy/paste
* [BUG-2105] - Window border is not captured on Windows 10
* [BUG-2108] - Capture last region doesn't work
* [BUG-2109] - Double-click on textbox causes NullReferenceException
* [BUG-2111] - Drag and Drop image file on editor doesn't work
* [BUG-2114] - Storage location reset to default if not available during start
* [BUG-2115] - Error while trying to upload screen shot to Jira
* [BUG-2116] - MemberAccessException when opening the color dialog
* [BUG-2119] - crash on editing (dragged line)
* [BUG-2120] - Greenshot Editor Canvas Changed to White Screen
* [BUG-2121] - NullReferenceException when moving text
* [BUG-2122] - Jira Plugin: Crash when uploading to Jira 
* [BUG-2124] - Flickr plugin generates wrong image link
* [BUG-2125] - Send to OneNote does not work
* [BUG-2126] - MemberAccessException during un-DropShadow of Ellipse
* [FEATURE-998] - Opening last capture in explorer should select/jump to the file
* [FEATURE-992] - When reusing the editor, it is not brought to the front (second try)

Added Taqbaylit as a new language.


1.2.9.112-9bc62ac RELEASE


This is a bugfix release for the Greenshot 1.2.9.104-3721c10 RELEASE

Bugs fixed:
* BUG-2065 OverflowException in the editor
* BUG-2077 It's not possible to disable the update check
* BUG-2080 Exception when opening the context menu
* BUG-2089 Greenshot crashes when hotkey registration fails and OneDrive is not installed
* BUG-2091 Greenshot .zip version has wrong log configuration

Features added:
* FEATURE-992 When reusing the editor, it is not brought to the front
* FEATURE-995 Paste images inside Greenshot editor to the current cursor location
* FEATURE-998 Opening last capture in explorer should select/jump to the file
* FEATURE-1000 Set default focus on the OK-button in the Greenshot quality dialog

Translations modified:
* Nederlands by Stephan Paternotte


1.2.9.104-3721c10 RELEASE

Fixed bugs:
* BUG-1762 Dropshadow & tornedge prompts for settings every time
* BUG-1812 Editor opens outside the screen
* BUG-1876 Already running message, multi user environment
* BUG-1884 OCR text has trailing blank spaces
* BUG-1887 Editor hangs on exit - hang time proportional to number of objects
* BUG-1890 Slight cropping around window on Windows 10
* BUG-1892 Greenshot saves blank JPG file with reduce colors
* BUG-1894 Imgur remote server error 429
* BUG-1896 JIRA Plugin doesn't work with JIRA Server v7.0
* BUG-1898 Specify GPLv3 in the license text
* BUG-1908 External Command, add commands at startup
* BUG-1918 Editor Speechbubble Artifacts when shadow and transparent
* BUG-1933 Greenshot Installer sets bad registry key permission
* BUG-1935 Delay when pasting and ShapeShifter from FlameFusion is running
* BUG-1941 Error when creating a speech bubble in the editor
* BUG-1945 Failure starting Greenshot at system startup
* BUG-1949 Can't delete Imgur upload
* BUG-1965 Activation border around window is visible in the capture
* BUG-1991 Greenshot portable (PAF) uses wrong log configuration
* BUG-1992 OutputFilePath setting is wrong config used on a different system
* BUG-2011 Editor issues when the font is gone or broken
* BUG-2043 Fixed an issue which occured after wakeup from sleep
* BUG-2059 The default frequency of update checks is now reduced from every day to every 14 days
* BUG-2070 Installing Greenshot deletes everything in the target directory
* BUG-2071 Authentication not completing with Box.com

Added features:
* FEATURE-863 The start value of the counters can now be changed
* FEATURE-916 Added icon save format (capture will be resized to 16x16, 32x32, 48x48 or 256x256)
* FEATURE-919 Added a way to increase the editor canvas size (use Ctrl + / Ctrl -)
* FEATURE-945 Added environment variables resolving to the external command plugin
* FEATURE-946 Updated to Inno-Setup 5.5.9 for improved installer security
* FEATURE-958 Greenshot is now using code signing certificates

Added or changed translation:
* Català by Gabriel Guix.
* Nederlands by Stephan Paternotte


1.2.8.14-b7c8384 RELEASE

Changes for this release:
This has Imgur improvements for the newer API version, this is a backport from Greenshot 1.2.9
With the move to a new hosting platform, we also noticed our update checks are way to often, this needed to be reduced.


1.2.8.12-cab854b Release 

Bugs Resolved:

BUG-1527 / BUG-1848 / BUG-1850 / BUG-1851 / BUG-1859 : Greenshot stops responding, hangs or crashes
BUG-1843 / BUG-1844 / BUG-1846 : Imgur problems with authenticated uploads
BUG-1864: Imgur link wasn't copied to the clipboard

Features:
FEATURE-896: Use Imgur with HTTPS (with changing the Imgur API from V2 to V3 this was already required for the upload anyway.)


1.2.7.2-342a506 RELEASE

Bugs Resolved:
* BUG-1809: OverflowException when changing the windows input language
* BUG-1835: Imgur: uploads were cancelled due to a timeout which was set too small
* BUG-1833: Imgur: API-Key issues when using anonymous uploads
* Github Pull 1: Fixed error calling external command. Thanks to viper3400


1.2.6.7-359dcf3 RELEASE

Bugs Resolved:
* BUG-1769: Switched to OAuth 2 for Picasa Authentication, OAuth 1.x will be terminated as of 20th of April 2015.
* BUG-1770: Fix problems when a font doesn't want to draw itself.
* Bug 1774: Problems logging in to Box.com

Features add:
* FEATURE-838: Added support for dragging an image from google image search results directly into the editor.


1.2.5.19-RELEASE-63412d3b9e29

Bugs Resolved:
* BUG-1578, BUG-1657: by default, Imgur plugin no longer passes title and filename to Imgur (for new installations, existing configuration files won't be affected)
* BUG-1655: Greenshot uses the default proxy, even if the "use default proxy" check-box is not checked.
* BUG-1730: File type in the Save-As dialog doesn't default to configured output format.
* BUG-1735: Editor crashed when trying to scale down a screenshot with text element
* BUG-1741: Fixed a bug in the confluence plug-in, error when searching for nothing, by improving the upload GUI.
* BUG-1750: Filename pattern is now checked for invalid characters to avoid exception when saving directly.
* BUG-1752: Fixed horizontal alignment for text boxes
* BUG-1753: Fixed problem with saving/loading of the drop shadow & torn edge effects

Languages:
* Portuguese (Portugal): application translation updates + new translation for plugins and help, thanks to Luis Neves
* Indonesian: translation updates, thanks to Afif Brika
* Korean: application translation updates + new translation for plugins and help, thanks to Jin-Hwan Jeong
* French: translation updates, thanks to thierry


1.2.4.10-RELEASE-94d66e09eb89

Bugs Resolved:
* BUG-1724: Error when using "Alt + PrintScreen" after clicking system tray


1.2.4.9-RELEASE-b4b6c6546d12

Features:
* Added the possibility to select the region to capture by using the keyboard, use the cursor keys to move the cursor (ctrl-key speeds up the movement) and the enter key to mark the start and ending.
* Added support for another clipboard format "Format17" (aka CF_DIBV5) which could improve the result of the "open image from clipboard" and drag&drop actions.
* Editor: a resize effect with settings window has been added.
* Editor: a settings window for the torn-edge effect has been added, the settings will be stored.
* Editor: a settings window for the drop shadow effect has been added, the settings will be stored.
* OneNote: Enabled and enhanced the OneNote destination, so we can test this and see if it's worth releasing.
* External command: If a command outputs an URI this will be captured and placed on the clipboard, the behaviour currently can only be modified in the greenshot.ini
* FEATURE-184, FEATURE-282, FEATURE-486: Image editor now has a speech bubble
* FEATURE-281, FEATURE-669, FEATURE-707, FEATURE-734: Image editor now has auto incrementing labels
* FEATURE-757: Greenshot will now store the last used region in the greenshot.ini, which makes it also available after a restart.
* FEATURE-758: Due to the fact that more and more high DPI displays are used, we added a setting to change the icon size.
* FEATURE-776: Improvement of the torn edge settings dialog
* FEATURE-777: Improvement of the font family settings, also changed the used font for every family making it easier to find your font.

Changes:
* Optimized Greenshots update check to use even less traffic by checking the time-stamp of the update feed before downloading it.
* JIRA: With JIRA 6.x using the SOAP (Webservice) API the access has gotten really slow, we improved the performance slightly by loading some information parallel. (In Greenshot 2.x we will move to another API.)
* Dynamic destination context-menu: If a destination has child items the parent is still selectable and executes the default export, we now no longer repeat the parent in the children.
* Dynamic destination context-menu: We are now leaving the sorting to the destination code, this allows us to e.g. show the default printer on top of the list.

Bugs Resolved:
* BUG-1559, BUG-1643: Repeating hotkeys are now prevented.
* BUG-1610: Image editor: 'Obfuscate' and 'Highlight' and more, now should rotate / resize correctly.
* BUG-1619: Image editor: Autocrop now also considers the elements.
* BUG-1620: Greenshot crashing Chrome running in 'Windows 8 Mode'
* BUG-1653: Accessibility issues: Editor "File" menu entry can't be activated and missing translations
* BUG-1667: removed horizontal alignment of textbox in input mode, as it caused problems with textbox focus and could not be implemented consistently anyway (no vertical alignment possible)
* BUG-1671: Fixed error that occurred when double-clicking systray icon before the first time a screenshot was saved to file
* BUG-1686: Shadow (drop shadow or torn edge) grows if a filter (highlight etc) is used and an element is moved around
* BUG-1688: While drawing a textbox there are black vertical lines inside
* BUG-1695: Fixed an issue with processing the response from Imgur, which caused the error "Value cannot be null. Parameter name: key"
* BUG-1699: UI jumps when Textbox or SpeechBubble tools are selected, and changing to another tool. (also the other way around)
* BUG-1700: IE capture only works once
* BUG-1709: Alignment of the text and speech-bubble wasn't always correctly saved in the .greenshot file
* BUG-1710: After resizing the canvas the edges had a slightly different color (1px)
* BUG-1719: color dialog didn't accept HTML color names
... and a lot more small bug fixes to make Greenshot more stable.

Languages:
* New language: Latvian (thanks to Kārlis Kalviškis for providing the translation)
* Updates for Ukrainian, Japanese and Italian translation


1.1.9.13-g01ce82d Windows 8.1 & Box bug-fix Release

Bugs resolved:
* BUG-1604, BUG-1631, BUG-1634: Capture problems since update to Windows 8.1 with multiple displays
* BUG-1627: Box upload failed since 1.1.8
* Unreported: The greenshot.ini values "NoGDICaptureForProduct" and "NoDWMCaptureForProduct" had incorrect defaults, which might cause issues with some applications when using the auto capture mode.

Features:
* FEATURE-697: Added the possibility to replace characters/strings when a pattern is used, to have more control over the resulting filename. E.G. ${title:r ,_} will cause all the spaces to be replaced by underscores.
* FEATURE-712: The amount of colour which images are reduce to, if the setting is active, has been made configurable in the greenshot.ini property OutputFileReduceColorsTo. Default stays at 256,
* FEATURE-723: Adding a newline when exporting to Word or an open Outlook email, this makes it possible to repeat exports.

Changes:
* Flickr plug-in: from June 27th, 2014 Flickr will only accept uploads via HTTPS! As the switch is already possible Greenshot has been changed accordingly.

Languages:
* Updated the French translation for the Microsoft Office plug-in


1.1.8.35-g8f5559c Bugfix Release

Bugs resolved:
* BUG-1578: Changed the behavior of creating filenames for Imgur to be configurable, default will be the date/time.
* BUG-1580: removed device names for capture fullscreen submenu in multi-monitor setups, which sometimes delivered inconsistent results or even garbage characters. Sticking to descriptive text like "bottom left", which is more useful anyway.
* BUG-1581: Ini-file reading is now done without locking the file, this should help a bit in cases where other applications have this file open.
* BUG-1600: Found that Greenshot uses a wrong URL format on the clipboard, this fix might solve some issues
* Bug: When capturing client windows on a DWM enabled system (Vista & Windows 7) with "auto" set, sometimes the capture had a blurred/transparent effect.
* Bug: Couldn't export to an Outlook 2013 reply which is being edited in the preview pane

Features:
* FEATURE-663: dark background for textbox input when editing bright-colored text
* FEATURE-667: destinations with subdestinations can now be clicked to invoke the "main" subdestination

Languages:
* Updates for Swedish translation and help, new Swedish translations for plugins and installer
* Improvements for German translation and help
* BUG-1608: Fixed typo in French translation

1.1.7 build 17 Bugfix Release

Changes:
* We moved our repository to BitBucket (GIT), this forced us to change the build script and the version

Bugs resolved (for bug details go to http://sourceforge.net/p/greenshot/bugs and search on the ID):
* BUG-1517: (now it is really fixed) export to Microsoft Word always goes to the last active Word instance.
* BUG-1589, BUG-1584 System.NullReferenceException
* BUG-1574: AccessException when loading plugins

Features:
* Changed the Box plug-in to use the new V2 API, which is mandatory from 13.12.2013. The Box plug-in now needs the .NET 3.5 Full Framework.


1.1.6 build 2779 Bugfix Release

Bugs resolved (for bug details go to http://sourceforge.net/p/greenshot/bugs and search on the ID):
* BUG-1515: Changed the settings GUI to clearly show that the interactive Window capture mode doesn't use the windows capture mode settings.
* BUG-1517: export to Microsoft Word always goes to the last active Word instance.
* BUG-1525, BUG-1486: Greenshot looses configuration settings. (At least we hope this is resolved)
* BUG-1528: export to Microsoft Excel isn't stored in file, which results in a "red cross" when opening on a different or MUCH later on the same computer.
* BUG-1544: EntryPointNotFoundException when using higlight area or blur
* BUG-1546: Exception in the editor when using multiple destination, among which the editor, and a picker (e.g. Word) is shown.
* Not reported: Canceling Imgur authorization or upload caused an NullPointerReference

Features:
* Added EXIF orientation support when copying images from the clipboard
* FEATURE-596: Added commandline option "/inidirectory <directory>" to specify the location of the greenshot.ini, this can e.g. be used for multi-profiles...
* Removed reading the greenshot.ini if it was changed manually outside of Greenshot while it is running, this should increase stability. People should now exit Greenshot before modifying this file manually.

Improvements:
* Printouts are now rotated counter-clockwise instead of clockwise, for most people this should be preferable (#1552)

1.1.5 build 2643 Bugfix Release

Bugs resolved (for bug details go to http://sourceforge.net/p/greenshot/bugs and search on the ID):
* BUG-1510: Under Windows Vista when trying to apply a drop-shadow or a torn-edge effect a GDIPlus error occurs.
* BUG-1512, BUG-1514: Will not print color
* Not reported: Annotations where not visible when exporting to Office destinations after writing in the Greenshot format.
* Not reported: Removed the update check in Greenshot for PortableApps

Languages:
* New translation: Estonian
* Updated translations: Russian, Polish and Italian
* New installer translation: Ukrainian
* New plugin translations: Polish


1.1.4 build 2622 Release

Features added:
* General: Added zoom when capturing with a option in the settings for disabling the zoom. (this can also be done via the "z" key while capturing.)
* General: Better Windows 8 integration: Capture window from list now has the apps and the interactive window capture is not confused by apps or the app launcher.
* General: Added Special-Folder support for the OutputPath/Filenames, now one can use the following values: MyPictures, MyMusic, MyDocuments, Personal, Desktop, ApplicationData, LocalApplicationData. Meaning one can now set the output path to e.g. ${MyPictures}
* General: Greenshot now also comes as a "for PortableApps" download, this now will be released just like the other files (installer & .zip)
* Editor: Added a new image format "Greenshot" which allows the user to save the current state and continue editing later. (was already in the 1.1.1.2550)
* Editor: The capture is now displayed in the center of the editor, the code for this was supplied by Viktar Karpach.
* Editor: Added horizontal and vertical alignment for text boxes.
* Printing: Added option to force monochrome (black/white) print
* Plug-in: Added Photobucket plugin

Bugs resolved (for bug details go to http://sourceforge.net/p/greenshot/bugs and search on the ID):
* BUG-1327, BUG-1401 & BUG-1410 : On Windows XP Firefox/java captures are mainly black. This fix should also work with other OS versions and applications.
* BUG-1340: Fixed issue with opening a screenshow from the clipboard which was created in a remote desktop
* BUG-1375, BUG-1396 & BUG-1397: Exporting captures to Microsoft Office applications give problems when the Office application shows a dialog, this is fixed by displaying a retry dialog with info.
* BUG-1375: Exported captures to Powerpoint were displayed cropped, which needed extra actions to correct.
* BUG-1378: Picasa-Web uploads didn't have a filename and the filename was shown as "UNSET" in Picasa-Web.
* BUG-1380: The window corners on Windows Vista & Windows 7 weren't cut correctly. While fixing this issue, we found some other small bugs which could cause small capture issues on Vista & 7 it also used more resources than needed.
* BUG-1386: Resize issues with some the plugin configuration dialogs.
* BUG-1390: Elements in 1.0 are drawn differently as in pre 1.0
* BUG-1391: Fixed missing filename in the Editor title
* BUG-1414: Pasting captures as HTML-inline in Thunderbird doesn't work when using 256-colors.
* BUG-1418: Fixed a problem with the editor initialization, in some small cases this gave an error as something happend at the same time.
* BUG-1426: Added some checks for some configuration values, if they were not set this caused an error
* BUG-1442: If copying an image from Outlook via the clipboard the image was cropped
* BUG-1443: Image exports to Microsoft Word didn't have the "Lock aspect ratio" set
* BUG-1444: Colors were disappearing when "Create an 8-bit image if colors are less than 256 while having a > 8 bits image" was turned on
* BUG-1462: Auto-filename generation cropping title text after period 
* BUG-1481: when pasting elements from one editor into another the element could end up outside the visible area 
* BUG-1484, BUG-1494: External Command plug-in issues. e.g. when clicking edit in the External Command plug-in settings, than cancel, and than edit again an error occured.
* BUG-1499: Stability improvements for when Greenshot tries to open the explorer.exe
* BUG-1500: Error while dragging an obfuscation
* BUG-1504: InvalidCastException when using the brightness-filter
* Reported in forum: Fixed a problem with the OCR, it sometimes didn't work. See: http://sourceforge.net/p/greenshot/discussion/676082/thread/31a08c8c
* Not reported: Flickr configuration for the Family, Friend & Public wasn't stored.
* Not reported: If Greenshot is linked in a Windows startup folder, the "Start with startup" checkbox wasn't checked.
* Not reported: Some shortcut keys in the editor didn't respond.
* Not reported: Fixed some issues with capturing windows that were larger than the visible screen, logic should now be more reliable.
* Not reported: Fixed some cases where Dragging & Dropping an image from a browser on the editor lost transparency.
* Not reported: Undo while in an Auto-Crop made the editor unusable.
* Not reported: When first selecting a printer, the main printer destination has been replaced by the selected one, making the Windows printer dialog unavailable for further prints
* Not reported: Open last capture in explorer doesn't open the right location
* Not reported: Fixed some issues where the sub-menus of the context menu moved to the next screen.
* Not reported: When having Outlook installed but not the Office plugin there was no EMail destination.
... and more

Languages:
* Added Indonesian
* Installer: Added Spanish
* Installer: Added Serbian
* Installer: Added Finnish
* General: Fixes for many languages

Known issues:
* Greenshot general: a captured I-Beam cursor isn't displayed correctly on the final result.
* Greenshot general: Not all hotkeys can be changed in the editor. When you want to use e.g. the pause or the Windows key, you will need to be modified the ini directly.
* Greenshot general: Can't capture 256 color screens
* Greenshot general: Hotkeys don't function when a UAC (elevated) process is active. This we won't change as it is a Windows security measure.
* Greenshot general: Capturing apps on Windows 8 when having more than one screen still causes some issues.
* Greenshot editor: Rotate only rotates the screenshot, not the added elements or cursor


1.0.6 build 2228 Release

Some features we added since 0.8:
* General: Greenshot will now run in 64 bit mode, if the OS supports it.
* General: Added a dynamic destination picker
* General: Added a preview when using the window capture from the context menu (Windows Vista and later)
* General: Added color reduction as an option and auto detection for image with less than 256 color. When using reduction this results in smaller files.
* General: Added direct printing to a selected printer
* General: Added some additional logic to the IE capture, which makes it possible to capture embedded IE web-sites from other applications.
* General: Changed multi-screen capture behaviour, assuming that capturing all screens is not a normal use-case. Now default behaviour is to capture the one with the mouse-cursor. Also the user can select which screen to capture from the context-menu.
* General: Changed the configuration to use a .ini with some advanced features. Fixed settings can't be changed in the settings. Settings, quicksettings and the Greenshot icon can be disabled. (See greenshot.ini and our website)
* General: Added and update many languages, see our website for the whole listing!
* General: Now one can use the shift key to fix the mouse coordinates while capturing. If you press and hold shift only the first direction in which you move can be change, the other stays fixed.
* General: Added an expert tab in the settings, here some Greenshot behavior can be changed
* General: Added more complex options for the filename generation
* Editor: Added Ctrl/shift logic to the editor, hard to explain (see help) but hold one of the keys down and draw..
* Editor: Added a color picker in the color dialog.
* Editor: Added undo/redo
* Editor: Added effects: shadow, torn edges, invert, border and grayscale
* Editor: Added rotate clockwise & counter clockwise
* Editor: Added freehand tool, this makes it possible to draw some things freehand. Every "stroke" (mouse-down to mouse-up) is one "object" which can be manipulated: move, delete, change Z-order and change the properties like color and thickness.
* Editor: Added auto crop
* Plug-in: Added Confluence plug-in to attach captures to Confluence pages.
* Plug-in: Added JIRA plug-in to attach captures to JIRA tickets.
* Plug-in: Added OCR plug-in, if MODI is available you can capture text from the screen and place it on the clipboard. See our website on pre-requisites for the OCR functionality.
* Plug-in: Added External command plug-in can be used to export captures to some another application or script.
* Plug-in: Added Box.com plug-in uploads your captures to your account
* Plug-in: Added Dropbox plug-in uploads your captures to your account
* Plug-in: Added Flickr plug-in uploads your captures to your account
* Plug-in: Added Imgur plug-in uploads your captures annonymously or to your account
* Plug-in: Added Picasa-Web plug-in uploads your captures to your account
* Plug-in: Added Office plug-in with destinations for Excel, Outlook, Word and Powerpoint

Bugs resolved:
* We resolved so many bugs that the list is actually way to long to show here.

Known issues:
* Greenshot general: a captured I-Beam cursor isn't displayed correctly on the final result.
* Greenshot general: Not all hotkeys can be changed in the editor. When you want to use e.g. the pause or the Windows key, you will need to be modified the ini directly.
* Greenshot general: Can't capture 256 color screens
* Greenshot general: Hotkeys don't function when a UAC (elevated) process is active. This we won't change as it is a Windows security measure.
* Greenshot editor: Rotate only rotates the screenshot, not the added elements or cursor



0.8.0 (Build 0627)

Bugs resolved:
* save-as dialog honors default storage location again
* fixed loop when holding down prnt key [ 1744618 ] - thanks to flszen.users.sourceforge.net for supplying a patch
* fixed displayed grippers after duplicating a line element
* fixed a lot of GDI+ exceptions when capturing by optimizing memory usage
* typo [ 2856747 ]
* fixed clipboard functionality, should be more stable (not throwing exception with "Requested Clipboard operation did not succeed.") Bugs [ 2517965, 2817349, 2936190 ] and many more
* fixed clipboard to work with OpenOffice [ 1842706 ]
* fixed initial startup problems when creating the startup shortcut
* fixed exceptions when save path not available, incorrect or no permission when writing (will use SaveAs)
* fixed camera sound is sometimes distorted
* fixed region selection doesn't take the right size
* fixed bug when capturing a maximized window: wrong size was taken and therefore additional pixels were captured
* fixed capture bug which prevents a lot of people to use Greenshot (in 0.7.9 this was a "GDI+" exception). Problem was due to allocating the bitmap in the memory of the graphic card which is not always big enough.
* fixed restoring geometry for editor (the editor will now be open on the last location)
* fixed problem when loading language files during windows startup
* fixed opening of bitmaps from the command-line
* highlight and obfuscate elements no longer share last used line thickness and color with other elements

Features added:
* Optimized memory usage
* Added crop tool
* Added clipboard capture
* Added plugin support
* Added Bitmaps as object
* Added filters for wiping sensitive information as was suggested in e.g. [ 2931946 ]
* Added open from file
* Added the captured window title (even when capturing a region) as option for the filename pattern
* Added shadows which where supplied as patch in 2638542 and 2983930
* Added Email as Output (a MAPI supporting Email client should be available)
* Added double-click on icon to open last save location in Windows Explorer (or replacements)
* Changed configuration loading to better support portable Greenshot usage.
* Changed language from compiled resources to flexible xml files, user can add their own languages
* Added "Select all" option for image editor
* Added "Drag to", you can now drag images or image files to the Greenshot image editor.


0.7

Bugs resolved:
* fixed "cancel button bug" in text editor
* fixed tooltip text for texteditor buttons [ 1883340 ]
* fixed typo in "hotkeys not registered" dialog [ 1914122 ]
* create directories if default storage location does not exist anymore [ 1935912 ]
* fixed behavior of quick settings menu to update when settings are changed via the normal settings dialog
* fixed multi screen problem that occurred when one of the screens had negative coordinates [ 1938771 , 2021295 ] - thanks to ChrisB (retrochrisb at users.sourceforge.net) for supplying the patch
* arrowheads no longer get lost when copy&pasting an arrow [ 2016055 ]
* areas which are out of the screen bounds are now ignored when capturing windows [ 1931964 ] 

Features added:
* releases now include an installer
* improved clickable area of lines
* optimized drawing tools behaviour [ 1844842 ]
* hitting Ctrl+Return while editing text now closes the text editor and applies the changes [ 1782537 ]
* changed textfield button icon to a more intuitive one
* implemented quick settings: most important settings are accessible from the context menu now [ 1728100 ]
* holding down the shift key enables selection of multiple elements [ 1810347 ]
* elements can now be shifted in hierarchy using the Object>Arrange menu or PgUp, PgDn, Home, End keys [ 1805249 ]
* store last used file extension
* display size of selection in region and window mode [ 1844806 ] - thanks again to James (flszen at users.sourceforge.net) for supplying the code
* added screenshot destination: save with dialog
* added possibility to configure multiple screenshot destinations at once (file/file (with dialog)/editor/clipboard/printer)
* added context menu to editor's status bar (after saving), allowing to copy the file path to the clipboard or to open the directory in windows explorer
* added option in settings dialog always to copy the file path to the clipboard right away when an image is saved
* added a pattern definition to settings dialog, allowing the configuration of dynamically generated file and directory names [ 1894028 ]
* added "requires restart" note to language option in settings dialog [ 1835668 ]

0.6

Bugs resolved:
* fixed refresh of displayed image after changing text [ 1782533 ]

Features added:
* added options for scale, rotation and centering of image printouts to page size [ 1842264, 1866043 ]
* adapted textbox drawing behaviour to rectangle and ellipse drawing behaviour
* image editor elements have 8 grippers for resizing now [ 1719232 ]
* added line drawing capability [ 1717343 ]
* added arrow drawing capability [ 1717343 ]
* show language dialog on first start [ 1835644 ]
* starting multiple instances is prevented now [ 1844013 ]
* added options for launching Greenshot on startup and for creating a desktop shortcut to settings dialog [ 1758908 ]
* moved configuration file location to Applicationdata folder [ 1735574 ] 
* display filename in image editor title bar after saving [ 1804071 ] 


0.5

Bugs resolved:
* multiple screens are supported now [ 1725037, 1797152, 1803090 ] - thanks to James (flszen at users.sourceforge.net) for supplying the patch

Features added:
* give error message when not all of the hotkeys can be registered [ 1731981 ]
* hide context menu before last-region capture [ 1727603 ]
* added help, preferences and and about items to image editor [ 1728099 ]
* removed save-as button [ 1724270 ]
* removed direct print functionality [ 1757153 ]
* added menuitem for closing the image editor [ 1731552 ]
* added buttons and colorpicker for border and background color [ 1711775 ] 
* save bitmap image to clipboard instead of jpeg [ 1721772 ]
* show JPEG quality dialog when saving JPEG [ 1721772 ]
* default JPEG quality is configurable now [ 1721772 ]
* configurable default screenshot destination [ 1744620, 1769633 ]
* drawing behaviors improved [ 1719232 ]
* added shortcut for last-region capture [ 1797514 ]


0.4

Bugs resolved:
* fixed behaviour when interrupting region selection by pressing ESC [ 1730244 ]
* save-as dialog: dot in filename yielded error messages [ 1734800 ]
* pasting by menu or shortcut did not work [ 1724236 ]
* duplicating an element yielded two new elements [ 1723373 ]
* fixed bug in build script which produced a wrong build number in about dialog [ 1728162 ]
* freeing memory after closing editor window [ 1732339 ]
* cvs tags during nant build [ 1730761 ]

Features added:
* added icon and title to help window [ 1731547 ]
* shapes can be moved using the arrow keys [ 1723438 ]
* made hotkeys de-activatable in settings dialog [ 1719973 ]
* added print button and print menu items [ 1716516 ]
* added tooltips for region and window mode [ 1711793 ]
* appended datetime string to default filename in save-as dialog [ 1711866 ]
* implemented option to skip image editor both in context menu and settings dialog [ 1724171 ]
* shift duplicated elements 10px left- and downwards [ 1723447 ]
* when clicking two overlapping elements, the one created later gets selected [ 1725175 ]
* created textboxes can now be edited with a doubleclick [ 1704408 ]
* selected font is now stored in the application config file [ 1704411 ]

