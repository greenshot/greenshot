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

using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.Helpers;
using Greenshot.IniFile;

namespace Greenshot.Destinations {
	/// <summary>
	/// Description of FileSaveAsDestination.
	/// </summary>
	public class FileDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(FileDestination));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		public const string DESIGNATION = "FileNoDialog";

		public override string Designation {
			get {
				return DESIGNATION;
			}
		}

		public override string Description {
			get {
				return Language.GetString(LangKey.quicksettings_destination_file);
			}
		}

		public override int Priority {
			get {
				return 0;
			}
		}

		public override Keys EditorShortcutKeys {
			get {
				return Keys.Control | Keys.S;
			}
		}
		
		public override Image DisplayIcon {
			get {
				return GreenshotPlugin.Core.GreenshotResources.getImage("Save.Image");
			}
		}

		public override bool ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			bool outputMade = false;
			string pattern = conf.OutputFileFilenamePattern;
			if (string.IsNullOrEmpty(pattern)) {
				pattern = "greenshot ${capturetime}";
			}
			string filename = FilenameHelper.GetFilenameFromPattern(pattern, conf.OutputFileFormat, captureDetails);
			string filepath = FilenameHelper.FillVariables(conf.OutputFilePath, false);
			string fullPath = Path.Combine(filepath,filename);
			
			// Catching any exception to prevent that the user can't write in the directory.
			// This is done for e.g. bugs #2974608, #2963943, #2816163, #2795317, #2789218, #3004642
			using (Image image = surface.GetImageForExport()) {
				try {
					// TODO: For now we overwrite, but this should be fixed some time
					ImageOutput.Save(image, fullPath, true);
					outputMade = true;
				} catch (Exception e) {
					LOG.Error("Error saving screenshot!", e);
					// Show the problem
					MessageBox.Show(Language.GetString(LangKey.error_save), Language.GetString(LangKey.error));
					// when save failed we present a SaveWithDialog
					fullPath = ImageOutput.SaveWithDialog(image, captureDetails);
					outputMade = (fullPath != null);
				}
			}
			// Don't overwite filename if no output is made
			if (outputMade) {
				surface.LastSaveFullPath = fullPath;
				surface.Modified = false;
				captureDetails.Filename = fullPath;
				surface.SendMessageEvent(this, SurfaceMessageTyp.FileSaved, Language.GetFormattedString(LangKey.editor_imagesaved, surface.LastSaveFullPath));
			} else {
				surface.SendMessageEvent(this, SurfaceMessageTyp.Info, "");
			}
			return outputMade;
		}
	}
}
