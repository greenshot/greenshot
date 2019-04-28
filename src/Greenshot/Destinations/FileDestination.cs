// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Addons.Resources;
using Greenshot.Gfx;

namespace Greenshot.Destinations
{
    /// <summary>
    ///     Description of FileSaveAsDestination.
    /// </summary>
    [Destination("FileNoDialog", DestinationOrder.FileNoDialog)]
    public class FileDestination : AbstractDestination
	{
	    private readonly ExportNotification _exportNotification;
	    private static readonly LogSource Log = new LogSource();

	    public FileDestination(
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification) : base(coreConfiguration, greenshotLanguage)
	    {
	        _exportNotification = exportNotification;
	    }

	    public override string Description => GreenshotLanguage.QuicksettingsDestinationFile;

		public override Keys EditorShortcutKeys => Keys.Control | Keys.S;

		public override IBitmapWithNativeSupport DisplayIcon => GreenshotResources.Instance.GetBitmap("Save.Image");

	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			bool outputMade;
			bool overwrite;
			string fullPath;
			// Get output settings from the configuration
			var outputSettings = new SurfaceOutputSettings(CoreConfiguration);

			if (captureDetails?.Filename != null)
			{
				// As we save a pre-selected file, allow to overwrite.
				overwrite = true;
				Log.Info().WriteLine("Using previous filename");
				fullPath = captureDetails.Filename;
				outputSettings.Format = ImageOutput.FormatForFilename(fullPath);
			}
			else
			{
				fullPath = CreateNewFilename(captureDetails);
				// As we generate a file, the configuration tells us if we allow to overwrite
				overwrite = CoreConfiguration.OutputFileAllowOverwrite;
			}
			if (CoreConfiguration.OutputFilePromptQuality)
			{
				var qualityDialog = new QualityDialog(outputSettings, CoreConfiguration, GreenshotLanguage);
				qualityDialog.ShowDialog();
			}

			// Catching any exception to prevent that the user can't write in the directory.
			// This is done for e.g. bugs #2974608, #2963943, #2816163, #2795317, #2789218, #3004642
			try
			{
				ImageOutput.Save(surface, fullPath, overwrite, outputSettings, CoreConfiguration.OutputFileCopyPathToClipboard);
				outputMade = true;
			}
			catch (ArgumentException ex1)
			{
				// Our generated filename exists, display 'save-as'
				Log.Info().WriteLine("Not overwriting: {0}", ex1.Message);
				// when we don't allow to overwrite present a new SaveWithDialog
				fullPath = ImageOutput.SaveWithDialog(surface, captureDetails);
				outputMade = fullPath != null;
			}
			catch (Exception ex2)
			{
				Log.Error().WriteLine(ex2, "Error saving screenshot!");
				// Show the problem
				MessageBox.Show(GreenshotLanguage.ErrorSave, GreenshotLanguage.Error);
				// when save failed we present a SaveWithDialog
				fullPath = ImageOutput.SaveWithDialog(surface, captureDetails);
				outputMade = fullPath != null;
			}
			// Don't overwrite filename if no output is made
			if (outputMade)
			{
				exportInformation.ExportMade = true;
				exportInformation.Filepath = fullPath;
				if (captureDetails != null)
				{
					captureDetails.Filename = fullPath;
				}
			    CoreConfiguration.OutputFileAsFullpath = fullPath;
			}

		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}

		private string CreateNewFilename(ICaptureDetails captureDetails)
		{
			string fullPath = null;
			Log.Info().WriteLine("Creating new filename");

		    CoreConfiguration.ValidateAndCorrect();

            var filename = FilenameHelper.GetFilenameFromPattern(CoreConfiguration.OutputFileFilenamePattern, CoreConfiguration.OutputFileFormat, captureDetails);
			var filepath = FilenameHelper.FillVariables(CoreConfiguration.OutputFilePath, false);
			try
			{
				fullPath = Path.Combine(filepath, filename);
			}
			catch (ArgumentException)
			{
				// configured filename or path not valid, show error message...
				Log.Info().WriteLine("Generated path or filename not valid: {0}, {1}", filepath, filename);

				MessageBox.Show(GreenshotLanguage.ErrorSaveInvalidChars, GreenshotLanguage.Error);
                // TODO: offer settings?
				// ... lets get the pattern fixed....
				//	fullPath = CreateNewFilename(captureDetails);
    			// ... cancelled.
				// fullPath = null;
			}
			return fullPath;
		}
	}
}