/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom,
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
using Greenshot.Addon.Dropbox.Entities;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;
using Greenshot.Addon.Windows;
using Dapplo.Utils;
using Dapplo.LogFacade;

namespace Greenshot.Addon.Dropbox
{
	[Destination(DropboxDesignation)]
	public sealed class DropboxDestination : AbstractDestination
	{
		private const string DropboxDesignation = "Dropbox";
		private static readonly LogSource Log = new LogSource();
		private static readonly Uri DropboxApiUri = new Uri("https://api.dropbox.com");
		private static readonly Uri DropboxContentUri = new Uri("https://content.dropboxapi.com/2/files/upload");
		private static readonly BitmapSource DropboxIcon;
		private OAuth2Settings _oAuth2Settings;
		private IHttpBehaviour _oAuthHttpBehaviour;

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

			_oAuth2Settings = new OAuth2Settings
			{
				AuthorizationUri = DropboxApiUri.
					AppendSegments("1","oauth2", "authorize").
					ExtendQuery(new Dictionary<string, string>{
								{ "response_type", "code"},
								{ "client_id", "{ClientId}" },
								{ "redirect_uri", "{RedirectUrl}" },
								{ "state", "{State}"}
					}),
				TokenUrl = DropboxApiUri.AppendSegments("1","oauth2", "token"),
				CloudServiceName = "Dropbox",
				ClientId = DropboxConfiguration.ClientId,
				ClientSecret = DropboxConfiguration.ClientSecret,
				AuthorizeMode = AuthorizeModes.LocalhostServer,
				RedirectUrl = "http://localhost:47336",
				Token = DropboxConfiguration
			};
			_oAuthHttpBehaviour = OAuth2HttpBehaviourFactory.Create(_oAuth2Settings);
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
						using (var streamContent = new StreamContent(stream))
						{
							streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
							return await UploadAsync(filename, streamContent, progress, pleaseWaitToken);
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
				Log.Info().WriteLine(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = string.Format(DropboxLanguage.UploadFailure, DropboxDesignation);
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				Log.Warn().WriteLine(e, "Dropbox export failed");
				MessageBox.Show(DropboxLanguage.UploadFailure + " " + e.Message, DropboxDesignation, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			return returnValue;
        }

		/// <summary>
		/// Upload the HttpContent to dropbox
		/// </summary>
		/// <param name="filename">Name of the file</param>
		/// <param name="content">HttpContent</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Url as string</returns>
		private async Task<string> UploadAsync(string filename, HttpContent content, IProgress<int> progress, CancellationToken cancellationToken = default(CancellationToken))
		{
			var oAuthHttpBehaviour =_oAuthHttpBehaviour.ShallowClone();
			// Use UploadProgress
			oAuthHttpBehaviour.UploadProgress = (percent) =>
			{
				UiContext.RunOn(() => progress.Report((int)(percent * 100)));
			};
			oAuthHttpBehaviour.MakeCurrent();

			// Build the upload content together
			var uploadContent = new Upload
			{
				Content = content
			};

			// This is needed
			if (!filename.StartsWith("/"))
			{
				filename = "/" + filename;
			}
			// Create the upload request parameters
			var parameters = new UploadRequest
			{
				Path = filename
			};
			// Make a Json string from the parameters
			var jsonString = SimpleJson.SerializeObject(parameters);
			// Minify, as it will be transported in the headers
			var minifiedJson = SimpleJson.Minify(jsonString);
			// Add it to the headers
			uploadContent.Headers.Add("Dropbox-API-Arg", minifiedJson);

			// Post everything, and return the upload reply or an error
			var response = await DropboxContentUri.PostAsync<HttpResponse<UploadReply,Error>>(uploadContent, cancellationToken);

			if (response.HasError)
			{
				throw new ApplicationException(response.ErrorResponse.Summary);
			}
			// Take the response from the upload, and use the information to request dropbox to create a link
			var createLinkRequest = new CreateLinkRequest
			{
				Path = response.Response.PathDisplay
			};
			var reply = await DropboxApiUri.AppendSegments("2", "sharing", "create_shared_link_with_settings").PostAsync<HttpResponse<CreateLinkReply, Error>>(createLinkRequest, cancellationToken);
			if (reply.HasError)
			{
				throw new ApplicationException(reply.ErrorResponse.Summary);
			}
			return reply.Response.Url;
		}
	}
}