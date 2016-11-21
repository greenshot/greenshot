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

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.Extensions;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Utils;
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Windows;
using MahApps.Metro.IconPacks;
using Greenshot.Addon.Extensions;
using Greenshot.CaptureCore;
using Greenshot.CaptureCore.Extensions;
using Greenshot.Core;
using Greenshot.Core.Interfaces;

#endregion

namespace Greenshot.Addon.Photobucket
{
	[Destination(PhotobucketDesignation)]
	public sealed class PhotobucketDestination : AbstractDestination
	{
		private const string PhotobucketDesignation = "Photobucket";
		private static readonly LogSource Log = new LogSource();
		private static readonly Uri PhotobucketApiUri = new Uri("http://api.photobucket.com");
		private OAuth1HttpBehaviour _oAuthHttpBehaviour;
		private OAuth1Settings _oAuthSettings;

		[Import]
		private IPhotobucketConfiguration PhotobucketConfiguration { get; set; }

		[Import]
		private IPhotobucketLanguage PhotobucketLanguage { get; set; }

		private async Task<INotification> ExportCaptureAsync(ICapture capture, string album, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = PhotobucketDesignation,
				SourceType = SourceTypes.Destination,
				Text = string.Format(PhotobucketLanguage.UploadSuccess, PhotobucketDesignation)
			};

			var outputSettings = new SurfaceOutputSettings(PhotobucketConfiguration.UploadFormat, PhotobucketConfiguration.UploadJpegQuality, false);
			try
			{
				var photobucketInfo = await PleaseWaitWindow.CreateAndShowAsync(Designation, PhotobucketLanguage.CommunicationWait, async (progress, pleaseWaitToken) =>
				{
					string filename = Path.GetFileName(FilenameHelper.GetFilename(PhotobucketConfiguration.UploadFormat, capture.CaptureDetails));
					return await UploadToPhotobucket(capture, outputSettings, album, capture.CaptureDetails.Title, filename, progress, token);
				}, token);

				// This causes an exeption if the upload failed :)
				Log.Debug().WriteLine("Uploaded to Photobucket page: {0}", photobucketInfo.Page);
				string uploadUrl = null;
				try
				{
					uploadUrl = PhotobucketConfiguration.UsePageLink ? photobucketInfo.Page : photobucketInfo.Original;
				}
				catch (Exception ex)
				{
					Log.Error().WriteLine(ex, "Can't write to clipboard: ");
				}

				if (uploadUrl != null)
				{
					returnValue.ImageLocation = new Uri(uploadUrl);
					if (PhotobucketConfiguration.AfterUploadLinkToClipBoard)
					{
						ClipboardHelper.SetClipboardData(uploadUrl);
					}
				}
			}
			catch (TaskCanceledException tcEx)
			{
				returnValue.Text = string.Format(PhotobucketLanguage.UploadFailure, PhotobucketDesignation);
				returnValue.NotificationType = NotificationTypes.Cancel;
				returnValue.ErrorText = tcEx.Message;
				Log.Info().WriteLine(tcEx.Message);
			}
			catch (Exception e)
			{
				returnValue.Text = string.Format(PhotobucketLanguage.UploadFailure, PhotobucketDesignation);
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				Log.Warn().WriteLine(e, "Photobucket export failed");
				MessageBox.Show(PhotobucketLanguage.UploadFailure + " " + e.Message, PhotobucketDesignation, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			return returnValue;
		}

		/// <summary>
		///     Setup
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
			Designation = PhotobucketDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, "0", token);
			Text = PhotobucketLanguage.UploadMenuItem;
			Icon = new PackIconMaterial
			{
				Kind = PackIconMaterialKind.Camera
			};

