﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Drawing;
using System.IO;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;

namespace GreenshotOCRPlugin {
	/// <summary>
	/// Description of OCRDestination.
	/// </summary>
	public class OCRDestination : AbstractDestination {
		private readonly OcrPlugin _plugin;
		
		public override string Designation => "OCR";

		public override string Description => "OCR";

		public override Image DisplayIcon {
			get {
				string exePath = PluginUtils.GetExePath("MSPVIEW.EXE");
				if (exePath != null && File.Exists(exePath)) {
					return PluginUtils.GetCachedExeIcon(exePath, 0);
				}
				return null;
			}
		}

		public OCRDestination(OcrPlugin plugin) {
			_plugin = plugin;
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(Designation, Description)
			{
				ExportMade = _plugin.DoOcr(surface) != null
			};
			return exportInformation;
		}
	}
}
