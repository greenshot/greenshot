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

using Dapplo.Config.Ini;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.OAuth;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

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
		/// <param name="oAuth2Settings">OAuth2Settings</param>
		/// <param name="capture">ICapture</param>
		/// <param name="progress">IProgress</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>url to uploaded image</returns>
		public static async Task<string> UploadToBoxAsync(OAuth2Settings oAuth2Settings, ICapture capture, IProgress<int> progress, CancellationToken cancellationToken = default(CancellationToken))
		{
			string filename = Path.GetFileName(FilenameHelper.GetFilename(Config.UploadFormat, capture.CaptureDetails));
			var outputSettings = new SurfaceOutputSettings(Config.UploadFormat, Config.UploadJpegQuality, false);

			var oauthHttpBehaviour = new HttpBehaviour();
			oauthHttpBehaviour.OnHttpMessageHandlerCreated = httpMessageHandler => new OAuth2HttpMessageHandler(oAuth2Settings, oauthHttpBehaviour, httpMessageHandler);

			// TODO: See if the PostAsync<Bitmap> can be used? Or at least the HttpContentFactory?
			using (var stream = new MemoryStream())
			{
				var multiPartContent = new MultipartFormDataContent();
				var parentIdContent = new StringContent(Config.FolderId);
				parentIdContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
				{
					Name = "\"parent_id\""
				};
				multiPartContent.Add(parentIdContent);
				ImageOutput.SaveToStream(capture, stream, outputSettings);
				stream.Position = 0;
				dynamic response;
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

						response = await UploadFileUri.PostAsync<dynamic, HttpContent>(multiPartContent, oauthHttpBehaviour, cancellationToken);
					}
				}

				if (response == null || !response.ContainsKey("total_count"))
				{
					return null;
				}

				if (Config.UseSharedLink)
				{
					var uriForSharedLink = FilesUri.AppendSegments((string) response.entries[0].id);
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
					// TODO: Add JsonObject
					var file = await uriForSharedLink.PostAsync<dynamic, JsonObject>(updateAcces, oauthHttpBehaviour, cancellationToken);
					return file.shared_link.url;
				}
				return string.Format("http://www.box.com/files/0/f/0/1/f_{0}", response.entries[0].id);
			}
		}
	}
}