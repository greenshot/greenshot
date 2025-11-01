/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using Greenshot.Base.Controls;

namespace Greenshot.Base.Core.OAuth
{
    /// <summary>
    /// Code to simplify OAuth 2
    /// </summary>
    public static class OAuth2Helper
    {
        private const string RefreshToken = "refresh_token";
        private const string AccessToken = "access_token";
        private const string Code = "code";
        private const string Error = "error";
        private const string ClientId = "client_id";
        private const string ClientSecret = "client_secret";
        private const string GrantType = "grant_type";
        private const string AuthorizationCode = "authorization_code";
        private const string RedirectUri = "redirect_uri";
        private const string ExpiresIn = "expires_in";

        /// <summary>
        /// Generate an OAuth 2 Token by using the supplied code
        /// </summary>
        /// <param name="settings">OAuth2Settings to update with the information that was retrieved</param>
        public static void GenerateRefreshToken(OAuth2Settings settings)
        {
            IDictionary<string, object> data = new Dictionary<string, object>
            {
                // Use the returned code to get a refresh code
                {
                    Code, settings.Code
                },
                {
                    ClientId, settings.ClientId
                },
                {
                    ClientSecret, settings.ClientSecret
                },
                {
                    GrantType, AuthorizationCode
                }
            };
            foreach (string key in settings.AdditionalAttributes.Keys)
            {
                data.Add(key, settings.AdditionalAttributes[key]);
            }

            HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(settings.TokenUrl, HTTPMethod.POST);
            NetworkHelper.UploadFormUrlEncoded(webRequest, data);
            string accessTokenJsonResult = NetworkHelper.GetResponseAsString(webRequest, true);

            IDictionary<string, object> refreshTokenResult = JSONHelper.JsonDecode(accessTokenJsonResult);
            if (refreshTokenResult.ContainsKey("error"))
            {
                if (refreshTokenResult.ContainsKey("error_description"))
                {
                    throw new Exception($"{refreshTokenResult["error"]} - {refreshTokenResult["error_description"]}");
                }

                throw new Exception((string) refreshTokenResult["error"]);
            }

            // gives as described here: https://developers.google.com/identity/protocols/OAuth2InstalledApp
            //  "access_token":"1/fFAGRNJru1FTz70BzhT3Zg",
            //  "expires_in":3920,
            //  "token_type":"Bearer",
            //  "refresh_token":"1/xEoDL4iW3cxlI7yDbSRFYNG01kVKM2C-259HOF2aQbI"
            if (refreshTokenResult.ContainsKey(AccessToken))
            {
                settings.AccessToken = (string) refreshTokenResult[AccessToken];
            }

            if (refreshTokenResult.ContainsKey(RefreshToken))
            {
                settings.RefreshToken = (string) refreshTokenResult[RefreshToken];
            }

            if (refreshTokenResult.ContainsKey(ExpiresIn))
            {
                object seconds = refreshTokenResult[ExpiresIn];
                if (seconds != null)
                {
                    settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds((double) seconds);
                }
            }

            settings.Code = null;
        }

        /// <summary>
        /// Used to update the settings with the callback information
        /// </summary>
        /// <param name="settings">OAuth2Settings</param>
        /// <param name="callbackParameters">IDictionary</param>
        /// <returns>true if the access token is already in the callback</returns>
        private static bool UpdateFromCallback(OAuth2Settings settings, IDictionary<string, string> callbackParameters)
        {
            if (!callbackParameters.ContainsKey(AccessToken))
            {
                return false;
            }

            if (callbackParameters.ContainsKey(RefreshToken))
            {
                // Refresh the refresh token :)
                settings.RefreshToken = callbackParameters[RefreshToken];
            }

            if (callbackParameters.ContainsKey(ExpiresIn))
            {
                var expiresIn = callbackParameters[ExpiresIn];
                settings.AccessTokenExpires = DateTimeOffset.MaxValue;
                if (expiresIn != null)
                {
                    if (double.TryParse(expiresIn, out var seconds))
                    {
                        settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds(seconds);
                    }
                }
            }

            settings.AccessToken = callbackParameters[AccessToken];
            return true;
        }

