/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
    /// The texts of the [Core] section of greenshot.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Core")]
    public interface ICoreLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Please report bugs to
        /// </summary>
        string AboutBugs { get; }

        /// <summary>
        /// If you like Greenshot, you are welcome to support us:
        /// </summary>
        string AboutDonations { get; }

        /// <summary>
        /// Greenshot is hosted by GitHub at
        /// </summary>
        string AboutHost { get; }

        /// <summary>
        /// Icons from Yusuke Kamiyamane's Fugue icon set (Creative Commons Attribution 3.0 license)
        /// </summary>
        string AboutIcons { get; }

        /// <summary>
        /// Copyright © 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
        /// Greenshot comes with ABSOLUTELY NO WARRANTY. This is free software, and you are welcome to redistribute it under certain conditions.
        /// Details about the GNU General Public License:
        /// </summary>
        string AboutLicense { get; }

        /// <summary>
        /// About Greenshot
        /// </summary>
        string AboutTitle { get; }

        /// <summary>
        ///
        /// </summary>
        string AboutTranslation { get; }

        /// <summary>
        /// Portable
        /// </summary>
        string AboutPortable { get; }

        /// <summary>
        /// BETA
        /// </summary>
        string AboutBeta { get; }

        /// <summary>
        /// Released as free software under the GNU General Public License (GPLv3).
        /// </summary>
        string AboutReleasedGpl { get; }

        /// <summary>
        /// Greenshot - System Information
        /// </summary>
        string AboutSysinfoTitle { get; }

        /// <summary>
        /// Greenshot cannot find the log file, expected location: {0}
        /// </summary>
        string AboutLogNotFound { get; }

        /// <summary>
        /// Log File Not Found
        /// </summary>
        string AboutLogNotFoundTitle { get; }

        /// <summary>
        /// Could not open greenshot.log: {0}
        /// </summary>
        string AboutLogError { get; }

        /// <summary>
        /// Error Opening Log
        /// </summary>
        string AboutLogErrorTitle { get; }

        /// <summary>
        /// Greenshot cannot find greenshot.ini, expected location: {0}
        /// </summary>
        string AboutIniNotFound { get; }

        /// <summary>
        /// Settings File Not Found
        /// </summary>
        string AboutIniNotFoundTitle { get; }

        /// <summary>
        /// Could not open greenshot.ini: {0}
        /// </summary>
        string AboutIniError { get; }

        /// <summary>
        /// Error Opening Settings
        /// </summary>
        string AboutIniErrorTitle { get; }

        /// <summary>
        /// Add
        /// </summary>
        string AccessibleAdd { get; }

        /// <summary>
        /// Region capture: the arrow keys move the cursor, Enter starts and finishes the selection, Space switches to window capture, Esc cancels, F1 shows all keys
        /// </summary>
        string AccessibleCaptureRegion { get; }

        /// <summary>
        /// Selection {0} × {1} pixels
        /// </summary>
        string AccessibleCaptureSelection { get; }

        /// <summary>
        /// {0}, {1} × {2} pixels
        /// </summary>
        string AccessibleCaptureWindow { get; }

        /// <summary>
        /// Window capture: Tab selects the next window, Enter captures it, Space switches to region capture, Esc cancels, F1 shows all keys
        /// </summary>
        string AccessibleCaptureWindowMode { get; }

        /// <summary>
        /// Choose a color
        /// </summary>
        string AccessibleChooseColor { get; }

        /// <summary>
        /// Close
        /// </summary>
        string AccessibleClose { get; }

        /// <summary>
        /// Choose a destination
        /// </summary>
        string AccessibleDestinationPicker { get; }

        /// <summary>
        /// The letter of each destination in the destination picker, as designation:letter separated by ;
        /// The letters stay the same whatever destinations are available, each letter is used once.
        /// </summary>
        string DestinationPickerKeys { get; }

        /// <summary>
        /// Information
        /// </summary>
        string AccessibleInformation { get; }

        /// <summary>
        /// Loading…
        /// </summary>
        string AccessibleLoading { get; }

        /// <summary>
        /// Maximize
        /// </summary>
        string AccessibleMaximize { get; }

        /// <summary>
        /// Minimize
        /// </summary>
        string AccessibleMinimize { get; }

        /// <summary>
        /// Question
        /// </summary>
        string AccessibleQuestion { get; }

        /// <summary>
        /// Remove
        /// </summary>
        string AccessibleRemove { get; }

        /// <summary>
        /// Restore
        /// </summary>
        string AccessibleRestore { get; }

        /// <summary>
        /// Switch to the dark theme
        /// </summary>
        string AccessibleThemeDark { get; }

        /// <summary>
        /// Switch to the light theme
        /// </summary>
        string AccessibleThemeLight { get; }

        /// <summary>
        /// Greenshot - the revolutionary screenshot utility
        /// </summary>
        string ApplicationTitle { get; }

        /// <summary>
        /// Close
        /// </summary>
        string BugreportCancel { get; }

        /// <summary>
        /// Sorry, an unexpected error occurred.
        ///
        /// The good news is: you can help us getting rid of it by filing a bug report.
        /// Please visit the URL below, create a new bug report and paste the contents from the text area into the description.
        ///
        /// Please add a meaningful summary and enclose any information you consider to be helpful for reproducing the issue.
        /// Also, we would highly appreciate if you checked whether a tracker item already exists for this bug. (You can use the search to find those quickly.) Thank you :)
        /// </summary>
        string BugreportInfo { get; }

        /// <summary>
        /// Error
        /// </summary>
        string BugreportTitle { get; }

        /// <summary>
        /// Greenshot - An Unexpected Error Occurred
        /// </summary>
        string BugreportWindowTitle { get; }

        /// <summary>
        /// Something went wrong
        /// </summary>
        string BugreportHeading { get; }

        /// <summary>
        /// Greenshot encountered an error, but don't worry, your work is safe. You can check if this issue has already been reported or file a new bug report with the details below.
        /// </summary>
        string BugreportIntro { get; }

        /// <summary>
        /// Installed Version:
        /// </summary>
        string BugreportInstalledVersion { get; }

        /// <summary>
        /// Latest Release:
        /// </summary>
        string BugreportLatestRelease { get; }

        /// <summary>
        /// Checking...
        /// </summary>
        string BugreportChecking { get; }

        /// <summary>
        /// Unavailable
        /// </summary>
        string BugreportUnavailable { get; }

        /// <summary>
        /// A newer version ({0}) is available online. Upgrading may already resolve this issue!
        /// </summary>
        string BugreportNewerVersion { get; }

        /// <summary>
        /// You are using the latest version ({0}).
        /// </summary>
        string BugreportLatestVersion { get; }

        /// <summary>
        /// Download Update
        /// </summary>
        string BugreportDownloadUpdate { get; }

        /// <summary>
        /// Issue Identifier (Stack Trace Hash)
        /// </summary>
        string BugreportIssueIdentifier { get; }

        /// <summary>
        /// No Stack
        /// </summary>
        string BugreportNoStack { get; }

        /// <summary>
        /// Search Existing Issues
        /// </summary>
        string BugreportSearchIssues { get; }

        /// <summary>
        /// Searching GitHub for this hash helps avoid filing duplicate issues and lets you quickly see if a fix or workaround is already available.
        /// </summary>
        string BugreportSearchHint { get; }

        /// <summary>
        /// Error Message:
        /// </summary>
        string BugreportErrorMessage { get; }

        /// <summary>
        /// An unexpected error occurred.
        /// </summary>
        string BugreportUnexpected { get; }

        /// <summary>
        /// Stack Trace:
        /// </summary>
        string BugreportStackTrace { get; }

        /// <summary>
        /// Copy Stack Trace
        /// </summary>
        string BugreportCopyStackTrace { get; }

        /// <summary>
        /// Copy Stack Trace to Clipboard
        /// </summary>
        string BugreportCopyStackTraceTooltip { get; }

        /// <summary>
        /// Show Details
        /// </summary>
        string BugreportShowDetails { get; }

        /// <summary>
        /// Hide Details
        /// </summary>
        string BugreportHideDetails { get; }

        /// <summary>
        /// Copied!
        /// </summary>
        string BugreportCopied { get; }

        /// <summary>
        /// Failed to copy
        /// </summary>
        string BugreportCopyFailed { get; }

        /// <summary>
        /// Report on GitHub
        /// </summary>
        string BugreportReportGithub { get; }

        /// <summary>
        /// Cancel
        /// </summary>
        string Cancel { get; }

        /// <summary>
        /// An unexpected error occurred while writing to the clipboard.
        /// </summary>
        string ClipboardError { get; }

        /// <summary>
        /// Greenshot wasn't able to write to the clipboard as the process {0} blocked the access.
        /// </summary>
        string ClipboardInuse { get; }

        /// <summary>
        /// Couldn't find a clipboard image.
        /// </summary>
        string ClipboardNoimage { get; }

        /// <summary>
        /// Alpha
        /// </summary>
        string ColorpickerAlpha { get; }

        /// <summary>
        /// Apply
        /// </summary>
        string ColorpickerApply { get; }

        /// <summary>
        /// Blue
        /// </summary>
        string ColorpickerBlue { get; }

        /// <summary>
        /// Green
        /// </summary>
        string ColorpickerGreen { get; }

        /// <summary>
        /// HTML color
        /// </summary>
        string ColorpickerHtmlcolor { get; }

        /// <summary>
        /// Pick a color from the screen: drag from here with the mouse
        /// </summary>
        string ColorpickerPipette { get; }

        /// <summary>
        /// Recently used colors
        /// </summary>
        string ColorpickerRecentcolors { get; }

        /// <summary>
        /// Red
        /// </summary>
        string ColorpickerRed { get; }

        /// <summary>
        /// Color picker
        /// </summary>
        string ColorpickerTitle { get; }

        /// <summary>
        /// Transparent
        /// </summary>
        string ColorpickerTransparent { get; }

        /// <summary>
        /// Color under the mouse cursor
        /// </summary>
        string ColorpickerUnderCursor { get; }

        /// <summary>
        /// The destination {0} rejected Greenshot access, probably a dialog is open. Close the dialog and try again.
        /// </summary>
        string ComRejected { get; }

        /// <summary>
        /// Greenshot access rejected
        /// </summary>
        string ComRejectedTitle { get; }

        /// <summary>
        /// Could not save Greenshot's configuration file. Please check access permissions for '{0}'.
        /// </summary>
        string ConfigUnauthorizedaccessWrite { get; }

        /// <summary>
        /// About Greenshot
        /// </summary>
        string ContextmenuAbout { get; }

        /// <summary>
        /// Capture region
        /// </summary>
        string ContextmenuCapturearea { get; }

        /// <summary>
        /// Open image from clipboard
        /// </summary>
        string ContextmenuCaptureclipboard { get; }

        /// <summary>
        /// Capture full screen
        /// </summary>
        string ContextmenuCapturefullscreen { get; }

        /// <summary>
        /// all
        /// </summary>
        string ContextmenuCapturefullscreenAll { get; }

        /// <summary>
        /// bottom
        /// </summary>
        string ContextmenuCapturefullscreenBottom { get; }

        /// <summary>
        /// left
        /// </summary>
        string ContextmenuCapturefullscreenLeft { get; }

        /// <summary>
        /// right
        /// </summary>
        string ContextmenuCapturefullscreenRight { get; }

        /// <summary>
        /// top
        /// </summary>
        string ContextmenuCapturefullscreenTop { get; }

        /// <summary>
        /// Capture last region
        /// </summary>
        string ContextmenuCapturelastregion { get; }

        /// <summary>
        /// Capture window
        /// </summary>
        string ContextmenuCapturewindow { get; }

        /// <summary>
        /// Capture window from list
        /// </summary>
        string ContextmenuCapturewindowfromlist { get; }

        /// <summary>
        /// This plugin has no configurable options.
        /// </summary>
        string PluginsNoOptions { get; }

        /// <summary>
        /// Load a plugin which isn't signed?
        /// </summary>
        string PluginUnsignedTitle { get; }

        /// <summary>
        /// The plugin {0} isn't signed by the makers of Greenshot, so it wasn't loaded.\n\nFolder: {1}\nSigned by: {2}\nSHA-256: {3}\n\nOnly load it when you trust where it comes from. It is loaded at the next start of Greenshot.
        /// </summary>
        string PluginUnsignedQuestion { get; }

        /// <summary>
        /// nobody
        /// </summary>
        string PluginUnsignedNoSigner { get; }

        /// <summary>
        /// Load
        /// </summary>
        string PluginUnsignedLoad { get; }

        /// <summary>
        /// Don't load
        /// </summary>
        string PluginUnsignedDontLoad { get; }

        /// <summary>
        /// Keys
        /// </summary>
        string CaptureKeysTitle { get; }

        /// <summary>
        /// Cancel the capture
        /// </summary>
        string CaptureKeyCancel { get; }

        /// <summary>
        /// Move the cursor one pixel
        /// </summary>
        string CaptureKeyMove { get; }

        /// <summary>
        /// Move the cursor 10 pixels
        /// </summary>
        string CaptureKeyMoveFast { get; }

        /// <summary>
        /// Hold to keep the selection to one direction
        /// </summary>
        string CaptureKeyFixDirection { get; }

        /// <summary>
        /// Show or hide the mouse cursor
        /// </summary>
        string CaptureKeyMouseCursor { get; }

        /// <summary>
        /// Show or hide the zoomer
        /// </summary>
        string CaptureKeyZoomer { get; }

        /// <summary>
        /// Switch between the frozen and the live screen
        /// </summary>
        string CaptureKeyLive { get; }

        /// <summary>
        /// Switch between region and window
        /// </summary>
        string CaptureKeyRegionWindow { get; }

        /// <summary>
        /// Capture text (OCR)
        /// </summary>
        string CaptureKeyText { get; }

        /// <summary>
        /// Capture a fixed size
        /// </summary>
        string CaptureKeyFixedSize { get; }

        /// <summary>
        /// Capture the area
        /// </summary>
        string CaptureKeyFixedSizeAccept { get; }

        /// <summary>
        /// Pick a size from the list
        /// </summary>
        string CaptureKeyFixedSizePick { get; }

        /// <summary>
        /// The previous or next size in the list
        /// </summary>
        string CaptureKeyFixedSizeStep { get; }

        /// <summary>
        /// Hold the area at another corner or in the middle
        /// </summary>
        string CaptureKeyFixedSizeCorner { get; }

        /// <summary>
        /// Help text of the key which starts and stops typing a width and height in the fixed size capture.
        /// </summary>
        string CaptureKeyFixedSizeType { get; }

        /// <summary>
        /// Help text of Backspace while typing a size in the fixed size capture.
        /// </summary>
        string CaptureKeyFixedSizeTyping { get; }

        /// <summary>
        /// Help text of Tab while typing a size in the fixed size capture: the other box.
        /// </summary>
        string CaptureKeyFixedSizeNextBox { get; }

        /// <summary>
        /// Cursor at the top left corner
        /// </summary>
        string CaptureFixedSizesAnchorTopLeft { get; }

        /// <summary>
        /// Cursor at the top right corner
        /// </summary>
        string CaptureFixedSizesAnchorTopRight { get; }

        /// <summary>
        /// Cursor at the bottom right corner
        /// </summary>
        string CaptureFixedSizesAnchorBottomRight { get; }

        /// <summary>
        /// Cursor at the bottom left corner
        /// </summary>
        string CaptureFixedSizesAnchorBottomLeft { get; }

        /// <summary>
        /// Cursor in the middle
        /// </summary>
        string CaptureFixedSizesAnchorCenter { get; }

        /// <summary>
        /// Sizes
        /// </summary>
        string CaptureFixedSizesTitle { get; }

        /// <summary>
        /// Recently captured
        /// </summary>
        string CaptureFixedSizesRecent { get; }

        /// <summary>
        /// Hold Alt to place it without snapping
        /// </summary>
        string CaptureFixedSizesNoSnap { get; }

        /// <summary>
        /// {0} at {1}, {2}
        /// </summary>
        string CaptureFixedSizesAt { get; }

        /// <summary>
        /// {0} at {1}, {2} on screen {3}
        /// </summary>
        string CaptureFixedSizesAtScreen { get; }

        /// <summary>
        /// {0} at {1}, {2} in {3}
        /// </summary>
        string CaptureFixedSizesAtProgram { get; }

        /// <summary>
        /// The label of the width box when typing a size in the fixed size capture.
        /// </summary>
        string CaptureFixedSizeWidth { get; }

        /// <summary>
        /// The label of the height box when typing a size in the fixed size capture.
        /// </summary>
        string CaptureFixedSizeHeight { get; }

        /// <summary>
        /// What the keys do, below the boxes when typing a size in the fixed size capture.
        /// </summary>
        string CaptureFixedSizeTypingHint { get; }

        /// <summary>
        /// Start or finish the selection
        /// </summary>
        string CaptureKeyRegionSelect { get; }

        /// <summary>
        /// Capture the window
        /// </summary>
        string CaptureKeyWindowAccept { get; }

        /// <summary>
        /// Show or hide window details
        /// </summary>
        string CaptureKeyWindowDetails { get; }

        /// <summary>
        /// Select the larger area around it
        /// </summary>
        string CaptureKeyWindowLarger { get; }

        /// <summary>
        /// Select the smaller area inside it
        /// </summary>
        string CaptureKeyWindowSmaller { get; }

        /// <summary>
        /// Select the next window
        /// </summary>
        string CaptureKeyWindowNext { get; }

        /// <summary>
        /// Select the previous window
        /// </summary>
        string CaptureKeyWindowPrevious { get; }

        /// <summary>
        /// Show or hide the keys
        /// </summary>
        string CaptureKeyHelp { get; }

        /// <summary>
        /// Show or hide the info
        /// </summary>
        string CaptureKeyInfo { get; }

        /// <summary>
        /// Preview of the selection: in a corner, at the cursor, off
        /// </summary>
        string CaptureKeySelectionPreview { get; }

        /// <summary>
        /// Info
        /// </summary>
        string CaptureInfoTitle { get; }

        /// <summary>
        /// Screen
        /// </summary>
        string CaptureInfoScreen { get; }

        /// <summary>
        /// All screens
        /// </summary>
        string CaptureInfoAllScreens { get; }

        /// <summary>
        /// Selection
        /// </summary>
        string CaptureInfoSelection { get; }

        /// <summary>
        /// None
        /// </summary>
        string CaptureInfoNoSelection { get; }

        /// <summary>
        /// Window
        /// </summary>
        string CaptureInfoWindow { get; }

        /// <summary>
        /// Mouse
        /// </summary>
        string CaptureInfoMouse { get; }

        /// <summary>
        /// Support Greenshot
        /// </summary>
        string ContextmenuDonate { get; }

        /// <summary>
        /// Exit
        /// </summary>
        string ContextmenuExit { get; }

        /// <summary>
        /// Help
        /// </summary>
        string ContextmenuHelp { get; }

        /// <summary>
        /// Import Recipe...
        /// </summary>
        string ContextmenuImportrecipe { get; }

        /// <summary>
        /// Recipe Manager...
        /// </summary>
        string ContextmenuManagerecipes { get; }

        /// <summary>
        /// Open image from file
        /// </summary>
        string ContextmenuOpenfile { get; }

        /// <summary>
        /// Open last capture location
        /// </summary>
        string ContextmenuOpenrecentcapture { get; }

        /// <summary>
        /// Quick preferences
        /// </summary>
        string ContextmenuQuicksettings { get; }

        /// <summary>
        /// Recipe Editor...
        /// </summary>
        string ContextmenuRecipeeditor { get; }

        /// <summary>
        /// Recipes
        /// </summary>
        string ContextmenuRecipes { get; }

        /// <summary>
        /// Reload Recipes
        /// </summary>
        string ContextmenuReloadrecipes { get; }

        /// <summary>
        /// Preferences...
        /// </summary>
        string ContextmenuSettings { get; }

        /// <summary>
        /// Error while exporting to {0}. Please try again.
        /// </summary>
        string DestinationExportfailed { get; }

        /// <summary>
        /// Error
        /// </summary>
        string Error { get; }

        /// <summary>
        /// An instance of Greenshot is already running.
        /// </summary>
        string ErrorMultipleinstances { get; }

        /// <summary>
        /// Cannot save file to {0}.
        /// Please check write accessibility of the selected storage location.
        /// </summary>
        string ErrorNowriteaccess { get; }

        /// <summary>
        /// The file "{0}" could not be opened.
        /// </summary>
        string ErrorOpenfile { get; }

        /// <summary>
        /// Could not open link '{0}'.
        /// </summary>
        string ErrorOpenlink { get; }
        /// <summary>
        /// The screenshot folder {0} can't be used: {1}
        /// This screenshot was saved to {2}. Self-Service (Files &amp; Configuration) shows why.
        /// </summary>
        string ErrorOutputfolderFallback { get; }

        /// <summary>
        /// Could not save screenshot, please find a suitable location.
        /// </summary>
        string ErrorSave { get; }

        /// <summary>
        /// The generated filename or directory name is not valid. Please fix the filename pattern and then try again.
        /// </summary>
        string ErrorSaveInvalidChars { get; }

        /// <summary>
        /// Expert
        /// </summary>
        string Expertsettings { get; }

        /// <summary>
        /// Exported to: {0}
        /// </summary>
        string ExportedTo { get; }

        /// <summary>
        /// An error occurred while exporting to {0}:
        /// </summary>
        string ExportedToError { get; }

        /// <summary>
        /// Edit
        /// </summary>
        string NotificationButtonEdit { get; }

        /// <summary>
        /// Barcode: {0}
        /// </summary>
        string NotificationBarcode { get; }

        /// <summary>
        /// QR code: {0}
        /// </summary>
        string NotificationQrcode { get; }

        /// <summary>
        /// Show in folder
        /// </summary>
        string NotificationButtonFolder { get; }

        /// <summary>
        /// Copy link
        /// </summary>
        string NotificationButtonCopylink { get; }

        /// <summary>
        /// Greenshot Help
        /// </summary>
        string HelpTitle { get; }

        /// <summary>
        /// Hotkeys
        /// </summary>
        string Hotkeys { get; }

        /// <summary>
        /// Please choose the JPEG quality for your image.
        /// </summary>
        string JpegqualitydialogChoosejpegquality { get; }

        /// <summary>
        /// OK
        /// </summary>
        string Ok { get; }

        /// <summary>
        /// Details
        /// </summary>
        string ErrorDetails { get; }

        /// <summary>
        /// Copy details
        /// </summary>
        string ErrorCopyDetails { get; }

        /// <summary>
        /// An error ocurred while trying to print.
        /// </summary>
        string PrintError { get; }

        /// <summary>
        /// Center printout on page
        /// </summary>
        string PrintoptionsAllowcenter { get; }

        /// <summary>
        /// Enlarge printout to fit paper size
        /// </summary>
        string PrintoptionsAllowenlarge { get; }

        /// <summary>
        /// Rotate printout to page orientation
        /// </summary>
        string PrintoptionsAllowrotate { get; }

        /// <summary>
        /// Shrink printout to fit paper size
        /// </summary>
        string PrintoptionsAllowshrink { get; }

        /// <summary>
        /// Color settings
        /// </summary>
        string PrintoptionsColors { get; }

        /// <summary>
        /// Save options as default and do not ask again
        /// </summary>
        string PrintoptionsDontaskagain { get; }

        /// <summary>
        /// Print with inverted colors
        /// </summary>
        string PrintoptionsInverted { get; }

        /// <summary>
        /// Page layout settings
        /// </summary>
        string PrintoptionsLayout { get; }

        /// <summary>
        /// Full color print
        /// </summary>
        string PrintoptionsPrintcolor { get; }

        /// <summary>
        /// Force grayscale printing
        /// </summary>
        string PrintoptionsPrintgrayscale { get; }

        /// <summary>
        /// Force black/white printing
        /// </summary>
        string PrintoptionsPrintmonochrome { get; }

        /// <summary>
        /// Print date / time at bottom of page
        /// </summary>
        string PrintoptionsTimestamp { get; }

        /// <summary>
        /// Greenshot print options
        /// </summary>
        string PrintoptionsTitle { get; }

        /// <summary>
        /// Save as default quality and do not ask again
        /// </summary>
        string QualitydialogDontaskagain { get; }

        /// <summary>
        /// Greenshot quality
        /// </summary>
        string QualitydialogTitle { get; }

        /// <summary>
        /// Automatic steps
        /// </summary>
        string QuicksettingsAutomaticsteps { get; }

        /// <summary>
        /// More…
        /// </summary>
        string QuicksettingsAutomaticstepsMore { get; }

        /// <summary>
        /// Save directly (using preferred file output settings)
        /// </summary>
        string QuicksettingsDestinationFile { get; }

        /// <summary>
        /// A newer version of Greenshot is available! Do you want to download Greenshot {0}?
        /// </summary>
        string UpdateFound { get; }

        /// <summary>
        /// Warning
        /// </summary>
        string Warning { get; }

        /// <summary>
        /// The hotkey(s) "{0}" could not be registered. This problem is probably caused by another tool{1} claiming usage of the same hotkey(s)! You could either change your hotkey settings or deactivate/change the software making use of the hotkey(s).
        ///
        ///     All Greenshot features still work directly from the tray icon context menu without hotkeys.
        /// </summary>
        string WarningHotkeys { get; }

        /// <summary>
        /// Configure QR code
        /// </summary>
        string ZxingContextmenuConfigure { get; }

        /// <summary>
        /// Scan screen captures for QR codes (shown while selecting and in the notification)
        /// </summary>
        string ZxingScanScreenCaptures { get; }

        /// <summary>
        /// Sign in to {0}
        /// </summary>
        string SigninTitle { get; }

        /// <summary>
        /// Cloud: e-mail address and API token
        /// </summary>
        string SigninKindCloud { get; }

        /// <summary>
        /// User name and password
        /// </summary>
        string SigninKindPassword { get; }

        /// <summary>
        /// Personal access token
        /// </summary>
        string SigninKindToken { get; }

        /// <summary>
        /// E-mail address
        /// </summary>
        string SigninEmail { get; }

        /// <summary>
        /// User name
        /// </summary>
        string SigninUserName { get; }

        /// <summary>
        /// Password
        /// </summary>
        string SigninPassword { get; }

        /// <summary>
        /// API token
        /// </summary>
        string SigninApiToken { get; }

        /// <summary>
        /// Token
        /// </summary>
        string SigninAccessToken { get; }

        /// <summary>
        /// Create an API token
        /// </summary>
        string SigninCreateApiToken { get; }

        /// <summary>
        /// Remember in the Windows Credential Manager
        /// </summary>
        string SigninRemember { get; }

        /// <summary>
        /// Sign in
        /// </summary>
        string SigninButton { get; }

        /// <summary>
        /// Connecting…
        /// </summary>
        string SigninConnecting { get; }

        /// <summary>
        /// Please fill in the credentials.
        /// </summary>
        string SigninMissing { get; }

        /// <summary>
        /// The server didn't accept these credentials.
        /// </summary>
        string SigninRejected { get; }

        /// <summary>
        /// The server doesn't accept a sign-in from Greenshot right now, usually after failed attempts. Sign in once in the browser, then try again.
        /// </summary>
        string SigninDenied { get; }

        /// <summary>
        /// Couldn't connect to the server: {0}
        /// </summary>
        string SigninFailed { get; }

        /// <summary>
        /// Account
        /// </summary>
        string SigninAccount { get; }

        /// <summary>
        /// Signed in as {0}
        /// </summary>
        string SigninAccountSignedIn { get; }

        /// <summary>
        /// Credentials are stored for this server, test the connection to check them.
        /// </summary>
        string SigninAccountStored { get; }

        /// <summary>
        /// Not signed in
        /// </summary>
        string SigninAccountNotSignedIn { get; }

        /// <summary>
        /// Sign in…
        /// </summary>
        string SigninAccountSignIn { get; }

        /// <summary>
        /// Test connection
        /// </summary>
        string SigninAccountTest { get; }

        /// <summary>
        /// Sign out
        /// </summary>
        string SigninAccountSignOut { get; }

        /// <summary>
        /// The viewer only shows the capture, it isn't saved: it is gone when you close the viewer. To keep it, export it (E or the menu with the right mouse button), e.g. save it to a file. H shows all keys.
        /// </summary>
        string ViewerUnsavedHint { get; }

        /// <summary>
        /// Export…
        /// </summary>
        string ViewerExport { get; }

        /// <summary>
        /// Real size (1:1)
        /// </summary>
        string ViewerActualSize { get; }

        /// <summary>
        /// Fit to the window
        /// </summary>
        string ViewerFit { get; }

        /// <summary>
        /// Always on top
        /// </summary>
        string ViewerAlwaysOnTop { get; }

        /// <summary>
        /// Title bar and border
        /// </summary>
        string ViewerTitleBar { get; }

        /// <summary>
        /// Mouse pointer
        /// </summary>
        string ViewerMousePointer { get; }

        /// <summary>
        /// Keys
        /// </summary>
        string ViewerHelp { get; }

        /// <summary>
        /// Close all viewers
        /// </summary>
        string ViewerCloseAll { get; }

        /// <summary>
        /// Overview, click or drag to move the visible part
        /// </summary>
        string ViewerOverview { get; }

        /// <summary>
        /// Keys of the viewer
        /// </summary>
        string ViewerHelpTitle { get; }

        /// <summary>
        /// Show the capture in its real size, one pixel of the capture is one pixel of the screen
        /// </summary>
        string ViewerKeyActualSize { get; }

        /// <summary>
        /// Show the capture as when the viewer opened
        /// </summary>
        string ViewerKeyFit { get; }

        /// <summary>
        /// Zoom in and out, also with the mouse wheel
        /// </summary>
        string ViewerKeyZoom { get; }

        /// <summary>
        /// Move the visible part, also by dragging the capture or in the overview
        /// </summary>
        string ViewerKeyPan { get; }

        /// <summary>
        /// Drag the capture with Ctrl to move the window, without Ctrl when all of the capture is visible
        /// </summary>
        string ViewerKeyMove { get; }

        /// <summary>
        /// Export the capture: save, copy, open in the editor…
        /// </summary>
        string ViewerKeyExport { get; }

        /// <summary>
        /// Keep the viewer on top of the other windows, or not
        /// </summary>
        string ViewerKeyAlwaysOnTop { get; }

        /// <summary>
        /// Show or hide the title bar and the border
        /// </summary>
        string ViewerKeyTitleBar { get; }

        /// <summary>
        /// Show or hide the captured mouse pointer
        /// </summary>
        string ViewerKeyMousePointer { get; }

        /// <summary>
        /// Open the menu, also with the right mouse button
        /// </summary>
        string ViewerKeyMenu { get; }

        /// <summary>
        /// Show or hide these keys
        /// </summary>
        string ViewerKeyHelp { get; }

        /// <summary>
        /// Close the viewer, or these keys when they are shown
        /// </summary>
        string ViewerKeyClose { get; }
    }
}
