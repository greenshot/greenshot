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
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Dapplo.Log;
using Dapplo.Windows.Clipboard;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Gfx;

namespace Greenshot.Addon.Win10
{
	/// <summary>
	/// This uses the OcrEngine from Windows 10 to perform OCR on the captured image.
	/// </summary>
	[Destination("WIN10OCR")]
	public class Win10OcrDestination : AbstractDestination
	{
	    private readonly ExportNotification _exportNotification;
	    private static readonly LogSource Log = new LogSource();

        /// <inheritdoc />
        public override string Description { get; } = "Windows 10 OCR";

		/// <summary>
		/// Icon for the OCR function, the icon was found via: http://help4windows.com/windows_8_imageres_dll.shtml
		/// </summary>
		public override IBitmapWithNativeSupport DisplayIcon=> PluginUtils.GetCachedExeIcon(FilenameHelper.FillCmdVariables(@"%windir%\system32\imageres.dll"), 97);

		/// <summary>
		/// Constructor, this is only debug information
		/// </summary>
		public Win10OcrDestination(
		    ICoreConfiguration coreConfiguration,
		    IGreenshotLanguage greenshotLanguage,
            ExportNotification exportNotification
		) : base(coreConfiguration, greenshotLanguage)
        {
            _exportNotification = exportNotification;
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
        public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
            var exportInformation = new ExportInformation(Designation, Description);
			try
			{
			    string text;
				var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
				using (var imageStream = new MemoryStream())
				{
					ImageOutput.SaveToStream(surface, imageStream, new SurfaceOutputSettings(CoreConfiguration));
					imageStream.Position = 0;

					var decoder = await BitmapDecoder.CreateAsync(imageStream.AsRandomAccessStream());
					var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

					var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
                    // TODO: Get the lines, words, bounding rectangles
					text = ocrResult.Text;
				}

				// Check if we found text
				if (!string.IsNullOrWhiteSpace(text))
				{
				    // Place the OCR text on the clipboard
				    using (var clipboardAccessToken = ClipboardNative.Access())
				    {
				        clipboardAccessToken.ClearContents();
                        clipboardAccessToken.SetAsUnicodeString(text);
				    }
				}
				exportInformation.ExportMade = true;
			}
			catch (Exception ex)
			{
				exportInformation.ExportMade = false;
				exportInformation.ErrorMessage = ex.Message;
			}

            _exportNotification.NotifyOfExport(this, exportInformation, surface);
			return exportInformation;

		}
	}
}
