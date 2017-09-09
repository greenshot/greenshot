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

using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Dapplo.Log;
using GreenshotPlugin.Core;
using GreenshotPlugin.Gfx;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotWin10Plugin
{
	/// <summary>
	/// This uses the OcrEngine from Windows 10 to perform OCR on the captured image.
	/// </summary>
	public class Win10OcrDestination : AbstractDestination
	{
		private static readonly LogSource Log = new LogSource();

		public override string Designation { get; } = "WIN10OCR";
		public override string Description { get; } = "Windows 10 OCR";

		/// <summary>
		/// Icon for the OCR function, the icon was found via: http://help4windows.com/windows_8_imageres_dll.shtml
		/// </summary>
		public override Bitmap DisplayIcon=> PluginUtils.GetCachedExeIcon(FilenameHelper.FillCmdVariables(@"%windir%\system32\imageres.dll"), 97);

		/// <summary>
		/// Constructor, this is only debug information
		/// </summary>
		public Win10OcrDestination()
		{
			var languages = OcrEngine.AvailableRecognizerLanguages;
			foreach (var language in languages)
			{
				Log.Debug().WriteLine("Found language {0} {1}", language.NativeName, language.LanguageTag);
			}
		}

		/// <summary>
		/// Run the Windows 10 OCR engine to process the text on the captured image
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns>ExportInformation</returns>
		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			try
			{
				var text = Task.Run(async () =>
				{
					var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
					using (var imageStream = new MemoryStream())
					{
						ImageOutput.SaveToStream(surface, imageStream, new SurfaceOutputSettings());
						imageStream.Position = 0;

						var decoder = await BitmapDecoder.CreateAsync(imageStream.AsRandomAccessStream());
						var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

						var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
						return ocrResult.Text;
					}
				}).Result;

				// Check if we found text
				if (!string.IsNullOrWhiteSpace(text))
				{
					// Place the OCR text on the 
					ClipboardHelper.SetClipboardData(text);
				}
				exportInformation.ExportMade = true;
			}
			catch (Exception ex)
			{
				exportInformation.ExportMade = false;
				exportInformation.ErrorMessage = ex.Message;
			}

			ProcessExport(exportInformation, surface);
			return exportInformation;

		}
	}
}
