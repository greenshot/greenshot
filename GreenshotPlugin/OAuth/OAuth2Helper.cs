/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using GreenshotPlugin.Controls;
using GreenshotPlugin.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotPlugin.OAuth {

	/// <summary>
	/// Code to simplify OAuth 2
	/// </summary>
	public static class OAuth2Helper {
		private const string REFRESH_TOKEN = "refresh_token";
		private const string ACCESS_TOKEN = "access_token";
		private const string CODE = "code";
		private const string CLIENT_ID = "client_id";
		private const string CLIENT_SECRET = "client_secret";
		private const string GRANT_TYPE = "grant_type";
		private const string AUTHORIZATION_CODE = "authorization_code";
		private const string REDIRECT_URI = "redirect_uri";
		private const string EXPIRES_IN = "expires_in";

		/// <summary>
		/// Generate an OAuth 2 Token by using the supplied code
		/// </summary>
		/// <param name="code">Code to get the RefreshToken</param>
		/// <param name="settings">OAuth2Settings to update with the information that was retrieved</param>
		private static async Task GenerateRefreshTokenAsync(OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			IDictionary<string, string> data = new Dictionary<string, string>();
			// Use the returned code to get a refresh code
			data.Add(CODE, settings.Code);
			data.Add(CLIENT_ID, settings.ClientId);
			data.Add(REDIRECT_URI, settings.RedirectUrl);
			data.Add(CLIENT_SECRET, settings.ClientSecret);
			data.Add(GRANT_TYPE, AUTHORIZATION_CODE);
			foreach (string key in settings.AdditionalAttributes.Keys) {
				data.Add(key, settings.AdditionalAttributes[key]);
			}

			dynamic refreshTokenResult;
			using (var responseMessage = await settings.TokenUrl.PostFormUrlEncodedAsync(data, token)) {
				refreshTokenResult = await responseMessage.GetAsJsonAsync();
			}

			if (refreshTokenResult.IsDefined("error")) {
				if (refreshTokenResult.IsDefined("error_description")) {
					throw new Exception(string.Format("{0} - {1}", refreshTokenResult.error, refreshTokenResult.error_description));
				} else {
					throw new Exception(refreshTokenResult.error);
				}
			} else {
				// gives as described here: https://developers.google.com/identity/protocols/OAuth2InstalledApp
				//  "access_token":"1/fFAGRNJru1FTz70BzhT3Zg",
				//	"expires_in":3920,
				//	"token_type":"Bearer",
				//	"refresh_token":"1/xEoDL4iW3cxlI7yDbSRFYNG01kVKM2C-259HOF2aQbI"
				settings.AccessToken = (string)refreshTokenResult.access_token;
				settings.RefreshToken = (string)refreshTokenResult.refresh_token;

				if (refreshTokenResult.IsDefined("expires_in")) {
					double expiresIn = refreshTokenResult.expires_in;
					settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds(expiresIn);
				}
				settings.Code = null;
			}
		}

		/// <summary>
		/// Go out and retrieve a new access token via refresh-token with the TokenUrl in the settings
		/// Will upate the access token, refresh token, expire date
		/// </summary>
		/// <param name="settings"></param>
		private static async Task GenerateAccessTokenAsync(OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			IDictionary<string, string> data = new Dictionary<string, string>();
			data.Add(REFRESH_TOKEN, settings.RefreshToken);
			data.Add(CLIENT_ID, settings.ClientId);
			data.Add(CLIENT_SECRET, settings.ClientSecret);
			data.Add(GRANT_TYPE, REFRESH_TOKEN);
			foreach (string key in settings.AdditionalAttributes.Keys) {
				data.Add(key, settings.AdditionalAttributes[key]);
			}

			dynamic accessTokenResult;
			using (var responseMessage = await settings.TokenUrl.PostFormUrlEncodedAsync(data, token)) {
				accessTokenResult = await responseMessage.GetAsJsonAsync();
			}

			if (accessTokenResult.IsDefined("error")) {
				var error = (string)accessTokenResult.error;
				if ("invalid_grant" == error) {
					// Refresh token has also expired, we need a new one!
					settings.RefreshToken = null;
					settings.AccessToken = null;
					settings.AccessTokenExpires = DateTimeOffset.MinValue;
					settings.Code = null;
					return;
				} else {
					if (accessTokenResult.IsDefined("error_description")) {
						throw new Exception(string.Format("{0} - {1}", error, accessTokenResult.error_description));
					} else {
						throw new Exception(error);
					}
				}
			} else {
				// gives as described here: https://developers.google.com/identity/protocols/OAuth2InstalledApp
				//  "access_token":"1/fFAGRNJru1FTz70BzhT3Zg",
				//	"expires_in":3920,
				//	"token_type":"Bearer"
				settings.AccessToken = (string)accessTokenResult.access_token;
				if (accessTokenResult.IsDefined("refresh_token")) {
					// Refresh the refresh token :)
					settings.RefreshToken = (string)accessTokenResult.refresh_token;
				}
				if (accessTokenResult.IsDefined("expires_in")) {
					double expiresIn = accessTokenResult.expires_in;
					settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds(expiresIn);
				}
			}
		}

		/// <summary>
		/// Authenticate by using the mode specified in the settings
		/// </summary>
		/// <param name="settings">OAuth2Settings</param>
		/// <param name="token">CancellationToken</param>
		/// <returns>false if it was canceled, true if it worked, exception if not</returns>
		private static async Task<bool> AuthenticateAsync(OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			bool completed = true;
			switch (settings.AuthorizeMode) {
				case OAuth2AuthorizeMode.LocalServer:
					completed = await AuthenticateViaLocalServerAsync(settings, token).ConfigureAwait(false);
					break;
				case OAuth2AuthorizeMode.EmbeddedBrowser:
					completed = await AuthenticateViaEmbeddedBrowserAsync(settings, token).ConfigureAwait(false);
					break;
				default:
					throw new NotImplementedException(string.Format("Authorize mode '{0}' is not 'yet' implemented.", settings.AuthorizeMode));
			}
			return completed;
		}

		/// <summary>
		/// Authenticate via an embedded browser
		/// If this works, return the code
		/// </summary>
		/// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
		/// <returns>true if completed, false if canceled</returns>
		private static async Task<bool> AuthenticateViaEmbeddedBrowserAsync(OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			if (string.IsNullOrEmpty(settings.CloudServiceName)) {
				throw new ArgumentNullException("CloudServiceName");
			}
			if (settings.BrowserSize == Size.Empty) {
				throw new ArgumentNullException("BrowserSize");
			}
			OAuthLoginForm loginForm = new OAuthLoginForm(string.Format("Authorize {0}", settings.CloudServiceName), settings.BrowserSize, settings.FormattedAuthUrl, settings.RedirectUrl);
			loginForm.ShowDialog();
			if (loginForm.IsOk) {
				string code;
				if (loginForm.CallbackParameters.TryGetValue(CODE, out code) && !string.IsNullOrEmpty(code)) {
					settings.Code = code;
					await GenerateRefreshTokenAsync(settings, token);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Authenticate via a local server by using the LocalServerCodeReceiver
		/// If this works, return the code
		/// </summary>
		/// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
		/// <returns>true if completed</returns>
		private static async Task<bool> AuthenticateViaLocalServerAsync(OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			IDictionary<string, string> result;
			using (var codeReceiver = new LocalServerCodeReceiver()) {
				result = await codeReceiver.ReceiveCodeAsync(settings, token);
			}

			string code;
			if (result.TryGetValue(CODE, out code) && !string.IsNullOrEmpty(code)) {
				settings.Code = code;
				await GenerateRefreshTokenAsync(settings, token);
				return true;
			}
			string error;
			if (result.TryGetValue("error", out error)) {
				string errorDescription;
				if (result.TryGetValue("error_description", out errorDescription)) {
					throw new Exception(errorDescription);
				}
				if ("access_denied" == error) {
					throw new UnauthorizedAccessException("Access denied");
				} else {
					throw new Exception(error);
				}
			}
			return false;
		}

		/// <summary>
		/// Check and authenticate or refresh tokens 
		/// </summary>
		/// <param name="settings">OAuth2Settings</param>
		private static async Task CheckAndAuthenticateOrRefreshAsync(OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			// Get Refresh / Access token
			if (string.IsNullOrEmpty(settings.RefreshToken)) {
				if (!await AuthenticateAsync(settings, token).ConfigureAwait(false)) {
					throw new Exception("Authentication cancelled");
				}
			}
			if (settings.IsAccessTokenExpired) {
				await GenerateAccessTokenAsync(settings, token).ConfigureAwait(false);
				// Get Refresh / Access token
				if (string.IsNullOrEmpty(settings.RefreshToken)) {
					if (!await AuthenticateAsync(settings, token).ConfigureAwait(false)) {
						throw new Exception("Authentication cancelled");
					}
					await GenerateAccessTokenAsync(settings, token).ConfigureAwait(false);
				}
			}
			if (settings.IsAccessTokenExpired) {
				throw new Exception("Authentication failed");
			}
		}

		/// <summary>
		/// create HttpClient ready for OAuth 2 access
		/// </summary>
		/// <param name="method">HttpMethod</param>
		/// <param name="uri"></param>
		/// <param name="settings">OAuth2Settings</param>
		/// <returns>HttpClient</returns>
		public static async Task<HttpClient> CreateOAuth2HttpClientAsync(Uri uri, OAuth2Settings settings, CancellationToken token = default(CancellationToken)) {
			await CheckAndAuthenticateOrRefreshAsync(settings, token).ConfigureAwait(false);

			var httpClient = uri.CreateHttpClient();
			if (!string.IsNullOrEmpty(settings.AccessToken)) {
				httpClient.SetBearer(settings.AccessToken);
			}
			return httpClient;
		}
	}
}