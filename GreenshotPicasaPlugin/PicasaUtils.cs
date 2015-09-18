/*
 * A Picasa Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
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
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.Extensions;
using GreenshotPlugin.OAuth;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace GreenshotPicasaPlugin {
	/// <summary>
	/// Description of PicasaUtils.
	/// </summary>
	public static class PicasaUtils {
		private const string PicasaScope = "https://picasaweb.google.com/data/";
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PicasaUtils));
		private static readonly PicasaConfiguration _config = IniConfig.Current.Get<PicasaConfiguration>();
		private const string AuthUrl = "https://accounts.google.com/o/oauth2/auth?response_type=code&client_id={ClientId}&redirect_uri={RedirectUrl}&state={State}&scope=" + PicasaScope;
		private static readonly Uri TokenUrl = new Uri("https://www.googleapis.com/oauth2/v3/token");
		private const string UploadUrl = "https://picasaweb.google.com/data/feed/api/user/{0}/albumid/{1}";

		/// <summary>
		/// Do the actual upload to Picasa
		/// </summary>
		/// <param name="surfaceToUpload">Image to upload</param>
		/// <param name="captureDetails">ICaptureDetails</param>
		/// <returns>url</returns>
		public static async Task<string> UploadToPicasa(ISurface surfaceToUpload, ICaptureDetails captureDetails, IProgress<int> progress, CancellationToken token = default(CancellationToken)) {
			string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
			var outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality);
			// Fill the OAuth2Settings
			var settings = new OAuth2Settings();
			settings.AuthUrlPattern = AuthUrl;
			settings.TokenUrl = TokenUrl;
			settings.CloudServiceName = "Picasa";
			settings.ClientId = PicasaCredentials.ClientId;
			settings.ClientSecret = PicasaCredentials.ClientSecret;
			settings.AuthorizeMode = OAuth2AuthorizeMode.LocalServer;

			// Copy the settings from the config, which is kept in memory and on the disk
			settings.RefreshToken = _config.RefreshToken;
			settings.AccessToken = _config.AccessToken;
			settings.AccessTokenExpires = _config.AccessTokenExpires;

			try {
				string response;
				var uploadUri = new Uri(string.Format(UploadUrl, _config.UploadUser, _config.UploadAlbum));
				using (var httpClient = await OAuth2Helper.CreateOAuth2HttpClientAsync(uploadUri, settings)) {
					if (_config.AddFilename) {
						httpClient.AddDefaultRequestHeader("Slug", Uri.EscapeDataString(filename));
					}
					using (var stream = new MemoryStream()) {
						ImageOutput.SaveToStream(surfaceToUpload, stream, outputSettings);
						stream.Position = 0;
						using (var uploadStream = new ProgressStream(stream, progress)) {
							using (var content = new StreamContent(uploadStream)) {
								content.Headers.Add("Content-Type", "image/" + outputSettings.Format);
								var responseMessage = await httpClient.PostAsync(uploadUri, content, token).ConfigureAwait(false);
								response = await responseMessage.GetAsStringAsync(token).ConfigureAwait(false);
							}
						}
					}
				}

				return ParseResponse(response);
			} finally {
				// Copy the settings back to the config, so they are stored.
				_config.RefreshToken = settings.RefreshToken;
				_config.AccessToken = settings.AccessToken;
				_config.AccessTokenExpires = settings.AccessTokenExpires;
			}
		}

		/// <summary>
		/// Parse the upload URL from the response
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		public static string ParseResponse(string response) {
			if (response == null) {
				return null;
			}
			try {
				var doc = new XmlDocument();
				doc.LoadXml(response);
				var nodes = doc.GetElementsByTagName("link", "*");
				if (nodes.Count > 0) {
					string url = null;
					foreach (XmlNode node in nodes) {
						if (node.Attributes != null) {
							url = node.Attributes["href"].Value;
							string rel = node.Attributes["rel"].Value;
							// Pictures with rel="http://schemas.google.com/photos/2007#canonical" are the direct link
							if (rel != null && rel.EndsWith("canonical")) {
								break;
							}
						}
					}
					return url;
				}
			} catch (Exception e) {
				LOG.ErrorFormat("Could not parse Picasa response due to error {0}, response was: {1}", e.Message, response);
			}
			return null;
		}
	}
}
