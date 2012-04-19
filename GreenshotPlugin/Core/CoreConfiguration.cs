/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Greenshot.Plugin;
using Greenshot.Interop.Office;
using Greenshot.IniFile;

namespace GreenshotPlugin.Core {
	public enum ClipboardFormat {
		PNG, DIB, HTML, HTMLDATAURL
	}
	public enum OutputFormat {
		bmp, gif, jpg, png, tiff
	}
	public enum WindowCaptureMode {
		Screen, GDI, Aero, AeroTransparent, Auto
	}

	/// <summary>
	/// Description of CoreConfiguration.
	/// </summary>
	[IniSection("Core", Description="Greenshot core configuration")]
	public class CoreConfiguration : IniSection {
		[IniProperty("Language", Description="The language in IETF format (e.g. en-US)")]
		public string Language;

		[IniProperty("RegionHotkey", Description="Hotkey for starting the region capture", DefaultValue="PrintScreen")]
		public string RegionHotkey;
		[IniProperty("WindowHotkey", Description="Hotkey for starting the window capture", DefaultValue="Alt + PrintScreen")]
		public string WindowHotkey;
		[IniProperty("FullscreenHotkey", Description="Hotkey for starting the fullscreen capture", DefaultValue="Ctrl + PrintScreen")]
		public string FullscreenHotkey;
		[IniProperty("LastregionHotkey", Description="Hotkey for starting the last region capture", DefaultValue="Shift + PrintScreen")]
		public string LastregionHotkey;
		[IniProperty("IEHotkey", Description="Hotkey for starting the IE capture", DefaultValue="Shift + Ctrl + PrintScreen")]
		public string IEHotkey;

		[IniProperty("IsFirstLaunch", Description="Is this the first time launch?", DefaultValue="true")]
		public bool IsFirstLaunch;
		[IniProperty("Destinations", Separator=",", Description="Which destinations? Options are: Editor, FileDefault, FileWithDialog, Clipboard, Printer, EMail", DefaultValue="Editor")]
		public List<string> OutputDestinations = new List<string>();
		[IniProperty("ClipboardFormats", Separator=",", Description="Specify which formats we copy on the clipboard? Options are: PNG,HTML and DIB", DefaultValue="PNG,HTML,DIB")]
		public List<ClipboardFormat> ClipboardFormats = new List<ClipboardFormat>();

		[IniProperty("CaptureMousepointer", Description="Should the mouse be captured?", DefaultValue="true")]
		public bool CaptureMousepointer;
		[IniProperty("CaptureWindowsInteractive", Description="Use interactive window selection to capture? (false=Capture active window)", DefaultValue="false")]
		public bool CaptureWindowsInteractive;
		[IniProperty("CaptureDelay", Description="Capture delay in millseconds.", DefaultValue="100")]
		public int CaptureDelay;
		[IniProperty("ScreenCaptureMode", Description = "The capture mode used to capture a screen. (Auto, FullScreen, Fixed)", DefaultValue = "Auto")]
		public ScreenCaptureMode ScreenCaptureMode;
		[IniProperty("ScreenToCapture", Description = "The screen number to capture when using ScreenCaptureMode Fixed.", DefaultValue = "1")]
		public int ScreenToCapture;
		[IniProperty("WindowCaptureMode", Description = "The capture mode used to capture a Window (Screen, GDI, Aero, AeroTransparent, Auto).", DefaultValue = "Auto")]
		public WindowCaptureMode WindowCaptureMode;
		[IniProperty("WindowCaptureAllChildLocations", Description="Enable/disable capture all children, very slow but will make it possible to use this information in the editor.", DefaultValue="False")]
		public bool WindowCaptureAllChildLocations;

		[IniProperty("DWMBackgroundColor", Description="The background color for a DWM window capture.")]
		public Color DWMBackgroundColor;

