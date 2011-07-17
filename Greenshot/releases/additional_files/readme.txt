Greenshot: A screenshot tool optimized for productivity. Save a screenshot or a part of the screen to a file within a second. Apply text and shapes to the screenshot. Offers capture of window, region or full screenshot. Supports several image formats.

CHANGE LOG:

0.8.1

Bugs resolved:
* Image editor problems when exiting Greenshot.
* Systray icon wasn't removed when stopping Greenshot
* Installer fixes for silent installation and the selected installer language will be passed to Greenshot
* Hotkeys on Windows 7 x64 weren't working, should be okay now.
* Changed variable naming from %VAR% to ${VAR}, a.o to prevent early resolving on the command-line
* Fixed problems with calculating the window size for Windows Vista & Windows 7.
* Fixed annoying bug in editor which made the screen jump if the editor had scrollbars, got even more annoying with the new IE capture feature.
* Fixed mousewheel scrolling in editor
* Capture & editor performance improved
* Fixed capture region selection screen losing focus
* Many other minor stability fixes
* At first start all available languages can be selected

Features added:
* Changed the configuration from a proprietary binary format to a readable & modifiable "greenshot.ini".
* Added the Dutch language as a third default language for all Greenshot parts (application, plugins and installer)
* Added all currently available languages to the installer.
* Added configurable hotkeys
* Added Aero (DWM) window capture on Windows Vista and later! Either the window is captured with transparency or it is possible to replace the transparent window border with a background color, making the capture look cleaner.
* Added Internet Explorer capture. Select your IE - Tab from the Greenshot context menu or use the default hotkey "Ctrl + Shift + PrintScreen" to capture the active IE page.
* Added OCR Plugin, this will only work when Microsoft Office 2003 or 2007 is installed. Unfortunately there is no way to check what languages Office supports, this needs to be set manually! To set the language, go into the Greenshot configuration screen, a new "plugin" tab is available. Click on the tab, on the OCR plugin and on the configure button. This should allow you to change the language which is used to OCR your selection!
* Added environment variable support for the filename and path. Now one can use e.g. "${TMP}"…
* Added "experimental" Windows "Enhanced" MetaFile (=Vector graphics) support. The bitmap can be resized "without" quality loss. To use this, e.g. drag/drop a "WMF" file from the Microsoft Office "Clipart" directory on the open Greenshot editor.
* Added Imgur (see: http://Imgur.com) plugin
* Added plugin white/black listing, mainly needed for administrators specifying which plugins will be loaded and which not.
* Added better Email export: Outlook support, creating HTML email with "in-body" image using the default signature for new Emails.
* Added GDI capturing windows with transparency, only works if Aero (DWM) is disabled!
* Added update check, if an update is detected a popup is shown asking if the user wants to download this
* Added HTML as clipboard format


Known bugs:
* When having multiple monitors the systray context menu will have options which apear on a different screen. (Issue in Microsoft Windows)
* The "I" Mouse-Cursor will not be rendered correctly on the final image. (Issue in Microsoft Windows)
* There might still be some minor rendering problems due to performance improvements, these will not be visible on the resulting image. We will fix them as soon as we find them.

0.8.0

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
