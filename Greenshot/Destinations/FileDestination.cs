/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using Dapplo.Utils;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Controls;
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;
using Greenshot.Forms;
using Dapplo.Log;
using MahApps.Metro.IconPacks;

namespace Greenshot.Destinations
{
	/// <summary>
	/// Description of FileDestination.
	/// </summary>
	[Destination(FileDesignation, 3)]
	public sealed class FileDestination : AbstractDestination
	{
		private const string FileDesignation = "File";
		private static readonly LogSource Log = new LogSource();

		[Import]
		private ICoreConfiguration CoreConfiguration
		{
			get;
			set;
		}

		[Import]
		private IGreenshotLanguage GreenshotLanguage
		{
			get;
			set;
		}

		protected override void Initialize()
		{
			base.Initialize();
			Text = GreenshotLanguage.SettingsDestinationFile;
			Designation = FileDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Icon = new PackIconModern
			{
				Kind = PackIconModernKind.Save
			};
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = FileDesignation,
				SourceType = SourceTypes.Destination,
				Text = Text
			};

			bool overwrite;
			string fullPath;
			// Get output settings from the configuration
			var outputSettings = new SurfaceOutputSettings();

			if (capture.CaptureDetails?.Filename != null)
			{
				// As we save a pre-selected file, allow to overwrite.
				overwrite = true;
				Log.Info().WriteLine("Using previous filename");
				fullPath = capture.CaptureDetails.Filename;
				outputSettings.Format = ImageOutput.FormatForFilename(fullPath);
			}
			else
			{
				fullPath = CreateNewFilename(capture.CaptureDetails);
				// As we generate a file, the configuration tells us if we allow to overwrite
				overwrite = CoreConfiguration.OutputFileAllowOverwrite;
			}
			if (CoreConfiguration.OutputFilePromptQuality)
			{
				var qualityDialog = new QualityDialog(outputSettings);
				qualityDialog.ShowDialog();
			}

			// Catching any exception to prevent that the user can't write in the directory.
			// This is done for e.g. bugs #2974608, #2963943, #2816163, #2795317, #2789218, #3004642
			try
			{
				var outputPath = fullPath;
				await UiContext.RunOn(() => fullPath = ImageOutput.Save(capture, outputPath, overwrite, outputSettings), token);
			}
			catch (ArgumentException ex1)
			{
				// Our generated filename exists, display 'save-as'
				Log.Info().WriteLine("Not overwriting: {0}", ex1.Message);
				// when we don't allow to overwrite present a new SaveWithDialog
				fullPath = ImageOutput.SaveWithDialog(capture, capture.CaptureDetails);
			}
			catch (Exception e)
			{
				Log.Error().WriteLine(e, "Save file failed");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				returnValue.Text = GreenshotLanguage.ErrorSave;
            }
			if (!string.IsNullOrEmpty(fullPath))
			{
				capture.CaptureDetails.Filename = fullPath;
                returnValue.ImageLocation = new Uri("file://" + fullPath);
				if (CoreConfiguration.OutputFileCopyPathToClipboard)
				{
					ClipboardHelper.SetClipboardData(fullPath);
				}

			}
			return returnValue;
		}

		private string CreateNewFilename(ICaptureDetails captureDetails)
		{
			string fullPath;
			Log.Info().WriteLine("Creating new filename");
			string pattern = CoreConfiguration.OutputFileFilenamePattern;
			if (string.IsNullOrEmpty(pattern))
			{
				pattern = "greenshot ${capturetime}";
			}
			string filename = FilenameHelper.GetFilenameFromPattern(pattern, CoreConfiguration.OutputFileFormat, captureDetails);
			string filepath = FilenameHelper.FillVariables(CoreConfiguration.OutputFilePath, false);
			try
			{
				fullPath = Path.Combine(filepath, filename);
			}
			catch (ArgumentException)
			{
				// configured filename or path not valid, show error message...
				Log.Info().WriteLine("Generated path or filename not valid: {0}, {1}", filepath, filename);

				MessageBox.Show(GreenshotLanguage.ErrorSaveInvalidChars, GreenshotLanguage.Error, MessageBoxButton.OK, MessageBoxImage.Error);
				// ... lets get the pattern fixed....
				var dialogResult = new SettingsForm().ShowDialog();
				if (dialogResult == System.Windows.Forms.DialogResult.OK)
				{
					// ... OK -> then try again:
					fullPath = CreateNewFilename(captureDetails);
				}
				else
				{
					// ... cancelled.
					fullPath = null;
				}
			}
			return fullPath;
		}
	}
}