			_oAuthSettings = new OAuth1Settings
			{
				Token = PhotobucketConfiguration,
				ClientId = PhotobucketConfiguration.ClientId,
				ClientSecret = PhotobucketConfiguration.ClientSecret,
				CloudServiceName = "Photo bucket",
				EmbeddedBrowserWidth = 1010,
				EmbeddedBrowserHeight = 400,
				AuthorizeMode = AuthorizeModes.EmbeddedBrowser,
				TokenUrl = PhotobucketApiUri.AppendSegments("login", "request"),
				TokenMethod = HttpMethod.Post,
				AccessTokenUrl = PhotobucketApiUri.AppendSegments("login", "access"),
				AccessTokenMethod = HttpMethod.Post,
				AuthorizationUri = PhotobucketApiUri.AppendSegments("apilogin", "login")
					.ExtendQuery(new Dictionary<string, string>
					{
						{OAuth1Parameters.Token.EnumValueOf(), "{RequestToken}"},
						{OAuth1Parameters.Callback.EnumValueOf(), "{RedirectUrl}"}
					}),
				RedirectUrl = "http://getgreenshot.org",
				CheckVerifier = false
			};
			var oAuthHttpBehaviour = OAuth1HttpBehaviourFactory.Create(_oAuthSettings);
			// Store the leftover values
			oAuthHttpBehaviour.OnAccessTokenValues = values =>
			{
				if ((values != null) && values.ContainsKey("subdomain"))
				{
					PhotobucketConfiguration.SubDomain = values["subdomain"];
				}
				if ((values != null) && values.ContainsKey("username"))
				{
					PhotobucketConfiguration.Username = values["username"];
				}
			};

			oAuthHttpBehaviour.BeforeSend = httpRequestMessage =>
			{
				if (PhotobucketConfiguration.SubDomain != null)
				{
					var uriBuilder = new UriBuilder(httpRequestMessage.RequestUri)
					{
						Host = PhotobucketConfiguration.SubDomain
					};
					httpRequestMessage.RequestUri = uriBuilder.Uri;
				}
			};
			// Reset the OAuth token if there is no subdomain, without this we need to request it again.
			if ((PhotobucketConfiguration.SubDomain == null) || (PhotobucketConfiguration.Username == null))
			{
				PhotobucketConfiguration.OAuthToken = null;
				PhotobucketConfiguration.OAuthTokenSecret = null;
			}
			_oAuthHttpBehaviour = oAuthHttpBehaviour;
		}

		/// <summary>
		///     Do the actual upload to Photobucket
		///     For more details on the available parameters, see: http://api.Photobucket.com/resources_anon
		/// </summary>
		/// <returns>PhotobucketResponse</returns>
		public async Task<PhotobucketInfo> UploadToPhotobucket(ICapture surfaceToUpload, SurfaceOutputSettings outputSettings, string albumPath, string title, string filename, IProgress<int> progress = null, CancellationToken token = default(CancellationToken))
		{
			string responseString;

			var oAuthHttpBehaviour = _oAuthHttpBehaviour.ShallowClone();

			// Use UploadProgress
			oAuthHttpBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int) (percent*100)), token); };
			_oAuthHttpBehaviour.MakeCurrent();
			if ((PhotobucketConfiguration.Username == null) || (PhotobucketConfiguration.SubDomain == null))
			{
				await PhotobucketApiUri.AppendSegments("users").ExtendQuery("format", "json").GetAsAsync<dynamic>(token);
			}
			if (PhotobucketConfiguration.Album == null)
			{
				PhotobucketConfiguration.Album = PhotobucketConfiguration.Username;
			}
			var uploadUri = PhotobucketApiUri.AppendSegments("album", PhotobucketConfiguration.Album, "upload");

			var signedParameters = new Dictionary<string, object> {{"type", "image"}};
			// add type
			// add title
			if (title != null)
			{
				signedParameters.Add("title", title);
			}
			// add filename
			if (filename != null)
			{
				signedParameters.Add("filename", filename);
			}
			// Add image
			using (var stream = new MemoryStream())
			{
				surfaceToUpload.SaveToStream(stream, outputSettings);
				stream.Position = 0;
				using (var streamContent = new StreamContent(stream))
				{
					streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
					streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
					{
						Name = "\"uploadfile\"",
						FileName = "\"" + filename + "\""
					};

					HttpBehaviour.Current.SetConfig(new HttpRequestMessageConfiguration
					{
						Properties = signedParameters
					});
					try
					{
						responseString = await uploadUri.PostAsync<string>(streamContent, token);
					}
					catch (Exception ex)
					{
						Log.Error().WriteLine(ex, "Error uploading to Photobucket.");
						throw;
					}
				}
			}

			if (responseString == null)
			{
				return null;
			}
			Log.Info().WriteLine(responseString);
			var photobucketInfo = PhotobucketInfo.FromUploadResponse(responseString);
			Log.Debug().WriteLine("Upload to Photobucket was finished");
			return photobucketInfo;
		}
	}
}