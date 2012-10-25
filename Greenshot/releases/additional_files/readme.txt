Greenshot: A screenshot tool optimized for productivity. Save a screenshot or a part of the screen to a file within a second. Apply text and shapes to the screenshot. Offers capture of window, region or full screenshot. Supports several image formats.


CHANGE LOG:
We changed the version to 1.0 after 5 years it should no longer have a "beta" feeling!

1.0.5 build #### Release Candidate 5

Bugs resolved
* Fixed translation issue in expert settings, clipboard format options not being updated right away after switching UI language, see bug #3576073
* Fixed bug #3578392: one of the clipboard formats we generate doesn't support transparency, but we forgot to remove it.
* Fixed a bug that prevented the clipboard formats to represent the general output settings. (decrease colors if possible etc)
* Fixed a bug that the context menu was shown 2x, it's not really visible but does not improve the performance.
* Fixed a bug when capturung a minimized IE window, now we first restore it (un-minimize) before capturing.
* Fixed bug #3579138 when selecting the context menu entry "Capture Window" it is trying to capture the active Window (which is not possible when using a context menu)
* Fixed bug #3576125 when opening an image from the clipboard which is created on a remote system (Remote Desktop) with native Windows Alt+Printscreen
* Fixed bug #3579467 after changing the language some translations in the context menu are not update, also hotkey changes were not visible.

Known issues:
* Greenshot general: the I-Beam cursor isn't displayed correctly on the final result.
* Greenshot general: Not all hotkeys can be changed in the editor. For example the pause or the Windows key need to be modified directly in the ini.
* Greenshot editor: Rotate only rotates the screenshot, not the added elements or cursor
* Confluence Plug-in: the retrieving of the current page from firefox only works on the currently displayed Firefox tab. This is a problem since Firefox 13 and it is currently unknown if there is a fix.


1.0.4 build 2177 Release Candidate 4

Bugs resolved:
* Fixed error during OAuth process due to authorization dialog not opening, see bug #3578480
* Fixed error in external command plugin, see bugs #3578391, #3578379, #3578377
* Fixed editor window not being resized to image size, see bug #3578325


1.0.3 build 2159 Release Candidate 3

Features added:
* The Imgur plugin now has login support, uncheck the "Annonymous" checkbox in the settings.
* Added new languages, see our blog
* Moved Office destinations to their own plugin, making it possible for people to install Greenshot without

Bugs resolved:
* Fixed OCR not working on Windows in 64 bit modus. People who want to use OCR without having office, read our FAQ!
* Fixed a performance issue with the windows capture.
* Fixed issues with Dropbox authentication, using our own OAuth implementation
* Fixed problems with generating transparency for shadow & torn edges.
* Fixed 2 bugs with the capture last region, see bug #3569703
* Fixed weird window titles showing up in the "Capture Internet Explorer from list"
* Fixed some more bugs with the dynamic destination picker and also fixed a small general issue with Office destinations.
* Removed unneeded files in the installer, making it smaller
* Fixed a lot of small translation texts and some icons in menus
* Fixed missing translations and enlarged some labels of settings for several plugins: external command, Jira, Imgur, Flickr and Picasa
* Fixed some upload issues, most uploads should have better performance and use less memory. Also removed some dependencies, which make the plugins and Greenshot smaller and work with .NET 2.0

Features removed:
* Remove any history management of all non annonymous upload plug-ins, Greenshot is for uploading not managing uploads.


1.0.2 build 2011 Release Candidate 2

Bugs resolved:
* Fixed a problem with the "please wait". It's now possible to cancel the operation, this was needed due to problems with Imgur and other plugin uploads
* Fixed a memory leak
* Fixed some problems with the IE capture
* Fixed some jira & confluence plugin bugs
* Fixed an Outlook export bug
* Fixed an issue with writing the greenshot.ini file
* Changed the installer to install only a default set of components, so Greenshot is not overloaded

