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

using Dapplo.Config.Ini;
using Dapplo.HttpExtensions;
using GreenshotPlugin.Core;
using GreenshotPlugin.OAuth;
using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotBoxPlugin
{
	/// <summary>
	/// Description of ImgurUtils.
	/// </summary>
	public static class BoxUtils
	{
		private static readonly IBoxConfiguration Config = IniConfig.Current.Get<IBoxConfiguration>();
		private static readonly Uri UploadFileUri = new Uri("https://upload.box.com/api/2.0/files/content");
		private static readonly Uri FilesUri = new Uri("https://www.box.com/api/2.0/files/");

		/// <summary>
		/// Do the actual upload to Box
		/// For more details on the available parameters, see: http://developers.box.net/w/page/12923951/ApiFunction_Upload%20and%20Download
		/// </summary>
		/// <param name="surfaceToUpload"></param>
		/// <param name="captureDetails"></param>
		/// <param name="progress"></param>
		/// <param name="token"></param>
		/// <returns>url to uploaded image</returns>
		public static async Task<string> UploadToBoxAsync(ISurface surfaceToUpload, ICaptureDetails captureDetails, IProgress<int> progress, CancellationToken token = default(CancellationToken))
		{
			string filename = Path.GetFileName(FilenameHelper.GetFilename(Config.UploadFormat, captureDetails));
			var outputSettings = new SurfaceOutputSettings(Config.UploadFormat, Config.UploadJpegQuality, false);

			// Fill the OAuth2Settings
			OAuth2Settings settings = new OAuth2Settings
			{
				AuthUrlPattern = "https://app.box.com/api/oauth2/authorize?client_id={ClientId}&response_type=code&state={State}&redirect_uri={RedirectUrl}", TokenUrl = new Uri("https://api.box.com/oauth2/token"), CloudServiceName = "Box", ClientId = Config.ClientId, ClientSecret = Config.ClientSecret, RedirectUrl = "https://www.box.com/home/", BrowserSize = new Size(1060, 600), AuthorizeMode = OAuth2AuthorizeMode.EmbeddedBrowser, RefreshToken = Config.RefreshToken, AccessToken = Config.AccessToken, AccessTokenExpires = Config.AccessTokenExpires
			};

			// Copy the settings from the config, which is kept in memory and on the disk

			try
			{
				using (var httpClient = await OAuth2Helper.CreateOAuth2HttpClientAsync(settings, token))
				{
					dynamic response;
					using (var stream = new MemoryStream())
					{
						var multiPartContent = new MultipartFormDataContent();
						var parentIdContent = new StringContent(Config.FolderId);
						parentIdContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
						{
							Name = "\"parent_id\""
						};
						multiPartContent.Add(parentIdContent);
						ImageOutput.SaveToStream(surfaceToUpload, stream, outputSettings);
						stream.Position = 0;
						using (var uploadStream = new ProgressStream(stream, progress))
						{
							using (var streamContent = new StreamContent(uploadStream))
							{
								streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream"); //"image/" + outputSettings.Format);
								streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
								{
									Name = "\"file\"", FileName = "\"" + filename + "\""
								}; // the extra quotes are important here
								multiPartContent.Add(streamContent);
								using (var responseMessage = await httpClient.PostAsync(UploadFileUri, multiPartContent, token))
								{
									response = await responseMessage.GetAsJsonAsync(token: token);
								}
							}
						}
					}

					if (response == null || !response.ContainsKey("total_count"))
					{
						return null;
					}

					if (Config.UseSharedLink)
					{
						var uri = FilesUri.AppendSegments((string) response.entries[0].id);
						var updateAcces = new JsonObject
						{
							{
								"shared_link", new JsonObject
								{
									{
										"access", "open"
									}
								}
							}
						};
						var content = new StringContent(updateAcces.ToString(), Encoding.UTF8);
						using (var responseMessage = await httpClient.PutAsync(uri, content, token))
						{
							var file = await responseMessage.GetAsJsonAsync(token: token);
							return file.shared_link.url;
						}
					}
					return string.Format("http://www.box.com/files/0/f/0/1/f_{0}", response.entries[0].id);
				}
			}
			finally
			{
				// Copy the settings back to the config, so they are stored.
				Config.RefreshToken = settings.RefreshToken;
				Config.AccessToken = settings.AccessToken;
				Config.AccessTokenExpires = settings.AccessTokenExpires;
			}
		}
	}
}