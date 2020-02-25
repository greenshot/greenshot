/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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
using GreenshotPlugin.Core;
using Windows.Storage.Streams;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Ocr;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotWin10Plugin
{
	/// <summary>
	/// This uses the OcrEngine from Windows 10 to perform OCR on the captured image.
	/// </summary>
	public class Win10OcrDestination : AbstractDestination, IOcrProvider
	{
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Win10OcrDestination));

		public override string Designation { get; } = "WIN10OCR";
		public override string Description { get; } = "Windows 10 OCR";

		/// <summary>
		/// Icon for the OCR function, the icon was found via: http://help4windows.com/windows_8_imageres_dll.shtml
		/// </summary>
		public override Image DisplayIcon => PluginUtils.GetCachedExeIcon(FilenameHelper.FillCmdVariables(@"%windir%\system32\imageres.dll"), 97);

		/// <summary>
		/// Constructor, this is only debug information
		/// </summary>
		public Win10OcrDestination()
		{
			// Set this as IOcrProvider
			SimpleServiceProvider.Current.AddService<IOcrProvider>(this);
			var languages = OcrEngine.AvailableRecognizerLanguages;
			foreach (var language in languages)
			{
				Log.DebugFormat("Found language {0} {1}", language.NativeName, language.LanguageTag);
			}
		}

        /// <summary>
        /// Scan the surface bitmap for text, and get the OcrResult
        /// </summary>
        /// <param name="surface">ISurface</param>
        /// <returns>OcrResult sync</returns>
        public Task<OcrInformation> DoOcrAsync(ISurface surface)
        {
            using var imageStream = new MemoryStream();
            ImageOutput.SaveToStream(surface, imageStream, new SurfaceOutputSettings());
            imageStream.Position = 0;
            var randomAccessStream = imageStream.AsRandomAccessStream();
            return DoOcrAsync(randomAccessStream);
        }

		/// <summary>
		/// Scan the Image for text, and get the OcrResult
		/// </summary>
		/// <param name="image">Image</param>
		/// <returns>OcrResult sync</returns>
		public async Task<OcrInformation> DoOcrAsync(Image image)
        {
            OcrInformation result;
            using (var imageStream = new MemoryStream())
            {
                ImageOutput.SaveToStream(image, null, imageStream, new SurfaceOutputSettings());
                imageStream.Position = 0;
                var randomAccessStream = imageStream.AsRandomAccessStream();

                result = await DoOcrAsync(randomAccessStream);
			}
			return result;
        }

		/// <summary>
		/// Scan the surface bitmap for text, and get the OcrResult
		/// </summary>
		/// <param name="randomAccessStream">IRandomAccessStream</param>
		/// <returns>OcrResult sync</returns>
		public async Task<OcrInformation> DoOcrAsync(IRandomAccessStream randomAccessStream)
        {
            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (ocrEngine is null)
            {
                return null;
            }
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
			var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

			var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);

            return CreateOcrInformation(ocrResult);
		}

        /// <summary>
        /// Create the OcrInformation
        /// </summary>
        /// <param name="ocrResult">OcrResult</param>
        /// <returns>OcrInformation</returns>
        private OcrInformation CreateOcrInformation(OcrResult ocrResult)
        {
            var result = new OcrInformation();

            foreach (var ocrLine in ocrResult.Lines)
            {
                var line = new Line(ocrLine.Words.Count)
                {
                    Text = ocrLine.Text
                };

                result.Lines.Add(line);

                for (var index = 0; index < ocrLine.Words.Count; index++)
                {
                    var ocrWord = ocrLine.Words[index];
                    var location = new Rectangle((int)ocrWord.BoundingRect.X, (int)ocrWord.BoundingRect.Y,
                        (int)ocrWord.BoundingRect.Width, (int)ocrWord.BoundingRect.Height);

                    var word = line.Words[index];
                    word.Text = ocrWord.Text;
                    word.Bounds = location;
                }
            }

            return result;
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
				var ocrResult = Task.Run(async () => await DoOcrAsync(surface)).Result;

				// Check if we found text
				if (!string.IsNullOrWhiteSpace(ocrResult.Text))
				{
					// Place the OCR text on the
					ClipboardHelper.SetClipboardData(ocrResult.Text);
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