Known issues:
* Greenshot general: I-Beam cursor isn't displayed correctly on the final result.
* Greenshot general: Not all hotkeys can be changed in the editor. For example the pause or the Windows key need to be modified directly in the ini.
* Greenshot editor: Rotate only rotates the bitmap, not the added elements or cursor
* Greenshot editor: The shadow and torn edges effects don't create a transparent background yet.
* Confluence Plug-in: the retrieving of the current page from firefox only works on the currently displayed Firefox tab. This is a problem since Firefox 13 and it is currently unknown if there is a fix.
* OCR Plug-in: OCR is not working on 64 bit Windows, as the MODI-OCR component from Microsoft is not available in 64 bit, in this case Greenshot should be run in 32-bit. We are working on this.


1.0.1 build 1980 Release Candidate 1

Features added:
* Greenshot will now run in 64 bit mode, if the OS supports it.
* Added a "destinations" concept, making it possible to select all destinations from the main settings or using them inside the editor.
* Added a "processor" concept, making it possible to modify the capture before it's send to a destination. Currently there is only an internal implementation which replaces the TitleFix plugin.
* Added Office destinations (Word, Excel, Powerpoint, OneNote & Outlook) with dynamic resolving of open "instances".
* Added Ctrl/shift logic to the editor, hard to explain (see help) but hold one of the keys down and draw..
* Added a color picker in the color dialog.
* Added effects: shadow, torn edges, invert, border and grayscale
* Added rotate clockwise & counter clockwise
* Added a preview when using the window capture from the context menu (Windows Vista and later)
* Added color reduction as an option and auto detection for image with less than 256 color. When using reduction this results in smaller files.
* Added direct printing to a selected printer
* Added some additional logic to the IE capture, which makes it possible to capture embedded IE web-sites from other applications.
* Changed multi-screen capture behaviour, assuming that capturing all screens is not a normal use-case. Now default behaviour is to capture the one with the mouse-cursor. Also the user can select which screen to capture from the context-menu.
* Many languages added and updated, see our website for the whole listing!
* Added some features for deploying Greenshot in companies: Fixed settings can't be changed in the settings. Settings, quicksettings and the Greenshot icon can be disabled. (See greenshot.ini)

Bugs resolved:
* Fixed a problem with temp-files being removed before they were used, now using a delay of ~10 hours
* Removed clipboard monitoring, which hopefully solves some problems with Virtual Machines
* Fixed click on editor button not working if the window didn't have focus
* Fixed problem with the print dialog not having focus if opened from the editor "print" button
* Fixed bug #3482709 print with timestamp cropped the image
* Removed the always active CaptureForm, which resulted in greenshot not "recovering" when a capture caused an exception.
* Improved the auto-capture mode to honor some settings better
* Synchronized the selected language to the plugins
* Fixed installer issues on Windows 8, Greenshot can be used on Windows 8. Although there are still has some small issues with the Windows 8 Release Preview but these most likely are Microsoft bugs.


0.8.1 Build 1483

Bugs resolved:
* Fixed a bug that windows which dissapear when they lose focus couldn't be selected in the "interactive window mode" (Press printscreen and than space). These are e.g. (context) menus and the Windows 7 Clock.
* Fixed graphics glitches when resizing the linethickness of an object from very thick into thin. (e.g. 50 -> 1)
* Fixed graphics glitches when duplicating a line or arrow, there suddenly are more "grippers" (those points which can be used to resize or change direction)-
* Fixed a minor issue when trying to select an arrow, the hit detection was slightly off.
* Fixed a problem that an invisible Greenshot window was showing when using Alt-Tab directly after the start of Greenshot. Although the window dissapears right after selecting, this was confusing.
* Fixed a problem with the main Greenshot context menu when entering and leaving the "capture windows" very quickly. (The context menu moved to the top-left corner of you screen)
* Fixed our balloon-tip which shows where Greenshot is after someone starts it the very first time.
* Fixed that our drop-down boxes in the settings weren't translated
* Fixed that it was possible to open multiple instances of the settings window, this would cause exceptions when someone changed something in it.
* Not a real bug or feature: but we are in the process of updating our english and german help to be more up to date. Dutch should follow soon...

Features added:
* The editor now has a freehand tool, this makes it possible to draw some things freehand. Every "stroke" (mouse-down to mouse-up) is one "object" which can be manipulated: move, delete, change Z-order and change the properties like color and thickness.
* The editor now has undo/redo
* Now one can use the shift key to fix the mouse coordinates while capturing. If you press and hold shift only the first direction in which you move can be change, the other stays fixed.

