# Greenshot 1.3 Release Changelogs

This document contains changelogs for all stable releases in the Greenshot 1.3 series.

---

## Version 1.3.312 (January 7, 2026)

Released: January 7, 2026

### What's Changed

Fixed an issue where external commands were not working due to backslash characters being blocked as part of security improvements.

### Downloads
- [Installer](https://github.com/greenshot/greenshot/releases/download/v1.3.312/Greenshot-INSTALLER-1.3.312-RELEASE.exe)
- [Portable ZIP](https://github.com/greenshot/greenshot/releases/download/v1.3.312/Greenshot-PORTABLE-1.3.312-RELEASE.zip)

### Technical Details
- **Issue**: #787 - External Commands Broken due to Backslash Character Being Blocked
- **Full Changelog**: https://github.com/greenshot/greenshot/compare/v1.3.311...v1.3.312

---

## Version 1.3.311 (January 7, 2026)

Released: January 7, 2026

### What's Changed

This is a critical security update that addresses a security vulnerability.

**Security Fix**: Fixed a security advisory ([GHSA-7hvw-q8q5-gpmj](https://github.com/greenshot/greenshot/security/advisories/GHSA-7hvw-q8q5-gpmj)). Thanks to @lihnucs for responsibly disclosing the issue with a detailed and well-analyzed report.

### Downloads
- [Installer](https://github.com/greenshot/greenshot/releases/download/v1.3.311/Greenshot-INSTALLER-1.3.311-RELEASE.exe)
- [Portable ZIP](https://github.com/greenshot/greenshot/releases/download/v1.3.311/Greenshot-PORTABLE-1.3.311-RELEASE.zip)

### Technical Details
- **Security Advisory**: GHSA-7hvw-q8q5-gpmj
- **Full Changelog**: https://github.com/greenshot/greenshot/compare/v1.3.304...v1.3.311

---

## Version 1.3.304 (October 30, 2025)

Released: October 30, 2025

### What's New

**Portable Version**: This release introduces an official portable ZIP package, making it easier to run Greenshot without installation.

### Bug Fixes

Fixed an issue where file associations and launching Greenshot with file arguments didn't work correctly when the application was already running.

### Downloads
- [Installer](https://github.com/greenshot/greenshot/releases/download/v1.3.304/Greenshot-INSTALLER-1.3.304-RELEASE.exe)
- [Portable ZIP](https://github.com/greenshot/greenshot/releases/download/v1.3.304/Greenshot-PORTABLE-1.3.304-RELEASE.zip) ⭐ *NEW!*

### Technical Details
- **PR #667**: Portable Zip Release by @Christian-Schulz
- **Issue #685**: File Association and Launch with File Arguments Not Working When Already Running
- **Full Changelog**: https://github.com/greenshot/greenshot/compare/v1.3.301...v1.3.304

---

## Version 1.3.301 (September 16, 2025)

Released: September 16, 2025

### What's Changed

This is a critical security update that addresses a security vulnerability.

**Security Fix**: Fixed a security advisory ([GHSA-8f7f-x7ww-xx5w](https://github.com/greenshot/greenshot/security/advisories/GHSA-8f7f-x7ww-xx5w)). Thanks to @RipFran for responsibly disclosing the problem with a very detailed and well-analyzed report and for helping with re-testing.

### Downloads
- [Installer](https://github.com/greenshot/greenshot/releases/download/v1.3.301/Greenshot-INSTALLER-1.3.301-RELEASE.exe)

### Technical Details
- **Security Advisory**: GHSA-8f7f-x7ww-xx5w
- **Full Changelog**: https://github.com/greenshot/greenshot/compare/v1.3.300...v1.3.301

---

## Version 1.3.300 (August 10, 2025)

Released: August 10, 2025

### Bug Fixes

Fixed plugin configuration options that were broken in previous 1.3 releases. The `IncludePlugins` and `ExcludePlugins` options from INI files now work correctly again.

### Downloads
- [Installer](https://github.com/greenshot/greenshot/releases/download/v1.3.300/Greenshot-INSTALLER-1.3.300-RELEASE.exe)

### Technical Details
- **PR #644**: Fix for IncludePlugins and ExcludePlugins options by @Christian-Schulz
- **Full Changelog**: https://github.com/greenshot/greenshot/compare/v1.3.296...v1.3.300

---

## Version 1.3.296 (July 26, 2025)

Released: July 26, 2025

### What's New

**Installation Choice**: You can now choose between installing Greenshot for all users (requires administrator privileges) or just for the current user.

### Bug Fixes

- Fixed registry entries for all-users installations
- Fixed shortcuts for all-users installations
- Resolved issues from #546, #611, and #598

### Downloads
- [Installer](https://github.com/greenshot/greenshot/releases/download/v1.3.296/Greenshot-INSTALLER-1.3.296-RELEASE.exe)

### Technical Details
- **PR #641**: Allow Choice between All-Users (Administrative) and Current-User Installation by @jklingen
- **Issues Fixed**: #625, #546, #611, #598
- **Full Changelog**: https://github.com/greenshot/greenshot/compare/v1.3.294...v1.3.296

---

## Version 1.3.292 (July 16, 2025)

Released: July 16, 2025

### Bug Fixes

Fixed a critical issue with elevated (administrator) installations where registry keys ended up in the wrong root key.

### Downloads
- [Installer](https://github.com/greenshot/greenshot/releases/download/v1.3.292/Greenshot-INSTALLER-1.3.292-RELEASE.exe)

### Technical Details
- **Issues Fixed**: #546, #619
- **Full Changelog**: https://github.com/greenshot/greenshot/compare/v1.3.291...v1.3.292

---

## Version 1.3.290 (May 23, 2025)

Released: May 23, 2025

**This was the first stable release of Greenshot 1.3 and marked as the final release for the 1.3 series.**

### Security

- **Fixed Critical Security Vulnerability**: Insecure Deserialization Arbitrary Code Execution - CVE-2023-34634 (#579)

### Major Features

#### Editor Improvements
- **Zoom Feature**: Added zoom functionality for the editor (#201)
- **CTRL+Wheel Zoom**: Added zoom with CTRL+Mouse wheel (#282)
- **Keyboard Shortcuts**: Added shortcuts (0-9, +/-) for foreground color, background color, line thickness, bold and shadow (#338, #366)
- **Fixed Ratio Scaling**: Improved scaling behavior with Shift modifier for consistent object scaling (#300, #514)
- **Crop Enhancement**: Enhanced ability to crop images vertically and horizontally (#249, #388)
- **Step Label Font**: Calculate optimal font size for step labels automatically (#457, #460)
- **Prevent Negative Font Size**: Fixed issue that could cause negative font sizes (#382)

#### Image Format Support
- **Enhanced Format Support**: Improved file format support (#385)

#### DPI Support
- **Improved DPI Handling**: Better support for high-DPI displays (#207, #254)
- **Propagate DPI Changes**: DPI changes now properly affect drawable containers, adorners, and resize grippers (#200)

#### Windows 10/11 Features
- **Windows 10 Secure Version Support**: Added support for newer Windows versions (#207)
- **Toast Notifications**: Improved Windows 10 toast notification support (#265, #487)
- **Windows 11 Print Screen Tool**: Added installer option to disable the default Windows 11 Print Screen tool (#484)

#### Filename Features
- **Random Alphanumerics**: Added placeholder for random alphanumeric characters in filenames (#216)

#### Bug Fixes
- **Unicode Text Drawing**: Fixed Unicode text rendering issues (#287)
- **Text Rendering in Containers**: Fixed text rendering inside text/speech bubble containers (#297)
- **Ellipse and Highlight Duplication**: Fixed duplication bug (#322, #331)
- **Arrow File Opening**: Fixed error when opening .greenshot files with arrows (#572, #574)
- **MAPI Detection**: Fixed MAPI detection issue (BUG-2693, #266)
- **Wine Support**: Fixed issues when running Greenshot via Wine (#262)
- **Initial Crop Selection**: Fixed initial crop selection (#407)
- **Confirm for Drawable Containers**: Added IsConfirmable property for IDrawableContainer (#399)
- **Resize Hotkey**: Added resize hotkey functionality (#480)

#### Architecture & Code Quality
- **Refactoring**: Refactored to use Dapplo.Windows library (#398)
- **Project Cleanup**: Various project cleanup improvements (#302)
- **Removed Embedded Browser**: Got rid of embedded browser component (#255)
- **Converters Fix**: Fixed converters for release 1.3 (#204)

#### Translations & Localization
- **Japanese**: Updated/Added Japanese language files (#69)
- **Italian**: Multiple updates to Italian language and help files (#224, #230, #237, #238, #241, #242, #245, #290, #291, #394)
- **English**: Updated English language files (#242)
- **German**: Updated German language files (#316)
- **Czech**: Corrected Czech translation (#330, #332)
- **Traditional Chinese**: Added Traditional Chinese to website and installer (#343, #345)
- **Various**: Fixed typos in multiple language files (#76, #301, #344)

#### URLs & Documentation
- **HTTPS URLs**: Updated URLs to use HTTPS (#292)
- **README**: Updated README.md (#351)

#### Other Improvements
- **Drag & Drop**: Improved Drag & Drop support with cleanup (#294)
- **Copyright Updates**: Code cleanup for copyright notices and other fixes (BUG-2736, #286)
- **Build & Release**: Added release script (#581)
- **Publish Unsigned Releases**: Set up automatic publishing of unsigned releases on commit (#583)
- **CloudFlare Cache**: Purge CloudFlare cache on pages build (#583)

### New Contributors
- @clpo13, @Rukoto, @peterfab9845, @k41c, @Ishmaeel, @Masv-MiR, @svatas, @EricCogen, @5idereal, @erl-mallard, @jdavila71, @jglathe, @FF-Brown, @jairbubbles

### Downloads
- [Installer](https://github.com/greenshot/greenshot/releases/download/v1.3.290/Greenshot-INSTALLER-1.3.290-RELEASE.exe)

### Technical Details
- **Full Changelog**: https://github.com/greenshot/greenshot/compare/Greenshot-RELEASE-1.2.10.6...v1.3.290

---

## About Greenshot 1.3

Greenshot 1.3 was built on the `release/1.3` branch and received multiple stable releases from May 2025 through January 2026. The series introduced significant improvements in:

- **Security**: Multiple critical security fixes
- **Editor functionality**: Zoom, keyboard shortcuts, and better DPI support
- **Installation flexibility**: Choice between per-user and all-users installation
- **Distribution**: Introduction of portable ZIP releases
- **Windows 11 compatibility**: Better integration with modern Windows versions

For more information about version management and release processes, see [docs/release-management.md](../release-management.md).

For detailed technical changelogs of all releases including continuous builds, visit the [GitHub Releases page](https://github.com/greenshot/greenshot/releases).
