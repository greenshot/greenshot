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
    /// The texts of the [Settings] section of greenshot.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Settings")]
    public interface ISettingsLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// AI tools
        /// </summary>
        string Aitools { get; }

        /// <summary>
        /// Access
        /// </summary>
        string AitoolsAccess { get; }

        /// <summary>
        /// Add
        /// </summary>
        string AitoolsAdd { get; }

        /// <summary>
        /// These programs may use Greenshot. Remove one with ✕ to be asked again the next time it connects.
        /// </summary>
        string AitoolsAllowed { get; }

        /// <summary>
        /// Not allowed until Greenshot restarts
        /// </summary>
        string AitoolsDenied { get; }

        /// <summary>
        /// Allow
        /// </summary>
        string AitoolsDeniedAllow { get; }

        /// <summary>
        /// Ask Again
        /// </summary>
        string AitoolsDeniedAskagain { get; }

        /// <summary>
        /// You answered "Don't Allow" (or closed the question) for these programs, so Greenshot refuses them without asking. Allow one here (saved with OK), or let Greenshot ask again the next time it connects.
        /// </summary>
        string AitoolsDeniedDescription { get; }

        /// <summary>
        /// Let AI tools use Greenshot
        /// </summary>
        string AitoolsEnabled { get; }

        /// <summary>
        /// AI tools (like Claude Desktop or VS Code) connect through greenshot-mcp.exe, a separate download that has to be next to Greenshot.exe. They can list your windows, take screenshots and run your recipes. While this is off, Greenshot refuses every request without asking. Each program still needs your permission the first time it connects.
        /// </summary>
        string AitoolsEnabledDescription { get; }

        /// <summary>
        /// Never share these applications
        /// </summary>
        string AitoolsExcluded { get; }

        /// <summary>
        /// A process name, like KeePass or keepass.exe; Enter adds it
        /// </summary>
        string AitoolsExcludedAddTooltip { get; }

        /// <summary>
        /// Windows of these applications are never listed or captured for AI tools. Names are process names without .exe, upper and lower case don't matter.
        /// </summary>
        string AitoolsExcludedDescription { get; }

        /// <summary>
        /// Download Greenshot-MCP for this version from the releases page and extract it next to Greenshot.exe:
        /// </summary>
        string AitoolsMcpDownload { get; }

        /// <summary>
        /// No program is allowed yet.
        /// </summary>
        string AitoolsNone { get; }

        /// <summary>
        /// Show a notification when an AI tool takes a screenshot
        /// </summary>
        string AitoolsNotify { get; }

        /// <summary>
        /// What AI tools may do
        /// </summary>
        string AitoolsPermissions { get; }

        /// <summary>
        /// Allowed programs
        /// </summary>
        string AitoolsPrograms { get; }

        /// <summary>
        /// Let AI tools propose recipes
        /// </summary>
        string AitoolsProposals { get; }

        /// <summary>
        /// You see every proposed recipe and decide whether to approve it. Its triggers and risky actions start switched off.
        /// </summary>
        string AitoolsProposalsDescription { get; }

        /// <summary>
        /// Remove
        /// </summary>
        string AitoolsRemove { get; }

        /// <summary>
        /// Show print options dialog every time an image is printed
        /// </summary>
        string Alwaysshowprintoptionsdialog { get; }

        /// <summary>
        /// Show quality dialog every time an image is saved
        /// </summary>
        string Alwaysshowqualitydialog { get; }

        /// <summary>
        /// Application Settings
        /// </summary>
        string Applicationsettings { get; }

        /// <summary>
        /// Launch Greenshot on startup
        /// </summary>
        string Autostartshortcut { get; }

        /// <summary>
        /// Capture
        /// </summary>
        string Capture { get; }

        /// <summary>
        /// Capture mousepointer
        /// </summary>
        string CaptureMousepointer { get; }

        /// <summary>
        /// Use interactive window capture mode
        /// </summary>
        string CaptureWindowsInteractive { get; }

        /// <summary>
        /// Update check interval in days (0=no check)
        /// </summary>
        string Checkperiod { get; }

        /// <summary>
        /// Configure
        /// </summary>
        string Configureplugin { get; }

        /// <summary>
        /// Copy file path to clipboard every time an image is saved
        /// </summary>
        string Copypathtoclipboard { get; }

        /// <summary>
        /// Destination
        /// </summary>
        string Destination { get; }

        /// <summary>
        /// Copy to clipboard
        /// </summary>
        string DestinationClipboard { get; }

        /// <summary>
        /// Open in image editor
        /// </summary>
        string DestinationEditor { get; }

        /// <summary>
        /// Add to editor
        /// </summary>
        string DestinationEditorAdd { get; }

        /// <summary>
        /// E-Mail
        /// </summary>
        string DestinationEmail { get; }

        /// <summary>
        /// Save directly (using settings below)
        /// </summary>
        string DestinationFile { get; }

        /// <summary>
        /// Save as (displaying dialog)
        /// </summary>
        string DestinationFileas { get; }

        /// <summary>
        /// Ask for destination every time
        /// </summary>
        string DestinationPicker { get; }

        /// <summary>
        /// Send to printer
        /// </summary>
        string DestinationPrinter { get; }

        /// <summary>
        /// Editor
        /// </summary>
        string Editor { get; }

        /// <summary>
        /// Create an 8-bit image if the colors are less than 256 while having a &gt; 8 bits image
        /// </summary>
        string ExpertAutoreducecolors { get; }

        /// <summary>
        /// Enable to enable beta-test features.
        /// </summary>
        string ExpertBetatester { get; }

        /// <summary>
        /// Check for unstable updates
        /// </summary>
        string ExpertCheckunstableupdates { get; }

        /// <summary>
        /// Clipboard formats
        /// </summary>
        string ExpertClipboardformats { get; }

        /// <summary>
        /// The number for the ${NUM} in the filename pattern
        /// </summary>
        string ExpertCounter { get; }

        /// <summary>
        /// I know what I am doing!
        /// </summary>
        string ExpertEnableexpert { get; }

        /// <summary>
        /// Enable recipes
        /// </summary>
        string ExpertEnablerecipes { get; }

        /// <summary>
        /// Printer footer pattern
        /// </summary>
        string ExpertFooterpattern { get; }

        /// <summary>
        /// Minimize memory footprint, but with a performance penalty (not advised).
        /// </summary>
        string ExpertMinimizememoryfootprint { get; }

        /// <summary>
        /// Draw the windows with the graphics card (uses more memory, takes effect after a restart)
        /// </summary>
        string ExpertHardwarerendering { get; }

        /// <summary>
        /// Take screenshots with DirectX (Windows Graphics Capture, needed for HDR screens)
        /// </summary>
        string ExpertGraphicscapture { get; }

        /// <summary>
        /// Keep DirectX ready for faster screenshots (uses more memory)
        /// </summary>
        string ExpertKeepgraphicscaptureready { get; }

        /// <summary>
        /// Prepare the capture in the background after the start (uses more memory)
        /// </summary>
        string ExpertPrewarmcapture { get; }

        /// <summary>
        /// Prepare the editor in the background after the start (uses more memory)
        /// </summary>
        string ExpertPrewarmeditor { get; }

        /// <summary>
        /// Most memory (MB) kept by unused buffers, 0 = no limit (takes effect after a restart)
        /// </summary>
        string ExpertBufferpoollimit { get; }

        /// <summary>
        /// Make some optimizations for usage with remote desktop
        /// </summary>
        string ExpertOptimizeforrdp { get; }

        /// <summary>
        /// Reuse editor if possible
        /// </summary>
        string ExpertReuseeditorifpossible { get; }

        /// <summary>
        /// Suppress the save dialog when closing the editor
        /// </summary>
        string ExpertSuppresssavedialogatclose { get; }

        /// <summary>
        /// Show window thumbnails in context menu (for Vista and windows 7)
        /// </summary>
        string ExpertThumbnailpreview { get; }

        /// <summary>
        /// Filename pattern
        /// </summary>
        string Filenamepattern { get; }

        /// <summary>
        /// General
        /// </summary>
        string General { get; }

        /// <summary>
        /// Icon size
        /// </summary>
        string Iconsize { get; }

        /// <summary>
        /// JPEG quality
        /// </summary>
        string Jpegquality { get; }

        /// <summary>
        /// Language
        /// </summary>
        string Language { get; }

        /// <summary>
        /// The following placeholders will be replaced automatically in the pattern defined:
        ///     ${YYYY} year, 4 digits
        ///     ${MM} month, 2 digits
        ///     ${DD} day, 2 digits
        ///     ${hh} hour, 2 digits
        ///     ${mm} minute, 2 digits
        ///     ${ss} second, 2 digits
        ///     ${NUM} incrementing number, 6 digits by default
        ///     ${NUM:p-2,0} incrementing number, 2 digits with leading zeros (01, 02, 03)
        ///     ${RRR...} random alphanumerics, same length as 'R's
        ///     ${title} Window title
        ///     ${user} Windows user
        ///     ${domain} Windows domain
        ///     ${hostname} PC name
        ///
        ///     You can also have Greenshot create directories dynamically, simply use the backslash symbol (\) to separate folders and filename.
        ///     To reset the ${NUM} counter, change its value in Expert settings.
        ///     Example: the pattern ${YYYY}-${MM}-${DD}\${hh}-${mm}-${ss}
        ///     will generate a folder for the current day in your default storage location, e.g. 2008-06-29, the contained screenshot file's name will be based on the current
        ///     time, e.g. 11_58_32 (plus extension defined in the settings)
        /// </summary>
        string MessageFilenamepattern { get; }

        /// <summary>
        /// Network and updates
        /// </summary>
        string Network { get; }

        /// <summary>
        /// Text recognition (OCR)
        /// </summary>
        string Ocr { get; }

        /// <summary>
        /// Language of the text
        /// </summary>
        string OcrLanguage { get; }

        /// <summary>
        /// Automatic (Windows language settings)
        /// </summary>
        string OcrLanguageAutomatic { get; }

        /// <summary>
        /// not installed
        /// </summary>
        string OcrLanguageNotinstalled { get; }

        /// <summary>
        /// Output
        /// </summary>
        string Output { get; }

        /// <summary>
        /// Play camera sound
        /// </summary>
        string Playsound { get; }

        /// <summary>
        /// Plugins
        /// </summary>
        string Plugins { get; }

        /// <summary>
        /// Created by
        /// </summary>
        string PluginsCreatedby { get; }

        /// <summary>
        /// DLL Path
        /// </summary>
        string PluginsDllpath { get; }

        /// <summary>
        /// Name
        /// </summary>
        string PluginsName { get; }

        /// <summary>
        /// Version
        /// </summary>
        string PluginsVersion { get; }

        /// <summary>
        /// Preferred Output File Settings
        /// </summary>
        string Preferredfilesettings { get; }

        /// <summary>
        /// Image format
        /// </summary>
        string Primaryimageformat { get; }

        /// <summary>
        /// Printer
        /// </summary>
        string Printer { get; }

        /// <summary>
        /// Print options
        /// </summary>
        string Printoptions { get; }

        /// <summary>
        /// Quality settings
        /// </summary>
        string Qualitysettings { get; }

        /// <summary>
        /// Approved recipes
        /// </summary>
        string Recipeapprovals { get; }

        /// <summary>
        /// Recipes from files, also those written by AI tools, and what you approved for them. Changes here take effect right away, also when you cancel the settings.
        /// </summary>
        string RecipeapprovalsDescription { get; }

        /// <summary>
        /// Details
        /// </summary>
        string RecipeapprovalsDetails { get; }

        /// <summary>
        /// Review...
        /// </summary>
        string RecipeapprovalsReview { get; }

        /// <summary>
        /// Revoke
        /// </summary>
        string RecipeapprovalsRevoke { get; }

        /// <summary>
        /// Recipes
        /// </summary>
        string Recipes { get; }

        /// <summary>
        /// Change…
        /// </summary>
        string RecipesChange { get; }

        /// <summary>
        /// Changed recipes
        /// </summary>
        string RecipesChanges { get; }

        /// <summary>
        /// No automatic step changes a recipe; switch one on above.
        /// </summary>
        string RecipesChangesNone { get; }

        /// <summary>
        /// Automatic steps for your captures, like a border or a caption, and the settings your recipes offer. They are used from the next capture on.
        /// </summary>
        string RecipesDescription { get; }

        /// <summary>
        /// Reset to defaults
        /// </summary>
        string RecipesReset { get; }

        /// <summary>
        /// All
        /// </summary>
        string RecipesScopeAll { get; }

        /// <summary>
        /// All captures
        /// </summary>
        string RecipesScopeAllcaptures { get; }

        /// <summary>
        /// all destinations
        /// </summary>
        string RecipesScopeAlldestinations { get; }

        /// <summary>
        /// Captures
        /// </summary>
        string RecipesScopeCaptures { get; }

        /// <summary>
        /// This automatic step changes the captures of the checked recipes, for the checked destinations. New recipes get it too.
        /// </summary>
        string RecipesScopeDescription { get; }

        /// <summary>
        /// Destinations
        /// </summary>
        string RecipesScopeDestinations { get; }

        /// <summary>
        /// No captures
        /// </summary>
        string RecipesScopeNocaptures { get; }

        /// <summary>
        /// Check at least one destination.
        /// </summary>
        string RecipesScopeNodestination { get; }

        /// <summary>
        /// None
        /// </summary>
        string RecipesScopeNone { get; }

        /// <summary>
        /// {0} of {1} captures
        /// </summary>
        string RecipesScopeSomecaptures { get; }

        /// <summary>
        /// Where {0} is used
        /// </summary>
        string RecipesScopeTitle { get; }

        /// <summary>
        /// Used for:
        /// </summary>
        string RecipesUsedfor { get; }

        /// <summary>
        /// Reduce the amount of colors to a maximum of 256
        /// </summary>
        string Reducecolors { get; }

        /// <summary>
        /// Register Hotkeys
        /// </summary>
        string Registerhotkeys { get; }

        /// <summary>
        /// Show flashlight
        /// </summary>
        string Showflashlight { get; }

        /// <summary>
        /// Show notifications
        /// </summary>
        string Shownotify { get; }

        /// <summary>
        /// Storage location
        /// </summary>
        string Storagelocation { get; }

        /// <summary>
        /// The folder browser could not be opened.
        ///
        ///     The storage location has been set to your Documents folder. You can type a different path directly into the box if needed.
        /// </summary>
        string StoragelocationFolderError { get; }

        /// <summary>
        /// Storage Location
        /// </summary>
        string StoragelocationFolderErrorTitle { get; }

        /// <summary>
        /// Settings
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Pattern used for generating filenames when saving screenshots
        /// </summary>
        string TooltipFilenamepattern { get; }

        /// <summary>
        /// Language of greenshot's user interface
        /// </summary>
        string TooltipLanguage { get; }

        /// <summary>
        /// The language Windows recognizes text in, for capturing text and the OCR destination. More languages can be added in the Windows settings (Time and language, Language and region).
        /// </summary>
        string TooltipOcrLanguage { get; }

        /// <summary>
        /// Image format used by default
        /// </summary>
        string TooltipPrimaryimageformat { get; }

        /// <summary>
        /// Defines whether the shortcuts Prnt, Ctrl + Print, Alt + Prnt are reserved for global use by Greenshot at program startup, until the program is shut down.
        /// </summary>
        string TooltipRegisterhotkeys { get; }

        /// <summary>
        /// Location where screenshots are stored by default (leave empty for saving to your desktop)
        /// </summary>
        string TooltipStoragelocation { get; }

        /// <summary>
        /// Use default system proxy
        /// </summary>
        string Usedefaultproxy { get; }

        /// <summary>
        /// Effects
        /// </summary>
        string Visualization { get; }

        /// <summary>
        /// Milliseconds to wait before capture
        /// </summary>
        string Waittime { get; }

        /// <summary>
        /// Window capture
        /// </summary>
        string Windowscapture { get; }

        /// <summary>
        /// Show magnifier
        /// </summary>
        string Zoom { get; }
    }
}
