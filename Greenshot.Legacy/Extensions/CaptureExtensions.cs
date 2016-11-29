//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Dapplo.Log;
using Greenshot.Core;
using Greenshot.Core.Configuration;
using Greenshot.Core.Gfx;
using Greenshot.Core.Interfaces;
using Greenshot.Legacy.Controls;
using Greenshot.Legacy.Utils;

namespace Greenshot.Legacy.Extensions
{
	/// <summary>
	/// Extensions for ICapture
	/// </summary>
	public static class CaptureExtensions
	{
		private static readonly LogSource Log = new LogSource();

		private static readonly IOutputConfiguration OutputConfiguration = IniConfig.Current.GetSubSection<IOutputConfiguration>();
		private static readonly ICoreTranslations CoreTranslations = LanguageLoader.Current.GetPart<ICoreTranslations>();
		/// <summary>
		///     Save with showing a dialog
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns>Path to filename</returns>
		public static string SaveWithDialog(this ICapture surface, ICaptureDetails captureDetails)
		{
			string returnValue = null;
			using (var saveImageFileDialog = new SaveImageFileDialog(OutputConfiguration, captureDetails))
			{
				var dialogResult = saveImageFileDialog.ShowDialog();
				if (dialogResult.Equals(DialogResult.OK))
				{
					try
					{
						string fileNameWithExtension = saveImageFileDialog.FileNameWithExtension;
						var outputSettings = new SurfaceOutputSettings(ImageOutput.FormatForFilename(fileNameWithExtension));
						if (OutputConfiguration.OutputFilePromptQuality)
						{
							var qualityDialog = new QualityDialog(outputSettings);
							qualityDialog.ShowDialog();
						}
						// TODO: For now we always overwrite, should be changed
						returnValue = surface.Save(fileNameWithExtension, true, outputSettings);
						if (OutputConfiguration.OutputFileCopyPathToClipboard)
						{
							ClipboardHelper.SetClipboardData(returnValue);
						}
					}
					catch (ExternalException)
					{
						MessageBox.Show(string.Format(CoreTranslations.ErrorNowriteaccess, saveImageFileDialog.FileName).Replace(@"\\", @"\"), CoreTranslations.Error);
					}
				}
			}
			return returnValue;
		}


		/// <summary>
		///     Create a tmpfile which has the name like in the configured pattern.
		///     Used e.g. by the email export
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <param name="outputSettings"></param>
		/// <returns>Path to image file</returns>
		public static string SaveNamedTmpFile(this ICapture surface, ICaptureDetails captureDetails, SurfaceOutputSettings outputSettings)
		{
			string pattern = OutputConfiguration.OutputFileFilenamePattern;
			if (string.IsNullOrEmpty(pattern?.Trim()))
			{
				pattern = "greenshot ${capturetime}";
			}
			string filename = FilenameHelper.GetFilenameFromPattern(pattern, outputSettings.Format, captureDetails);
			// Prevent problems with "other characters", which causes a problem in e.g. Outlook 2007 or break our HTML
			filename = Regex.Replace(filename, @"[^\d\w\.]", "_");
			// Remove multiple "_"
			filename = Regex.Replace(filename, @"_+", "_");
			string tmpFile = Path.Combine(Path.GetTempPath(), filename);

			Log.Debug().WriteLine("Creating TMP File: {0}", tmpFile);

			// Catching any exception to prevent that the user can't write in the directory.
			// This is done for e.g. bugs #2974608, #2963943, #2816163, #2795317, #2789218
			try
			{
				tmpFile = surface.Save(tmpFile, true, outputSettings);
				ImageOutput.AddTmpFile(tmpFile);
			}
			catch (Exception e)
			{
				// Show the problem
				MessageBox.Show(e.Message, "Error");
				// when save failed we present a SaveWithDialog
				tmpFile = SaveWithDialog(surface, captureDetails);
			}
			return tmpFile;
		}


		/// <summary>
		///     Saves image to specific path with specified quality
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="fullPath"></param>
		/// <param name="allowOverwrite"></param>
		/// <param name="outputSettings"></param>
		/// <returns>Full save path</returns>
		public static string Save(this ICapture surface, string fullPath, bool allowOverwrite, SurfaceOutputSettings outputSettings)
		{
			fullPath = CheckFullPath(fullPath, allowOverwrite);
			Log.Debug().WriteLine("Saving surface to {0}", fullPath);
			// Create the stream and call SaveToStream
			using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
			{
				surface.SaveToStream(stream, outputSettings);
			}

			return fullPath;
		}

		/// <summary>
		///     Saves ISurface to stream with specified output settings
		/// </summary>
		/// <param name="capture">ICapture to save</param>
		/// <param name="stream">Stream to save to</param>
		/// <param name="outputSettings">SurfaceOutputSettings</param>
		public static void SaveToStream(this ICapture capture, Stream stream, SurfaceOutputSettings outputSettings)
		{
			Image imageToSave;
			bool disposeImage = CreateImageFromCapture(capture, outputSettings, out imageToSave);
			ImageOutput.SaveToStream(imageToSave, capture, stream, outputSettings);
			// cleanup if needed
			if (disposeImage)
			{
				imageToSave?.Dispose();
			}
		}

		/// <summary>
		///     Create an image from a surface with the settings from the output settings applied
		/// </summary>
		/// <param name="capture"></param>
		/// <param name="outputSettings"></param>
		/// <param name="imageToSave"></param>
		/// <returns>true if the image must be disposed</returns>
		public static bool CreateImageFromCapture(this ICapture capture, SurfaceOutputSettings outputSettings, out Image imageToSave)
		{
			bool disposeImage = false;

			// TODO: Check no elements
			if ((outputSettings.Format == OutputFormat.greenshot) || outputSettings.SaveBackgroundOnly)
			{
				// We save the image of the surface, this should not be disposed
				imageToSave = capture.Image;
			}
			else
			{
				// We create the export image of the surface to save
				imageToSave = capture.GetImageForExport();
				disposeImage = true;
			}

			// The following block of modifications should be skipped when saving the greenshot format, no effects or otherwise!
			if (outputSettings.Format == OutputFormat.greenshot)
			{
				return disposeImage;
			}
			Image tmpImage;
			if ((outputSettings.Effects != null) && (outputSettings.Effects.Count > 0))
			{
				// apply effects, if there are any
				using (Matrix matrix = new Matrix())
				{
					tmpImage = ImageHelper.ApplyEffects(imageToSave, outputSettings.Effects, matrix);
				}
				if (tmpImage != null)
				{
					if (disposeImage)
					{
						imageToSave.Dispose();
					}
					imageToSave = tmpImage;
					disposeImage = true;
				}
			}

			// check for color reduction, forced or automatically, only when the DisableReduceColors is false 
			if (outputSettings.DisableReduceColors || (!OutputConfiguration.OutputFileAutoReduceColors && !outputSettings.ReduceColors))
			{
				return disposeImage;
			}
			bool isAlpha = Image.IsAlphaPixelFormat(imageToSave.PixelFormat);
			if (outputSettings.ReduceColors || (!isAlpha && OutputConfiguration.OutputFileAutoReduceColors))
			{
				using (var quantizer = new WuQuantizer((Bitmap)imageToSave))
				{
					int colorCount = quantizer.GetColorCount();
					Log.Info().WriteLine("Image with format {0} has {1} colors", imageToSave.PixelFormat, colorCount);
					if (!outputSettings.ReduceColors && (colorCount >= 256))
					{
						return disposeImage;
					}
					try
					{
						Log.Info().WriteLine("Reducing colors on bitmap to 256.");
						tmpImage = quantizer.GetQuantizedImage(OutputConfiguration.OutputFileReduceColorsTo);
						if (disposeImage)
						{
							imageToSave.Dispose();
						}
						imageToSave = tmpImage;
						// Make sure the "new" image is disposed
						disposeImage = true;
					}
					catch (Exception e)
					{
						Log.Warn().WriteLine("Error occurred while Quantizing the image, ignoring and using original. Error: ", e);
					}
				}
			}
			else if (isAlpha && !outputSettings.ReduceColors)
			{
				Log.Info().WriteLine("Skipping 'optional' color reduction as the image has alpha");
			}
			return disposeImage;
		}


		/// <summary>
		///     Check the supplied path, an if overwrite is neeeded
		/// </summary>
		/// <param name="fullPath"></param>
		/// <param name="allowOverwrite"></param>
		/// <returns></returns>
		private static string CheckFullPath(string fullPath, bool allowOverwrite)
		{
			fullPath = FilenameHelper.MakeFQFilenameSafe(fullPath);
			string path = Path.GetDirectoryName(fullPath);

			// check whether path exists - if not create it
			if (path != null)
			{
				DirectoryInfo di = new DirectoryInfo(path);
				if (!di.Exists)
				{
					Directory.CreateDirectory(di.FullName);
				}
			}

			if (!allowOverwrite && File.Exists(fullPath))
			{
				ArgumentException throwingException = new ArgumentException("File '" + fullPath + "' already exists.");
				throwingException.Data.Add("fullPath", fullPath);
				throw throwingException;
			}

			return fullPath;
		}


		/// <summary>
		///     Helper method to create a temp image file
		/// </summary>
		/// <param name="surface"></param>
		/// <param name="outputSettings"></param>
		/// <param name="destinationPath"></param>
		/// <returns></returns>
		public static string SaveToTmpFile(this ICapture surface, SurfaceOutputSettings outputSettings, string destinationPath)
		{
			string tmpFile = Path.GetRandomFileName() + "." + outputSettings.Format;
			// Prevent problems with "other characters", which could cause problems
			tmpFile = Regex.Replace(tmpFile, @"[^\d\w\.]", "");
			if (destinationPath == null)
			{
				destinationPath = Path.GetTempPath();
			}
			string tmpFullPath = Path.Combine(destinationPath, tmpFile);
			Log.Debug().WriteLine("Creating TMP File : {0}", tmpFullPath);

			try
			{
				tmpFullPath = Save(surface, tmpFullPath, true, outputSettings);
				ImageOutput.AddTmpFile(tmpFullPath);
			}
			catch (Exception)
			{
				return null;
			}
			return tmpFullPath;
		}
	}
}
