/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.IO;

namespace GreenshotPlugin.Core {
	public enum Destination {
		Editor, FileDefault, FileWithDialog, Clipboard, Printer, EMail
	}
	public enum OutputFormat {
		bmp, gif, jpg, png, tiff
	}
	public enum WindowCaptureMode {
		Screen, GDI, Aero, AeroTransparent, Auto
	}
	public enum EmailFormat {
		TXT, HTML
	}
	public enum UpdateCheckInterval {
		Never,
		Daily,
		Weekly,
		Monthly
	}
	public enum EmailExport {
		AlwaysNew,
		TryOpenElseNew
	}
	/// <summary>
	/// Description of CoreConfiguration.
	/// </summary>
	[IniSection("Core", Description="Greenshot core configuration")]
	public class CoreConfiguration : IniSection {
		[IniProperty("Language", Description="The language in IETF format (e.g. en-EN)", DefaultValue="en-EN")]
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
		public List<Destination> OutputDestinations = new List<Destination>();

		[IniProperty("CaptureMousepointer", Description="Should the mouse be captured?", DefaultValue="true")]
		public bool CaptureMousepointer;
		[IniProperty("CaptureWindowsInteractive", Description="Use interactive window selection to capture? (false=Capture active window)", DefaultValue="false")]
		public bool CaptureWindowsInteractive;
		[IniProperty("CaptureDelay", Description="Capture delay in millseconds.", DefaultValue="100")]
		public int CaptureDelay;
		[IniProperty("WindowCaptureMode", Description="The capture mode used to capture a Window.", DefaultValue="Auto")]
		public WindowCaptureMode WindowCaptureMode;
		[IniProperty("DWMBackgroundColor", Description="The background color for a DWM window capture.")]
		public Color DWMBackgroundColor;

		[IniProperty("PlayCameraSound", Description="Play a camera sound after taking a capture.", DefaultValue="false")]
		public bool PlayCameraSound = false;
		
		[IniProperty("OutputFilePath", Description="Output file path.")]
		public string OutputFilePath;
		[IniProperty("OutputFileFilenamePattern", Description="Filename pattern for screenshot.", DefaultValue="${capturetime}_${title}")]
		public string OutputFileFilenamePattern;
		[IniProperty("OutputFileFormat", Description="Default file type for writing screenshots. (bmp, gif, jpg, png, tiff)", DefaultValue="png")]
		public OutputFormat OutputFileFormat = OutputFormat.png;
		[IniProperty("OutputFileReduceColors", Description="If set to true, than the colors of the output file are reduced to 256 (8-bit) colors", DefaultValue="false")]
		public bool OutputFileReduceColors;
		
		[IniProperty("OutputEMailFormat", Description="Default type for emails. (txt, html)", DefaultValue="html")]
		public EmailFormat OutputEMailFormat = EmailFormat.HTML;
		[IniProperty("OutputOutlookMethod", Description="How to export to outlook (AlwaysNew= always open a new one, TryOpenElseNew=look for open email else create a new)", DefaultValue="AlwaysNew")]
		public EmailExport OutputOutlookMethod;

		[IniProperty("OutputFileCopyPathToClipboard", Description="When saving a screenshot, copy the path to the clipboard?", DefaultValue="true")]
		public bool OutputFileCopyPathToClipboard;
		[IniProperty("OutputFileAsFullpath", Description="SaveAs Full path?")]
		public string OutputFileAsFullpath;
		
		[IniProperty("OutputFileJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int OutputFileJpegQuality;
		[IniProperty("OutputFilePromptJpegQuality", Description="Ask for the JPEQ quality before saving?", DefaultValue="false")]
		public bool OutputFilePromptJpegQuality;
		[IniProperty("OutputFileIncrementingNumber", Description="The number for the %NUM% in the filename pattern, is increased automatically after each save.", DefaultValue="1")]
		public uint OutputFileIncrementingNumber;
		
		[IniProperty("OutputPrintPromptOptions", Description="Ask for print options when printing?", DefaultValue="true")]
		public bool OutputPrintPromptOptions;
		[IniProperty("OutputPrintAllowRotate", Description="Allow rotating the picture for fitting on paper?", DefaultValue="true")]
		public bool OutputPrintAllowRotate;
		[IniProperty("OutputPrintAllowEnlarge", Description="Allow growing the picture for fitting on paper?", DefaultValue="true")]
		public bool OutputPrintAllowEnlarge;
		[IniProperty("OutputPrintAllowShrink", Description="Allow shrinking the picture for fitting on paper?", DefaultValue="true")]
		public bool OutputPrintAllowShrink;
		[IniProperty("OutputPrintCenter", Description="Center image when printing?", DefaultValue="true")]
		public bool OutputPrintCenter;
		[IniProperty("OutputPrintInverted", Description="Print image inverted (use e.g. for console captures)", DefaultValue="false")]
		public bool OutputPrintInverted;
		[IniProperty("OutputPrintTimestamp", Description="Print timestamp on print?", DefaultValue="true")]
		public bool OutputPrintTimestamp;
		[IniProperty("UseProxy", Description="Use your global proxy?", DefaultValue="True")]
		public bool UseProxy;
		[IniProperty("IECapture", Description="Enable/disable IE capture", DefaultValue="True")]
		public bool IECapture;

		[IniProperty("IncludePlugins", Description="Comma separated list of Plugins which are allowed. If something in the list, than every plugin not in the list will not be loaded!")]
		public List<string> IncludePlugins;
		[IniProperty("ExcludePlugins", Description="Comma separated list of Plugins which are NOT allowed.")]
		public List<string> ExcludePlugins;

		[IniProperty("UpdateCheckInterval", Description="How many days between every update check? (0=no checks)", DefaultValue="1")]
		public int UpdateCheckInterval;
		
		[IniProperty("LastUpdateCheck", Description="Last update check")]
		public DateTime LastUpdateCheck;

		// change to false for releases
		public bool CheckUnstable = true;

		/// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public override object GetDefault(string property) {
			switch(property) {
				case "PluginWhitelist":
				case "PluginBacklist":
					return new List<string>();
				case "OutputFileAsFullpath":
					return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"dummy.png");
				case "OutputFilePath":
					return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

				case "DWMBackgroundColor":
					return Color.White;
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
			return base.PreCheckValue(propertyName, propertyValue);
		}

		/// <summary>
		/// This method will be called after reading the configuration, so eventually some corrections can be made
		/// </summary>
		public override void PostCheckValues() {
			if (OutputDestinations == null) {
				OutputDestinations = new List<Destination>();
			}
			if (OutputDestinations.Count == 0) {
				OutputDestinations.Add(Destination.Editor);
			}
		}
	}
}