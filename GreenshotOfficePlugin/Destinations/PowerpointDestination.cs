﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.Interop.Office;
using System.Text.RegularExpressions;

namespace GreenshotOfficePlugin {
	/// <summary>
	/// Description of PowerpointDestination.
	/// </summary>
	public class PowerpointDestination : AbstractDestination {
		private const int IconApplication = 0;
		private const int IconPresentation = 1;

		private static readonly string ExePath;
		private readonly string _presentationName;
		
		static PowerpointDestination() {
			ExePath = PluginUtils.GetExePath("POWERPNT.EXE");
			if (ExePath != null && File.Exists(ExePath)) {
				WindowDetails.AddProcessToExcludeFromFreeze("powerpnt");
			} else {
				ExePath = null;
			}
		}

		public PowerpointDestination() {
		}

		public PowerpointDestination(string presentationName) {
			_presentationName = presentationName;
		}

		public override string Designation => "Powerpoint";

		public override string Description {
			get
			{
				if (_presentationName == null) {
					return "Microsoft Powerpoint";
				}
				return _presentationName;
			}
		}

		public override int Priority => 4;

		public override bool IsDynamic => true;

		public override bool IsActive => base.IsActive && ExePath != null;

		public override Image DisplayIcon {
			get {
				if (!string.IsNullOrEmpty(_presentationName)) {
					return PluginUtils.GetCachedExeIcon(ExePath, IconPresentation);
				}

				return PluginUtils.GetCachedExeIcon(ExePath, IconApplication);
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			foreach (string presentationName in PowerpointExporter.GetPowerpointPresentations()) {
				yield return new PowerpointDestination(presentationName);
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
            base.SetDefaults(surface);
            ExportInformation exportInformation = new ExportInformation(Designation, Description);
			string tmpFile = captureDetails.Filename;
			Size imageSize = Size.Empty;
			if (tmpFile == null || surface.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$")) {
				tmpFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
				imageSize = surface.Image.Size;
			}
			if (_presentationName != null) {
				exportInformation.ExportMade = PowerpointExporter.ExportToPresentation(_presentationName, tmpFile, imageSize, captureDetails.Title);
			} else {
				if (!manuallyInitiated) {
					var presentations = PowerpointExporter.GetPowerpointPresentations();
					if (presentations != null && presentations.Count > 0) {
						var destinations = new List<IDestination> {new PowerpointDestination()};
						foreach (string presentation in presentations) {
							destinations.Add(new PowerpointDestination(presentation));
						}
						// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
						return ShowPickerMenu(false, surface, captureDetails, destinations);
					}
				} else if (!exportInformation.ExportMade) {
					exportInformation.ExportMade = PowerpointExporter.InsertIntoNewPresentation(tmpFile, imageSize, captureDetails.Title);
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
