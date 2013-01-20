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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.Helpers;
using Greenshot.IniFile;

namespace Greenshot.Destinations {
	/// <summary>
	/// Description of FileWithDialog.
	/// </summary>
	public class FileWithDialogDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(FileWithDialogDestination));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		public const string DESIGNATION = "FileDialog";

		public override string Designation {
			get {
				return DESIGNATION;
			}
		}

		public override string Description {
			get {
				return Language.GetString(LangKey.settings_destination_fileas);
			}
		}

		public override int Priority {
			get {
				return 0;
			}
		}
		
		public override Keys EditorShortcutKeys {
			get {
				return Keys.Control | Keys.Shift | Keys.S;
			}
		}

		public override Image DisplayIcon {
			get {
				return GreenshotPlugin.Core.GreenshotResources.getImage("Save.Image");
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			ExportInformation exportInformation = new ExportInformation(this.Designation, this.Description);
			string savedTo = null;
			// Bug #2918756 don't overwrite path if SaveWithDialog returns null!
			savedTo = ImageOutput.SaveWithDialog(surface, captureDetails);
			if (savedTo != null) {
				exportInformation.ExportMade = true;
				exportInformation.Filepath = savedTo;
				captureDetails.Filename = savedTo;
				conf.OutputFileAsFullpath = savedTo;
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
