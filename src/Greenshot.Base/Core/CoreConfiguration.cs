/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Description of CoreConfiguration.
    /// </summary>
    [IniSection("Core", Description = "Greenshot core configuration")]
    public class CoreConfiguration : IniSection, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [IniProperty("Language", Description = "The language in IETF format (e.g. en-US)")]
        public string Language { get; set; }
        public const string _Language = nameof(Language);

        [IniProperty("RegionHotkey", Description = "Hotkey for starting the region capture", DefaultValue = "PrintScreen")]
        public string RegionHotkey { get; set; }
        public const string _RegionHotkey = nameof(RegionHotkey);

        [IniProperty("WindowHotkey", Description = "Hotkey for starting the window capture", DefaultValue = "Alt + PrintScreen")]
        public string WindowHotkey { get; set; }
        public const string _WindowHotkey = nameof(WindowHotkey);

        [IniProperty("FullscreenHotkey", Description = "Hotkey for starting the fullscreen capture", DefaultValue = "Ctrl + PrintScreen")]
        public string FullscreenHotkey { get; set; }
        public const string _FullscreenHotkey = nameof(FullscreenHotkey);

        [IniProperty("LastregionHotkey", Description = "Hotkey for starting the last region capture", DefaultValue = "Shift + PrintScreen")]
        public string LastregionHotkey { get; set; }
        public const string _LastregionHotkey = nameof(LastregionHotkey);

        [IniProperty("IEHotkey", Description = "Hotkey for starting the IE capture", DefaultValue = "Shift + Ctrl + PrintScreen")]
        public string IEHotkey { get; set; }
        public const string _IEHotkey = nameof(IEHotkey);

        [IniProperty("ClipboardHotkey", Description = "Hotkey for opening the clipboard contents into the editor", ExcludeIfNull = true)]
        public string ClipboardHotkey { get; set; }
        public const string _ClipboardHotkey = nameof(ClipboardHotkey);

        [IniProperty("IsFirstLaunch", Description = "Is this the first time launch?", DefaultValue = "true")]
        public bool IsFirstLaunch { get; set; }
        public const string _IsFirstLaunch = nameof(IsFirstLaunch);

        [IniProperty("Destinations", Separator = ",",
            Description = "Which destinations? Possible options (more might be added by plugins) are: Editor, FileDefault, FileWithDialog, Clipboard, Printer, EMail, Picker",
            DefaultValue = "Picker")]
        public List<string> OutputDestinations { get; set; } = new List<string>();
        public const string _OutputDestinations = nameof(OutputDestinations);

        [IniProperty("ClipboardFormats", Separator = ",", Description = "Specify which formats we copy on the clipboard? Options are: PNG, HTML, HTMLDATAURL and DIB",
            DefaultValue = "PNG,DIB")]
        public List<ClipboardFormat> ClipboardFormats { get; set; } = new List<ClipboardFormat>();
        public const string _ClipboardFormats = nameof(ClipboardFormats);

        [IniProperty("CaptureMousepointer", Description = "Should the mouse be captured?", DefaultValue = "true")]
        public bool CaptureMousepointer { get; set; }
        public const string _CaptureMousepointer = nameof(CaptureMousepointer);

        [IniProperty("CaptureWindowsInteractive", Description = "Use interactive window selection to capture? (false=Capture active window)", DefaultValue = "false")]
        public bool CaptureWindowsInteractive { get; set; }
        public const string _CaptureWindowsInteractive = nameof(CaptureWindowsInteractive);

        [IniProperty("CaptureDelay", Description = "Capture delay in millseconds.", DefaultValue = "100")]
        public int CaptureDelay { get; set; }
        public const string _CaptureDelay = nameof(CaptureDelay);

        [IniProperty("ScreenCaptureMode", Description = "The capture mode used to capture a screen. (Auto, FullScreen, Fixed)", DefaultValue = "Auto")]
        public ScreenCaptureMode ScreenCaptureMode { get; set; }
        public const string _ScreenCaptureMode = nameof(ScreenCaptureMode);

        [IniProperty("ScreenToCapture", Description = "The screen number to capture when using ScreenCaptureMode Fixed.", DefaultValue = "1")]
        public int ScreenToCapture { get; set; }
        public const string _ScreenToCapture = nameof(ScreenToCapture);

        [IniProperty("WindowCaptureMode", Description = "The capture mode used to capture a Window (Screen, GDI, Aero, AeroTransparent, Auto).", DefaultValue = "Auto")]
        public WindowCaptureMode WindowCaptureMode { get; set; }
        public const string _WindowCaptureMode = nameof(WindowCaptureMode);

        [IniProperty("WindowCaptureAllChildLocations",
            Description = "Enable/disable capture all children, very slow but will make it possible to use this information in the editor.", DefaultValue = "False")]
        public bool WindowCaptureAllChildLocations { get; set; }
        public const string _WindowCaptureAllChildLocations = nameof(WindowCaptureAllChildLocations);

        [IniProperty("DWMBackgroundColor", Description = "The background color for a DWM window capture.")]
        public Color DWMBackgroundColor { get; set; }
        public const string _DWMBackgroundColor = nameof(DWMBackgroundColor);

        [IniProperty("PlayCameraSound", LanguageKey = "settings_playsound", Description = "Play a camera sound after taking a capture.", DefaultValue = "false")]
        public bool PlayCameraSound { get; set; } = false;
        public const string _PlayCameraSound = nameof(PlayCameraSound);

        [IniProperty("ShowTrayNotification", LanguageKey = "settings_shownotify", Description = "Show a notification from the systray when a capture is taken.",
            DefaultValue = "true")]
        public bool ShowTrayNotification { get; set; } = true;
        public const string _ShowTrayNotification = nameof(ShowTrayNotification);

        [IniProperty("OutputFilePath", Description = "Output file path.")]
        public string OutputFilePath { get; set; }
        public const string _OutputFilePath = nameof(OutputFilePath);

        [IniProperty("OutputFileAllowOverwrite",
            Description = "If the target file already exists True will make Greenshot always overwrite and False will display a 'Save-As' dialog.", DefaultValue = "true")]
        public bool OutputFileAllowOverwrite { get; set; }
        public const string _OutputFileAllowOverwrite = nameof(OutputFileAllowOverwrite);

        [IniProperty("OutputFileFilenamePattern", Description = "Filename pattern for screenshot.", DefaultValue = "${capturetime:d\"yyyy-MM-dd HH_mm_ss\"}-${title}")]
        public string OutputFileFilenamePattern { get; set; }
        public const string _OutputFileFilenamePattern = nameof(OutputFileFilenamePattern);

        [IniProperty("OutputFileFormat", Description = "Default file type for writing screenshots. (bmp, gif, jpg, png, tiff)", DefaultValue = "png")]
        public OutputFormat OutputFileFormat { get; set; } = OutputFormat.png;
        public const string _OutputFileFormat = nameof(OutputFileFormat);

        [IniProperty("OutputFileReduceColors", Description = "If set to true, than the colors of the output file are reduced to 256 (8-bit) colors", DefaultValue = "false")]
        public bool OutputFileReduceColors { get; set; }
        public const string _OutputFileReduceColors = nameof(OutputFileReduceColors);

        [IniProperty("OutputFileAutoReduceColors",
            Description = "If set to true the amount of colors is counted and if smaller than 256 the color reduction is automatically used.", DefaultValue = "false")]
        public bool OutputFileAutoReduceColors { get; set; }
        public const string _OutputFileAutoReduceColors = nameof(OutputFileAutoReduceColors);

        [IniProperty("OutputFileReduceColorsTo", Description = "Amount of colors to reduce to, when reducing", DefaultValue = "256")]
        public int OutputFileReduceColorsTo { get; set; }
        public const string _OutputFileReduceColorsTo = nameof(OutputFileReduceColorsTo);

        [IniProperty("OutputFileCopyPathToClipboard", Description = "When saving a screenshot, copy the path to the clipboard?", DefaultValue = "true")]
        public bool OutputFileCopyPathToClipboard { get; set; }
        public const string _OutputFileCopyPathToClipboard = nameof(OutputFileCopyPathToClipboard);

        [IniProperty("OutputFileAsFullpath", Description = "SaveAs Full path?")]
        public string OutputFileAsFullpath { get; set; }
        public const string _OutputFileAsFullpath = nameof(OutputFileAsFullpath);

        [IniProperty("OutputFileJpegQuality", Description = "JPEG file save quality in %.", DefaultValue = "80")]
        public int OutputFileJpegQuality { get; set; }
        public const string _OutputFileJpegQuality = nameof(OutputFileJpegQuality);

        [IniProperty("OutputFilePromptQuality", Description = "Ask for the quality before saving?", DefaultValue = "false")]
        public bool OutputFilePromptQuality { get; set; }
        public const string _OutputFilePromptQuality = nameof(OutputFilePromptQuality);

        [IniProperty("OutputFileIncrementingNumber", Description = "The number for the ${NUM} in the filename pattern, is increased automatically after each save.",
            DefaultValue = "1")]
        public uint OutputFileIncrementingNumber { get; set; }
        public const string _OutputFileIncrementingNumber = nameof(OutputFileIncrementingNumber);

        [IniProperty("OutputPrintPromptOptions", LanguageKey = "settings_alwaysshowprintoptionsdialog", Description = "Ask for print options when printing?",
            DefaultValue = "true")]
        public bool OutputPrintPromptOptions { get; set; }
        public const string _OutputPrintPromptOptions = nameof(OutputPrintPromptOptions);

        [IniProperty("OutputPrintAllowRotate", LanguageKey = "printoptions_allowrotate", Description = "Allow rotating the picture for fitting on paper?", DefaultValue = "false")]
        public bool OutputPrintAllowRotate { get; set; }
        public const string _OutputPrintAllowRotate = nameof(OutputPrintAllowRotate);

        [IniProperty("OutputPrintAllowEnlarge", LanguageKey = "printoptions_allowenlarge", Description = "Allow growing the picture for fitting on paper?", DefaultValue = "false")]
        public bool OutputPrintAllowEnlarge { get; set; }
        public const string _OutputPrintAllowEnlarge = nameof(OutputPrintAllowEnlarge);

        [IniProperty("OutputPrintAllowShrink", LanguageKey = "printoptions_allowshrink", Description = "Allow shrinking the picture for fitting on paper?", DefaultValue = "true")]
        public bool OutputPrintAllowShrink { get; set; }
        public const string _OutputPrintAllowShrink = nameof(OutputPrintAllowShrink);

        [IniProperty("OutputPrintCenter", LanguageKey = "printoptions_allowcenter", Description = "Center image when printing?", DefaultValue = "true")]
        public bool OutputPrintCenter { get; set; }
        public const string _OutputPrintCenter = nameof(OutputPrintCenter);

        [IniProperty("OutputPrintInverted", LanguageKey = "printoptions_inverted", Description = "Print image inverted (use e.g. for console captures)", DefaultValue = "false")]
        public bool OutputPrintInverted { get; set; }
        public const string _OutputPrintInverted = nameof(OutputPrintInverted);

        [IniProperty("OutputPrintGrayscale", LanguageKey = "printoptions_printgrayscale", Description = "Force grayscale printing", DefaultValue = "false")]
        public bool OutputPrintGrayscale { get; set; }
        public const string _OutputPrintGrayscale = nameof(OutputPrintGrayscale);

        [IniProperty("OutputPrintMonochrome", LanguageKey = "printoptions_printmonochrome", Description = "Force monorchrome printing", DefaultValue = "false")]
        public bool OutputPrintMonochrome { get; set; }
        public const string _OutputPrintMonochrome = nameof(OutputPrintMonochrome);

        [IniProperty("OutputPrintMonochromeThreshold", Description = "Threshold for monochrome filter (0 - 255), lower value means less black", DefaultValue = "127")]
        public byte OutputPrintMonochromeThreshold { get; set; }
        public const string _OutputPrintMonochromeThreshold = nameof(OutputPrintMonochromeThreshold);

        [IniProperty("OutputPrintFooter", LanguageKey = "printoptions_timestamp", Description = "Print footer on print?", DefaultValue = "true")]
        public bool OutputPrintFooter { get; set; }
        public const string _OutputPrintFooter = nameof(OutputPrintFooter);

        [IniProperty("OutputPrintFooterPattern", Description = "Footer pattern", DefaultValue = "${capturetime:d\"D\"} ${capturetime:d\"T\"} - ${title}")]
        public string OutputPrintFooterPattern { get; set; }
        public const string _OutputPrintFooterPattern = nameof(OutputPrintFooterPattern);

        [IniProperty("NotificationSound", Description = "The wav-file to play when a capture is taken, loaded only once at the Greenshot startup", DefaultValue = "default")]
        public string NotificationSound { get; set; }
        public const string _NotificationSound = nameof(NotificationSound);

        [IniProperty("UseProxy", Description = "Use your global proxy?", DefaultValue = "True")]
        public bool UseProxy { get; set; }
        public const string _UseProxy = nameof(UseProxy);

        [IniProperty("IECapture", Description = "Enable/disable IE capture", DefaultValue = "True")]
        public bool IECapture { get; set; }
        public const string _IECapture = nameof(IECapture);

        [IniProperty("IEFieldCapture", Description = "Enable/disable IE field capture, very slow but will make it possible to annotate the fields of a capture in the editor.",
            DefaultValue = "False")]
        public bool IEFieldCapture { get; set; }
        public const string _IEFieldCapture = nameof(IEFieldCapture);

        [IniProperty("WindowClassesToCheckForIE", Description = "Comma separated list of Window-Classes which need to be checked for a IE instance!",
            DefaultValue = "AfxFrameOrView70,IMWindowClass")]
        public List<string> WindowClassesToCheckForIE { get; set; }
        public const string _WindowClassesToCheckForIE = nameof(WindowClassesToCheckForIE);

        [IniProperty("AutoCropDifference",
            Description =
                "Sets how to compare the colors for the autocrop detection, the higher the more is 'selected'. Possible values are from 0 to 255, where everything above ~150 doesn't make much sense!",
            DefaultValue = "10")]
        public int AutoCropDifference { get; set; }
        public const string _AutoCropDifference = nameof(AutoCropDifference);

        [IniProperty("IncludePlugins",
            Description = "Comma separated list of Plugins which are allowed. If something in the list, than every plugin not in the list will not be loaded!")]
        public List<string> IncludePlugins { get; set; }
        public const string _IncludePlugins = nameof(IncludePlugins);

        [IniProperty("ExcludePlugins", Description = "Comma separated list of Plugins which are NOT allowed.")]
        public List<string> ExcludePlugins { get; set; }
        public const string _ExcludePlugins = nameof(ExcludePlugins);

        [IniProperty("ExcludeDestinations", Description = "Comma separated list of destinations which should be disabled.")]
        public List<string> ExcludeDestinations { get; set; }
        public const string _ExcludeDestinations = nameof(ExcludeDestinations);

        [IniProperty("UpdateCheckInterval", Description = "How many days between every update check? (0=no checks)", DefaultValue = "14")]
        public int UpdateCheckInterval { get; set; }
        public const string _UpdateCheckInterval = nameof(UpdateCheckInterval);

        [IniProperty("LastUpdateCheck", Description = "Last update check")]
        public DateTime LastUpdateCheck { get; set; }
        public const string _LastUpdateCheck = nameof(LastUpdateCheck);

        [IniProperty("DisableSettings", Description = "Enable/disable the access to the settings, can only be changed manually in this .ini", DefaultValue = "False")]
        public bool DisableSettings { get; set; }
        public const string _DisableSettings = nameof(DisableSettings);

        [IniProperty("DisableQuickSettings", Description = "Enable/disable the access to the quick settings, can only be changed manually in this .ini", DefaultValue = "False")]
        public bool DisableQuickSettings { get; set; }
        public const string _DisableQuickSettings = nameof(DisableQuickSettings);

        [IniProperty("DisableTrayicon", Description = "Disable the trayicon, can only be changed manually in this .ini", DefaultValue = "False")]
        public bool HideTrayicon { get; set; }
        public const string _HideTrayicon = nameof(HideTrayicon);

        [IniProperty("HideExpertSettings", Description = "Hide expert tab in the settings, can only be changed manually in this .ini", DefaultValue = "False")]
        public bool HideExpertSettings { get; set; }
        public const string _HideExpertSettings = nameof(HideExpertSettings);

        [IniProperty("ThumnailPreview", Description = "Enable/disable thumbnail previews", DefaultValue = "True")]
        public bool ThumnailPreview { get; set; }
        public const string _ThumnailPreview = nameof(ThumnailPreview);

        [IniProperty("NoGDICaptureForProduct", Description = "List of productnames for which GDI capturing is skipped (using fallback).", DefaultValue = "IntelliJ IDEA")]
        public List<string> NoGDICaptureForProduct { get; set; }
        public const string _NoGDICaptureForProduct = nameof(NoGDICaptureForProduct);

        [IniProperty("NoDWMCaptureForProduct", Description = "List of productnames for which DWM capturing is skipped (using fallback).", DefaultValue = "Citrix ICA Client")]
        public List<string> NoDWMCaptureForProduct { get; set; }
        public const string _NoDWMCaptureForProduct = nameof(NoDWMCaptureForProduct);

        [IniProperty("OptimizeForRDP", Description = "Make some optimizations for usage with remote desktop", DefaultValue = "False")]
        public bool OptimizeForRDP { get; set; }
        public const string _OptimizeForRDP = nameof(OptimizeForRDP);

        [IniProperty("DisableRDPOptimizing", Description = "Disable all optimizations for usage with remote desktop", DefaultValue = "False")]
        public bool DisableRDPOptimizing { get; set; }
        public const string _DisableRDPOptimizing = nameof(DisableRDPOptimizing);

        [IniProperty("MinimizeWorkingSetSize", Description = "Optimize memory footprint, but with a performance penalty!", DefaultValue = "False")]
        public bool MinimizeWorkingSetSize { get; set; }
        public const string _MinimizeWorkingSetSize = nameof(MinimizeWorkingSetSize);

        [IniProperty("WindowCaptureRemoveCorners", Description = "Remove the corners from a window capture", DefaultValue = "True")]
        public bool WindowCaptureRemoveCorners { get; set; }
        public const string _WindowCaptureRemoveCorners = nameof(WindowCaptureRemoveCorners);

        [IniProperty("CheckForUnstable", Description = "Also check for unstable version updates", DefaultValue = "False")]
        public bool CheckForUnstable { get; set; }
        public const string _CheckForUnstable = nameof(CheckForUnstable);

        [IniProperty("ActiveTitleFixes", Description = "The fixes that are active.")]
        public List<string> ActiveTitleFixes { get; set; }
        public const string _ActiveTitleFixes = nameof(ActiveTitleFixes);

        [IniProperty("TitleFixMatcher", Description = "The regular expressions to match the title with.")]
        public Dictionary<string, string> TitleFixMatcher { get; set; }
        public const string _TitleFixMatcher = nameof(TitleFixMatcher);

        [IniProperty("TitleFixReplacer", Description = "The replacements for the matchers.")]
        public Dictionary<string, string> TitleFixReplacer { get; set; }
        public const string _TitleFixReplacer = nameof(TitleFixReplacer);

        [IniProperty("ExperimentalFeatures", Description = "A list of experimental features, this allows us to test certain features before releasing them.", ExcludeIfNull = true)]
        public List<string> ExperimentalFeatures { get; set; }
        public const string _ExperimentalFeatures = nameof(ExperimentalFeatures);

        [IniProperty("EnableSpecialDIBClipboardReader", Description = "Enable a special DIB clipboard reader", DefaultValue = "True")]
        public bool EnableSpecialDIBClipboardReader { get; set; }
        public const string _EnableSpecialDIBClipboardReader = nameof(EnableSpecialDIBClipboardReader);

        [IniProperty("WindowCornerCutShape", Description = "The cutshape which is used to remove the window corners, is mirrorred for all corners", DefaultValue = "5,3,2,1,1")]
        public List<int> WindowCornerCutShape { get; set; }
        public const string _WindowCornerCutShape = nameof(WindowCornerCutShape);

        [IniProperty("LeftClickAction",
            Description =
                "Specify what action is made if the tray icon is left clicked, if a double-click action is specified this action is initiated after a delay (configurable via the windows double-click speed)",
            DefaultValue = "SHOW_CONTEXT_MENU")]
        public ClickActions LeftClickAction { get; set; }
        public const string _LeftClickAction = nameof(LeftClickAction);

        [IniProperty("DoubleClickAction", Description = "Specify what action is made if the tray icon is double clicked", DefaultValue = "OPEN_LAST_IN_EXPLORER")]
        public ClickActions DoubleClickAction { get; set; }
        public const string _DoubleClickAction = nameof(DoubleClickAction);

        [IniProperty("ZoomerEnabled", Description = "Sets if the zoomer is enabled", DefaultValue = "True")]
        public bool ZoomerEnabled { get; set; }
        public const string _ZoomerEnabled = nameof(ZoomerEnabled);

        [IniProperty("ZoomerOpacity",
            Description = "Specify the transparency for the zoomer, from 0-1 (where 1 is no transparency and 0 is complete transparent. An usefull setting would be 0.7)",
            DefaultValue = "1")]
        public float ZoomerOpacity { get; set; }
        public const string _ZoomerOpacity = nameof(ZoomerOpacity);

        [IniProperty("MaxMenuItemLength",
            Description = "Maximum length of submenu items in the context menu, making this longer might cause context menu issues on dual screen systems.", DefaultValue = "25")]
        public int MaxMenuItemLength { get; set; }
        public const string _MaxMenuItemLength = nameof(MaxMenuItemLength);

        [IniProperty("MailApiTo", Description = "The 'to' field for the email destination (settings for Outlook can be found under the Office section)", DefaultValue = "")]
        public string MailApiTo { get; set; }
        public const string _MailApiTo = nameof(MailApiTo);

        [IniProperty("MailApiCC", Description = "The 'CC' field for the email destination (settings for Outlook can be found under the Office section)", DefaultValue = "")]
        public string MailApiCC { get; set; }
        public const string _MailApiCC = nameof(MailApiCC);

        [IniProperty("MailApiBCC", Description = "The 'BCC' field for the email destination (settings for Outlook can be found under the Office section)", DefaultValue = "")]
        public string MailApiBCC { get; set; }
        public const string _MailApiBCC = nameof(MailApiBCC);

        [IniProperty("OptimizePNGCommand",
            Description =
                "Optional command to execute on a temporary PNG file, the command should overwrite the file and Greenshot will read it back. Note: this command is also executed when uploading PNG's!",
            DefaultValue = "")]
        public string OptimizePNGCommand { get; set; }
        public const string _OptimizePNGCommand = nameof(OptimizePNGCommand);

        [IniProperty("OptimizePNGCommandArguments",
            Description =
                "Arguments for the optional command to execute on a PNG, {0} is replaced by the temp-filename from Greenshot. Note: Temp-file is deleted afterwards by Greenshot.",
            DefaultValue = "\"{0}\"")]
        public string OptimizePNGCommandArguments { get; set; }
        public const string _OptimizePNGCommandArguments = nameof(OptimizePNGCommandArguments);

        [IniProperty("LastSaveWithVersion", Description = "Version of Greenshot which created this .ini")]
        public string LastSaveWithVersion { get; set; }
        public const string _LastSaveWithVersion = nameof(LastSaveWithVersion);

        [IniProperty("ProcessEXIFOrientation", Description = "When reading images from files or clipboard, use the EXIF information to correct the orientation",
            DefaultValue = "True")]
        public bool ProcessEXIFOrientation { get; set; }
        public const string _ProcessEXIFOrientation = nameof(ProcessEXIFOrientation);

        [IniProperty("LastCapturedRegion", Description = "The last used region, for reuse in the capture last region")]
        public NativeRect LastCapturedRegion { get; set; }
        public const string _LastCapturedRegion = nameof(LastCapturedRegion);

        [IniProperty("Win10BorderCrop", Description = "The capture is cropped with these settings, e.g. when you don't want to color around it -1,-1"), DefaultValue("0,0")]
        public NativeSize Win10BorderCrop { get; set; }
        public const string _Win10BorderCrop = nameof(Win10BorderCrop);

        private NativeSize _iconSize;

        [IniProperty("BaseIconSize",
            Description = "Defines the base size of the icons (e.g. for the buttons in the editor), default value 16,16 and it's scaled to the current DPI",
            DefaultValue = "16,16")]
        public NativeSize IconSize
        {
            get { return _iconSize; }
            set
            {
                Size newSize = value;
                if (newSize != Size.Empty)
                {
                    if (newSize.Width < 16)
                    {
                        newSize.Width = 16;
                    }
                    else if (newSize.Width > 256)
                    {
                        newSize.Width = 256;
                    }

                    newSize.Width = (newSize.Width / 16) * 16;
                    if (newSize.Height < 16)
                    {
                        newSize.Height = 16;
                    }
                    else if (IconSize.Height > 256)
                    {
                        newSize.Height = 256;
                    }

                    newSize.Height = (newSize.Height / 16) * 16;
                }

                if (_iconSize != newSize)
                {
                    _iconSize = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IconSize"));
                }
            }
        }
        public const string _IconSize = nameof(IconSize);

        [IniProperty("WebRequestTimeout", Description = "The connect timeout value for web requests, these are seconds", DefaultValue = "100")]
        public int WebRequestTimeout { get; set; }
        public const string _WebRequestTimeout = nameof(WebRequestTimeout);

        [IniProperty("WebRequestReadWriteTimeout", Description = "The read/write timeout value for web requests, these are seconds", DefaultValue = "100")]
        public int WebRequestReadWriteTimeout { get; set; }
        public const string _WebRequestReadWriteTimeout = nameof(WebRequestReadWriteTimeout);

        public bool UseLargeIcons => IconSize.Width >= 32 || IconSize.Height >= 32;
        public const string _UseLargeIcons = nameof(UseLargeIcons);

        /// <summary>
        /// A helper method which returns true if the supplied experimental feature is enabled
        /// </summary>
        /// <param name="experimentalFeature"></param>
        /// <returns></returns>
        public bool IsExperimentalFeatureEnabled(string experimentalFeature)
        {
            return ExperimentalFeatures != null && ExperimentalFeatures.Contains(experimentalFeature);
        }

        private string CreateOutputFilePath()
        {
            if (IniConfig.IsPortable)
            {
                string pafOutputFilePath = Path.Combine(Application.StartupPath, @"..\..\Documents\Pictures\Greenshots");
                if (!Directory.Exists(pafOutputFilePath))
                {
                    try
                    {
                        Directory.CreateDirectory(pafOutputFilePath);
                        return pafOutputFilePath;
                    }
                    catch (Exception ex)
                    {
                        // Problem creating directory, fallback to Desktop
                        LOG.Warn(ex);
                    }
                }
                else
                {
                    return pafOutputFilePath;
                }
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }
        
        /// <summary>
        /// Supply values we can't put as defaults
        /// </summary>
        /// <param name="property">The property to return a default for</param>
        /// <returns>object with the default value for the supplied property</returns>
        public override object GetDefault(string property) =>
            property switch
            {
                nameof(ExcludePlugins) => new List<string>(),
                nameof(IncludePlugins) => new List<string>(),
                nameof(OutputFileAsFullpath) => IniConfig.IsPortable ? Path.Combine(Application.StartupPath, @"..\..\Documents\Pictures\Greenshots\dummy.png") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dummy.png"),
                nameof(OutputFilePath) => CreateOutputFilePath(),
                nameof(DWMBackgroundColor) => Color.Transparent,
                nameof(ActiveTitleFixes) => new List<string> {
                    "Firefox",
                    "IE",
                    "Chrome"
                },
                nameof(TitleFixMatcher) => new Dictionary<string, string> {
                    { "Firefox", " - Mozilla Firefox.*" },
                    { "IE", " - (Microsoft|Windows) Internet Explorer.*" },
                    { "Chrome", " - Google Chrome.*" }
                },
                nameof(TitleFixReplacer) => new Dictionary<string, string> {
                    { "Firefox", string.Empty },
                    { "IE", string.Empty },
                    { "Chrome", string.Empty }
                },
                _ => null
            };
        
        /// <summary>
        /// This method will be called before converting the property, making to possible to correct a certain value
        /// Can be used when migration is needed
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="propertyValue">The string value of the property</param>
        /// <returns>string with the propertyValue, modified or not...</returns>
        public override string PreCheckValue(string propertyName, string propertyValue)
        {
            // Changed the separator, now we need to correct this
            if ("Destinations".Equals(propertyName))
            {
                if (propertyValue != null)
                {
                    return propertyValue.Replace('|', ',');
                }
            }

            if ("OutputFilePath".Equals(propertyName))
            {
                if (string.IsNullOrEmpty(propertyValue))
                {
                    return null;
                }
            }

            return base.PreCheckValue(propertyName, propertyValue);
        }

        /// <summary>
        /// This method will be called before writing the configuration
        /// </summary>
        public override void BeforeSave()
        {
            try
            {
                // Store version, this can be used later to fix settings after an update
                LastSaveWithVersion = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// This method will be called after reading the configuration, so eventually some corrections can be made
        /// </summary>
        public override void AfterLoad()
        {
            // Comment with releases
            // CheckForUnstable = true;

            if (string.IsNullOrEmpty(LastSaveWithVersion))
            {
                try
                {
                    // Store version, this can be used later to fix settings after an update
                    LastSaveWithVersion = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
                }
                catch
                {
                    // ignored
                }

                // Disable the AutoReduceColors as it causes issues with Mozzila applications and some others
                OutputFileAutoReduceColors = false;
            }

            bool isUpgradeFrom12 = LastSaveWithVersion?.StartsWith("1.2") ?? false;
            // Fix for excessive feed checking
            if (UpdateCheckInterval != 0 && UpdateCheckInterval <= 7 && isUpgradeFrom12)
            {
                UpdateCheckInterval = 14;
            }

            if (UpdateCheckInterval > 365)
            {
                UpdateCheckInterval = 365;
            }

            // Enable OneNote if upgrading from 1.1
            if (ExcludeDestinations != null && ExcludeDestinations.Contains("OneNote"))
            {
                if (LastSaveWithVersion != null && LastSaveWithVersion.StartsWith("1.1"))
                {
                    ExcludeDestinations.Remove("OneNote");
                }
            }

            if (OutputDestinations == null)
            {
                OutputDestinations = new List<string>();
            }

            // Make sure there is an output!
            if (OutputDestinations.Count == 0)
            {
                OutputDestinations.Add("Editor");
            }

            // Prevent both settings at once, bug #3435056
            if (OutputDestinations.Contains("Clipboard") && OutputFileCopyPathToClipboard)
            {
                OutputFileCopyPathToClipboard = false;
            }

            // Make sure we have clipboard formats, otherwise a paste doesn't make sense!
            if (ClipboardFormats == null || ClipboardFormats.Count == 0)
            {
                ClipboardFormats = new List<ClipboardFormat>
                {
                    ClipboardFormat.PNG,
                    ClipboardFormat.HTML,
                    ClipboardFormat.DIB
                };
            }

            // Make sure the lists are lowercase, to speedup the check
            if (NoGDICaptureForProduct != null)
            {
                // Fix error in configuration
                if (NoGDICaptureForProduct.Count >= 2)
                {
                    if ("intellij".Equals(NoGDICaptureForProduct[0]) && "idea".Equals(NoGDICaptureForProduct[1]))
                    {
                        NoGDICaptureForProduct.RemoveRange(0, 2);
                        NoGDICaptureForProduct.Add("Intellij Idea");
                        IsDirty = true;
                    }
                }

                for (int i = 0; i < NoGDICaptureForProduct.Count; i++)
                {
                    NoGDICaptureForProduct[i] = NoGDICaptureForProduct[i].ToLower();
                }
            }

            if (NoDWMCaptureForProduct != null)
            {
                // Fix error in configuration
                if (NoDWMCaptureForProduct.Count >= 3)
                {
                    if ("citrix".Equals(NoDWMCaptureForProduct[0]) && "ica".Equals(NoDWMCaptureForProduct[1]) && "client".Equals(NoDWMCaptureForProduct[2]))
                    {
                        NoDWMCaptureForProduct.RemoveRange(0, 3);
                        NoDWMCaptureForProduct.Add("Citrix ICA Client");
                        IsDirty = true;
                    }
                }

                for (int i = 0; i < NoDWMCaptureForProduct.Count; i++)
                {
                    NoDWMCaptureForProduct[i] = NoDWMCaptureForProduct[i].ToLower();
                }
            }

            if (AutoCropDifference < 0)
            {
                AutoCropDifference = 0;
            }

            if (AutoCropDifference > 255)
            {
                AutoCropDifference = 255;
            }

            if (OutputFileReduceColorsTo < 2)
            {
                OutputFileReduceColorsTo = 2;
            }

            if (OutputFileReduceColorsTo > 256)
            {
                OutputFileReduceColorsTo = 256;
            }

            if (WebRequestTimeout <= 10)
            {
                WebRequestTimeout = 100;
            }

            if (WebRequestReadWriteTimeout < 1)
            {
                WebRequestReadWriteTimeout = 100;
            }
        }

        /// <summary>
        /// Validate the OutputFilePath, and if this is not correct it will be set to the default
        /// Added for BUG-1992, reset the OutputFilePath / OutputFileAsFullpath if they don't exist (e.g. the configuration is used on a different PC)
        /// </summary>
        public void ValidateAndCorrectOutputFilePath()
        {
            if (!Directory.Exists(OutputFilePath))
            {
                OutputFilePath = GetDefault(nameof(OutputFilePath)) as string;
            }
        }

        /// <summary>
        /// Validate the OutputFileAsFullpath, and if this is not correct it will be set to the default
        /// Added for BUG-1992, reset the OutputFilePath / OutputFileAsFullpath if they don't exist (e.g. the configuration is used on a different PC)
        /// </summary>
        public void ValidateAndCorrectOutputFileAsFullpath()
        {
            var outputFilePath = Path.GetDirectoryName(OutputFileAsFullpath);
            if (outputFilePath == null || (!File.Exists(OutputFileAsFullpath) && !Directory.Exists(outputFilePath)))
            {
                OutputFileAsFullpath = GetDefault(nameof(OutputFileAsFullpath)) as string;
            }
        }

  
    }
}