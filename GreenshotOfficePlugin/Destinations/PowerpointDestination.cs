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
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.Interop.Office;
using Greenshot.IniFile;

namespace GreenshotOfficePlugin {
	/// <summary>
	/// Description of PowerpointDestination.
	/// </summary>
	public class PowerpointDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PowerpointDestination));
		private static string exePath = null;
		private static Image applicationIcon = null;
		private static Image presentationIcon = null;
		private string presentationName = null;
		
		static PowerpointDestination() {
			exePath = PluginUtils.GetExePath("POWERPNT.EXE");
			if (exePath != null && File.Exists(exePath)) {
				applicationIcon = PluginUtils.GetExeIcon(exePath, 0);
				presentationIcon = PluginUtils.GetExeIcon(exePath, 1);
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
					return presentationIcon;
				}

				return applicationIcon;
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
			if (tmpFile == null || surface.Modified) {
				using (Image image = surface.GetImageForExport()) {
					tmpFile = ImageOutput.SaveNamedTmpFile(image, captureDetails, new OutputSettings());
					imageSize = image.Size;
				}
			}
			if (presentationName != null) {
				PowerpointExporter.ExportToPresentation(presentationName, tmpFile, imageSize, captureDetails.Title);
				exportInformation.ExportMade = true;
			} else {
				if (!manuallyInitiated) {
					List<string> presentations = PowerpointExporter.GetPowerpointPresentations();
					if (presentations != null && presentations.Count > 0) {
						List<IDestination> destinations = new List<IDestination>();
						destinations.Add(new PowerpointDestination());
						foreach (string presentation in presentations) {
							destinations.Add(new PowerpointDestination(presentation));
						}
						// Return the ExportInformation from the picker without processing, as this indirectly comes from us self
						return ShowPickerMenu(false, surface, captureDetails, destinations);
					}
				} else if (!exportInformation.ExportMade) {
					PowerpointExporter.InsertIntoNewPresentation(tmpFile, imageSize, captureDetails.Title);
					exportInformation.ExportMade = true;
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
