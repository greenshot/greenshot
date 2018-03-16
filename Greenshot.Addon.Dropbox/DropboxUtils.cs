#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Drawing;
using Dapplo.Ini;
using Dapplo.Log;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

#endregion

namespace Greenshot.Addon.Dropbox
{
	/// <summary>
	///     Description of DropboxUtils.
	/// </summary>
	public class DropboxUtils
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly IDropboxPluginConfiguration DropboxConfig = IniConfig.Current.Get<IDropboxPluginConfiguration>();

		private DropboxUtils()
		{
		}

		public static string UploadToDropbox(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string filename)
		{
			var oAuth = new OAuthSession(DropBoxCredentials.CONSUMER_KEY, DropBoxCredentials.CONSUMER_SECRET)
			{
				BrowserSize = new Size(1080, 650),
				CheckVerifier = false,
				AccessTokenUrl = "https://api.dropbox.com/1/oauth/access_token",
				AuthorizeUrl = "https://api.dropbox.com/1/oauth/authorize",
				RequestTokenUrl = "https://api.dropbox.com/1/oauth/request_token",
				LoginTitle = "Dropbox authorization",
				Token = DropboxConfig.DropboxToken,
				TokenSecret = DropboxConfig.DropboxTokenSecret
			};

			try
			{
				var imageToUpload = new SurfaceContainer(surfaceToUpload, outputSettings, filename);
				var uploadResponse = oAuth.MakeOAuthRequest(HTTPMethod.POST, "https://api-content.dropbox.com/1/files_put/sandbox/" + OAuthSession.UrlEncode3986(filename), null, null,
					imageToUpload);
				Log.Debug().WriteLine("Upload response: {0}", uploadResponse);
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, "Upload error: ");
				throw;
			}
			finally
			{
				if (!string.IsNullOrEmpty(oAuth.Token))
				{
					DropboxConfig.DropboxToken = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret))
				{
					DropboxConfig.DropboxTokenSecret = oAuth.TokenSecret;
				}
			}

			// Try to get a URL to the uploaded image
			try
			{
				var responseString = oAuth.MakeOAuthRequest(HTTPMethod.GET, "https://api.dropbox.com/1/shares/sandbox/" + OAuthSession.UrlEncode3986(filename), null, null, null);
				if (responseString != null)
				{
					Log.Debug().WriteLine("Parsing output: {0}", responseString);
					var returnValues = JSONHelper.JsonDecode(responseString);
					if (returnValues.ContainsKey("url"))
					{
						return returnValues["url"] as string;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, "Can't parse response.");
			}
			return null;
		}
	}
}