0.8.1-RC7 (Build 1427)

Bugs resolved:
* Fixed a nasty bug that some email clients (e.g. iPad, Thunderbird) didn't display inline images in Greenshot generated emails. Unfortunately this only works with Office 2007 and later.
* Fixed Bug #3433566: Exception when clicking on the update message "balloon", this only prevents a stacktrace and shows "can't open link ....".
* Fixed Bug #3435368: Transparency issues
* Trying to fix Bug #3428802, problems with missing/corrupted "accessibility.dll". Which is a .NET problem on someones PC.
* Changed the installer to optimize the Greenshot installation, this should cause Greenshot to start quicker.
* Added a shadow option to bitmaps inside the editor. e.g Bitmaps coming from drag & drop or insert window
* Fixed some small stabillity issues that were reported with the RC6

0.8.1-RC6 (Build 1392)

Bugs resolved:
* Fixed bug #3431881: Image opened via Greenshot is locked until Greenshot was closed.
* Fixed bug #3431223: Problems storing & retrieving the editor settings
* Fixed bug #3432313: Exception when clipboard is busy
* Fixed bug #3431100: At first run Greenshot doesn't use the selected language and gives an exception when going into the settings.
* Fixed a bug with the "TitleFix" Plugin, due to an error in the configuration reading this wasn't active.
* Fixed bug #3429716: When trying to upload to an older version of JIRA the error "No such operation 'addBase64EncodedAttachmentsToIssue'" appears.
  As a fallback the old, but unfortunately not 100% working, method 'addAttachmentsToIssue' will be used.
  For details see: https://jira.atlassian.com/browse/JRA-11693

0.8.1-RC5 (Build 1372)

Bugs resolved:
* Fixed bug #3430555: Greenshot got wrong state when using "Capture Last region" with no region selected
* Fixed bug #3430560: Capturing the active window didn't set the region
* Fixed installer problems with the .NET 2.0 Language Pack, we removed this requirement as Greenshot doesn't need this.
* Fixed some translations

0.8.1-RC4 (Build 1366)

Bugs resolved:
* Fixed a problem with cleaning up tmp files (Bug #3429670)
* Made the zip work directly out from the directory it's extracted in.
* Fixed instability when loading older/incompatibel plugins.
* Fixed a startup order problem which made Greenshot unusable.
* Fixed problems with the Outlook exporter which had difficulties exporting into an open Email.
  Usually a new is created, this option can only be enabled via the greenshot.ini! You can find it under "OutputOutlookMethod", needs to be set to "TryOpenElseNew"

Features added:
* Added "Auto Crop", an option in the edit menu of the editor.
  This will go into crop mode and select the biggest solid (!!) rectangle around your capture, so you can remove it.
  Usefull if you want to capture something on a plain background and don't want to put a lot of effort in crop to the smallest bitmap.
  Hard to explain, just try it...
* Added ClipboardFormats setting to the greenshot.ini, you can find it as "ClipboardFormats"
  If people have problems with the HTML format which was added, it can be removed here: ClipboardFormats=PNG,DIB

0.8.1-RC3 (Build 1339)

Bugs resolved:
* Fixed multiple stability issues when capturing
* Fixed problems with cleanup of the "Please wait" popup.
* Installer fixes for silent installation and the selected installer language will be passed to Greenshot
* At first start all available languages can be selected
* Hotkeys on Windows 7 x64 weren't working, should be okay now.
* Changed variable naming from %VAR% to ${VAR}, a.o to prevent early resolving on the command-line
* Fixed annoying bug in editor which made the screen jump if the editor had scrollbars, got even more annoying with the new IE capture feature.
* Fixed mousewheel scrolling in editor
* Fixed capture region selection screen losing focus
* Many other minor stability fixes

Known bugs:
* When having multiple monitors the systray context menu will have options which apear on a different screen. (Issue in Microsoft Windows)
* The "I" Mouse-Cursor will not be rendered correctly on the saved image. (Issue in Microsoft Windows)
* There might still be some minor rendering problems due to our performance improvements, these will not be visible on the final saved/exported image. We will fix them as soon as we find them.

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
