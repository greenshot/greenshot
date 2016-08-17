/*
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
	public class ExcelDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExcelDestination));
		private const int ICON_APPLICATION = 0;
		private const int ICON_WORKBOOK = 1;
		private static readonly string exePath = null;
		private readonly string workbookName = null;

		static ExcelDestination() {
			exePath = PluginUtils.GetExePath("EXCEL.EXE");
			if (exePath != null && File.Exists(exePath)) {
				WindowDetails.AddProcessToExcludeFromFreeze("excel");
			} else {
				exePath = null;
			}
		}

		public ExcelDestination() {
		}

		public ExcelDestination(string workbookName) {
			this.workbookName = workbookName;
		}

		public override string Designation {
			get {
				return "Excel";
			}
		}

		public override string Description {
			get {
				if (workbookName == null) {
					return "Microsoft Excel";
				} else {
					return workbookName;
				}
			}
		}

		public override int Priority {
			get {
				return 5;
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
				if (!string.IsNullOrEmpty(workbookName)) {
					return PluginUtils.GetCachedExeIcon(exePath, ICON_WORKBOOK);
				}
				return PluginUtils.GetCachedExeIcon(exePath, ICON_APPLICATION);
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			foreach (string workbookName in ExcelExporter.GetWorkbooks()) {
				yield return new ExcelDestination(workbookName);
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(Designation, Description);
			bool createdFile = false;
			string imageFile = captureDetails.Filename;
			if (imageFile == null || surface.Modified || !Regex.IsMatch(imageFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$")) {
				imageFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
				createdFile = true;
			}
			if (workbookName != null) {
				ExcelExporter.InsertIntoExistingWorkbook(workbookName, imageFile, surface.Image.Size);
			} else {
				ExcelExporter.InsertIntoNewWorkbook(imageFile, surface.Image.Size);
			}
			exportInformation.ExportMade = true;
			ProcessExport(exportInformation, surface);
			// Cleanup imageFile if we created it here, so less tmp-files are generated and left
			if (createdFile) {
				ImageOutput.DeleteNamedTmpFile(imageFile);
			}
			return exportInformation;
		}
	}
}
