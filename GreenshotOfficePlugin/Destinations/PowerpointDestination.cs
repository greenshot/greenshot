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

using Greenshot.Plugin;
using GreenshotOfficePlugin.OfficeExport;
using GreenshotPlugin.Core;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace GreenshotOfficePlugin {
	/// <summary>
	/// Description of PowerpointDestination.
	/// </summary>
	public class PowerpointDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PowerpointDestination));
		private const int ICON_APPLICATION = 0;
		private const int ICON_PRESENTATION = 1;

		private static string exePath = null;
		private string presentationName = null;
		
		static PowerpointDestination() {
			exePath = PluginUtils.GetExePath("POWERPNT.EXE");
			if (exePath != null && File.Exists(exePath)) {
				WindowDetails.AddProcessToExcludeFromFreeze("powerpnt");
			} else {
				exePath = null;
			}
		}

		public PowerpointDestination() {
		}

		public PowerpointDestination(string presentationName) {
			this.presentationName = presentationName;
		}

		public override string Designation {
			get {
				return "Powerpoint";
			}
		}

		public override string Description {
			get {
				if (presentationName == null) {
					return "Microsoft Powerpoint";
				} else {
					return presentationName;
				}
			}
		}

		public override int Priority {
			get {
				return 4;
			}
		}
		
		public override bool isDynamic {
			get {
				return true;
			}
		}

		public override bool isActive {
			get {
				return base.isActive && exePath != null;
			}
		}

		public override Image DisplayIcon {
			get {
				if (!string.IsNullOrEmpty(presentationName)) {
					return PluginUtils.GetCachedExeIcon(exePath, ICON_PRESENTATION);
				}

				return PluginUtils.GetCachedExeIcon(exePath, ICON_APPLICATION);
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			foreach (string presentationName in PowerpointExporter.GetPowerpointPresentations()) {
				yield return new PowerpointDestination(presentationName);
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			string tmpFile = captureDetails.Filename;
			Size imageSize = Size.Empty;
			if (tmpFile == null || surface.Modified || !Regex.IsMatch(tmpFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$")) {
				tmpFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
				imageSize = surface.Image.Size;
			}
			if (presentationName != null) {
				exportInformation.ExportMade = PowerpointExporter.ExportToPresentation(presentationName, tmpFile, imageSize, captureDetails.Title);
			} else {
				if (!manuallyInitiated) {
					bool initialValue = false;
					IList<IDestination> destinations = new List<IDestination>();
					foreach (var presentation in PowerpointExporter.GetPowerpointPresentations()) {
						if (!initialValue) {
							destinations.Add(new PowerpointDestination());
							initialValue = true;
						}
						destinations.Add(new PowerpointDestination(presentation));
					}
					if (destinations.Count > 0) {
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
