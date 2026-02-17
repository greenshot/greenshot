# Greenshot 1.4 Development Changelog

This document contains the changelog for Greenshot 1.4, which is currently in active development as continuous builds (unstable releases).

**Note**: Version 1.4 is not yet released as a stable version. All builds are continuous/development builds and are unsigned. These builds are marked as "UNSTABLE-UNSIGNED" and should be considered beta quality.

## About Greenshot 1.4

Greenshot 1.4 represents the next major version currently under active development. Development began in November 2025 after the final 1.3 stable release (1.3.312 in January 2026).

**Current Status**: Continuous Development Builds (Prereleases)
**Latest Build**: v1.4.107 (February 14, 2026)
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

#### 📸 Capture Improvements

**New Capture Technology** (#932)
- Implemented new capture technology for better screenshot quality
- Improved compatibility with modern Windows applications
- By @Lakritzator

**Improved Cursor Capture** (#863)
- Enhanced cursor capture logic to reflect real cursor size
- More accurate cursor representation in screenshots
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

#### ⚙️ Configuration & Settings

**MAPI Client Detection** (#861)
- Fixed MAPI Client detection on HKCU registry
- By @htcfreek

**Destination Picker** (#911)
- Fixed destination picker functionality
- Updated label in settings for improved clarity (#909)
- By @Christian-Schulz and @jklingen

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

#### 🏗️ Development & Infrastructure

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

### February 2026
- **v1.4.107** (Feb 14) - Latest build
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
✨ ImageSharp format support  
✨ Modernized plugin APIs (Confluence, Dropbox)  
✨ Better Windows 10/11 integration  
✨ Improved cursor capture  
✨ Enhanced memory management  

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

⚠️ **Important**: Since 1.4 builds are unsigned, you may see Windows SmartScreen warnings. This is expected for continuous builds.

---

## Known Issues

As 1.4 is in active development, there may be issues. Please check the [GitHub Issues](https://github.com/greenshot/greenshot/issues) page for:
- Known bugs and limitations
- Planned features
- Work in progress

Report any issues you encounter to help improve Greenshot!

---

*Last updated: February 14, 2026*  
*Latest continuous build: v1.4.107-gce9bdd971b*
