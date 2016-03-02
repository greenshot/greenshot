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
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;

namespace Greenshot.Addon.WindowsOcr
{
	[Destination(OcrDesignation), PartNotDiscoverable]
	public sealed class OcrDestination : AbstractDestination
	{
		private const string OcrDesignation = "Ocr";
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(OcrDestination));
		private static readonly System.Windows.Media.Imaging.BitmapSource OcrIcon;

		static OcrDestination()
		{
			string exePath = PluginUtils.GetExePath("EXPLORER.EXE");
			if (exePath != null && File.Exists(exePath))
			{
				OcrIcon = PluginUtils.GetCachedExeIcon(exePath, 0).ToBitmapSource();
			}
		}

		[Import]
		private IGreenshotHost GreenshotHost
		{
			get;
			set;
		}

		/// <summary>
		/// Setup
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
			Designation = OcrDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Text = OcrDesignation;
			Icon = OcrIcon;

			var languages = OcrEngine.AvailableRecognizerLanguages;
			foreach (var language in languages)
			{
				Log.Information("Found language {0} {1}", language.NativeName, language.LanguageTag);
			}
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = OcrDesignation,
				SourceType = SourceTypes.Destination,
				Text = OcrDesignation
			};

			try
			{
				var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
				using (var imageStream = new MemoryStream())
				{
					ImageOutput.SaveToStream(capture, imageStream, new SurfaceOutputSettings());
					imageStream.Position = 0;

					var decoder = await BitmapDecoder.CreateAsync(imageStream.AsRandomAccessStream());
					var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

					var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
					ClipboardHelper.SetClipboardData(ocrResult.Text);
				}				
			}
			catch (TaskCanceledException tcEx)
			{
				returnValue.Text = "Share cancelled.";
				returnValue.NotificationType = NotificationTypes.Cancel;
				returnValue.ErrorText = tcEx.Message;
				Log.Information(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = "Share failed.";
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				Log.Warning(e, "Share export failed");
			}
			return returnValue;
        }
	}
}