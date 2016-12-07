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
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Dapplo.Ini;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Utils;
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Extensions;
using Greenshot.CaptureCore.Extensions;
using Greenshot.Core;
using Greenshot.Core.Interfaces;
using Greenshot.Legacy.Extensions;

#endregion

namespace Greenshot.Addon.Picasa
{
	/// <summary>
	///     Description of PicasaUtils.
	/// </summary>
	public static class PicasaUtils
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly IPicasaConfiguration PicasaConfiguration = IniConfig.Current.Get<IPicasaConfiguration>();

		/// <summary>
		///     Parse the upload URL from the response
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		public static string ParseResponse(string response)
		{
			if (response == null)
			{
				return null;
			}
			try
			{
				var doc = new XmlDocument();
				doc.LoadXml(response);
				var nodes = doc.GetElementsByTagName("link", "*");
				if (nodes.Count > 0)
				{
					string url = null;
					foreach (XmlNode node in nodes)
					{
						if (node.Attributes != null)
						{
							url = node.Attributes["href"].Value;
							string rel = node.Attributes["rel"].Value;
							// Pictures with rel="http://schemas.google.com/photos/2007#canonical" are the direct link
							if ((rel != null) && rel.EndsWith("canonical"))
							{
								break;
							}
						}
					}
					return url;
				}
			}
			catch (Exception e)
			{
				Log.Error().WriteLine("Could not parse Picasa response due to error {0}, response was: {1}", e.Message, response);
			}
			return null;
		}

		/// <summary>
		///     Do the actual upload to Picasa
		/// </summary>
		/// <param name="capture">ICapture</param>
		/// <param name="progress">IProgress</param>
		/// <param name="token">CancellationToken</param>
		/// <returns>url</returns>
		public static async Task<string> UploadToPicasa(ICapture capture, IProgress<int> progress, CancellationToken token = default(CancellationToken))
		{
			string filename = Path.GetFileName(FilenameHelper.GetFilename(PicasaConfiguration.UploadFormat, capture.CaptureDetails));
			var outputSettings = new SurfaceOutputSettings(PicasaConfiguration.UploadFormat, PicasaConfiguration.UploadJpegQuality);
			// Fill the OAuth2Settings

			var oAuth2Settings = new OAuth2Settings
			{
				AuthorizationUri = new Uri("https://accounts.google.com").AppendSegments("o", "oauth2", "auth").
					ExtendQuery(new Dictionary<string, string>
					{
						{"response_type", "code"},
						{"client_id", "{ClientId}"},
						{"redirect_uri", "{RedirectUrl}"},
						{"state", "{State}"},
						{"scope", "https://picasaweb.google.com/data/"}
					}),
				TokenUrl = new Uri("https://www.googleapis.com/oauth2/v3/token"),
				CloudServiceName = "Picasa",
				ClientId = PicasaConfiguration.ClientId,
				ClientSecret = PicasaConfiguration.ClientSecret,
				RedirectUrl = "http://getgreenshot.org",
				AuthorizeMode = AuthorizeModes.LocalhostServer,
				Token = PicasaConfiguration
			};

			var oAuthHttpBehaviour = HttpBehaviour.Current.ShallowClone();

			// Use UploadProgress
			oAuthHttpBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int) (percent*100))); };
			oAuthHttpBehaviour.OnHttpMessageHandlerCreated = httpMessageHandler => new OAuth2HttpMessageHandler(oAuth2Settings, oAuthHttpBehaviour, httpMessageHandler);
			if (PicasaConfiguration.AddFilename)
			{
				oAuthHttpBehaviour.OnHttpClientCreated = httpClient => httpClient.AddDefaultRequestHeader("Slug", Uri.EscapeDataString(filename));
			}

			string response;
			var uploadUri = new Uri("https://picasaweb.google.com/data/feed/api/user").AppendSegments(PicasaConfiguration.UploadUser, "albumid", PicasaConfiguration.UploadAlbum);
			using (var stream = new MemoryStream())
			{
				capture.SaveToStream(stream, outputSettings);
				stream.Position = 0;
				using (var content = new StreamContent(stream))
				{
					content.Headers.Add("Content-Type", "image/" + outputSettings.Format);

					oAuthHttpBehaviour.MakeCurrent();
					response = await uploadUri.PostAsync<string>(content, token);
				}
			}

			return ParseResponse(response);
		}
	}
}