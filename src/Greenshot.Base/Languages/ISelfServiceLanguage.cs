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
        /// Open Log Viewer
        /// </summary>
        string BtnOpenLogviewer { get; }

        /// <summary>
        /// Refresh (F5)
        /// </summary>
        string BtnRefresh { get; }

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
        /// Load previous 64 KB chunk of log lines
        /// </summary>
        string LogviewerTooltipLoadEarlier { get; }

        /// <summary>
        /// Load all remaining earlier log lines
        /// </summary>
        string LogviewerTooltipLoadEntire { get; }

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
        /// Press 1-5 to navigate tabs, F5 to refresh, and T to toggle Dark/Light mode.
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
