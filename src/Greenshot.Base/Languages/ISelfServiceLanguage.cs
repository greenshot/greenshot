/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Base.Languages
{
    /// <summary>
    /// The texts of the [SelfService] section of greenshot.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("SelfService")]
    public interface ISelfServiceLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Copy Path
        /// </summary>
        string BtnCopyPath { get; }

        /// <summary>
        /// Copy System Info
        /// </summary>
        string BtnCopySysinfo { get; }

        /// <summary>
        /// Open Folder
        /// </summary>
        string BtnOpenFolder { get; }

        /// <summary>
        /// Open Log Viewer
        /// </summary>
        string BtnOpenLogviewer { get; }

        /// <summary>
        /// Refresh (F5)
        /// </summary>
        string BtnRefresh { get; }

        /// <summary>
        /// Reset Filename Pattern
        /// </summary>
        string BtnResetFilenamePattern { get; }

        /// <summary>
        /// Reset Folder to Default
        /// </summary>
        string BtnResetOutputPath { get; }

        /// <summary>
        /// Self-Service [S]
        /// </summary>
        string ButtonText { get; }

        /// <summary>
        /// DIAGNOSTIC CATEGORIES
        /// </summary>
        string CategoriesHeader { get; }

        /// <summary>
        /// Installation Integrity
        /// </summary>
        string CategoryChecksum { get; }

        /// <summary>
        /// Verify files against checksum.SHA256, detect corruptions or missing files
        /// </summary>
        string CategoryChecksumSub { get; }

        /// <summary>
        /// Clipboard Diagnostics
        /// </summary>
        string CategoryClipboard { get; }

        /// <summary>
        /// Real-time format analysis, active formats, and blocker process checker
        /// </summary>
        string CategoryClipboardSub { get; }

        /// <summary>
        /// Files &amp; Configuration
        /// </summary>
        string CategoryFiles { get; }

        /// <summary>
        /// Greenshot configuration file, log file paths, and quick glance viewer
        /// </summary>
        string CategoryFilesSub { get; }

        /// <summary>
        /// Hotkeys &amp; Conflicts
        /// </summary>
        string CategoryHotkeys { get; }

        /// <summary>
        /// Greenshot shortcuts, conflict detection (OneDrive, Windows 11 Snipping Tool, Dropbox)
        /// </summary>
        string CategoryHotkeysSub { get; }

        /// <summary>
        /// Delete old Imgur uploads
        /// </summary>
        string CategoryImgur { get; }

        /// <summary>
        /// Greenshot still has the delete keys of your earlier anonymous uploads
        /// </summary>
        string CategoryImgurSub { get; }

        /// <summary>
        /// System &amp; Hardware
        /// </summary>
        string CategorySystem { get; }

        /// <summary>
        /// OS, display scaling, memory usage, and .NET runtime environment
        /// </summary>
        string CategorySystemSub { get; }

        /// <summary>
        /// Copy Integrity Report
        /// </summary>
        string ChecksumBtnCopyReport { get; }

        /// <summary>
        /// Copy Details
        /// </summary>
        string ChecksumBtnCopySelected { get; }

        /// <summary>
        /// Re-check All Files
        /// </summary>
        string ChecksumBtnValidate { get; }

        /// <summary>
        /// Actual SHA-256
        /// </summary>
        string ChecksumColActualHash { get; }

        /// <summary>
        /// Expected SHA-256
        /// </summary>
        string ChecksumColExpectedHash { get; }

        /// <summary>
        /// File / Relative Path
        /// </summary>
        string ChecksumColFile { get; }

        /// <summary>
        /// Size
        /// </summary>
        string ChecksumColSize { get; }

        /// <summary>
        /// Status
        /// </summary>
        string ChecksumColStatus { get; }

        /// <summary>
        /// Integrity report copied to clipboard!
        /// </summary>
        string ChecksumCopiedReport { get; }

        /// <summary>
        /// Actual Hash:
        /// </summary>
        string ChecksumDetailActual { get; }

        /// <summary>
        /// Expected Hash:
        /// </summary>
        string ChecksumDetailExpected { get; }

        /// <summary>
        /// ✔️ File checksum matches expected signature. File is authentic and unmodified.
        /// </summary>
        string ChecksumDetailHintMatch { get; }

        /// <summary>
        /// ❌ Checksum mismatch! The file differs from the official release. It may be corrupted or modified.
        /// </summary>
        string ChecksumDetailHintMismatch { get; }

        /// <summary>
        /// ⚠️ File is recorded in checksum.SHA256 but does not exist on disk.
        /// </summary>
        string ChecksumDetailHintMissing { get; }

        /// <summary>
        /// ℹ️ File is present on disk but not listed in the checksum.SHA256 manifest.
        /// </summary>
        string ChecksumDetailHintUnlisted { get; }

        /// <summary>
        /// Full Path:
        /// </summary>
        string ChecksumDetailPath { get; }

        /// <summary>
        /// Selected File Details
        /// </summary>
        string ChecksumDetailSelected { get; }

        /// <summary>
        /// No files match the current search or filter criteria.
        /// </summary>
        string ChecksumEmptyList { get; }

        /// <summary>
        /// All
        /// </summary>
        string ChecksumFilterAll { get; }

        /// <summary>
        /// Issues Only
        /// </summary>
        string ChecksumFilterIssues { get; }

        /// <summary>
        /// Mismatched
        /// </summary>
        string ChecksumFilterMismatch { get; }

        /// <summary>
        /// Missing
        /// </summary>
        string ChecksumFilterMissing { get; }

        /// <summary>
        /// Unlisted
        /// </summary>
        string ChecksumFilterUnlisted { get; }

        /// <summary>
        /// Valid
        /// </summary>
        string ChecksumFilterValid { get; }

        /// <summary>
        /// Checksum manifest: {0}
        /// </summary>
        string ChecksumManifestLocation { get; }

        /// <summary>
        /// Checksum validation canceled.
        /// </summary>
        string ChecksumCanceled { get; }

        /// <summary>
        /// Validation error: {0}
        /// </summary>
        string ChecksumValidationError { get; }

        /// <summary>
        /// Opened Explorer for: {0}
        /// </summary>
        string ChecksumOpenedExplorer { get; }

        /// <summary>
        /// Checksum file not found, opened containing folder: {0}
        /// </summary>
        string ChecksumOpenedFolder { get; }

        /// <summary>
        /// Failed to open file: {0}
        /// </summary>
        string ChecksumOpenFailed { get; }

        /// <summary>
        /// MISMATCHED (CORRUPT)
        /// </summary>
        string ChecksumMetricCorrupt { get; }

        /// <summary>
        /// MISSING FILES
        /// </summary>
        string ChecksumMetricMissing { get; }

        /// <summary>
        /// TOTAL CHECKED
        /// </summary>
        string ChecksumMetricTotal { get; }

        /// <summary>
        /// UNLISTED FILES
        /// </summary>
        string ChecksumMetricUnlisted { get; }

        /// <summary>
        /// VALID / MATCHED
        /// </summary>
        string ChecksumMetricValid { get; }

        /// <summary>
        /// Validating file {0} of {1}...
        /// </summary>
        string ChecksumSearching { get; }

        /// <summary>
        /// Search by file name, path, or hash...
        /// </summary>
        string ChecksumSearchPlaceholder { get; }

        /// <summary>
        /// {0} file(s) failed checksum verification. Files may have been corrupted, incompletely updated, or modified. Consider reinstalling Greenshot.
        /// </summary>
        string ChecksumStatusCorruptDesc { get; }

        /// <summary>
        /// Possible Corrupt Installation Detected!
        /// </summary>
        string ChecksumStatusCorruptTitle { get; }

        /// <summary>
        /// Valid
        /// </summary>
        string ChecksumStatusMatch { get; }

        /// <summary>
        /// Mismatch (Corrupt)
        /// </summary>
        string ChecksumStatusMismatch { get; }

        /// <summary>
        /// Missing
        /// </summary>
        string ChecksumStatusMissing { get; }

        /// <summary>
        /// {0} expected file(s) are missing from the installation directory.
        /// </summary>
        string ChecksumStatusMissingDesc { get; }

        /// <summary>
        /// Missing Installation Files Detected!
        /// </summary>
        string ChecksumStatusMissingTitle { get; }

        /// <summary>
        /// The manifest file 'checksum.SHA256' was not found in: {0}
        /// </summary>
        string ChecksumStatusNotfoundDesc { get; }

        /// <summary>
        /// Checksum Manifest File Not Found
        /// </summary>
        string ChecksumStatusNotfoundTitle { get; }

        /// <summary>
        /// All files listed in checksum.SHA256 match their expected hashes.
        /// </summary>
        string ChecksumStatusOkDesc { get; }

        /// <summary>
        /// Installation Integrity Verified
        /// </summary>
        string ChecksumStatusOkTitle { get; }

        /// <summary>
        /// Unlisted
        /// </summary>
        string ChecksumStatusUnlisted { get; }

        /// <summary>
        /// Verify installed application and plugin files against the SHA-256 checksum manifest.
        /// </summary>
        string ChecksumSub { get; }

        /// <summary>
        /// Installation Integrity &amp; Checksum Verification
        /// </summary>
        string ChecksumTitle { get; }

        /// <summary>
        /// BLOCKED
        /// </summary>
        string ClipboardBadgeBlocked { get; }

        /// <summary>
        /// Clear Log
        /// </summary>
        string ClipboardBtnClearLog { get; }

        /// <summary>
        /// Start Monitor Loop
        /// </summary>
        string ClipboardBtnStartMonitor { get; }

        /// <summary>
        /// Stop Monitor Loop
        /// </summary>
        string ClipboardBtnStopMonitor { get; }

        /// <summary>
        /// Clipboard may be busy or locked
        /// </summary>
        string ClipboardBusy { get; }

        /// <summary>
        /// None
        /// </summary>
        string ClipboardNone { get; }

        /// <summary>
        /// (No owner or system clipboard)
        /// </summary>
        string ClipboardNoOwner { get; }

        /// <summary>
        /// None (or system clipboard)
        /// </summary>
        string ClipboardNoneSystem { get; }

        /// <summary>
        /// Error checking clipboard status
        /// </summary>
        string ClipboardStatusError { get; }

        /// <summary>
        /// Unknown Process
        /// </summary>
        string ClipboardUnknownProcess { get; }

        /// <summary>
        /// (No Title)
        /// </summary>
        string ClipboardNoTitle { get; }

        /// <summary>
        /// Unknown Path
        /// </summary>
        string ClipboardUnknownPath { get; }

        /// <summary>
        /// Access Denied
        /// </summary>
        string ClipboardAccessDenied { get; }

        /// <summary>
        /// (Protected or Elevated Process)
        /// </summary>
        string ClipboardProtectedProcess { get; }

        /// <summary>
        /// Native clipboard data
        /// </summary>
        string ClipboardNativeData { get; }

        /// <summary>
        /// Text: "{0}"
        /// </summary>
        string ClipboardTextPreview { get; }

        /// <summary>
        /// {0:N0} chars
        /// </summary>
        string ClipboardChars { get; }

        /// <summary>
        /// (+{0} more)
        /// </summary>
        string ClipboardMoreFiles { get; }

        /// <summary>
        /// Files: {0}
        /// </summary>
        string ClipboardFilesPreview { get; }

        /// <summary>
        /// {0} files
        /// </summary>
        string ClipboardFilesCount { get; }

        /// <summary>
        /// Raster bitmap image data
        /// </summary>
        string ClipboardRasterImage { get; }

        /// <summary>
        /// Image
        /// </summary>
        string ClipboardImage { get; }

        /// <summary>
        /// PNG compressed image data
        /// </summary>
        string ClipboardPngImage { get; }

        /// <summary>
        /// HTML formatted text fragment
        /// </summary>
        string ClipboardHtmlFragment { get; }

        /// <summary>
        /// Idle
        /// </summary>
        string ClipboardIdle { get; }

        /// <summary>
        /// Current Clipboard Data Owner:
        /// </summary>
        string ClipboardDataOwner { get; }

        /// <summary>
        /// (Clipboard is currently empty)
        /// </summary>
        string ClipboardEmpty { get; }

        /// <summary>
        /// Executable:   {0}
        /// </summary>
        string ClipboardExecutable { get; }

        /// <summary>
        /// Executable: {0}
        /// </summary>
        string ClipboardExecutableLabel { get; }

        /// <summary>
        /// Monitored via Dapplo.Windows.Clipboard
        /// </summary>
        string ClipboardFormatsSub { get; }

        /// <summary>
        /// Active Formats Currently on Clipboard (Auto-refreshed)
        /// </summary>
        string ClipboardFormatsTitle { get; }

        /// <summary>
        /// Executable:
        /// </summary>
        string ClipboardLabelExecutable { get; }

        /// <summary>
        /// Path:
        /// </summary>
        string ClipboardLabelPath { get; }

        /// <summary>
        /// Process ID:
        /// </summary>
        string ClipboardLabelPid { get; }

        /// <summary>
        /// Process Name:
        /// </summary>
        string ClipboardLabelProcessName { get; }

        /// <summary>
        /// Window Title:
        /// </summary>
        string ClipboardLabelWindowTitle { get; }

        /// <summary>
        /// Process Locking Clipboard:
        /// </summary>
        string ClipboardLockingProcess { get; }

        /// <summary>
        /// Monitor log cleared
        /// </summary>
        string ClipboardLogCleared { get; }

        /// <summary>
        /// Loops every 600ms to detect transient or permanent locks by background applications.
        /// </summary>
        string ClipboardLoopDesc { get; }

        /// <summary>
        /// Live Clipboard Blocker Monitor Loop
        /// </summary>
        string ClipboardLoopTitle { get; }

        /// <summary>
        /// Clipboard loop monitor active
        /// </summary>
        string ClipboardMonitorActive { get; }

        /// <summary>
        /// Started continuous clipboard monitoring loop (interval: 600ms)
        /// </summary>
        string ClipboardMonitorStartedMsg { get; }

        /// <summary>
        /// Monitor stopped
        /// </summary>
        string ClipboardMonitorStopped { get; }

        /// <summary>
        /// Stopped clipboard monitoring loop
        /// </summary>
        string ClipboardMonitorStoppedMsg { get; }

        /// <summary>
        /// Clipboard contains no data
        /// </summary>
        string ClipboardNoData { get; }

        /// <summary>
        /// No formats currently active
        /// </summary>
        string ClipboardNoFormats { get; }

        /// <summary>
        /// Path: {0}
        /// </summary>
        string ClipboardPath { get; }

        /// <summary>
        /// Process ID:   {0}
        /// </summary>
        string ClipboardPid { get; }

        /// <summary>
        /// Process Name: {0}
        /// </summary>
        string ClipboardProcessName { get; }

        /// <summary>
        /// Error querying formats
        /// </summary>
        string ClipboardQueryError { get; }

        /// <summary>
        /// Refresh
        /// </summary>
        string ClipboardRefresh { get; }

        /// <summary>
        /// Clipboard is Accessible (Unlocked)
        /// </summary>
        string ClipboardStatusAccessible { get; }

        /// <summary>
        /// Clipboard is BLOCKED by {0} (PID: {1})
        /// </summary>
        string ClipboardStatusBlockedBy { get; }

        /// <summary>
        /// Clipboard is currently free and accessible.
        /// </summary>
        string ClipboardStatusFree { get; }

        /// <summary>
        /// Last data set by: {0}
        /// </summary>
        string ClipboardStatusLastOwner { get; }

        /// <summary>
        /// Clipboard appears to be locked by an external application!
        /// </summary>
        string ClipboardStatusLocked { get; }

        /// <summary>
        /// Real-time format analysis (auto-refreshed on updates) and process blocker detection.
        /// </summary>
        string ClipboardSub { get; }

        /// <summary>
        /// Test Clipboard Now
        /// </summary>
        string ClipboardTestNow { get; }

        /// <summary>
        /// Clipboard Diagnostics &amp; Blocker Checker
        /// </summary>
        string ClipboardTitle { get; }

        /// <summary>
        /// Window: "{0}"
        /// </summary>
        string ClipboardWindowLabel { get; }

        /// <summary>
        /// Window Title: {0}
        /// </summary>
        string ClipboardWindowTitle { get; }

        /// <summary>
        /// Standard ANSI text string
        /// </summary>
        string ClipformatAnsi { get; }

        /// <summary>
        /// Device-dependent bitmap (GDI)
        /// </summary>
        string ClipformatBitmap { get; }

        /// <summary>
        /// Bitmap image format
        /// </summary>
        string ClipformatBmp { get; }

        /// <summary>
        /// Custom registered format
        /// </summary>
        string ClipformatCustom { get; }

        /// <summary>
        /// Device-independent bitmap (DIB)
        /// </summary>
        string ClipformatDib { get; }

        /// <summary>
        /// DIB version 5 bitmap
        /// </summary>
        string ClipformatDibv5 { get; }

        /// <summary>
        /// List of files dragged or copied (HDROP)
        /// </summary>
        string ClipformatHdrop { get; }

        /// <summary>
        /// Hypertext Markup Language (HTML)
        /// </summary>
        string ClipformatHtml { get; }

        /// <summary>
        /// Locale identifier for clipboard text
        /// </summary>
        string ClipformatLocale { get; }

        /// <summary>
        /// Enhanced Windows Metafile
        /// </summary>
        string ClipformatMetafile { get; }

        /// <summary>
        /// OEM text string
        /// </summary>
        string ClipformatOem { get; }

        /// <summary>
        /// Portable Network Graphics (PNG)
        /// </summary>
        string ClipformatPng { get; }

        /// <summary>
        /// Rich Text Format (RTF)
        /// </summary>
        string ClipformatRtf { get; }

        /// <summary>
        /// TIFF image data
        /// </summary>
        string ClipformatTiff { get; }

        /// <summary>
        /// Standard Unicode (UTF-16) text string
        /// </summary>
        string ClipformatUnicode { get; }

        /// <summary>
        /// Copied!
        /// </summary>
        string Copied { get; }

        /// <summary>
        /// File exists: {0}
        /// </summary>
        string FilesExists { get; }

        /// <summary>
        /// Greenshot Configuration File (greenshot.ini)
        /// </summary>
        string FilesInifileTitle { get; }

        /// <summary>
        /// Greenshot Log File (greenshot.log)
        /// </summary>
        string FilesLogfileTitle { get; }

        /// <summary>
        /// File not found
        /// </summary>
        string FilesNotFound { get; }

        /// <summary>
        /// Configured folder
        /// </summary>
        string FilesOutputConfigured { get; }

        /// <summary>
        /// The folder contains variables like ${YYYY}: Greenshot fills them in when it saves, and creates the folder.
        /// </summary>
        string FilesOutputContainsVariables { get; }

        /// <summary>
        /// The drive or network share {0} isn't available: until it is, Greenshot saves to the default folder (Desktop) and shows a notification.
        /// </summary>
        string FilesOutputDriveMissing { get; }

        /// <summary>
        /// Next file, for example
        /// </summary>
        string FilesOutputExample { get; }

        /// <summary>
        /// Folder used
        /// </summary>
        string FilesOutputExpanded { get; }

        /// <summary>
        /// The filename pattern doesn't give a valid filename. {0}
        /// </summary>
        string FilesOutputInvalidFilename { get; }

        /// <summary>
        /// The folder doesn't exist yet: Greenshot creates it at the next save.
        /// </summary>
        string FilesOutputMissing { get; }

        /// <summary>
        /// A network folder: when it can't be reached (e.g. without VPN), Greenshot saves to the default folder (Desktop) for that time and shows a notification.
        /// </summary>
        string FilesOutputNetwork { get; }

        /// <summary>
        /// No folder configured: Greenshot saves to the default folder (Desktop).
        /// </summary>
        string FilesOutputNotConfigured { get; }

        /// <summary>
        /// Greenshot can't write to the folder: {0} Check the permissions, or whether the controlled folder access of Windows Security blocks Greenshot.
        /// </summary>
        string FilesOutputNotWritable { get; }

        /// <summary>
        /// The folder exists and Greenshot can write to it.
        /// </summary>
        string FilesOutputOk { get; }

        /// <summary>
        /// OneDrive synchronizes the folder (also the Desktop, when OneDrive backs it up): files can be online only, or show sync conflicts.
        /// </summary>
        string FilesOutputOnedrive { get; }

        /// <summary>
        /// The filename pattern creates subfolders.
        /// </summary>
        string FilesOutputPatternFolders { get; }

        /// <summary>
        /// Reset to: {0}
        /// </summary>
        string FilesOutputResetDone { get; }

        /// <summary>
        /// Screenshot folder
        /// </summary>
        string FilesScreenshotFolder { get; }

        /// <summary>
        /// Screenshot folder copied to clipboard.
        /// </summary>
        string FilesScreenshotFolderCopied { get; }

        /// <summary>
        /// Log file
        /// </summary>
        string FilesLogFile { get; }

        /// <summary>
        /// Configuration file
        /// </summary>
        string FilesConfigFile { get; }

        /// <summary>
        /// No path specified for {0}.
        /// </summary>
        string FilesNoPath { get; }

        /// <summary>
        /// Opened Explorer for {0}.
        /// </summary>
        string FilesOpenedExplorer { get; }

        /// <summary>
        /// File not found, opened containing folder: {0}
        /// </summary>
        string FilesOpenedFolder { get; }

        /// <summary>
        /// {0} does not exist at: {1}
        /// </summary>
        string FilesDoesNotExist { get; }

        /// <summary>
        /// Failed to open Explorer: {0}
        /// </summary>
        string FilesExplorerFailed { get; }

        /// <summary>
        /// Opened Log Viewer window.
        /// </summary>
        string FilesLogviewerOpened { get; }

        /// <summary>
        /// Could not open log viewer: {0}
        /// </summary>
        string FilesLogviewerFailed { get; }

        /// <summary>
        /// Log path copied to clipboard.
        /// </summary>
        string FilesLogPathCopied { get; }

        /// <summary>
        /// Config path copied to clipboard.
        /// </summary>
        string FilesConfigPathCopied { get; }

        /// <summary>
        /// {0:N0} bytes
        /// </summary>
        string FilesSizeBytes { get; }

        /// <summary>
        /// Screenshot Folder (where Greenshot saves)
        /// </summary>
        string FilesOutputTitle { get; }

        /// <summary>
        /// The full path of a file has {0} characters, more than the {1} that Windows allows by default.
        /// </summary>
        string FilesOutputTooLong { get; }

        /// <summary>
        /// Open the dedicated Log Viewer window to scroll, search, refresh, or copy the complete recent log entries with clear scrollbars.
        /// </summary>
        string FilesQuickglanceDesc { get; }

        /// <summary>
        /// Ready to inspect log file
        /// </summary>
        string FilesQuickglanceStatus { get; }

        /// <summary>
        /// Quick Glance: Recent Log Information
        /// </summary>
        string FilesQuickglanceTitle { get; }

        /// <summary>
        /// Greenshot configuration, log file access, Explorer integration and separate log viewer.
        /// </summary>
        string FilesSub { get; }

        /// <summary>
        /// File Locations &amp; Diagnostics Log
        /// </summary>
        string FilesTitle { get; }

        /// <summary>
        /// Re-register Greenshot Hotkeys
        /// </summary>
        string HotkeysBtnReregister { get; }

        /// <summary>
        /// Greenshot Configured Shortcuts
        /// </summary>
        string HotkeysConfiguredTitle { get; }

        /// <summary>
        /// Lightshot screenshot tool (claims PrintScreen)
        /// </summary>
        string HotkeysContenderLightshot { get; }

        /// <summary>
        /// PicPick graphic design and screen capture tool
        /// </summary>
        string HotkeysContenderPicpick { get; }

        /// <summary>
        /// Windows Snip &amp; Sketch overlay process
        /// </summary>
        string HotkeysContenderScreenclipping { get; }

        /// <summary>
        /// Full screen capture utility with global hotkeys
        /// </summary>
        string HotkeysContenderSharex { get; }

        /// <summary>
        /// TechSmith Snagit screen capture application
        /// </summary>
        string HotkeysContenderSnagit { get; }

        /// <summary>
        /// TechSmith Snagit 64-bit screen capture application
        /// </summary>
        string HotkeysContenderSnagit64 { get; }

        /// <summary>
        /// Built-in Windows Snipping Tool process
        /// </summary>
        string HotkeysContenderSnipping { get; }

        /// <summary>
        /// Dropbox has a feature 'Share screenshots using Dropbox' in Dropbox Preferences &gt; Backups. When enabled, Dropbox claims PrintScreen and Alt+PrintScreen at system startup. If your hotkeys do not respond, open Dropbox Preferences, go to Backups, and uncheck 'Share screenshots using Dropbox'.
        /// </summary>
        string HotkeysDropboxDesc { get; }

        /// <summary>
        /// Dropbox process is not running.
        /// </summary>
        string HotkeysDropboxNotrunning { get; }

        /// <summary>
        /// Dropbox process is currently running.
        /// </summary>
        string HotkeysDropboxRunning { get; }

        /// <summary>
        /// Dropbox Screenshot Sharing
        /// </summary>
        string HotkeysDropboxTitle { get; }

        /// <summary>
        /// Not Running
        /// </summary>
        string HotkeysNotrunning { get; }

        /// <summary>
        /// OneDrive has a setting 'Automatically save screenshots I capture to OneDrive'. When enabled, OneDrive registers PrintScreen before Greenshot. The button above updates the OneDrive settings file and registry to disable this behavior.
        /// </summary>
        string HotkeysOnedriveDesc { get; }

        /// <summary>
        /// 🚫 Disable OneDrive Screenshot Interception
        /// </summary>
        string HotkeysOnedriveDisableBtn { get; }

        /// <summary>
        /// Could not modify OneDrive configuration: {0}
        /// </summary>
        string HotkeysOnedriveError { get; }

        /// <summary>
        /// OneDrive screenshot interception is disabled (clean).
        /// </summary>
        string HotkeysOnedriveStatusDisabled { get; }

        /// <summary>
        /// OneDrive screenshot capture is currently ENABLED and may intercept PrintScreen!
        /// </summary>
        string HotkeysOnedriveStatusEnabled { get; }

        /// <summary>
        /// OneDrive was not detected on this system.
        /// </summary>
        string HotkeysOnedriveStatusNotdetected { get; }

        /// <summary>
        /// Successfully updated OneDrive configuration to disable screenshot interception.
        /// </summary>
        string HotkeysOnedriveSuccess { get; }

        /// <summary>
        /// Microsoft OneDrive Hotkey Hijack Check
        /// </summary>
        string HotkeysOnedriveTitle { get; }

        /// <summary>
        /// Opened Windows Keyboard Settings
        /// </summary>
        string HotkeysOpenedSettings { get; }

        /// <summary>
        /// Other Known Screen Capture Applications
        /// </summary>
        string HotkeysOtherTitle { get; }

        /// <summary>
        /// Hotkeys re-registered (some keys may have conflicts).
        /// </summary>
        string HotkeysReregisterConflict { get; }

        /// <summary>
        /// Failed to re-register hotkeys: {0}
        /// </summary>
        string HotkeysReregisterFailed { get; }

        /// <summary>
        /// Successfully re-registered Greenshot hotkeys!
        /// </summary>
        string HotkeysReregisterSuccess { get; }

        /// <summary>
        /// Running (PID {0})
        /// </summary>
        string HotkeysRunning { get; }

        /// <summary>
        /// In modern Windows 11 updates, Microsoft turned on 'Use the Print screen key to open Snipping Tool' by default. This intercepts PrintScreen before any desktop tool can handle it. Disabling it restores normal PrintScreen functionality.
        /// </summary>
        string HotkeysSnippingDesc { get; }

        /// <summary>
        /// 🚫 Disable Snipping Tool Takeover
        /// </summary>
        string HotkeysSnippingDisableBtn { get; }

        /// <summary>
        /// ⚙️ Open Windows Keyboard Settings
        /// </summary>
        string HotkeysSnippingSettingsBtn { get; }

        /// <summary>
        /// Windows Snipping Tool PrintScreen takeover is disabled (clean).
        /// </summary>
        string HotkeysSnippingStatusDisabled { get; }

        /// <summary>
        /// Windows Snipping Tool PrintScreen takeover is currently ENABLED!
        /// </summary>
        string HotkeysSnippingStatusEnabled { get; }

        /// <summary>
        /// Windows Snipping Tool takeover is not applicable on this Windows version.
        /// </summary>
        string HotkeysSnippingStatusNa { get; }

        /// <summary>
        /// Successfully disabled Windows Snipping Tool PrintScreen takeover in Windows Registry.
        /// </summary>
        string HotkeysSnippingSuccess { get; }

        /// <summary>
        /// Windows 10 / 11 Snipping Tool Takeover
        /// </summary>
        string HotkeysSnippingTitle { get; }

        /// <summary>
        /// Disabled
        /// </summary>
        string HotkeysStatusDisabled { get; }

        /// <summary>
        /// Not Registered / Conflict
        /// </summary>
        string HotkeysStatusFailed { get; }

        /// <summary>
        /// Registered
        /// </summary>
        string HotkeysStatusOk { get; }

        /// <summary>
        /// Verify Greenshot shortcuts and resolve hotkey hijacking by OneDrive, Windows Snipping Tool, and Dropbox.
        /// </summary>
        string HotkeysSub { get; }

        /// <summary>
        /// Hotkeys &amp; Application Conflicts
        /// </summary>
        string HotkeysTitle { get; }

        /// <summary>
        /// Updated at {0}
        /// </summary>
        string HotkeysUpdatedAt { get; }

        /// <summary>
        /// {0} Issue
        /// </summary>
        string HotkeysIssue { get; }

        /// <summary>
        /// {0} Issues
        /// </summary>
        string HotkeysIssues { get; }

        /// <summary>
        /// Could not update registry: {0}
        /// </summary>
        string HotkeysRegistryError { get; }

        /// <summary>
        /// Could not open Windows Settings: {0}
        /// </summary>
        string HotkeysSettingsError { get; }

        /// <summary>
        /// Uploaded
        /// </summary>
        string ImgurColumnDate { get; }

        /// <summary>
        /// Image
        /// </summary>
        string ImgurColumnImage { get; }

        /// <summary>
        /// Delete from Imgur
        /// </summary>
        string ImgurDelete { get; }

        /// <summary>
        /// Delete {0} image(s) from Imgur? This can't be undone.
        /// </summary>
        string ImgurDeleteQuestion { get; }

        /// <summary>
        /// Save delete links
        /// </summary>
        string ImgurExport { get; }

        /// <summary>
        /// Delete this image
        /// </summary>
        string ImgurExportDelete { get; }

        /// <summary>
        /// Forget the delete keys
        /// </summary>
        string ImgurForget { get; }

        /// <summary>
        /// Greenshot forgets the delete keys and can no longer delete these images for you, the images stay on Imgur. Save the delete links first to keep them. Continue?
        /// </summary>
        string ImgurForgetQuestion { get; }

        /// <summary>
        /// Open
        /// </summary>
        string ImgurOpen { get; }

        /// <summary>
        /// Greenshot no longer uploads to Imgur. For the images you uploaded anonymously with an earlier Greenshot it still has the delete keys, so it can delete them for you. Only Greenshot can do that, Imgur has no other way for anonymous uploads.
        /// </summary>
        string ImgurSub { get; }

        /// <summary>
        /// Delete your old Imgur uploads
        /// </summary>
        string ImgurTitle { get; }

        /// <summary>
        /// Copied entire log content to clipboard ({0:N0} characters)
        /// </summary>
        string LogviewerCopied { get; }

        /// <summary>
        /// Copy Log
        /// </summary>
        string LogviewerCopy { get; }

        /// <summary>
        /// (Log file is empty)
        /// </summary>
        string LogviewerEmpty { get; }

        /// <summary>
        /// Log level
        /// </summary>
        string LogviewerLevel { get; }

        /// <summary>
        /// Load Earlier (+64 KB)
        /// </summary>
        string LogviewerLoadEarlier { get; }

        /// <summary>
        /// Load Entire Log
        /// </summary>
        string LogviewerLoadEntire { get; }

        /// <summary>
        /// Refresh Tail (F5)
        /// </summary>
        string LogviewerRefresh { get; }

        /// <summary>
        /// Search
        /// </summary>
        string LogviewerSearch { get; }

        /// <summary>
        /// {0:N0} of {1:N0} log entries contain "{2}"
        /// </summary>
        string LogviewerSearchMatches { get; }

        /// <summary>
        /// Loaded complete log file: {0} ({1:N0} bytes, {2} lines)
        /// </summary>
        string LogviewerStatusComplete { get; }

        /// <summary>
        /// Error reading log file: {0}
        /// </summary>
        string LogviewerStatusError { get; }

        /// <summary>
        /// Log file does not currently exist at: {0}
        /// </summary>
        string LogviewerStatusNotfound { get; }

        /// <summary>
        /// Showing end of log file: {0} ({1:N0} bytes loaded of {2:N0} bytes total, {3} lines)
        /// </summary>
        string LogviewerStatusPartial { get; }

        /// <summary>
        /// Greenshot Log Viewer
        /// </summary>
        string LogviewerTitle { get; }

        /// <summary>
        /// How much Greenshot writes to the log, until Greenshot restarts. Debug writes the most.
        /// </summary>
        string LogviewerTooltipLevel { get; }

        /// <summary>
        /// Searched at {0} | Last modified: {1}
        /// </summary>
        string LogviewerSearchedAt { get; }

        /// <summary>
        /// Size: 0 KB | 0 lines
        /// </summary>
        string LogviewerInfoEmpty { get; }

        /// <summary>
        /// Empty log file | Last modified: {0}
        /// </summary>
        string LogviewerStatusEmpty { get; }

        /// <summary>
        /// (No log entries in chunk)
        /// </summary>
        string LogviewerNoEntries { get; }

        /// <summary>
        /// Read error
        /// </summary>
        string LogviewerReadError { get; }

        /// <summary>
        /// Loaded earlier chunk at {0}
        /// </summary>
        string LogviewerLoadedEarlier { get; }

        /// <summary>
        /// Error loading earlier chunk: {0}
        /// </summary>
        string LogviewerErrorEarlier { get; }

        /// <summary>
        /// Complete log loaded at {0}
        /// </summary>
        string LogviewerLoadedComplete { get; }

        /// <summary>
        /// Error loading entire log: {0}
        /// </summary>
        string LogviewerErrorComplete { get; }

        /// <summary>
        /// Showing ~{0:N0} lines ({1} of {2})
        /// </summary>
        string LogviewerInfoPartial { get; }

        /// <summary>
        /// Showing tail {0} of {1} total. Earlier log lines available.
        /// </summary>
        string LogviewerChunkPartial { get; }

        /// <summary>
        /// Complete log: ~{0:N0} lines ({1})
        /// </summary>
        string LogviewerInfoComplete { get; }

        /// <summary>
        /// All {0} loaded.
        /// </summary>
        string LogviewerChunkComplete { get; }

        /// <summary>
        /// Loaded at {0} | Last modified: {1}
        /// </summary>
        string LogviewerLoadedAt { get; }

        /// <summary>
        /// Load previous 64 KB chunk of log lines
        /// </summary>
        string LogviewerTooltipLoadEarlier { get; }

        /// <summary>
        /// Load all remaining earlier log lines
        /// </summary>
        string LogviewerTooltipLoadEntire { get; }

        /// <summary>
        /// Shows only the log entries that contain this text, from the whole log file (Ctrl+F)
        /// </summary>
        string LogviewerTooltipSearch { get; }

        /// <summary>
        /// Wrap text
        /// </summary>
        string LogviewerWrap { get; }

        /// <summary>
        /// Esc: Close  |  F5: Refresh  |  T: Theme
        /// </summary>
        string ShortcutsHint { get; }

        /// <summary>
        /// 💡 Navigation
        /// </summary>
        string SidebarNavigationHeader { get; }

        /// <summary>
        /// Press the number of a section (1 is the first) to open it, F5 to refresh, and T to toggle Dark/Light mode.
        /// </summary>
        string SidebarNavigationTip { get; }

        /// <summary>
        /// Refreshed!
        /// </summary>
        string StatusRefreshed { get; }

        /// <summary>
        /// Copied report to clipboard!
        /// </summary>
        string SysinfoCopied { get; }

        /// <summary>
        /// GC MANAGED HEAP
        /// </summary>
        string SysMemoryGc { get; }

        /// <summary>
        /// PROCESS MEMORY
        /// </summary>
        string SysMemoryProcess { get; }

        /// <summary>
        /// SYSTEM PHYSICAL RAM
        /// </summary>
        string SysMemoryRam { get; }

        /// <summary>
        /// Full Environment Diagnostic Report
        /// </summary>
        string SysReportTitle { get; }

        /// <summary>
        /// Peak:
        /// </summary>
        string SysPeak { get; }

        /// <summary>
        /// Private:
        /// </summary>
        string SysPrivate { get; }

        /// <summary>
        /// Memory Load:
        /// </summary>
        string SysMemoryLoad { get; }

        /// <summary>
        /// {0} GB free of {1} GB
        /// </summary>
        string SysMemoryFree { get; }

        /// <summary>
        /// {0}% in use
        /// </summary>
        string SysMemoryInUse { get; }

        /// <summary>
        /// Unknown
        /// </summary>
        string SysUnknown { get; }

        /// <summary>
        /// PID: {0} | Threads: {1} | Handles: {2} | GDI: {3} | USER: {4}
        /// </summary>
        string SysProcessStats { get; }

        /// <summary>
        /// Failed to refresh metrics
        /// </summary>
        string SysRefreshFailed { get; }

        /// <summary>
        /// Updated at {0}
        /// </summary>
        string StatusUpdatedAt { get; }

        /// <summary>
        /// Copy failed: {0}
        /// </summary>
        string StatusCopyFailed { get; }

        /// <summary>
        /// Error: {0}
        /// </summary>
        string StatusError { get; }

        /// <summary>
        /// Ready
        /// </summary>
        string StatusReady { get; }

        /// <summary>
        /// System information ready
        /// </summary>
        string StatusSystemReady { get; }

        /// <summary>
        /// File locations ready
        /// </summary>
        string StatusFilesReady { get; }

        /// <summary>
        /// Clipboard diagnostics ready
        /// </summary>
        string StatusClipboardReady { get; }

        /// <summary>
        /// Hotkey diagnostics ready
        /// </summary>
        string StatusHotkeysReady { get; }

        /// <summary>
        /// Checksum validation ready
        /// </summary>
        string StatusChecksumReady { get; }

        /// <summary>
        /// Integration (Debug)
        /// </summary>
        string DebugTitle { get; }

        /// <summary>
        /// Register Native Messaging host &amp; URL scheme for current user (HKCU)
        /// </summary>
        string DebugSub { get; }

        /// <summary>
        /// Integration &amp; Extension Debugger
        /// </summary>
        string DebugHeader { get; }

        /// <summary>
        /// Configure and test local Native Messaging hosts &amp; greenshot:// URL protocol in HKCU
        /// </summary>
        string DebugHeaderSub { get; }

        /// <summary>
        /// Extension Folder
        /// </summary>
        string DebugExtensionFolder { get; }

        /// <summary>
        /// Registry &amp; Binary Status (Current User / HKCU)
        /// </summary>
        string DebugStatusTitle { get; }

        /// <summary>
        /// Proxy Executable:
        /// </summary>
        string DebugProxy { get; }

        /// <summary>
        /// Chrome Host (HKCU):
        /// </summary>
        string DebugChrome { get; }

        /// <summary>
        /// Edge Host (HKCU):
        /// </summary>
        string DebugEdge { get; }

        /// <summary>
        /// Firefox Host (HKCU):
        /// </summary>
        string DebugFirefox { get; }

        /// <summary>
        /// URL Protocol (greenshot://):
        /// </summary>
        string DebugUrlProtocol { get; }

        /// <summary>
        /// Browser Extension ID (Unpacked Developer Mode)
        /// </summary>
        string DebugExtensionIdTitle { get; }

        /// <summary>
        /// Chromium browsers (Chrome, Edge, Brave) enforce allowed_origins with the exact extension ID generated when loading an unpacked extension. Paste your extension ID below to update org.greenshot.proxy.json.
        /// </summary>
        string DebugExtensionIdDesc { get; }

        /// <summary>
        /// Extension ID
        /// </summary>
        string DebugExtensionId { get; }

        /// <summary>
        /// Save ID to Manifest
        /// </summary>
        string DebugSaveId { get; }

        /// <summary>
        /// Registry Actions
        /// </summary>
        string DebugActions { get; }

        /// <summary>
        /// Register Debug Host &amp; Protocol (HKCU)
        /// </summary>
        string DebugRegister { get; }

        /// <summary>
        /// Unregister from HKCU
        /// </summary>
        string DebugUnregister { get; }

        /// <summary>
        /// Test greenshot:// URL Scheme
        /// </summary>
        string DebugTestUrl { get; }

        /// <summary>
        /// Note: Registering in HKCU modifies only your user account (no admin privileges needed). Chrome, Edge, and Firefox will immediately detect this debug build's greenshot-proxy.
        /// </summary>
        string DebugNote { get; }

        /// <summary>
        /// Found: {0}
        /// </summary>
        string DebugProxyFound { get; }

        /// <summary>
        /// Missing in {0}
        /// </summary>
        string DebugProxyMissing { get; }

        /// <summary>
        /// Registered (HKCU): {0}
        /// </summary>
        string DebugRegistered { get; }

        /// <summary>
        /// Not registered in HKCU
        /// </summary>
        string DebugNotRegistered { get; }

        /// <summary>
        /// Successfully registered Chrome, Edge, Firefox Native Hosts and greenshot:// URL Scheme in HKCU!
        /// </summary>
        string DebugRegisterSuccess { get; }

        /// <summary>
        /// Registration failed: {0}
        /// </summary>
        string DebugRegisterFailed { get; }

        /// <summary>
        /// Removed debug native messaging hosts and greenshot:// protocol from HKCU.
        /// </summary>
        string DebugUnregisterSuccess { get; }

        /// <summary>
        /// Unregister failed: {0}
        /// </summary>
        string DebugUnregisterFailed { get; }

        /// <summary>
        /// Saved extension ID to org.greenshot.proxy.json!
        /// </summary>
        string DebugSaveIdSuccess { get; }

        /// <summary>
        /// Failed to save extension ID: {0}
        /// </summary>
        string DebugSaveIdFailed { get; }

        /// <summary>
        /// Launched 'greenshot://about'. The About dialog should open.
        /// </summary>
        string DebugTestUrlLaunched { get; }

        /// <summary>
        /// Failed to launch URL scheme: {0}
        /// </summary>
        string DebugTestUrlFailed { get; }

        /// <summary>
        /// Extension directory not found: {0}
        /// </summary>
        string DebugExtensionFolderMissing { get; }

        /// <summary>
        /// Error opening folder: {0}
        /// </summary>
        string DebugExtensionFolderError { get; }

        /// <summary>
        /// Detailed environment, memory utilization, screen layout, and process metrics.
        /// </summary>
        string SystemSub { get; }

        /// <summary>
        /// System &amp; Environment Diagnostics
        /// </summary>
        string SystemTitle { get; }

        /// <summary>
        /// ⏱️ Uptime:
        /// </summary>
        string SysUptimeLabel { get; }

        /// <summary>
        /// Self-Service
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Open Greenshot Self-Service &amp; Troubleshooting (S)
        /// </summary>
        string Tooltip { get; }

        /// <summary>
        /// Close (Esc)
        /// </summary>
        string TooltipClose { get; }

        /// <summary>
        /// Maximize / Restore
        /// </summary>
        string TooltipMaximize { get; }

        /// <summary>
        /// Minimize
        /// </summary>
        string TooltipMinimize { get; }

        /// <summary>
        /// Open full log in a separate dedicated window
        /// </summary>
        string TooltipOpenLogviewer { get; }

        /// <summary>
        /// Toggle Theme (Light / Dark)
        /// </summary>
        string TooltipTheme { get; }

        /// <summary>
        /// Greenshot Self-Service &amp; Diagnostics
        /// </summary>
        string WindowTitle { get; }
    }
}
