Greenshot: A screenshot tool optimized for productivity. Save a screenshot or a part of the screen to a file within a second. Apply text and shapes to the screenshot. Offers capture of window, region or full screenshot. Supports several image formats.

CHANGE LOG:

1.1.0 build $WCREV$ UNSTABLE
Unstable means that we didn't go through extensive testing yet. This might give you: features that aren't finished yet, missing translations or new bugs.

Features:
* General: Added zoom when capturing
* General: Better Windows 8 integration: Capture window from list now has the apps and the interactive window capture is not confused by apps or the app launcher.
* General: Added Special-Folder support for the OutputPath/Filenames, now one can use the following values: MyPictures, MyMusic, MyDocuments, Personal, Desktop, ApplicationData, LocalApplicationData. Meaning one can now set the output path to e.g. ${MyPictures}
* Editor: The capture is now displayed in the center of the editor, the code for this was supplied by Viktar Karpach.
* Editor: Added horizontal and vertical alignment for text boxes.
* Printing: Added option to force monochrome (black/white) print
* Plug-in: Added Photobucket plugin
* Plug-in: Removed unneeded code from the Confluence Plug-in, this makes the Greenshot installer / .zip a bit smaller.

Languages:
* Installer: Added Spanish
* Installer: Added Serbian
* General: Fixes for Italian, Serbian, Slovak, Urkainian

Bugs resolved (for bug details go to http://sourceforge.net/p/greenshot/bugs and search on the ID):
* Bug #1327, #1401 & #1410 : On Windows XP Firefox/java captures are mainly black. This fix should also work with other OS versions and applications.
* Bug #1340: Fixed issue with opening a screenshow from the clipboard which was created in a remote desktop
* Bug #1375, #1396 & #1397: Exporting captures to Microsoft Office applications give problems when the Office application shows a dialog, this is fixed by displaying a retry dialog with info.
* Bug #1375: Exported captures to Powerpoint were displayed cropped, which needed extra actions to correct.
* Bug #1378: Picasa-Web uploads didn't have a filename and the filename was shown as "UNSET" in Picasa-Web.
* Bug #1380: The window corners on Windows Vista & Windows 7 weren't cut correctly.
* While fixing #1380 we found some other small bugs which could cause small capture issues on Vista & 7 it also used more resources than needed.
* Bug #1386: resize issues with some the plugin configuration dialogs.
* Bug #1390: Elements in 1.0 are drawn differently as in pre 1.0
* Bug #1391: Fixed missing filename in the Editor title
* Bug #1414: Pasting captures as HTML-inline in Thunderbird doesn't work when using 256-colors.
* Bug #1418: Fixed a problem with the editor initialization, in some small cases this gave an error as something happend at the same time.
* Bug #1426: Added some checks for some configuration values, if they were not set this caused an error
* Bug #1444: Colors were disappearing when "Create an 8-bit image if colors are less than 256 while having a > 8 bits image" was turned on
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

Known issues:
* Greenshot general: a captured I-Beam cursor isn't displayed correctly on the final result.
* Greenshot general: Not all hotkeys can be changed in the editor. When you want to use e.g. the pause or the Windows key, you will need to be modified the ini directly.
* Greenshot general: Can't capture 256 color screens
* Greenshot general: Hotkeys don't function when a UAC (elevated) process is active. This we won't change as it is a Windows security measure.
* Greenshot editor: Rotate only rotates the screenshot, not the added elements or cursor
* Still having some dual screen problems with Windows 8, we are working on it.

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
