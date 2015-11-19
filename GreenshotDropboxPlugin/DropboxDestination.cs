/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using log4net;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Destination;
using GreenshotPlugin.Interfaces.Plugin;
using GreenshotPlugin.Extensions;
namespace GreenshotDropboxPlugin
{
	[Destination(DropboxDesignation)]
	public sealed class DropboxDestination : AbstractDestination
	{
		private const string DropboxDesignation = "Dropbox";
		private static readonly ILog LOG = LogManager.GetLogger(typeof (DropboxDestination));
		private static readonly BitmapSource DropboxIcon;

		static DropboxDestination()
		{
			var resources = new ComponentResourceManager(typeof(DropboxPlugin));
			using (var dropboxImage = (Bitmap) resources.GetObject("Dropbox"))
			{
				DropboxIcon = dropboxImage.ToBitmapSource();
			}

		}

		[Import]
		private IDropboxConfiguration DropboxConfiguration
		{
			get;
			set;
		}

		[Import]
		private IDropboxLanguage DropboxLanguage
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
			Designation = DropboxDesignation;
			Export = async (caller, capture, token) => await ExportCaptureAsync(capture, token);
			Text = DropboxLanguage.UploadMenuItem;
			Icon = DropboxIcon;
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = DropboxDesignation,
				SourceType = SourceTypes.Destination,
				Text = string.Format(DropboxLanguage.UploadSuccess, DropboxDesignation)
			};
			var outputSettings = new SurfaceOutputSettings(DropboxConfiguration.UploadFormat, DropboxConfiguration.UploadJpegQuality, false);
			try
			{
				var url = await PleaseWaitWindow.CreateAndShowAsync(Designation, DropboxLanguage.CommunicationWait, async (progress, pleaseWaitToken) =>
				{
					string filename = Path.GetFileName(FilenameHelper.GetFilename(DropboxConfiguration.UploadFormat, capture.CaptureDetails));
					using (var stream = new MemoryStream())
					{
						ImageOutput.SaveToStream(capture, stream, outputSettings);
						stream.Position = 0;
						using (var uploadStream = new ProgressStream(stream, progress))
						{
							using (var streamContent = new StreamContent(uploadStream))
							{
								streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
								return await DropboxUtils.UploadToDropbox(streamContent, filename);
							}
						}
					}
				}, token);

				if (url != null)
				{
					returnValue.ImageLocation = new Uri(url);
					if (DropboxConfiguration.AfterUploadLinkToClipBoard)
					{
						ClipboardHelper.SetClipboardData(url);
					}
				}

			}
			catch (TaskCanceledException tcEx)
			{
				returnValue.Text = string.Format(DropboxLanguage.UploadFailure, DropboxDesignation);
                returnValue.NotificationType = NotificationTypes.Cancel;
				returnValue.ErrorText = tcEx.Message;
				LOG.Info(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = string.Format(DropboxLanguage.UploadFailure, DropboxDesignation);
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				LOG.Warn(e);
				MessageBox.Show(DropboxLanguage.UploadFailure + " " + e.Message, DropboxDesignation, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			return returnValue;
        }
	}
}