/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Imaging;
using System.IO;

namespace Greenshot.Core {
	/// <summary>
	/// Description of CoreConfiguration.
	/// </summary>
	[IniSection("Core", Description="Greenshot core configuration")]
	public class CoreConfiguration : IniSection {
		[IniProperty("Language", Description="The language in IETF format (e.g. en-EN)", DefaultValue="en-EN")]
		public string Language;
		[IniProperty("RegisterHotkeys", Description="Register the hotkeys?", DefaultValue="true")]
		public bool RegisterHotkeys;
		[IniProperty("IsFirstLaunch", Description="Is this the first time launch?", DefaultValue="true")]
		public bool IsFirstLaunch;
		[IniProperty("Destinations", Description="Which destinations? Options are: Editor, FileDefault, FileWithDialog, Clipboard, Printer, EMail", DefaultValue="Editor")]
		public List<String> OutputDestinations;

		[IniProperty("CaptureMousepointer", Description="Should the mouse be captured?", DefaultValue="true")]
		public bool CaptureMousepointer;
		[IniProperty("CaptureWindowsInteractive", Description="Use interactive window selection to capture? (false=Capture active window)", DefaultValue="false")]
		public bool CaptureWindowsInteractive;
		[IniProperty("CaptureDelay", Description="Capture delay in millseconds.", DefaultValue="100")]
		public int CaptureDelay;
		[IniProperty("CaptureCompleteWindow", Description="Try capturing the complete window.", DefaultValue="false")]
		public bool CaptureCompleteWindow;
		[IniProperty("CaptureWindowContent", Description="Try capturing only the content of the window (IE/Firefox).", DefaultValue="false")]
		public bool? CaptureWindowContent;

		[IniProperty("ShowFlashlight", Description="Show a flash after taking a capture.", DefaultValue="false")]
		public bool ShowFlash = false;
		[IniProperty("PlayCameraSound", Description="Play a camera sound after taking a capture.", DefaultValue="false")]
		public bool PlayCameraSound = false;
		
		public string Output_File_Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
		[IniProperty("OutputFileFilenamePattern", Description="Filename pattern for screenshot.", DefaultValue="%title%_%YYYY%-%MM%-%DD%_%hh%-%mm%-%ss%")]
		public string OutputFileFilenamePattern;
		[IniProperty("OutputFileFormat", Description="Default file type for writing screenshots. (Bmp, Gif, Jepg, Png, Tiff)", DefaultValue="Png")]
		public ImageFormat OutputFileFormat = ImageFormat.Png;
		[IniProperty("OutputFileCopyPathToClipboard", Description="When saving a screenshot, copy the path to the clipboard?", DefaultValue="true")]
		public bool OutputFileCopyPathToClipboard;

		[IniProperty("OutputFileJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int OutputFileJpegQuality;
		[IniProperty("OutputFilePromptJpegQuality", Description="Ask for the JPEQ quality before saving?", DefaultValue="false")]
		public bool OutputFilePromptJpegQuality;
		[IniProperty("OutputFileIncrementingNumber", Description="The number for the %NUM% in the filename pattern, is increased automatically after each save.", DefaultValue="1")]
		public uint OutputFileIncrementingNumber;
		
		public string Output_FileAs_Fullpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"dummy.png");
		
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
		[IniProperty("OutputPrintTimestamp", Description="Print timestamp on print?", DefaultValue="true")]
		public bool OutputPrintTimestamp;

		[IniProperty("EditorWindowSize", Description="Size of the editor.", DefaultValue="540, 380")]
		public Size EditorWindowSize;
		[IniProperty("EditorWindowLocation", Description="Location of the editor.", DefaultValue="100, 100")]
		public Point EditorWindowLocation;
		[IniProperty("EditorWindowState", Description="The window state of the editor. (Normal or Maximized)", DefaultValue="Normal")]
		public String EditorWindowState;

		public Color[] Editor_RecentColors = new Color[12];
		public Font Editor_Font = null;
		
		// Not storing, this doesn't make sense to store (or does it?)
		public Rectangle EditorPreviousScreenbounds = Rectangle.Empty;
	}
}