		[IniProperty("PlayCameraSound", LanguageKey="settings_playsound",Description="Play a camera sound after taking a capture.", DefaultValue="false")]
		public bool PlayCameraSound = false;
		[IniProperty("ShowTrayNotification", LanguageKey="settings_shownotify",Description="Show a notification from the systray when a capture is taken.", DefaultValue="true")]
		public bool ShowTrayNotification = true;
		
		[IniProperty("OutputFilePath", Description="Output file path.")]
		public string OutputFilePath;
		[IniProperty("OutputFileFilenamePattern", Description="Filename pattern for screenshot.", DefaultValue="${capturetime:d\"yyyy-MM-dd HH_mm_ss\"}-${title}")]
		public string OutputFileFilenamePattern;
		[IniProperty("OutputFileFormat", Description="Default file type for writing screenshots. (bmp, gif, jpg, png, tiff)", DefaultValue="png")]
		public OutputFormat OutputFileFormat = OutputFormat.png;
		[IniProperty("OutputFileReduceColors", Description="If set to true, than the colors of the output file are reduced to 256 (8-bit) colors", DefaultValue="false")]
		public bool OutputFileReduceColors;
		[IniProperty("OutputFileAutoReduceColors", Description = "If set to true the amount of colors is counted and if smaller than 256 the color reduction is automatically used.", DefaultValue = "true")]
		public bool OutputFileAutoReduceColors;

		[IniProperty("OutlookEmailFormat", Description = "Default type for emails. (Text, HTML)", DefaultValue="HTML")]
		public EmailFormat OutlookEmailFormat;
		[IniProperty("OutlookAllowExportInMeetings", Description = "Allow export in meeting items", DefaultValue="False")]
		public bool OutlookAllowExportInMeetings;

		[IniProperty("OutputFileCopyPathToClipboard", Description="When saving a screenshot, copy the path to the clipboard?", DefaultValue="true")]
		public bool OutputFileCopyPathToClipboard;
		[IniProperty("OutputFileAsFullpath", Description="SaveAs Full path?")]
		public string OutputFileAsFullpath;
		