        /// <summary>
        /// Go out and retrieve a new access token via refresh-token with the TokenUrl in the settings
        /// Will update the access token, refresh token, expire date
        /// </summary>
        /// <param name="settings"></param>
        public static void GenerateAccessToken(OAuth2Settings settings)
        {
            IDictionary<string, object> data = new Dictionary<string, object>
            {
                {
                    RefreshToken, settings.RefreshToken
                },
                {
                    ClientId, settings.ClientId
                },
                {
                    ClientSecret, settings.ClientSecret
                },
                {
                    GrantType, RefreshToken
                }
            };
            foreach (string key in settings.AdditionalAttributes.Keys)
            {
                data.Add(key, settings.AdditionalAttributes[key]);
            }

            HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(settings.TokenUrl, HTTPMethod.POST);
            NetworkHelper.UploadFormUrlEncoded(webRequest, data);
            string accessTokenJsonResult = NetworkHelper.GetResponseAsString(webRequest, true);

            // gives as described here: https://developers.google.com/identity/protocols/OAuth2InstalledApp
            //  "access_token":"1/fFAGRNJru1FTz70BzhT3Zg",
            //  "expires_in":3920,
            //  "token_type":"Bearer",

            IDictionary<string, object> accessTokenResult = JSONHelper.JsonDecode(accessTokenJsonResult);
            if (accessTokenResult.ContainsKey("error"))
            {
                if ("invalid_grant" == (string) accessTokenResult["error"])
                {
                    // Refresh token has also expired, we need a new one!
                    settings.RefreshToken = null;
                    settings.AccessToken = null;
                    settings.AccessTokenExpires = DateTimeOffset.MinValue;
                    settings.Code = null;
                    return;
                }

                if (accessTokenResult.ContainsKey("error_description"))
                {
                    throw new Exception($"{accessTokenResult["error"]} - {accessTokenResult["error_description"]}");
                }

                throw new Exception((string) accessTokenResult["error"]);
            }

            if (accessTokenResult.ContainsKey(AccessToken))
            {
                settings.AccessToken = (string) accessTokenResult[AccessToken];
                settings.AccessTokenExpires = DateTimeOffset.MaxValue;
            }

            if (accessTokenResult.ContainsKey(RefreshToken))
            {
                // Refresh the refresh token :)
                settings.RefreshToken = (string) accessTokenResult[RefreshToken];
            }

            if (accessTokenResult.ContainsKey(ExpiresIn))
            {
                object seconds = accessTokenResult[ExpiresIn];
                if (seconds != null)
                {
                    settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds((double) seconds);
                }
            }
        }

        /// <summary>
        /// Authorize by using the mode specified in the settings
        /// </summary>
        /// <param name="settings">OAuth2Settings</param>
        /// <returns>false if it was canceled, true if it worked, exception if not</returns>
        public static bool Authorize(OAuth2Settings settings)
        {
            var completed = settings.AuthorizeMode switch
            {
                OAuth2AuthorizeMode.LocalServer => AuthorizeViaLocalServer(settings),
                OAuth2AuthorizeMode.EmbeddedBrowser => AuthorizeViaEmbeddedBrowser(settings),
                OAuth2AuthorizeMode.JsonReceiver => AuthorizeViaDefaultBrowser(settings),
                _ => throw new NotImplementedException($"Authorize mode '{settings.AuthorizeMode}' is not 'yet' implemented."),
            };
            return completed;
        }

        /// <summary>
        /// Authorize via the default browser, via the Greenshot website.
        /// It will wait for a Json post.
        /// If this works, return the code
        /// </summary>
        /// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
        /// <returns>true if completed, false if canceled</returns>
        private static bool AuthorizeViaDefaultBrowser(OAuth2Settings settings)
        {
            var codeReceiver = new LocalJsonReceiver();
            IDictionary<string, string> result = codeReceiver.ReceiveCode(settings);

            if (result == null || result.Count == 0)
            {
                return false;
            }

            foreach (var key in result.Keys)
            {
                switch (key)
                {
                    case AccessToken:
                        settings.AccessToken = result[key];
                        break;
                    case ExpiresIn:
                        if (int.TryParse(result[key], out var seconds))
                        {
                            settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds(seconds);
                        }

                        break;
                    case RefreshToken:
                        settings.RefreshToken = result[key];
                        break;
                }
            }

            if (result.TryGetValue("error", out var error))
            {
                if (result.TryGetValue("error_description", out var errorDescription))
                {
                    throw new Exception(errorDescription);
                }

                if ("access_denied" == error)
                {
                    throw new UnauthorizedAccessException("Access denied");
                }

                throw new Exception(error);
            }

            if (result.TryGetValue(Code, out var code) && !string.IsNullOrEmpty(code))
            {
                settings.Code = code;
                GenerateRefreshToken(settings);
                return !string.IsNullOrEmpty(settings.AccessToken);
            }

            return true;
        }

