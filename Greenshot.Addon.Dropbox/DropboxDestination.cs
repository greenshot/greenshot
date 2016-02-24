/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom,
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
using System.Collections.Generic;
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
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.OAuth;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;
using Greenshot.Addon.Windows;

namespace Greenshot.Addon.Dropbox
{
	[Destination(DropboxDesignation)]
	public sealed class DropboxDestination : AbstractDestination
	{
		private const string DropboxDesignation = "Dropbox";
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(DropboxDestination));
		private static readonly Uri DropboxUri = new Uri("https://api.dropbox.com/1");
		private static readonly BitmapSource DropboxIcon;
		private OAuth2Settings _oauth2Settings;

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
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, token);
			Text = DropboxLanguage.UploadMenuItem;
			Icon = DropboxIcon;

			_oauth2Settings = new OAuth2Settings
			{
				AuthorizationUri = DropboxUri.
					AppendSegments("oauth2", "authorize").
					ExtendQuery(new Dictionary<string, string>{
								{ "response_type", "code"},
								{ "client_id", "{ClientId}" },
								{ "redirect_uri", "{RedirectUrl}" },
								{ "state", "{State}"}
					}),
				TokenUrl = DropboxUri.AppendSegments("oauth2", "token"),
				CloudServiceName = "Dropbox",
				ClientId = DropboxConfiguration.ClientId,
				ClientSecret = DropboxConfiguration.ClientSecret,
				AuthorizeMode = AuthorizeModes.EmbeddedBrowser,
				RedirectUrl = "http://getgreenshot.org",
				Token = DropboxConfiguration
			};
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
				Log.Information(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = string.Format(DropboxLanguage.UploadFailure, DropboxDesignation);
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				Log.Warning(e, "Dropbox export failed");
				MessageBox.Show(DropboxLanguage.UploadFailure + " " + e.Message, DropboxDesignation, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			return returnValue;
        }
	}
}