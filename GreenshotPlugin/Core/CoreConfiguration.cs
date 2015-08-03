/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Config.Extension;
using Dapplo.Config.Ini;
using Greenshot.Plugin;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// Supporting clipboard formats
	/// </summary>
	public enum ClipboardFormat
	{
		PNG, DIB, HTML, HTMLDATAURL, BITMAP, DIBV5
	}

	/// <summary>
	/// Supporting output formats
	/// </summary>
	public enum OutputFormat
	{
		bmp, gif, jpg, png, tiff, greenshot
	}

	/// <summary>
	/// All available window capture modes
	/// </summary>
	public enum WindowCaptureMode
	{
		Screen, GDI, Aero, AeroTransparent, Auto
	}

	public enum BuildStates
	{
		UNSTABLE,
		RELEASE_CANDIDATE,
		RELEASE
	}

	/// <summary>
	/// Specify what click actions there are, and Greenshot can respond to.
	/// Used in the double/left/right-click actions
	/// </summary>
	public enum ClickActions
	{
		DO_NOTHING,
		OPEN_LAST_IN_EXPLORER,
		OPEN_LAST_IN_EDITOR,
		OPEN_SETTINGS,
		SHOW_CONTEXT_MENU,
		CAPTURE_REGION,
		CAPTURE_SCREEN,
		CAPTURE_WINDOW,
		CAPTURE_LAST_REGION
	}

	/// <summary>
	/// Used to tag certain configuration files with a value.
	/// </summary>
	public enum ConfigTags {
		// This specifies the language key for the translation of a setting
		LanguageKey
	}

	/// <summary>
	/// Description of CoreConfiguration.
	/// </summary>
	[IniSection("Core"), Description("Greenshot core configuration")]
	public interface CoreConfiguration : IIniSection<CoreConfiguration>, INotifyPropertyChanged, ITagging<CoreConfiguration>
	{
		[Description("The language in IETF format (e.g. en-US)")]
		string Language
		{
			get;
			set;
		}

		[Description("Hotkey for starting the region capture"), DefaultValue("PrintScreen")]
		string RegionHotkey
		{
			get;
			set;
		}

		[Description("Hotkey for starting the window capture"), DefaultValue("Alt + PrintScreen")]
		string WindowHotkey
		{
			get;
			set;
		}

		[Description("Hotkey for starting the fullscreen capture"), DefaultValue("Ctrl + PrintScreen")]
		string FullscreenHotkey
		{
			get;
			set;
		}

		[Description("Hotkey for starting the last region capture"), DefaultValue("Shift + PrintScreen")]
		string LastregionHotkey
		{
			get;
			set;
		}

		[Description("Hotkey for starting the IE capture"), DefaultValue("Shift + Ctrl + PrintScreen")]
		string IEHotkey
		{
			get;
			set;
		}

		[Description("Is this the first time launch?"), DefaultValue("true")]
		bool IsFirstLaunch
		{
			get;
			set;
		}

		[Description("Which destinations? Possible options (more might be added by plugins) are: Editor, FileDefault, FileWithDialog, Clipboard, Printer, EMail, Picker"), DefaultValue("Picker")]
		IList<string> OutputDestinations
		{
			get;
			set;
		}

		[Description("Specify which formats we copy on the clipboard? Options are: PNG, HTML, HTMLDATAURL and DIB"), DefaultValue("PNG,DIB")]
		IList<ClipboardFormat> ClipboardFormats
		{
			get;
			set;
		}

		[Description("Should the mouse be captured?"), DefaultValue("true")]
		bool CaptureMousepointer
		{
			get;
			set;
		}

		[Description("Use interactive window selection to capture? (false=Capture active window)"), DefaultValue("false")]
		bool CaptureWindowsInteractive
		{
			get;
			set;
		}

		[Description("Capture delay in millseconds."), DefaultValue("100")]
		int CaptureDelay
		{
			get;
			set;
		}

		[Description("The capture mode used to capture a screen. (Auto, FullScreen, Fixed)"), DefaultValue("Auto")]
		ScreenCaptureMode ScreenCaptureMode
		{
			get;
			set;
		}

		[Description("The screen number to capture when using ScreenCaptureMode Fixed."), DefaultValue("1")]
		int ScreenToCapture
		{
			get;
			set;
		}

		[Description("The capture mode used to capture a Window (Screen, GDI, Aero, AeroTransparent, Auto)."), DefaultValue("Auto")]
		WindowCaptureMode WindowCaptureMode
		{
			get;
			set;
		}

		[Description("Enable/disable capture all children, very slow but will make it possible to use this information in the editor."), DefaultValue("False")]
		bool WindowCaptureAllChildLocations
		{
			get;
			set;
		}

		[Description("The background color for a DWM window capture.")]
		Color DWMBackgroundColor
		{
			get;
			set;
		}

		[Description("Play a camera sound after taking a capture."), DefaultValue("false"), Tag(ConfigTags.LanguageKey, "settings_playsound")]
		bool PlayCameraSound
		{
			get;
			set;
		}

		[Description("Show a notification from the systray when a capture is taken."), DefaultValue("true"), Tag(ConfigTags.LanguageKey, "settings_shownotify")]
		bool ShowTrayNotification
		{
			get;
			set;
		}

		[Description("Output file path.")]
		string OutputFilePath
		{
			get;
			set;
		}

		[Description("If the target file already exists True will make Greenshot always overwrite and False will display a 'Save-As' dialog."), DefaultValue("true")]
		bool OutputFileAllowOverwrite
		{
			get;
			set;
		}

		[Description("Filename pattern for screenshot."), DefaultValue("${capturetime:d\"yyyy-MM-dd HH_mm_ss\"}-${title}")]
		string OutputFileFilenamePattern
		{
			get;
			set;
		}

		[Description("Default file type for writing screenshots. (bmp, gif, jpg, png, tiff)"), DefaultValue("png")]
		OutputFormat OutputFileFormat
		{
			get;
			set;
		}

		[Description("If set to true, than the colors of the output file are reduced to 256 (8-bit) colors"), DefaultValue("false")]
		bool OutputFileReduceColors
		{
			get;
			set;
		}

		[Description("If set to true the amount of colors is counted and if smaller than 256 the color reduction is automatically used."), DefaultValue("false")]
		bool OutputFileAutoReduceColors
		{
			get;
			set;
		}

		[Description("Amount of colors to reduce to, when reducing"), DefaultValue("256")]
		int OutputFileReduceColorsTo
		{
			get;
			set;
		}

		[Description("When saving a screenshot, copy the path to the clipboard?"), DefaultValue("true")]
		bool OutputFileCopyPathToClipboard
		{
			get;
			set;
		}

		[Description("SaveAs Full path?")]
		string OutputFileAsFullpath
		{
			get;
			set;
		}

		[Description("JPEG file save quality in %."), DefaultValue("80")]
		int OutputFileJpegQuality
		{
			get;
			set;
		}

		[Description("Ask for the quality before saving?"), DefaultValue("false")]
		bool OutputFilePromptQuality
		{
			get;
			set;
		}

		[Description("The number for the ${NUM} in the filename pattern, is increased automatically after each save."), DefaultValue("1")]
		uint OutputFileIncrementingNumber
		{
			get;
			set;
		}

		[Description("Ask for print options when printing?"), DefaultValue("true"), Tag(ConfigTags.LanguageKey, "settings_alwaysshowprintoptionsdialog")]
		bool OutputPrintPromptOptions
		{
			get;
			set;
		}

		[Description("Allow rotating the picture for fitting on paper?"), DefaultValue("false"), Tag(ConfigTags.LanguageKey, "printoptions_allowrotate")]
		bool OutputPrintAllowRotate
		{
			get;
			set;
		}

		[Description("Allow growing the picture for fitting on paper?"), DefaultValue("false"), Tag(ConfigTags.LanguageKey, "printoptions_allowenlarge")]
		bool OutputPrintAllowEnlarge
		{
			get;
			set;
		}

		[Description("Allow shrinking the picture for fitting on paper?"), DefaultValue("true"), Tag(ConfigTags.LanguageKey, "printoptions_allowshrink")]
		bool OutputPrintAllowShrink
		{
			get;
			set;
		}

		[Description("Center image when printing?"), DefaultValue("true"), Tag(ConfigTags.LanguageKey, "printoptions_allowcenter")]
		bool OutputPrintCenter
		{
			get;
			set;
		}

		[Description("Print image inverted (use e.g. for console captures)"), DefaultValue("false"), Tag(ConfigTags.LanguageKey, "printoptions_inverted")]
		bool OutputPrintInverted
		{
			get;
			set;
		}

		[Description("Force grayscale printing"), DefaultValue("false"), Tag(ConfigTags.LanguageKey, "printoptions_printgrayscale")]
		bool OutputPrintGrayscale
		{
			get;
			set;
		}

		[Description("Force monorchrome printing"), DefaultValue("false"), Tag(ConfigTags.LanguageKey, "printoptions_printmonochrome")]
		bool OutputPrintMonochrome
		{
			get;
			set;
		}

		[Description("Threshold for monochrome filter (0 - 255), lower value means less black"), DefaultValue("127")]
		byte OutputPrintMonochromeThreshold
		{
			get;
			set;
		}

		[Description("Print footer on print?"), DefaultValue("true"), Tag(ConfigTags.LanguageKey, "printoptions_timestamp")]
		bool OutputPrintFooter
		{
			get;
			set;
		}

		[Description("Footer pattern"), DefaultValue("${capturetime:d\"D\"} ${capturetime:d\"T\"} - ${title}")]
		string OutputPrintFooterPattern
		{
			get;
			set;
		}

		[Description("The wav-file to play when a capture is taken, loaded only once at the Greenshot startup"), DefaultValue("default")]
		string NotificationSound
		{
			get;
			set;
		}

		[Description("Use your global proxy?"), DefaultValue("True")]
		bool UseProxy
		{
			get;
			set;
		}

		[Description("Enable/disable IE capture"), DefaultValue("True")]
		bool IECapture
		{
			get;
			set;
		}

		[Description("Enable/disable IE field capture, very slow but will make it possible to annotate the fields of a capture in the editor."), DefaultValue("False")]
		bool IEFieldCapture
		{
			get;
			set;
		}

		[Description("The capture mode used to capture IE (Screen, GDI)."), DefaultValue("Screen")]
		WindowCaptureMode IECaptureMode
		{
			get;
			set;
		}

		[Description("Comma separated list of Window-Classes which need to be checked for a IE instance!"), DefaultValue("AfxFrameOrView70,IMWindowClass")]
		IList<string> WindowClassesToCheckForIE
		{
			get;
			set;
		}

		[Description("Sets how to compare the colors for the autocrop detection, the higher the more is 'selected'. Possible values are from 0 to 255, where everything above ~150 doesn't make much sense!"), DefaultValue("10")]
		int AutoCropDifference
		{
			get;
			set;
		}


		[Description("Comma separated list of Plugins which are allowed. If something in the list, than every plugin not in the list will not be loaded!")]
		IList<string> IncludePlugins
		{
			get;
			set;
		}

		[Description("Comma separated list of Plugins which are NOT allowed.")]
		IList<string> ExcludePlugins
		{
			get;
			set;
		}

		[Description("Comma separated list of destinations which should be disabled.")]
		IList<string> ExcludeDestinations
		{
			get;
			set;
		}

		[Description("How many days between every update check? (0=no checks)"), DefaultValue("7")]
		int UpdateCheckInterval
		{
			get;
			set;
		}

		[Description("Last update check")]
		DateTimeOffset LastUpdateCheck
		{
			get;
			set;
		}

		[Description("Enable/disable the access to the settings, can only be changed manually in this .ini"), DefaultValue("False")]
		bool DisableSettings
		{
			get;
			set;
		}

		[Description("Enable/disable the access to the quick settings, can only be changed manually in this .ini"), DefaultValue("False")]
		bool DisableQuickSettings
		{
			get;
			set;
		}

		[Description("Disable the trayicon, can only be changed manually in this .ini"), DefaultValue("False")]
		bool HideTrayicon
		{
			get;
			set;
		}

		[Description("Hide expert tab in the settings, can only be changed manually in this .ini"), DefaultValue("False")]
		bool HideExpertSettings
		{
			get;
			set;
		}

		[Description("Enable/disable thumbnail previews"), DefaultValue("True")]
		bool ThumnailPreview
		{
			get;
			set;
		}

		[Description("List of productnames for which GDI capturing is skipped (using fallback)."), DefaultValue("IntelliJ IDEA")]
		IList<string> NoGDICaptureForProduct
		{
			get;
			set;
		}

		[Description("List of productnames for which DWM capturing is skipped (using fallback)."), DefaultValue("Citrix ICA Client")]
		IList<string> NoDWMCaptureForProduct
		{
			get;
			set;
		}

		[Description("Make some optimizations for usage with remote desktop"), DefaultValue("False")]
		bool OptimizeForRDP
		{
			get;
			set;
		}

		[Description("Optimize memory footprint, but with a performance penalty!"), DefaultValue("False")]
		bool MinimizeWorkingSetSize
		{
			get;
			set;
		}

		[Description("Remove the corners from a window capture"), DefaultValue("True")]
		bool WindowCaptureRemoveCorners
		{
			get;
			set;
		}

		[Description("Also check for unstable version updates"), DefaultValue("False")]
		bool CheckForUnstable
		{
			get;
			set;
		}

		[Description("The fixes that are active.")]
		IList<string> ActiveTitleFixes
		{
			get;
			set;
		}

		[Description("The regular expressions to match the title with.")]
		IDictionary<string, string> TitleFixMatcher
		{
			get;
			set;
		}

		[Description("The replacements for the matchers.")]
		IDictionary<string, string> TitleFixReplacer
		{
			get;
			set;
		}

		[Description("Enable a special DIB clipboard reader"), DefaultValue("True")]
		bool EnableSpecialDIBClipboardReader
		{
			get;
			set;
		}


		[Description("The cutshape which is used to remove the window corners, is mirrorred for all corners"), DefaultValue("5,3,2,1,1")]
		IList<int> WindowCornerCutShape
		{
			get;
			set;
		}

		[Description("Specify what action is made if the tray icon is left clicked, if a double-click action is specified this action is initiated after a delay (configurable via the windows double-click speed)"), DefaultValue("SHOW_CONTEXT_MENU")]
		ClickActions LeftClickAction
		{
			get;
			set;
		}

		[Description("Specify what action is made if the tray icon is double clicked"), DefaultValue("OPEN_LAST_IN_EXPLORER")]
		ClickActions DoubleClickAction
		{
			get;
			set;
		}

		[Description("Sets if the zoomer is enabled"), DefaultValue("True")]
		bool ZoomerEnabled
		{
			get;
			set;
		}

		[Description("Specify the transparency for the zoomer, from 0-1 (where 1 is no transparency and 0 is complete transparent. An usefull setting would be 0.7)"), DefaultValue("1")]
		float ZoomerOpacity
		{
			get;
			set;
		}

		[Description("Maximum length of submenu items in the context menu, making this longer might cause context menu issues on dual screen systems."), DefaultValue("25")]
		int MaxMenuItemLength
		{
			get;
			set;
		}

		[Description("The 'to' field for the email destination (settings for Outlook can be found under the Office section)"), DefaultValue("")]
		string MailApiTo
		{
			get;
			set;
		}

		[Description("The 'CC' field for the email destination (settings for Outlook can be found under the Office section)"), DefaultValue("")]
		string MailApiCC
		{
			get;
			set;
		}

		[Description("The 'BCC' field for the email destination (settings for Outlook can be found under the Office section)"), DefaultValue("")]
		string MailApiBCC
		{
			get;
			set;
		}

		[Description("Optional command to execute on a temporary PNG file, the command should overwrite the file and Greenshot will read it back. Note: this command is also executed when uploading PNG's!"), DefaultValue("")]
		string OptimizePNGCommand
		{
			get;
			set;
		}

		[Description("Arguments for the optional command to execute on a PNG, {0} is replaced by the temp-filename from Greenshot. Note: Temp-file is deleted afterwards by Greenshot."), DefaultValue("\"{0}\"")]
		string OptimizePNGCommandArguments
		{
			get;
			set;
		}

		[Description("Version of Greenshot which created this .ini")]
		string LastSaveWithVersion
		{
			get;
			set;
		}

		[Description("When reading images from files or clipboard, use the EXIF information to correct the orientation"), DefaultValue("True")]
		bool ProcessEXIFOrientation
		{
			get;
			set;
		}

		[Description("The last used region, for reuse in the capture last region")]
		Rectangle LastCapturedRegion
		{
			get;
			set;
		}

		[Description("Defines the size of the icons (e.g. for the buttons in the editor), default value 16,16 anything bigger will cause scaling"), DefaultValue("16,16")]
		Size IconSize {
			get;
			set;
		}

		[Description("The connect timeout value for http-connections, these are seconds"), DefaultValue("120")]
		int HttpConnectionTimeout
		{
			get;
			set;
		}

		[Description("The read/write timeout value for webrequets, these are seconds"), DefaultValue("100")]
		int WebRequestReadWriteTimeout
		{
			get;
			set;
		}

		/// <summary>
		/// FEATURE-709 / FEATURE-419: Add the possibility to ignore the hotkeys
		/// </summary>
		[Description("Ignore the hotkey if currently one of the specified processes is active")]
		IList<string> IgnoreHotkeyProcessList {
			get;
			set;
		}
	}

	public static class CoreConfigurationChecker {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(CoreConfigurationChecker));
		/// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public static object GetDefault(string property) {
			switch (property) {
				case "PluginWhitelist":
				case "PluginBacklist":
					return new List<string>();
				case "OutputFileAsFullpath":
					if (PortableHelper.IsPortable) {
						return Path.Combine(Application.StartupPath, @"..\..\Documents\Pictures\Greenshots\dummy.png");
					} else {
						return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dummy.png");
					}
				case "OutputFilePath":
					if (PortableHelper.IsPortable) {
						string pafOutputFilePath = Path.Combine(Application.StartupPath, @"..\..\Documents\Pictures\Greenshots");
						if (!Directory.Exists(pafOutputFilePath)) {
							try {
								Directory.CreateDirectory(pafOutputFilePath);
								return pafOutputFilePath;
							} catch (Exception ex) {
								LOG.Warn(ex);
								// Problem creating directory, fallback to Desktop
							}
						} else {
							return pafOutputFilePath;
						}
					}
					return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				case "DWMBackgroundColor":
					return Color.Transparent;
				case "ActiveTitleFixes":
					IList<string> activeDefaults = new List<string>();
					activeDefaults.Add("Firefox");
					activeDefaults.Add("IE");
					activeDefaults.Add("Chrome");
					return activeDefaults;
				case "TitleFixMatcher":
					IDictionary<string, string> matcherDefaults = new Dictionary<string, string>();
					matcherDefaults.Add("Firefox", " - Mozilla Firefox.*");
					matcherDefaults.Add("IE", " - (Microsoft|Windows) Internet Explorer.*");
					matcherDefaults.Add("Chrome", " - Google Chrome.*");
					return matcherDefaults;
				case "TitleFixReplacer":
					IDictionary<string, string> replacerDefaults = new Dictionary<string, string>();
					replacerDefaults.Add("Firefox", "");
					replacerDefaults.Add("IE", "");
					replacerDefaults.Add("Chrome", "");
					return replacerDefaults;
			}
			return null;
		}

		/// <summary>
		/// This method will be called before converting the property, making to possible to correct a certain value
		/// Can be used when migration is needed
		/// </summary>
		/// <param name="propertyName">The name of the property</param>
		/// <param name="propertyValue">The string value of the property</param>
		/// <returns>string with the propertyValue, modified or not...</returns>
		public static string PreCheckValue(string propertyName, string propertyValue) {
			// Changed the separator, now we need to correct this
			if ("Destinations".Equals(propertyName)) {
				if (propertyValue != null) {
					return propertyValue.Replace('|', ',');
				}
			}
			if ("OutputFilePath".Equals(propertyName)) {
				if (string.IsNullOrEmpty(propertyValue)) {
					return null;
				}
			}
			return propertyValue;
		}

		/// <summary>
		/// This method will be called before writing the configuration
		/// </summary>
		public static void BeforeSave(CoreConfiguration coreConfiguration) {
			try {
				// Store version, this can be used later to fix settings after an update
				coreConfiguration.LastSaveWithVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
			} catch {
			}
		}

		public static bool UseLargeIcons(Size iconSize) {
			return iconSize.Width >= 32 || iconSize.Height >= 32;
		}

		/// <summary>
		/// Helper method to limit the icon size, keep it sensible
		/// </summary>
		/// <param name="iconSize">Size</param>
		/// <returns>Size</returns>
		public static Size FixIconSize(Size iconSize) {
			// check the icon size value
			if (iconSize != Size.Empty) {
				if (iconSize.Width < 16) {
					iconSize.Width = 16;
				} else if (iconSize.Width > 256) {
					iconSize.Width = 256;
				}
				iconSize.Width = (iconSize.Width / 16) * 16;
				if (iconSize.Height < 16) {
					iconSize.Height = 16;
				} else if (iconSize.Height > 256) {
					iconSize.Height = 256;
				}
				iconSize.Height = (iconSize.Height / 16) * 16;
			}
			return iconSize;
		}

		public static void AfterLoad(CoreConfiguration coreConfiguration) {
			// Comment with releases
			// CheckForUnstable = true;

			// check the icon size value
			Size iconSize = FixIconSize(coreConfiguration.IconSize);
			if (iconSize != coreConfiguration.IconSize) {
				coreConfiguration.IconSize = iconSize;
			}

			if (string.IsNullOrEmpty(coreConfiguration.LastSaveWithVersion)) {
				try {
					// Store version, this can be used later to fix settings after an update
					coreConfiguration.LastSaveWithVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
				} catch {

				}
				// Disable the AutoReduceColors as it causes issues with Mozzila applications and some others
				coreConfiguration.OutputFileAutoReduceColors = false;
			}

			// Enable OneNote if upgrading from 1.1
			if (coreConfiguration.ExcludeDestinations != null && coreConfiguration.ExcludeDestinations.Contains("OneNote")) {
				if (coreConfiguration.LastSaveWithVersion != null && coreConfiguration.LastSaveWithVersion.StartsWith("1.1")) {
					coreConfiguration.ExcludeDestinations.Remove("OneNote");
				} else {
					// TODO: Remove with the release
					coreConfiguration.ExcludeDestinations.Remove("OneNote");
				}
			}

			if (coreConfiguration.OutputDestinations == null) {
				coreConfiguration.OutputDestinations = new List<string>();
			}

			// Make sure there is an output!
			if (coreConfiguration.OutputDestinations.Count == 0) {
				coreConfiguration.OutputDestinations.Add("Editor");
			}

			// Prevent both settings at once, bug #3435056
			if (coreConfiguration.OutputDestinations.Contains("Clipboard") && coreConfiguration.OutputFileCopyPathToClipboard) {
				coreConfiguration.OutputFileCopyPathToClipboard = false;
			}

			// Make sure we have clipboard formats, otherwise a paste doesn't make sense!
			if (coreConfiguration.ClipboardFormats == null || coreConfiguration.ClipboardFormats.Count == 0) {
				coreConfiguration.ClipboardFormats = new List<ClipboardFormat>();
				coreConfiguration.ClipboardFormats.Add(ClipboardFormat.PNG);
				coreConfiguration.ClipboardFormats.Add(ClipboardFormat.HTML);
				coreConfiguration.ClipboardFormats.Add(ClipboardFormat.DIB);
			}

			// Make sure the lists are lowercase, to speedup the check
			if (coreConfiguration.NoGDICaptureForProduct != null) {
				// Fix error in configuration
				if (coreConfiguration.NoGDICaptureForProduct.Count >= 2) {
					if ("intellij".Equals(coreConfiguration.NoGDICaptureForProduct[0]) && "idea".Equals(coreConfiguration.NoGDICaptureForProduct[1])) {
						coreConfiguration.NoGDICaptureForProduct.RemoveAt(0);
						coreConfiguration.NoGDICaptureForProduct.RemoveAt(0);
						coreConfiguration.NoGDICaptureForProduct.Add("Intellij Idea");
					}
				}
				for (int i = 0; i < coreConfiguration.NoGDICaptureForProduct.Count; i++) {
					coreConfiguration.NoGDICaptureForProduct[i] = coreConfiguration.NoGDICaptureForProduct[i].ToLower();
				}
			}
			if (coreConfiguration.NoDWMCaptureForProduct != null) {
				// Fix error in configuration
				if (coreConfiguration.NoDWMCaptureForProduct.Count >= 3) {
					if ("citrix".Equals(coreConfiguration.NoDWMCaptureForProduct[0]) && "ica".Equals(coreConfiguration.NoDWMCaptureForProduct[1]) && "client".Equals(coreConfiguration.NoDWMCaptureForProduct[2])) {
						coreConfiguration.NoGDICaptureForProduct.RemoveAt(0);
						coreConfiguration.NoGDICaptureForProduct.RemoveAt(0);
						coreConfiguration.NoGDICaptureForProduct.RemoveAt(0);
						coreConfiguration.NoDWMCaptureForProduct.Add("Citrix ICA Client");
					}
				}
				for (int i = 0; i < coreConfiguration.NoDWMCaptureForProduct.Count; i++) {
					coreConfiguration.NoDWMCaptureForProduct[i] = coreConfiguration.NoDWMCaptureForProduct[i].ToLower();
				}
			}

			if (coreConfiguration.AutoCropDifference < 0) {
				coreConfiguration.AutoCropDifference = 0;
			}
			if (coreConfiguration.AutoCropDifference > 255) {
				coreConfiguration.AutoCropDifference = 255;
			}
			if (coreConfiguration.OutputFileReduceColorsTo < 2) {
				coreConfiguration.OutputFileReduceColorsTo = 2;
			}
			if (coreConfiguration.OutputFileReduceColorsTo > 256) {
				coreConfiguration.OutputFileReduceColorsTo = 256;
			}

			if (coreConfiguration.HttpConnectionTimeout < 1) {
				coreConfiguration.HttpConnectionTimeout = 10;
			}
			if (coreConfiguration.WebRequestReadWriteTimeout < 1) {
				coreConfiguration.WebRequestReadWriteTimeout = 100;
			}
			// Make sure the path is lowercase
			if (coreConfiguration.IgnoreHotkeyProcessList != null && coreConfiguration.IgnoreHotkeyProcessList.Count > 0) {
				for (int i = 0; i < coreConfiguration.IgnoreHotkeyProcessList.Count; i++) {
					coreConfiguration.IgnoreHotkeyProcessList[i] = coreConfiguration.IgnoreHotkeyProcessList[i].ToLowerInvariant();
				}
			}
		}
	}
}