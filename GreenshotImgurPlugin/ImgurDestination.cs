/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using GreenshotPlugin.Core;
using GreenshotPlugin.Windows;

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;
using GreenshotPlugin.Interfaces.Plugin;
using GreenshotPlugin.Extensions;
namespace GreenshotImgurPlugin
{
	[Destination(ImgurDesignation)]
	public sealed class ImgurDestination : AbstractDestination
	{
		private const string ImgurDesignation = "Imgur";
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(ImgurDestination));
		private static readonly BitmapSource ImgurIcon;

		static ImgurDestination()
		{
			var resources = new ComponentResourceManager(typeof(ImgurPlugin));
			using (var imgurImage = (Bitmap) resources.GetObject("Imgur"))
			{
				ImgurIcon = imgurImage.ToBitmapSource();
			}

		}

		[Import]
		private IImgurConfiguration ImgurConfiguration
		{
			get;
			set;
		}

		[Import]
		private IImgurLanguage ImgurLanguage
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
			Designation = ImgurDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Text = ImgurLanguage.UploadMenuItem;
			Icon = ImgurIcon;
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = ImgurDesignation,
				SourceType = SourceTypes.Destination,
				Text = string.Format(ImgurLanguage.UploadSuccess, ImgurDesignation)
			};
			var outputSettings = new SurfaceOutputSettings(ImgurConfiguration.UploadFormat, ImgurConfiguration.UploadJpegQuality, ImgurConfiguration.UploadReduceColors);
			try
			{
				string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(ImgurConfiguration.FilenamePattern, ImgurConfiguration.UploadFormat, capture.CaptureDetails));
				var imgurInfo = await PleaseWaitWindow.CreateAndShowAsync(Designation, ImgurLanguage.CommunicationWait, async (progress, pleaseWaitToken) =>
				{
					return await ImgurUtils.UploadToImgurAsync(capture, outputSettings, capture.CaptureDetails.Title, filename, progress, pleaseWaitToken);
				}, token);

				if (imgurInfo != null)
				{
					returnValue.ImageLocation = imgurInfo.SmallSquare;
					if (ImgurConfiguration.UsePageLink)
					{
						if (imgurInfo.Page != null)
						{
							returnValue.ImageLocation = new Uri(imgurInfo.Page.AbsoluteUri);
						}
					}
					else if (imgurInfo.Original != null)
					{
						returnValue.ImageLocation = new Uri(imgurInfo.Original.AbsoluteUri);
					}
					try
					{
						if (ImgurConfiguration.CopyUrlToClipboard && returnValue.ImageLocation != null)
						{
							ClipboardHelper.SetClipboardData(returnValue.ImageLocation);
						}
					}
					catch (Exception ex)
					{
						LOG.Error("Can't write to clipboard: ", ex);
					}
				}
			}
			catch (TaskCanceledException tcEx)
			{
				returnValue.Text = string.Format(ImgurLanguage.UploadFailure, ImgurDesignation);
                returnValue.NotificationType = NotificationTypes.Cancel;
				returnValue.ErrorText = tcEx.Message;
				LOG.Information(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = string.Format(ImgurLanguage.UploadFailure, ImgurDesignation);
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				LOG.Warning(e, "Upload to Imgur gave an exception");
				MessageBox.Show(ImgurLanguage.UploadFailure + " " + e.Message, ImgurDesignation, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			return returnValue;
        }
	}
}