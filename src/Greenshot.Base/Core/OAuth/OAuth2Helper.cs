/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Controls;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;

namespace Greenshot.Base.Core.OAuth
{
    /// <summary>
    /// Code to simplify OAuth 2, everything is async (rule R12): the authorization waits for the browser without blocking a thread.
    /// </summary>
    public static class OAuth2Helper
    {
        private const string RefreshToken = "refresh_token";
        private const string AccessToken = "access_token";
        private const string Code = "code";
        private const string ClientId = "client_id";
        private const string ClientSecret = "client_secret";
        private const string GrantType = "grant_type";
        private const string AuthorizationCode = "authorization_code";
        private const string RedirectUri = "redirect_uri";
        private const string ExpiresIn = "expires_in";

        /// <summary>
        /// One authorization / token refresh per cloud service at a time, a second upload waits for the first to get the token.
        /// </summary>
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> ServiceLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Generate an OAuth 2 Token by using the supplied code
        /// </summary>
        /// <param name="settings">OAuth2Settings to update with the information that was retrieved</param>
        /// <param name="cancellationToken">CancellationToken</param>
        public static async Task GenerateRefreshTokenAsync(OAuth2Settings settings, CancellationToken cancellationToken)
        {
            IDictionary<string, object> data = new Dictionary<string, object>
            {
                // Use the returned code to get a refresh code
                { Code, settings.Code },
                { ClientId, settings.ClientId },
                { ClientSecret, settings.ClientSecret },
                { RedirectUri, settings.RedirectUrl },
                { GrantType, AuthorizationCode }
            };
            foreach (string key in settings.AdditionalAttributes.Keys)
            {
                data.Add(key, settings.AdditionalAttributes[key]);
            }

            string accessTokenJsonResult = await NetworkHelper.PostFormUrlEncodedAsync(settings.TokenUrl, data, cancellationToken, true).ConfigureAwait(false);

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
        /// <param name="cancellationToken">CancellationToken</param>
        public static async Task GenerateAccessTokenAsync(OAuth2Settings settings, CancellationToken cancellationToken)
        {
            IDictionary<string, object> data = new Dictionary<string, object>
            {
                { RefreshToken, settings.RefreshToken },
                { ClientId, settings.ClientId },
                { ClientSecret, settings.ClientSecret },
                { GrantType, RefreshToken }
            };
            foreach (string key in settings.AdditionalAttributes.Keys)
            {
                data.Add(key, settings.AdditionalAttributes[key]);
            }

            string accessTokenJsonResult = await NetworkHelper.PostFormUrlEncodedAsync(settings.TokenUrl, data, cancellationToken, true).ConfigureAwait(false);

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
        /// Authorize by using the mode specified in the settings, this needs the user (a browser).
        /// </summary>
        /// <param name="settings">OAuth2Settings</param>
        /// <param name="userInteraction">IUserInteraction, the authorization fails with an InteractionRequiredException when it's not interactive</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>false if it was canceled, true if it worked, exception if not</returns>
        public static Task<bool> AuthorizeAsync(OAuth2Settings settings, IUserInteraction userInteraction, CancellationToken cancellationToken)
        {
            if (userInteraction != null && !userInteraction.IsInteractive)
            {
                throw new InteractionRequiredException($"Authorize {settings.CloudServiceName}");
            }

            return settings.AuthorizeMode switch
            {
                OAuth2AuthorizeMode.LocalServer => AuthorizeViaLocalServerAsync(settings, cancellationToken),
                OAuth2AuthorizeMode.EmbeddedBrowser => AuthorizeViaEmbeddedBrowserAsync(settings, cancellationToken),
                OAuth2AuthorizeMode.JsonReceiver => AuthorizeViaDefaultBrowserAsync(settings, cancellationToken),
                _ => throw new NotImplementedException($"Authorize mode '{settings.AuthorizeMode}' is not 'yet' implemented."),
            };
        }

        /// <summary>
        /// Authorize via the default browser, via the Greenshot website.
        /// It will wait for a Json post.
        /// If this works, return the code
        /// </summary>
        /// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>true if completed, false if canceled</returns>
        private static async Task<bool> AuthorizeViaDefaultBrowserAsync(OAuth2Settings settings, CancellationToken cancellationToken)
        {
            var codeReceiver = new LocalJsonReceiver();
            IDictionary<string, string> result = await codeReceiver.ReceiveCodeAsync(settings, cancellationToken).ConfigureAwait(false);

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

            ThrowOnError(result);

            if (result.TryGetValue(Code, out var code) && !string.IsNullOrEmpty(code))
            {
                settings.Code = code;
                await GenerateRefreshTokenAsync(settings, cancellationToken).ConfigureAwait(false);
                return !string.IsNullOrEmpty(settings.AccessToken);
            }

            return true;
        }

        private static void ThrowOnError(IDictionary<string, string> result)
        {
            if (!result.TryGetValue("error", out var error))
            {
                return;
            }

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

        /// <summary>
        /// Authorize via an embedded browser (a form on the UI thread)
        /// If this works, return the code
        /// </summary>
        /// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>true if completed, false if canceled</returns>
        private static async Task<bool> AuthorizeViaEmbeddedBrowserAsync(OAuth2Settings settings, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(settings.CloudServiceName))
            {
                throw new ArgumentNullException(nameof(settings.CloudServiceName));
            }

            if (settings.BrowserSize == Size.Empty)
            {
                throw new ArgumentNullException(nameof(settings.BrowserSize));
            }

            var callbackParameters = await UiDispatcher.Current.InvokeAsync(() =>
            {
                using var loginForm = new OAuthLoginForm($"Authorize {settings.CloudServiceName}", settings.BrowserSize, settings.FormattedAuthUrl, settings.RedirectUrl);
                loginForm.ShowDialog();
                return loginForm.IsOk ? loginForm.CallbackParameters : null;
            }, cancellationToken).ConfigureAwait(false);
            if (callbackParameters == null)
            {
                return false;
            }

            if (callbackParameters.TryGetValue(Code, out var code) && !string.IsNullOrEmpty(code))
            {
                settings.Code = code;
                await GenerateRefreshTokenAsync(settings, cancellationToken).ConfigureAwait(false);
                return true;
            }

            return UpdateFromCallback(settings, callbackParameters);
        }

        /// <summary>
        /// Authorize via a local server by using the LocalServerCodeReceiver
        /// If this works, return the code
        /// </summary>
        /// <param name="settings">OAuth2Settings with the Auth / Token url etc</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>true if completed</returns>
        private static async Task<bool> AuthorizeViaLocalServerAsync(OAuth2Settings settings, CancellationToken cancellationToken)
        {
            var codeReceiver = new LocalServerCodeReceiver();
            IDictionary<string, string> result = await codeReceiver.ReceiveCodeAsync(settings, cancellationToken).ConfigureAwait(false);

            if (result.TryGetValue(Code, out var code) && !string.IsNullOrEmpty(code))
            {
                settings.Code = code;
                await GenerateRefreshTokenAsync(settings, cancellationToken).ConfigureAwait(false);
                return true;
            }

            ThrowOnError(result);
            return false;
        }

        /// <summary>
        /// Check and authenticate or refresh tokens, one at a time per cloud service.
        /// </summary>
        /// <param name="settings">OAuth2Settings</param>
        /// <param name="userInteraction">IUserInteraction for the authorization, null: the default</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>false when the user didn't authorize</returns>
        public static async Task<bool> CheckAndAuthenticateOrRefreshAsync(OAuth2Settings settings, IUserInteraction userInteraction, CancellationToken cancellationToken)
        {
            var serviceLock = ServiceLocks.GetOrAdd(settings.CloudServiceName ?? settings.TokenUrl ?? string.Empty, _ => new SemaphoreSlim(1, 1));
            await serviceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                userInteraction ??= UserInteraction.Current;
                // Get Refresh / Access token
                if (string.IsNullOrEmpty(settings.RefreshToken) && !await AuthorizeAsync(settings, userInteraction, cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }

                if (settings.IsAccessTokenExpired)
                {
                    await GenerateAccessTokenAsync(settings, cancellationToken).ConfigureAwait(false);
                    // Get Refresh / Access token
                    if (string.IsNullOrEmpty(settings.RefreshToken))
                    {
                        if (!await AuthorizeAsync(settings, userInteraction, cancellationToken).ConfigureAwait(false))
                        {
                            return false;
                        }

                        await GenerateAccessTokenAsync(settings, cancellationToken).ConfigureAwait(false);
                    }
                }

                if (settings.IsAccessTokenExpired)
                {
                    throw new Exception("Authentication failed");
                }

                return true;
            }
            finally
            {
                serviceLock.Release();
            }
        }

        /// <summary>
        /// Create a request with the OAuth 2 bearer token, authorizes or refreshes the token first when needed.
        /// </summary>
        /// <param name="method">HttpMethod</param>
        /// <param name="url"></param>
        /// <param name="settings">OAuth2Settings</param>
        /// <param name="userInteraction">IUserInteraction for the authorization, null: the default</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>HttpRequestMessage, null when the user didn't authorize</returns>
        public static async Task<HttpRequestMessage> CreateOAuth2RequestAsync(HttpMethod method, string url, OAuth2Settings settings, IUserInteraction userInteraction, CancellationToken cancellationToken)
        {
            if (!await CheckAndAuthenticateOrRefreshAsync(settings, userInteraction, cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            var request = new HttpRequestMessage(method, url);
            if (!string.IsNullOrEmpty(settings.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
            }

            return request;
        }
    }
}
