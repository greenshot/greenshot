/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.IO;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace GreenshotOCR {
	/// <summary>
	/// Description of OCRDestination.
	/// </summary>
	public class OCRDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OCRDestination));
		private static OCRConfiguration config = IniConfig.GetIniSection<OCRConfiguration>();
		private const int MIN_WIDTH = 130;
		private const int MIN_HEIGHT = 130;
		private static Image icon = null;
		private OcrPlugin plugin;
		
		static OCRDestination() {
			string exePath = PluginUtils.GetExePath("MSPVIEW.EXE");
			if (exePath != null && File.Exists(exePath)) {
				icon = PluginUtils.GetExeIcon(exePath, 0);
			}
		}

		public override string Designation {
			get {
				return "OCR";
			}
		}

		public override string Description {
			get {
				return "OCR";
			}
		}

		public override Image DisplayIcon {
			get {
				return icon;
			}
		}

		public OCRDestination(OcrPlugin plugin) {
			this.plugin = plugin;
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			exportInformation.ExportMade = plugin.DoOCR(surface) != null;
			return exportInformation;
		}
	}
}
