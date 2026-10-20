# Greenshot 1.4 Development Changelog

This document contains the changelog for Greenshot 1.4, which is currently in active development as continuous builds (unstable releases).

**Note**: Version 1.4 is not yet released as a stable version. All builds are continuous/development builds and are unsigned. These builds are marked as "UNSTABLE-UNSIGNED" and should be considered beta quality.

## About Greenshot 1.4

Greenshot 1.4 represents the next major version currently under active development. Development began in November 2025 after the final 1.3 stable release (1.3.312 in January 2026).

**Current Status**: Continuous Development Builds (Prereleases)
**Latest Build**: v1.4.108 (March 14, 2026)
**Release Branch**: `main`

### How to Get 1.4 Builds

Continuous builds are automatically created for every commit to the `main` branch:
- Visit the [GitHub Releases page](https://github.com/greenshot/greenshot/releases)
- Look for releases tagged with version numbers like `v1.4.X-gXXXXXXXXXX (continuous build)`
- Download the installer or portable ZIP marked as "UNSTABLE-UNSIGNED"

**⚠️ Important**: These builds are not code-signed and are intended for testing purposes. For production use, please use the latest stable 1.3 release.

---

## Major Features & Improvements in 1.4

### New Features

#### 🎨 Editor Enhancements

**Tabs in the editor**
- An editor window can show several captures as tabs, like a browser
- Drag a tab to change its place, into another editor window, or out of the window to open it in its own window
- A new capture opens as a tab in the editor you used last, the settings can turn this or the tabs off
- The "+" after the tabs adds a new image, its size and color are in the settings
- Closing a window with several changed captures asks once: save all, discard all or cancel

**Emoji Support** (#358)
- Added a new "Emoji" object type to the editor
- Insert emojis directly into your screenshots
- Contributed by @jairbubbles

**Text Redaction with OCR** (Beta) (#947)
- Automatically detect and redact text in screenshots using OCR technology
- Useful for privacy protection and sensitive information removal
- Beta feature by @Lakritzator

**Remove Transparency Feature** (#945)
- New option to remove transparency from images in the editor
- Helpful for certain export formats and use cases
- Added by @Lakritzator

#### ⚙️ Command Line Interface

**Redesigned CLI with System.CommandLine**
- Migrated from hand-written parser to the industry-standard System.CommandLine library
- Changed argument style from Windows-style `/` to standard `--` prefixes
- Command line help is now automatically generated with comprehensive option descriptions
- Old format (`/help`, `/exit`, `/reload`, `/norun`, `/language`, `/inidirectory`) replaced with modern alternatives (`--help`, `--exit`, `--reload`, `--no-run`, `--language <code>`, `--ini-directory <path>`)
- Added hidden `--restore` option for Windows Restart Manager integration
- Run `Greenshot --help` to see all available options

#### 🔄 Windows Restart Manager Integration

**Automatic State Recovery During Updates**
- Greenshot now integrates with Windows Restart Manager to seamlessly handle system updates and maintenance
- Automatically registers for restart with Windows, allowing it to participate in graceful shutdown scenarios
- When Windows Update or other installers request a shutdown, Greenshot saves all open image editors to temporary `.greenshot` files
- Upon restart by the Windows Restart Manager, Greenshot automatically reopens all previously open editors
- Enhances reliability during Windows Updates, preventing loss of unsaved work
- Uses the `--restore` command line argument internally to detect restart scenarios

#### 📸 Capture Improvements

**Capture a fixed size** (#152, #1525)
- Press F while selecting a region to capture an area of a fixed size, e.g. 1280 × 720 or 32 × 32
- The area follows the mouse and lines up with the edges of windows and the screen; hold Alt to place it freely
- The mouse holds the area at its top left corner, so the magnifier shows exactly where it starts; press C for another corner or the middle
- The magnifier shows the edges of the area (and of a region you drag), so even a 16 × 16 area can be placed to the pixel
- A list shows the sizes: pick one with 1 to 9 (also on the number pad), or go through them with Page Up and Page Down
- The sizes of your last region captures are in the list too, so you can capture the same size again
- Add and remove your own sizes in the settings, on the Capture tab
- A preview in a corner shows what will be captured, enlarged when it is small and reduced when it is large; this works for regions and windows too. Press P to move it next to the mouse in place of the magnifier, and again to turn it off

**Capture without freezing the screen**
- Turn off *Freeze the screen while selecting* on the Capture tab to select on the live screen: videos keep playing and HDR content keeps its brightness
- The selection appears at once and only the selected area is captured, when you finish the selection
- Press L while selecting to switch between the frozen and the live screen

**New Capture Technology** (#932)
- Implemented new capture technology for better screenshot quality
- Improved compatibility with modern Windows applications
- By @Lakritzator

**Windows Graphics Capture** (#542)
- Screenshots use the modern Windows Graphics Capture, one monitor at a time, which works better with modern Windows applications
- HDR screens are supported: bright parts of HDR games and videos keep their color and detail, like with the Xbox Game Bar
- Screenshots without HDR content keep their colors exactly as on the screen, also on HDR screens
- By @Lakritzator

**Improved Cursor Capture** (#863)
- Enhanced cursor capture logic to reflect real cursor size
- More accurate cursor representation in screenshots
- The captured cursor is only as big as the visible pointer, without the empty space around it
- By @Lakritzator

**Windows 11 Capture Behavior** (#771)
- Reverted the change that made Greenshot prefer PrintWindow on Windows 11
- Improved capture compatibility
- By @Lakritzator

**Chromium Browser Title Detection** (#757, #767)
- Fixed window title detection for Chromium-based browsers
- Better integration with modern web browsers
- By @danrhodes

#### 🔌 Plugin Improvements

**Windows 10/11 Integration** (#802)
- Integrated Windows 10 plugin into main codebase
- Better native Windows integration
- By @Lakritzator

**Confluence Plugin** (#869)
- Replaced deprecated SOAP API with REST API
- Modern API integration for Confluence
- By @Copilot

**Dropbox Plugin** (#885)
- Fixed OAuth2 flow by migrating from deprecated implicit flow to code flow
- Ensures continued Dropbox integration
- By @Copilot

**JIRA Plugin** (#871)
- Various fixes to improve JIRA plugin functionality
- By @Lakritzator

**OneNote Plugin** (#883)
- Fixed COM instantiation failure on modern Windows versions
- By @Copilot

**Word Plugin** (#870)
- Handle COM interface casting failures gracefully in Word version detection
- By @Copilot

**Outlook Plugin** (#920)
- Remove the Outlook Share button when using New Outlook
- Better compatibility with modern Outlook versions
- By @danrhodes

**Windows Share & OCR** (#919)
- Fixed Windows Share functionality to work with Windows 10 & 11
- Renamed OCR feature for clarity
- By @danrhodes

#### 🖼️ Image Format Support

**ImageSharp Integration** (#921)
- Added support for ImageSharp decoders and encoders
- Extended file format support
- By @Lakritzator

**PNG Quality Fix** (#744)
- Avoid quality parameter for PNG in file format handler
- Ensures lossless PNG exports
- By @danrhodes

**DIB Format** (#813)
- Fixed GCHandle memory leak in DibFileFormatHandler
- By @tsiakoulias

#### 🌍 Internationalization

**Translation Updates**
- Turkish: New installer language file (#639), updates (#689, #827)
- Swedish: Updated translation (#406)
- Polish: Updated translation (#585)
- Japanese: Language files update (#586)
- Persian/Farsi: Improved language file (#522)

**Custom Translation Agent** (#908)
- Added custom agent for translations to improve localization workflow
- By @jklingen

#### 🔔 Notifications

**Better notifications** (#1516)
- After a capture you get one notification that shows a small preview and lists where the capture went
- Click the notification to send the capture somewhere else, even after Greenshot was closed
- Buttons on the notification let you edit the capture, open its folder or copy the link of an upload
- If an export fails, the notification says which one and why
- QR codes found in a capture are shown on the notification, and their links can be copied
- Greenshot appears only once in the Windows notification settings
- Choose in the settings which notifications you want to see (all, warnings and errors, errors only, or none) and whether they show a preview

#### 🩺 Help with problems

**Log viewer in Self-Service**
- Read Greenshot's log in its own window and copy it, for example for an issue report
- Search the log: only the entries with your text are shown, including the details of an error
- New entries appear while the log viewer is open, also in a search
- Choose how much Greenshot writes to the log, until Greenshot restarts
- Links like `greenshot://log?search=HDR` open the log viewer with that search, a link never changes a setting

#### 🖼️ Imgur

**Imgur uploads removed, delete your old ones in Self-Service**
- Greenshot no longer uploads to Imgur: Imgur would only allow it with paid or capped access for all Greenshot users
- If you uploaded images anonymously with an earlier Greenshot, it still has their delete keys: Self-Service lists those images so you can delete them from Imgur
- Save the delete links as a web page to keep them

#### 🩺 Self-Service

**Screenshots no longer end up on the Desktop unnoticed**
- When the screenshot folder couldn't be found, Greenshot switched it to the Desktop for good, without a message: e.g. a network drive without VPN, a renamed folder, or a folder with variables like ${YYYY}
- Now a missing folder is created, variables are filled in first, and only when the folder really can't be used (e.g. the network drive isn't there), Greenshot saves that one screenshot to the Desktop and says so: the setting stays

**Check where screenshots are saved**
- Self-Service shows whether the screenshot folder exists, whether Greenshot can write to it, and whether it's a network or OneDrive folder
- It also tells if the drive of the folder is missing, or the filename pattern gives no valid name
- The folder and the filename pattern can be reset to the default with one click

#### ♿ Accessibility

**Screen readers, keyboard, high contrast and text size** (settings, Self-Service, welcome, dialogs, plugins, recipe editor, capture)
- Narrator and other screen readers can name the buttons, lists and fields, and hear status changes
- The keyboard focus is always visible, also in dark mode
- Greenshot follows the Windows "Text size" setting and the "Animation effects" switch
- In the capture window, Tab and Shift+Tab select the next or previous window, and a screen reader hears the mode, window and size

### Bug Fixes & Improvements

#### 🐛 Critical Fixes

**Memory Leaks**
- Fixed GDI handle leaks in zoom matrices (#875)
- Fixed memory leak in Surface.Dispose method (#874)
- Fixed GCHandle memory leak in DibFileFormatHandler (#813)
- Dispose GDI resources on application exit (#865)
- Use 'using' statements for GDI+ resources in CaptureForm (#887)
- Dispose unused file streams in ClipboardHelper (#876)
- By @danrhodes and @tsiakoulias

**Drawing & Rendering Issues**
- Fixed black boxes/artifacts when using zoom levels like 75% (#944)
- Fixed blank objects when drawing at 66% or 75% zoom (#592)
- Fixed UI drawing glitches (zoomer and annotation drawing) (#948)
- Fixed TrackBar clipping in TornEdgeSettingsForm (#950)
- By @Lakritzator, @FF-Brown, and @Copilot

**File Handling**
- Fixed opening .greenshot files with emojis (#843)
- Fixed opening older .greenshot files with StepLabels (#844)
- By @Christian-Schulz

**COM & Windows Integration**
- Handle unsupported COM event subscription gracefully (#867)
- Fixed null reference in cursor setter (#873)
- Fixed pointer conversion for bitmap data (#864)
- Restore bitmap lock state after drawing (#878)
- By @danrhodes

**Remote Desktop / RemoteApp (RAIL)** (#1324)
- Fixed the system tray icon permanently disappearing after taking a capture when using Greenshot over a RemoteApp/RAIL session (e.g. Azure Virtual Desktop)
- Greenshot no longer hides and re-shows the tray icon to dismiss a stale balloon tip when running in a Terminal Server/RDP session, since the RAIL shell could fail to resynchronize the icon recreation back to the local taskbar
- By @Copilot

#### ⚙️ Configuration & Settings

**Switching plugins and destinations on or off**
- Plugins and destinations can be switched on or off in the settings, without typing their names into greenshot.ini
- Administrators can block plugins and destinations, or allow only some and block everything else, with greenshot-fixed.ini, and decide if users may switch off more; the settings show the lines to copy
- A destination of a plugin which isn't loaded is no longer removed from the settings
- A plugin which isn't signed by the makers of Greenshot isn't loaded until you allow it

**MAPI Client Detection** (#861)
- Fixed MAPI Client detection on HKCU registry
- By @htcfreek

**Destination Picker** (#911)
- Fixed destination picker functionality
- Updated label in settings for improved clarity (#909)
- By @Christian-Schulz and @jklingen

**Settings Dialog Folder Browser**
- Fixed crash when clicking Browse button in Settings dialog with inaccessible or invalid storage path
- Folder browser now gracefully falls back to My Documents when the configured path is unavailable, too long, or doesn't exist
- Prevents `System.InvalidOperationException: Unable to retrieve the root folder` error
- By @Copilot

**External Commands**
- An external command whose program isn't found when Greenshot starts (e.g. on a USB stick or a network drive) is no longer deleted from the settings, it is only left out of the menu until Greenshot starts with the program available again

**Plugin Configuration** (#733)
- Improved plugin configuration handling
- By @xqtp

**INI Configuration** (#595)
- Added thread-safe lock for IniConfig.SectionMap
- By @Christian-Schulz

#### 🎨 UI/UX Improvements

**Context Menu** (#670)
- Fixed delay in context menu
- By @Christian-Schulz

**Balloon Notifications** (#752)
- Fixed click on balloon tip to open Explorer
- By @Christian-Schulz

**Zoomer Improvements**
- Refactored zoomer opacity ImageAttributes handling (#866)
- By @danrhodes

**Clipboard Handling** (#877)
- Refactored clipboard retry logic in ClipboardHelper
- By @danrhodes

**Expert Tab** (#782)
- Amended spacing on Expert Tab
- By @danrhodes

**Group Box/Label** (#915)
- Simple group box/label extend
- By @danrhodes

**Portable App** (#890)
- Fixed missing emoji files in portable app
- By @Christian-Schulz

**Print Footer** (#820, #804)
- Use Greenshot's UI language for date formatting in print footer
- By @tsiakoulias

#### 🔒 Security Improvements

**TLS Protocol** (#814)
- Removed deprecated TLS 1.0/1.1 protocols
- Modern security standards
- By @tsiakoulias

**Clipboard data is treated as untrusted**
- Fixed an out-of-bounds read when pasting a crafted CF_DIBV5 (DIB) image: DIB data is now decoded by Dapplo.Windows.Clipboard's `DibImage`, from managed arrays with validated headers, also for `.dib` files
- Pasting editor elements no longer deserializes .NET objects from the clipboard with an unrestricted BinaryFormatter: elements are placed in Greenshot's own `Greenshot.DrawableContainerList` format, and on paste only the types a `.greenshot` file may contain are created, with a size limit
- Names of dropped or pasted virtual files (e.g. Outlook attachments) are sanitized

#### 🏗️ Development & Infrastructure

**Clipboard on Dapplo.Windows.Clipboard 3.2**
- All clipboard reads and writes use Dapplo.Windows.Clipboard instead of WinForms / WPF / OLE: the clipboard works on any thread, is only open for a moment (content is prepared before and decoded after), and waiting for a busy clipboard doesn't block the UI
- When another application keeps the clipboard open, the message names that application (before, it named the last application that copied something)
- Image copy: the formats are placed as text, PNG, DIBV5, DIB, HTML (richest first); DIBV5 keeps the transparency, also of premultiplied bitmaps; CF_HTML gets correct UTF-8 offsets; the `BITMAP` setting now places a DIB (Windows provides CF_BITMAP from it) instead of a .NET-only object
- Image paste and drop share one code path; images in HTML copied from Chrome / Edge (CF_HTML) are found now, relative image URLs are resolved, and JPEG, TIFF and uppercase file extensions are recognized
- Copying text from pipeline steps (e.g. the QR code step) and "copy path to clipboard" work from background threads
- Clipboard triggers recognize DIB, DIBV5 and file formats directly, and the format filter accepts a comma separated list

**Command Line Parsing**
- Migrated to System.CommandLine 2.0.3 for robust CLI argument parsing
- Replaced hand-written parser with industry-standard library
- Arguments now use `--` prefix (e.g., `--help`, `--exit`, `--reload`, `--no-run`)
- Language selection: `--language <language-code>` (was `/language [code]`)
- INI directory: `--ini-directory <directory>` (was `/inidirectory [path]`)
- Auto-generated help output with full option descriptions
- Added hidden `--restore` option for Windows Restart Manager support

**Build & Release**
- Added Chocolatey push support for releases (#742)
- Updated release scripts and versioning configuration (#717)
- Amended setup.iss to fall inline with standard practices (#837)
- Updated silent installation arguments (#786)
- By @Lakritzator, @jklingen, @danrhodes, and @hjenkel

**Code Quality**
- Fixed compiler error CS9273 in C#14 (#712)
- Fixed mixed code indentation (#618)
- Fixed mixed line endings (#617)
- Fixed comment typos (#616)
- Fixed typos in About dialogue box (#634)
- Fixed various typos (#895, #904)
- Housekeeping changes (#773, #928)
- By @Christian-Schulz, @Kissaki, @leewilmott, @ruyut, @Mr-Update, and @Lakritzator

**Documentation**
- Added copilot-instructions.md for repository onboarding (#749)
- Updated README.md (#556)
- By @Copilot and @devdrum

### New Contributors in 1.4

The following contributors made their first contributions to Greenshot during the 1.4 development cycle:

- @Copilot - Multiple AI-assisted improvements and fixes
- @danrhodes - Extensive memory leak fixes and improvements
- @ruyut - Typo fixes
- @htcfreek - MAPI detection fix
- @tsiakoulias - Memory leak fixes and localization improvements
- @Kissaki - Code quality improvements
- @leewilmott - UI typo fix
- @hjenkel - Installation improvements
- @devdrum - Documentation
- @armanatory - Persian translation
- @piotrtrkostrzewski94 - Polish translation
- @dotar - Swedish translation

---

## Release Timeline

### March 2026
- **v1.4.108** (Mar 14) - **Windows Graphics Capture API (Beta)**

### February 2026
- **v1.4.107** (Feb 14) - Windows Share & OCR fixes
- **v1.4.106** (Feb 13)
- **v1.4.105** (Feb 13) - Show beta tester info in about dialog
- **v1.4.104** (Feb 13) - Extend ServiceProvider
- **v1.4.103** (Feb 12) - Fix TrackBar clipping
- **v1.4.102** (Feb 11) - UI drawing glitch fixes
- **v1.4.100** (Feb 11) - **Text redaction with OCR (Beta)**
- **v1.4.99** (Feb 11) - **Remove transparency feature**
- **v1.4.98** (Feb 11) - Fix zoom artifacts
- **v1.4.96** (Feb 8) - **New capture technology**
- **v1.4.95** (Feb 7) - Housekeeping
- **v1.4.94** (Feb 7) - JIRA plugin fixes
- **v1.4.93-92** (Feb 5-6) - ImageSharp support
- **v1.4.91-90** (Feb 5) - Outlook & Windows Share fixes
- **v1.4.88-87** (Feb 4) - Destination picker & UI improvements

### January 2026
- **v1.4.85-83** (Jan 29) - Translation agent, Dropbox OAuth fix
- **v1.4.82-78** (Jan 28-29) - Memory leak fixes, OneNote fix
- **v1.4.77** (Jan 28) - **Confluence REST API migration**
- **v1.4.75-68** (Jan 27) - Multiple memory leak fixes, Word plugin fix
- **v1.4.67-61** (Jan 20-26) - Cursor capture, file opening fixes, MAPI fix
- **v1.4.60-53** (Jan 16-20) - File format compatibility fixes
- **v1.4.49-43** (Jan 14-15) - Chromium browser support and various fixes
- **v1.4.42-33** (Jan 10-14) - Portable app improvements, translations
- **v1.4.32-25** (Jan 9-10) - Plugin improvements, Windows integration

### December 2025
- **v1.4.24-17** (Dec 23-24) - Print footer localization, Windows 10 plugin integration
- **v1.4.16-5** (Dec 23) - Context menu fix, PNG quality fix, INI config fix, **Emoji support**

### November 2025
- **v1.4.1** (Nov 18) - **First 1.4 development build**, versioning configuration

---

## Comparison with 1.3

Greenshot 1.4 builds upon the stable 1.3 release with these key additions:

### New in 1.4
✨ Emoji support in editor  
✨ OCR-based text redaction (Beta)  
✨ Remove transparency feature  
✨ New capture technology  
✨ Windows Graphics Capture (WGC) API for higher-quality captures (Beta)  
✨ ImageSharp format support  
✨ Modernized plugin APIs (Confluence, Dropbox)  
✨ Better Windows 10/11 integration  
✨ Windows Restart Manager integration for seamless updates  
✨ Improved cursor capture  
✨ Enhanced memory management  
✨ Modern command line interface with System.CommandLine  

### Inherited from 1.3
✅ All security fixes from 1.3.290-1.3.312  
✅ Zoom functionality  
✅ High-DPI support  
✅ Portable ZIP distribution  
✅ Installation choice (per-user/all-users)  
✅ Keyboard shortcuts for editor  
✅ Enhanced crop functionality  

---

## Getting Involved

Greenshot 1.4 is under active development, and we welcome contributions!

- **Report Issues**: [GitHub Issues](https://github.com/greenshot/greenshot/issues)
- **Submit Pull Requests**: [Contributing Guide](../../CONTRIBUTING.md)
- **Test Builds**: Download and test continuous builds
- **Translations**: Help improve language support

---

## Technical Information

- **Version Scheme**: Uses NerdBank.GitVersioning
- **Build Type**: Continuous/unstable (unsigned)
- **Base Version**: 1.4 (as defined in src/version.json)
- **Development Branch**: `main`
- **Build System**: GitHub Actions workflow

For more information about the release process, see [docs/release-management.md](../release-management.md).

For detailed commit-by-commit changes, visit the [GitHub Releases page](https://github.com/greenshot/greenshot/releases) or the [Full Changelog](https://github.com/greenshot/greenshot/compare/v1.3.312...main).

---

## Migration Notes

If you're upgrading from 1.3 to a 1.4 continuous build:

1. **Backup your configuration**: Your settings in `%APPDATA%\Greenshot` should be preserved, but it's good practice to back them up
2. **Uninstall 1.3**: Completely uninstall the stable 1.3 release first
3. **Install 1.4 build**: Install the latest 1.4 continuous build
4. **Test thoroughly**: As these are development builds, test your usual workflows
5. **Update command line scripts**: If you use Greenshot with command line arguments, update from `/` style (e.g., `/exit`, `/reload`) to `--` style (e.g., `--exit`, `--reload`)

### Plugin API changes (clipboard)

- The `ClipboardHelper` methods which took a WinForms `IDataObject` were removed, and so were `GetDataObject()`, `GetFromDataObject`, `SetClipboardData(Type, object)`, the `ISurface` overloads of `SetClipboardData` / `TrySetClipboardData`, `GetImage()`, `GetImages`, `ContainsFormat`, `CurrentClipboardOwner` and `TrySetDataObjectOnce`
- Read with a `ClipboardSnapshot` (`ClipboardHelper.ReadSnapshot` / `ReadSnapshotAsync`, e.g. with `SelectImageReadFormats()`) and the `IClipboardDataSource` methods (`GetFirstImage`, `GetDrawables`, `GetText`, `GetHtmlImageUrls`, `ContainsImage`); for a drop use `new DataObjectReader((System.Runtime.InteropServices.ComTypes.IDataObject)e.Data)`
- Write with `ClipboardHelper.SetClipboardData(ClipboardContents)` / `SetClipboardDataAsync`, `SetClipboardData(string)`, or `IClipboardService`; `ClipboardHelper.ClipboardContent.DataObject` was replaced by `Contents` (a Dapplo `ClipboardContents`)
- Clipboard format names are the Win32 names reported by Dapplo (`CF_DIB`, `CF_DIBV5`, `CF_HDROP`, …) instead of the WinForms names

### Command Line Argument Changes

If you have scripts or shortcuts that use Greenshot's command line arguments, you'll need to update them:

| Old Argument | New Argument | Description |
|--------------|--------------|-------------|
| `/help`, `/h`, `/?` | `--help`, `-h` | Show help information |
| `/exit` | `--exit` | Close running Greenshot instance |
| `/reload` | `--reload` | Reload configuration |
| `/norun` | `--no-run` | Start in configuration mode |
| `/language [code]` | `--language <language-code>` | Set interface language |
| `/inidirectory [path]` | `--ini-directory <directory>` | Set configuration directory |
| `[filename...]` | `[files...]` | Capture files (unchanged) |

Run `Greenshot --help` to see the complete list of available options with descriptions.

⚠️ **Important**: Since 1.4 builds are unsigned, you may see Windows SmartScreen warnings. This is expected for continuous builds.

---

## Known Issues

As 1.4 is in active development, there may be issues. Please check the [GitHub Issues](https://github.com/greenshot/greenshot/issues) page for:
- Known bugs and limitations
- Planned features
- Work in progress

Report any issues you encounter to help improve Greenshot!

---

*Last updated: March 14, 2026*  
*Latest continuous build: v1.4.108-g2fddb97*
