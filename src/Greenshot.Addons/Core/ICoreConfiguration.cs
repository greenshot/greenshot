// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General License for more details.
// 
// You should have received a copy of the GNU General License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Dapplo.CaliburnMicro.Configuration;
using Dapplo.Config.Attributes;
using Dapplo.Config.Ini;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addons.Core.Enums;
using Greenshot.Core.Configuration;

namespace Greenshot.Addons.Core
{
    /// <summary>
    ///     Core configuration.
    /// </summary>
    [IniSection("Core")]
    [Description("Greenshot core configuration")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public interface ICoreConfiguration : IIniSection, IFileConfiguration, ICaptureConfiguration, IUiConfiguration
    {
        [Description("The language in IETF format (e.g. en-US)")]
        string Language { get; set; }

        [Description("Hotkey for starting the region capture")]
        [DefaultValue("PrintScreen")]
        string RegionHotkey { get; set; }

        [Description("Hotkey for starting the window capture")]
        [DefaultValue("Alt + PrintScreen")]
        string WindowHotkey { get; set; }

        [Description("Hotkey for starting the fullscreen capture")]
        [DefaultValue("Ctrl + PrintScreen")]
        string FullscreenHotkey { get; set; }

        [Description("Hotkey for starting the last region capture")]
        [DefaultValue("Shift + PrintScreen")]
        string LastregionHotkey { get; set; }

        [Description("Hotkey for starting the IE capture")]
        [DefaultValue("Shift + Ctrl + PrintScreen")]
        string IEHotkey { get; set; }

        [Description("Is this the first time launch?")]
        [DefaultValue(true)]
        bool IsFirstLaunch { get; set; }

        [Description("Which destinations? Possible options (more might be added by plugins) are: Editor, FileDefault, FileWithDialog, Clipboard, Printer, EMail, Picker")]
        [DefaultValue("Picker")]
        IList<string> OutputDestinations { get; set; }

        [Description("Which destinations does the picker show? Empty means all, which was the default.")]
        IList<string> PickerDestinations { get; set; }

        [Description("Specify which formats we copy on the clipboard? Options are: PNG, HTML, HTMLDATAURL and DIB")]
        [DefaultValue("PNG,DIB")]
        IList<ClipboardFormats> ClipboardFormats { get; set; }

        [Description("Enable/disable capture all children, very slow but will make it possible to use this information in the editor.")]
        [DefaultValue(false)]
        bool WindowCaptureAllChildLocations { get; set; }

        [Description("Play a camera sound after taking a capture.")]
        [DefaultValue(false)]
        bool PlayCameraSound { get; set; }

        [Description("Show a notification from the systray when a capture is taken.")]
        [DefaultValue(true)]
        bool ShowTrayNotification { get; set; }

        [Description("When saving a screenshot, copy the path to the clipboard?")]
        [DefaultValue(true)]
        bool OutputFileCopyPathToClipboard { get; set; }

        [Description("SaveAs Full path?")]
        string OutputFileAsFullpath { get; set; }

        [Description("Ask for print options when printing?")]
        [DefaultValue(true)]
        [Tag(ConfigTags.LanguageKey, "settings_alwaysshowprintoptionsdialog")]
        bool OutputPrintPromptOptions { get; set; }

        [Description("Allow rotating the picture for fitting on paper?")]
        [DefaultValue(false)]
        [Tag(ConfigTags.LanguageKey, "printoptions_allowrotate")]
        bool OutputPrintAllowRotate { get; set; }

        [Description("Allow growing the picture for fitting on paper?")]
        [DefaultValue(false)]
        [Tag(ConfigTags.LanguageKey, "printoptions_allowenlarge")]
        bool OutputPrintAllowEnlarge { get; set; }

        [Description("Allow shrinking the picture for fitting on paper?")]
        [DefaultValue(true)]
        [Tag(ConfigTags.LanguageKey, "printoptions_allowshrink")]
        bool OutputPrintAllowShrink { get; set; }

        [Description("Center image when printing?")]
        [DefaultValue(true)]
        [Tag(ConfigTags.LanguageKey, "printoptions_allowcenter")]
        bool OutputPrintCenter { get; set; }

        [Description("Print image inverted (use e.g. for console captures)")]
        [DefaultValue(false)]
        [Tag(ConfigTags.LanguageKey, "printoptions_inverted")]
        bool OutputPrintInverted { get; set; }

        [Description("Force grayscale printing")]
        [DefaultValue(false)]
        [Tag(ConfigTags.LanguageKey, "printoptions_printgrayscale")]
        bool OutputPrintGrayscale { get; set; }

        [Description("Force monorchrome printing")]
        [DefaultValue(false)]
        [Tag(ConfigTags.LanguageKey, "printoptions_printmonochrome")]
        bool OutputPrintMonochrome { get; set; }

        [Description("Threshold for monochrome filter (0 - 255), lower value means less black")]
        [DefaultValue(127)]
        byte OutputPrintMonochromeThreshold { get; set; }

        [Description("Print footer on print?")]
        [DefaultValue(true)]
        [Tag(ConfigTags.LanguageKey, "printoptions_timestamp")]
        bool OutputPrintFooter { get; set; }

        [Description("Footer pattern")]
        [DefaultValue("${capturetime:d\"D\"} ${capturetime:d\"T\"} - ${title}")]
        string OutputPrintFooterPattern { get; set; }

        [Description("The wav-file to play when a capture is taken, loaded only once at the Greenshot startup")]
        [DefaultValue("default")]
        string NotificationSound { get; set; }

        [Description("Use your global proxy?")]
        [DefaultValue(true)]
        bool UseProxy { get; set; }

        [Description("Enable/disable IE capture")]
        [DefaultValue(true)]
        bool IECapture { get; set; }

        [Description("Enable/disable IE field capture, very slow but will make it possible to annotate the fields of a capture in the editor.")]
        [DefaultValue(false)]
        bool IEFieldCapture { get; set; }

        [Description("Comma separated list of Window-Classes which need to be checked for a IE instance!")]
        [DefaultValue("AfxFrameOrView70,IMWindowClass")]
        IList<string> WindowClassesToCheckForIE { get; set; }

        [Description("Sets how to compare the colors for the autocrop detection, the higher the more is 'selected'. Possible values are from 0 to 255, where everything above ~150 doesn't make much sense!")]
        [DefaultValue(10)]
        int AutoCropDifference { get; set; }

        [Description("Comma separated list of Plugins which are allowed. If something in the list, than every plugin not in the list will not be loaded!")]
        IList<string> IncludePlugins { get; set; }

        [Description("Comma separated list of Plugins which are NOT allowed.")]
        IList<string> ExcludePlugins { get; set; }

        [Description("Comma separated list of destinations which should be disabled.")]
        IList<string> ExcludeDestinations { get; set; }

        [Description("Should Greenshot check for updates?")]
        [DefaultValue(true)]
        bool CheckForUpdates { get; set; }

        [Description("How many days between every update check? (0=no checks)")]
        [DefaultValue(14)]
        int UpdateCheckInterval { get; set; }

        [Description("Last update check")]
        DateTime LastUpdateCheck { get; set; }

        [Description("Enable/disable the access to the settings, can only be changed manually in this .ini")]
        [DefaultValue(false)]
        bool DisableSettings { get; set; }

        [Description("Enable/disable the access to the quick settings, can only be changed manually in this .ini")]
        [DefaultValue(false)]
        bool DisableQuickSettings { get; set; }

        [Description("Disable the trayicon, can only be changed manually in this .ini")]
        [DefaultValue(false)]
        bool HideTrayicon { get; set; }

        [Description("Hide expert tab in the settings, can only be changed manually in this .ini")]
        [DefaultValue(false)]
        bool HideExpertSettings { get; set; }

        [Description("Enable/disable thumbnail previews")]
        [DefaultValue(true)]
        bool ThumnailPreview { get; set; }

        [Description("Make some optimizations for usage with remote desktop")]
        [DefaultValue(false)]
        bool OptimizeForRDP { get; set; }

        [Description("Disable all optimizations for usage with remote desktop")]
        [DefaultValue(false)]
        bool DisableRDPOptimizing { get; set; }

        [Description("Optimize memory footprint, but with a performance penalty!")]
        [DefaultValue(false)]
        bool MinimizeWorkingSetSize { get; set; }

        [Description("Also check for unstable version updates")]
        [DefaultValue(false)]
        bool CheckForUnstable { get; set; }

        [Description("The fixes that are active.")]
        IList<string> ActiveTitleFixes { get; set; }

        [Description("The regular expressions to match the title with.")]
        IDictionary<string, string> TitleFixMatcher { get; set; }

        [Description("The replacements for the matchers.")]
        IDictionary<string, string> TitleFixReplacer { get; set; }

        [Description("A list of experimental features, this allows us to test certain features before releasing them.")]
        IList<string> ExperimentalFeatures { get; set; }

        [Description("Enable a special DIB clipboard reader")]
        [DefaultValue(true)]
        bool EnableSpecialDIBClipboardReader { get; set; }

        [Description("Specify what action is made if the tray icon is left clicked, if a double-click action is specified this action is initiated after a delay (configurable via the windows double-click speed)")]
        [DefaultValue(ClickActions.SHOW_CONTEXT_MENU)]
        ClickActions LeftClickAction { get; set; }

        [Description("Specify what action is made if the tray icon is double clicked")]
        [DefaultValue(ClickActions.OPEN_LAST_IN_EXPLORER)]
        ClickActions DoubleClickAction { get; set; }

        [Description("Sets if the zoomer is enabled")]
        [DefaultValue(true)]
        bool ZoomerEnabled { get; set; }

        [Description("Specify the transparency for the zoomer, from 0-1 (where 1 is no transparency and 0 is complete transparent. An usefull setting would be 0.7)")]
        [DefaultValue(1)]
        float ZoomerOpacity { get; set; }

        [Description("Maximum length of submenu items in the context menu, making this longer might cause context menu issues on dual screen systems.")]
        [DefaultValue(25)]
        int MaxMenuItemLength { get; set; }

        [Description("The 'to' field for the email destination (settings for Outlook can be found under the Office section)")]
        [DefaultValue("")]
        string MailApiTo { get; set; }

        [Description("The 'CC' field for the email destination (settings for Outlook can be found under the Office section)")]
        [DefaultValue("")]
        string MailApiCC { get; set; }

        [Description("The 'BCC' field for the email destination (settings for Outlook can be found under the Office section)")]
        [DefaultValue("")]
        string MailApiBCC { get; set; }

        [Description("Version of Greenshot which created this .ini")]
        string LastSaveWithVersion { get; set; }

        [Description("When reading images from files or clipboard, use the EXIF information to correct the orientation")]
        [DefaultValue(true)]
        bool ProcessEXIFOrientation { get; set; }

        [Description("The last used region, for reuse in the capture last region")]
        NativeRect LastCapturedRegion { get; set; }

        [Description("Defines the base size of the icons (e.g. for the buttons in the editor), default value 16,16 anything bigger will cause scaling")]
        [DefaultValue("16,16")]
        NativeSize IconSize { get; set; }

        [Description("The connect timeout value for webrequets, these are seconds")]
        [DefaultValue(100)]
        int WebRequestTimeout { get; set; }

        [Description("The read/write timeout value for webrequets, these are seconds")]
        [DefaultValue(100)]
        int WebRequestReadWriteTimeout { get; set; }

        [Description("True to enable scrolling capture, this is done whenever a scrolling window is selected")]
        [DefaultValue(true)]
        bool IsScrollingCaptureEnabled { get; set; }

        [IniPropertyBehavior(Read = false, Write = false)]
        [DefaultValue(false)]
        bool IsPortable { get; set; }

        [Description("The permissions for Greenshot functionality")]
        ISet<string> Permissions { get; set; }
    }
}