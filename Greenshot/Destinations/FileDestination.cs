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

using System;
using System.Drawing;
using System.IO;
using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.IniFile;
using GreenshotPlugin.Controls;
using log4net;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;

namespace Greenshot.Destinations {
	/// <summary>
	/// Description of FileSaveAsDestination.
	/// </summary>
	public class FileDestination : AbstractDestination {
		private static ILog LOG = LogManager.GetLogger(typeof(FileDestination));
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

		public override System.Windows.Forms.Keys EditorShortcutKeys {
			get {
				return System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S;
			}
		}
		
		public override Image DisplayIcon {
			get {
				return GreenshotResources.getImage("Save.Image");
			}
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken)) {
			ExportInformation exportInformation = new ExportInformation(Designation, Description);
			bool outputMade;
			bool overwrite;
			string fullPath;
			// Get output settings from the configuration
			SurfaceOutputSettings outputSettings = new SurfaceOutputSettings();

			if (captureDetails != null && captureDetails.Filename != null) {
				// As we save a pre-selected file, allow to overwrite.
				overwrite = true;
				LOG.InfoFormat("Using previous filename");
				fullPath = captureDetails.Filename;
				outputSettings.Format = ImageOutput.FormatForFilename(fullPath);
			} else {
				fullPath = CreateNewFilename(captureDetails);
				// As we generate a file, the configuration tells us if we allow to overwrite
				overwrite = conf.OutputFileAllowOverwrite;
			}
			if (conf.OutputFilePromptQuality) {
				QualityDialog qualityDialog = new QualityDialog(outputSettings);
				qualityDialog.ShowDialog();
			}

			// Catching any exception to prevent that the user can't write in the directory.
			// This is done for e.g. bugs #2974608, #2963943, #2816163, #2795317, #2789218, #3004642
			try {
				TaskScheduler scheduler = TaskScheduler.FromCurrentSynchronizationContext();
				Task<string> task = Task.Factory.StartNew(() => ImageOutput.Save(surface, fullPath, overwrite, outputSettings), default(CancellationToken), TaskCreationOptions.None, scheduler);
				fullPath = await task;
				if (conf.OutputFileCopyPathToClipboard)
				{
					ClipboardHelper.SetClipboardData(fullPath);
				}
				outputMade = true;
			} catch (ArgumentException ex1) {
				// Our generated filename exists, display 'save-as'
				LOG.InfoFormat("Not overwriting: {0}", ex1.Message);
				// when we don't allow to overwrite present a new SaveWithDialog
				fullPath = ImageOutput.SaveWithDialog(surface, captureDetails);
				outputMade = (fullPath != null);
			} catch (Exception ex2) {
				LOG.Error("Error saving screenshot!", ex2);
				// Show the problem
				MessageBox.Show(Designation, Language.GetString(LangKey.error_save) + " " + ex2.Message, MessageBoxButton.OK, MessageBoxImage.Error);
				// when save failed we present a SaveWithDialog
				fullPath = ImageOutput.SaveWithDialog(surface, captureDetails);
				outputMade = (fullPath != null);
			}
			// Don't overwrite filename if no output is made
			if (outputMade) {
				exportInformation.ExportMade = outputMade;
				exportInformation.Filepath = fullPath;
				captureDetails.Filename = fullPath;
				conf.OutputFileAsFullpath = fullPath;
			}

			ProcessExport(exportInformation, surface);
			return exportInformation;
		}

		private static string CreateNewFilename(ICaptureDetails captureDetails) {
			string fullPath;
			LOG.InfoFormat("Creating new filename");
			string pattern = conf.OutputFileFilenamePattern;
			if (string.IsNullOrEmpty(pattern)) {
				pattern = "greenshot ${capturetime}";
			}
			string filename = FilenameHelper.GetFilenameFromPattern(pattern, conf.OutputFileFormat, captureDetails);
			string filepath = FilenameHelper.FillVariables(conf.OutputFilePath, false);
			try {
				fullPath = Path.Combine(filepath, filename);
			} catch (ArgumentException) {
				// configured filename or path not valid, show error message...
				LOG.InfoFormat("Generated path or filename not valid: {0}, {1}", filepath, filename);

				MessageBox.Show(Language.GetString(LangKey.error_save_invalid_chars), Language.GetString(LangKey.error), MessageBoxButton.OK, MessageBoxImage.Error);
				// ... lets get the pattern fixed....
				var dialogResult = new SettingsForm().ShowDialog();
				if (dialogResult == System.Windows.Forms.DialogResult.OK) { 
					// ... OK -> then try again:
					fullPath = CreateNewFilename(captureDetails);
				} else { 
					// ... cancelled.
					fullPath = null;
				}
				
			}
			return fullPath;
		}
	}
}
