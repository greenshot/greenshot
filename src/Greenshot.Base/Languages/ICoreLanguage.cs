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
        /// Configure {0}
        /// </summary>
        string ContextmenuConfigurePlugin { get; }

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
        /// Switch between region and window
        /// </summary>
        string CaptureKeyRegionWindow { get; }

        /// <summary>
        /// Capture text (OCR)
        /// </summary>
        string CaptureKeyText { get; }

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
        /// Show or hide the keys
        /// </summary>
        string CaptureKeyHelp { get; }

        /// <summary>
        /// Show or hide the info
        /// </summary>
        string CaptureKeyInfo { get; }

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
        /// Create a quicklink to the configuration in the context menu
        /// </summary>
        string QuicklinkEnable { get; }

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
        /// Right-click here or press the {0} key.
        /// </summary>
        string TooltipFirststart { get; }

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
        /// Enable QR code scanning on region capture
        /// </summary>
        string ZxingScanOnCapture { get; }
    }
}