        /// <summary>
        /// Authorize via an embedded browser
        /// If this works, return the code
        /// </summary>
        /// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
        /// <returns>true if completed, false if canceled</returns>
        private static bool AuthorizeViaEmbeddedBrowser(OAuth2Settings settings)
        {
            if (string.IsNullOrEmpty(settings.CloudServiceName))
            {
                throw new ArgumentNullException(nameof(settings.CloudServiceName));
            }

            if (settings.BrowserSize == Size.Empty)
            {
                throw new ArgumentNullException(nameof(settings.BrowserSize));
            }

            OAuthLoginForm loginForm = new OAuthLoginForm($"Authorize {settings.CloudServiceName}", settings.BrowserSize, settings.FormattedAuthUrl, settings.RedirectUrl);
            loginForm.ShowDialog();
            if (!loginForm.IsOk) return false;
            if (loginForm.CallbackParameters.TryGetValue(Code, out var code) && !string.IsNullOrEmpty(code))
            {
                settings.Code = code;
                GenerateRefreshToken(settings);
                return true;
            }

            return UpdateFromCallback(settings, loginForm.CallbackParameters);
        }

        /// <summary>
        /// Authorize via a local server by using the LocalServerCodeReceiver
        /// If this works, return the code
        /// </summary>
        /// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
        /// <returns>true if completed</returns>
        private static bool AuthorizeViaLocalServer(OAuth2Settings settings)
        {
            var codeReceiver = new LocalServerCodeReceiver();
            IDictionary<string, string> result = codeReceiver.ReceiveCode(settings);

            if (result.TryGetValue(Code, out var code) && !string.IsNullOrEmpty(code))
            {
                settings.Code = code;
                GenerateRefreshToken(settings);
                return true;
            }

            if (result.TryGetValue("error", out var error))
            {
                if (result.TryGetValue("error_description", out var errorDescription))
                {
                    throw new Exception(errorDescription);
                }

                if ("access_denied" == error)
                {
                    throw new UnauthorizedAccessException("Access denied");
                }

                throw new Exception(error);
            }

            return false;
        }

        /// <summary>
        /// Simple helper to add the Authorization Bearer header
        /// </summary>
        /// <param name="webRequest">WebRequest</param>
        /// <param name="settings">OAuth2Settings</param>
        public static void AddOAuth2Credentials(HttpWebRequest webRequest, OAuth2Settings settings)
        {
            if (!string.IsNullOrEmpty(settings.AccessToken))
            {
                webRequest.Headers.Add("Authorization", "Bearer " + settings.AccessToken);
            }
        }

        /// <summary>
        /// Check and authenticate or refresh tokens
        /// </summary>
        /// <param name="settings">OAuth2Settings</param>
        public static void CheckAndAuthenticateOrRefresh(OAuth2Settings settings)
        {
            // Get Refresh / Access token
            if (string.IsNullOrEmpty(settings.RefreshToken))
            {
                if (!Authorize(settings))
                {
                    throw new Exception("Authentication cancelled");
                }
            }

            if (settings.IsAccessTokenExpired)
            {
                GenerateAccessToken(settings);
                // Get Refresh / Access token
                if (string.IsNullOrEmpty(settings.RefreshToken))
                {
                    if (!Authorize(settings))
                    {
                        throw new Exception("Authentication cancelled");
                    }

                    GenerateAccessToken(settings);
                }
            }

            if (settings.IsAccessTokenExpired)
            {
                throw new Exception("Authentication failed");
            }
        }

        /// <summary>
        /// CreateWebRequest ready for OAuth 2 access
        /// </summary>
        /// <param name="method">HTTPMethod</param>
        /// <param name="url"></param>
        /// <param name="settings">OAuth2Settings</param>
        /// <returns>HttpWebRequest</returns>
        public static HttpWebRequest CreateOAuth2WebRequest(HTTPMethod method, string url, OAuth2Settings settings)
        {
            CheckAndAuthenticateOrRefresh(settings);

            HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(url, method);
            AddOAuth2Credentials(webRequest, settings);
            return webRequest;
        }
    }
}