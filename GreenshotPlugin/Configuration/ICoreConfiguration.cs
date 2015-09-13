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
	public interface ICoreConfiguration :
		// More configurations
		IOutputConfiguration, IPrinterConfiguration, IExtensionConfiguration, IUIConfiguration, ICaptureConfiguration, IMiscConfiguration,
		// Ini-Framework
		IIniSection<ICoreConfiguration>, INotifyPropertyChanged, ITagging<ICoreConfiguration>
	{

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

		[Description("Output file path.")]
		string OutputFilePath
		{
			get;
			set;
		}

		[Description("If the target file already exists True will make Greenshot always overwrite and False will display a 'Save-As' dialog."), DefaultValue(true)]
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

		[Description("When saving a screenshot, copy the path to the clipboard?"), DefaultValue(true)]
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

		[Description("The number for the ${NUM} in the filename pattern, is increased automatically after each save."), DefaultValue("1")]
		uint OutputFileIncrementingNumber
		{
			get;
			set;
		}

		[Description("Use your global proxy?"), DefaultValue(true)]
		bool UseProxy
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

		[Description("The connect timeout value for http-connections, these are seconds"), DefaultValue(120)]
		int HttpConnectionTimeout
		{
			get;
			set;
		}

		[Description("The read/write timeout value for webrequets, these are seconds"), DefaultValue(100)]
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

	/// <summary>
	/// TODO: This is not used
	/// </summary>
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
		public static void BeforeSave(ICoreConfiguration coreConfiguration) {
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

		public static void AfterLoad(ICoreConfiguration coreConfiguration) {
			// Comment with releases
			// CheckForUnstable = true;

			// check the icon size value
			var ics = coreConfiguration.IconSize;
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
				coreConfiguration.OutputDestinations.Add("Picker");
			}

			// Prevent both settings (path to clipboard & image to clipboard) at once, bug #3435056
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