		[IniProperty("OutputFileJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int OutputFileJpegQuality;
		[IniProperty("OutputFilePromptQuality", Description="Ask for the quality before saving?", DefaultValue="false")]
		public bool OutputFilePromptQuality;
		[IniProperty("OutputFileIncrementingNumber", Description="The number for the ${NUM} in the filename pattern, is increased automatically after each save.", DefaultValue="1")]
		public uint OutputFileIncrementingNumber;
		
		[IniProperty("OutputPrintPromptOptions", LanguageKey="settings_alwaysshowprintoptionsdialog", Description="Ask for print options when printing?", DefaultValue="true")]
		public bool OutputPrintPromptOptions;
		[IniProperty("OutputPrintAllowRotate", LanguageKey="printoptions_allowrotate", Description="Allow rotating the picture for fitting on paper?", DefaultValue="true")]
		public bool OutputPrintAllowRotate;
		[IniProperty("OutputPrintAllowEnlarge", LanguageKey="printoptions_allowenlarge", Description="Allow growing the picture for fitting on paper?", DefaultValue="true")]
		public bool OutputPrintAllowEnlarge;
		[IniProperty("OutputPrintAllowShrink", LanguageKey="printoptions_allowshrink", Description="Allow shrinking the picture for fitting on paper?", DefaultValue="true")]
		public bool OutputPrintAllowShrink;
		[IniProperty("OutputPrintCenter", LanguageKey="printoptions_allowcenter", Description="Center image when printing?", DefaultValue="true")]
		public bool OutputPrintCenter;
		[IniProperty("OutputPrintInverted", LanguageKey="printoptions_inverted", Description="Print image inverted (use e.g. for console captures)", DefaultValue="false")]
		public bool OutputPrintInverted;
		[IniProperty("OutputPrintFooter", LanguageKey = "printoptions_timestamp", Description = "Print footer on print?", DefaultValue = "true")]
		public bool OutputPrintFooter;
		[IniProperty("OutputPrintFooterPattern", Description = "Footer pattern", DefaultValue = "${capturetime:d\"D\"} ${capturetime:d\"T\"} - ${title}")]
		public string OutputPrintFooterPattern;

		[IniProperty("UseProxy", Description="Use your global proxy?", DefaultValue="True")]
		public bool UseProxy;
		[IniProperty("IECapture", Description="Enable/disable IE capture", DefaultValue="True")]
		public bool IECapture;
		[IniProperty("IEFieldCapture", Description="Enable/disable IE field capture, very slow but will make it possible to annotate the fields of a capture in the editor.", DefaultValue="False")]
		public bool IEFieldCapture;
		[IniProperty("AutoCropDifference", Description="Sets how to compare the colors for the autocrop detection, the higher the more is 'selected'. Possible values are from 0 to 255, where everything above ~150 doesn't make much sense!", DefaultValue="10")]
		public int AutoCropDifference;

		[IniProperty("IncludePlugins", Description="Comma separated list of Plugins which are allowed. If something in the list, than every plugin not in the list will not be loaded!")]
		public List<string> IncludePlugins;
		[IniProperty("ExcludePlugins", Description="Comma separated list of Plugins which are NOT allowed.")]
		public List<string> ExcludePlugins;

		[IniProperty("UpdateCheckInterval", Description="How many days between every update check? (0=no checks)", DefaultValue="1")]
		public int UpdateCheckInterval;
		
		[IniProperty("LastUpdateCheck", Description="Last update check")]
		public DateTime LastUpdateCheck;

		[IniProperty("ThumnailPreview", Description="Enable/disable thumbnail previews", DefaultValue="True")]
		public bool ThumnailPreview;
		
		[IniProperty("NoGDICaptureForProduct", Description="List of products for which GDI capturing doesn't work.", DefaultValue="IntelliJ IDEA")]
		public List<string> NoGDICaptureForProduct;
		[IniProperty("NoDWMCaptureForProduct", Description="List of products for which DWM capturing doesn't work.", DefaultValue="Citrix ICA Client")]
		public List<string> NoDWMCaptureForProduct;

		[IniProperty("OptimizeForRDP", Description="Make some optimizations for usage with remote desktop", DefaultValue="False")]
		public bool OptimizeForRDP;

		[IniProperty("ActiveTitleFixes", Description="The fixes that are active.")]
		public List<string> ActiveTitleFixes;
		
		[IniProperty("TitleFixMatcher", Description="The regular expressions to match the title with.")]
		public Dictionary<string, string> TitleFixMatcher;

		[IniProperty("TitleFixReplacer", Description="The replacements for the matchers.")]
		public Dictionary<string, string> TitleFixReplacer;

		[IniProperty("ExperimentalFeatures", Description="A list which allows us to enable certain experimental features", ExcludeIfNull=true)]
		public List<string> ExperimentalFeatures;
		
		/// <summary>
		/// A helper method which returns true if the supplied experimental feature is enabled
		/// </summary>
		/// <param name="experimentalFeature"></param>
		/// <returns></returns>
		public bool isExperimentalFeatureEnabled(string experimentalFeature) {
			return (ExperimentalFeatures != null && ExperimentalFeatures.Contains(experimentalFeature));
		}

		/// <summary>
		/// Helper method to check if it is allowed to capture the process using DWM
		/// </summary>
		/// <param name="process">Process owning the window</param>
		/// <returns>true if it's allowed</returns>
		public bool isDWMAllowed(Process process) {
			if (process != null) {
				if (NoDWMCaptureForProduct != null && NoDWMCaptureForProduct.Count > 0) {
					try {
						string productName = process.MainModule.FileVersionInfo.ProductName;
						if (productName != null && NoDWMCaptureForProduct.Contains(productName.ToLower())) {
							return false;
						}
					} catch (Exception ex) {
						LOG.Warn(ex);
					}
				}
			}
			return true;
		}
		
		/// <summary>
		/// Helper method to check if it is allowed to capture the process using GDI
		/// </summary>
		/// <param name="processName">Process owning the window</param>
		/// <returns>true if it's allowed</returns>
		public bool isGDIAllowed(Process process) {
			if (process != null) {
				if (NoGDICaptureForProduct != null && NoGDICaptureForProduct.Count > 0) {
					try {
						string productName = process.MainModule.FileVersionInfo.ProductName;
						if (productName != null && NoGDICaptureForProduct.Contains(productName.ToLower())) {
							return false;
						}
					} catch (Exception ex) {
						LOG.Warn(ex);
					}
				}
			}
			return true;
		}

		// change to false for releases
		public bool CheckUnstable = true;

		/// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public override object GetDefault(string property) {
			switch(property) {
				case "OutputPrintFooterPattern":

					break;
				case "PluginWhitelist":
				case "PluginBacklist":
					return new List<string>();
				case "OutputFileAsFullpath":
					if (IniConfig.IsPortable) {
						return Path.Combine(Application.StartupPath, @"..\..\Documents\Pictures\Greenshots\dummy.png");
					} else {
						return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"dummy.png");
					}
				case "OutputFilePath":
					if (IniConfig.IsPortable) {
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
					List<string> activeDefaults = new List<string>();
					activeDefaults.Add("Firefox");
					activeDefaults.Add("IE");
					activeDefaults.Add("Chrome");
					return activeDefaults; 
				case "TitleFixMatcher":
					Dictionary<string, string> matcherDefaults = new Dictionary<string, string>();
					matcherDefaults.Add("Firefox", " - Mozilla Firefox.*");
					matcherDefaults.Add("IE", " - (Microsoft|Windows) Internet Explorer.*");
					matcherDefaults.Add("Chrome", " - Google Chrome.*");
					return matcherDefaults; 
				case "TitleFixReplacer":
					Dictionary<string, string> replacerDefaults = new Dictionary<string, string>();
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
		public override string PreCheckValue(string propertyName, string propertyValue) {
			// Changed the separator, now we need to correct this
			if ("Destinations".Equals(propertyName)) {
				if (propertyValue != null) {
					return propertyValue.Replace('|',',');
				}
			}
			if("OutputFilePath".Equals(propertyName)) {
				if (string.IsNullOrEmpty(propertyValue)) {
					return null;
				}
			}
			return base.PreCheckValue(propertyName, propertyValue);
		}

		/// <summary>
		/// This method will be called after reading the configuration, so eventually some corrections can be made
		/// </summary>
		public override void AfterLoad() {
			if (OutputDestinations == null) {
				OutputDestinations = new List<string>();
			}
			// Make sure there is an output!
			if (OutputDestinations.Count == 0) {
				OutputDestinations.Add("Editor");
			}
			// Prevent both settings at once, bug #3435056
			if (OutputDestinations.Contains("Clipboard") && OutputFileCopyPathToClipboard) {
				OutputFileCopyPathToClipboard = false;
			}

			// Make sure we have clipboard formats, otherwise a paste doesn't make sense!
			if (ClipboardFormats == null || ClipboardFormats.Count == 0) {
				ClipboardFormats = new List<ClipboardFormat>();
				ClipboardFormats.Add(ClipboardFormat.PNG);
				ClipboardFormats.Add(ClipboardFormat.HTML);
				ClipboardFormats.Add(ClipboardFormat.DIB);
			}

			// Make sure the lists are lowercase, to speedup the check
			if (NoGDICaptureForProduct != null) {
				for(int i=0; i< NoGDICaptureForProduct.Count; i++) {
					NoGDICaptureForProduct[i] = NoGDICaptureForProduct[i].ToLower();
				}
			}
			if (NoDWMCaptureForProduct != null) {
				for(int i=0; i< NoDWMCaptureForProduct.Count; i++) {
					NoDWMCaptureForProduct[i] = NoDWMCaptureForProduct[i].ToLower();
				}
			}
				
			if (AutoCropDifference < 0) {
				AutoCropDifference = 0;
			}
			if (AutoCropDifference > 255) {
				AutoCropDifference = 255;
			}
		}
	}
}