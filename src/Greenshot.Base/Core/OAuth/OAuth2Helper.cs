/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Serialization;
using log4net;

namespace Greenshot.Base.Core.OAuth
{
    /// <summary>
    /// Code to simplify OAuth 2, everything is async (rule R12): the authorization waits for the browser without blocking a thread.
    /// </summary>
    public static class OAuth2Helper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OAuth2Helper));
        private const string RefreshToken = "refresh_token";
        private const string AccessToken = "access_token";
        private const string Code = "code";
        private const string ClientId = "client_id";
        private const string ClientSecret = "client_secret";
        private const string GrantType = "grant_type";
        private const string AuthorizationCode = "authorization_code";
        private const string RedirectUri = "redirect_uri";
        private const string ExpiresIn = "expires_in";
        private const string CodeVerifier = "code_verifier";

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
            var data = new Dictionary<string, string>
            {
                // Use the returned code to get a refresh code
                { Code, settings.Code },
                { ClientId, settings.ClientId },
                { ClientSecret, settings.ClientSecret },
                { RedirectUri, settings.RedirectUrl },
                { GrantType, AuthorizationCode }
            };
            if (!string.IsNullOrEmpty(settings.CodeVerifier))
            {
                data.Add(CodeVerifier, settings.CodeVerifier);
            }
            foreach (string key in settings.AdditionalAttributes.Keys)
            {
                data.Add(key, settings.AdditionalAttributes[key]);
            }

            // The error content of the token endpoint has the reason (e.g. invalid_grant), it is read below
            string accessTokenJsonResult = (await HttpService.Current.PostFormAsync(settings.TokenUrl, data, cancellationToken).ConfigureAwait(false)).Content;

            IDictionary<string, object> refreshTokenResult = JsonSerializer.Deserialize<Dictionary<string, object>>(accessTokenJsonResult, GreenshotJson.Options);
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
                    settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds(Convert.ToDouble(seconds));
                }
            }

            settings.Code = null;
        }

        /// <summary>
        /// Go out and retrieve a new access token via refresh-token with the TokenUrl in the settings
        /// Will update the access token, refresh token, expire date
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="cancellationToken">CancellationToken</param>
        public static async Task GenerateAccessTokenAsync(OAuth2Settings settings, CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, string>
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

            // The error content of the token endpoint has the reason (e.g. invalid_grant), it is read below
            string accessTokenJsonResult = (await HttpService.Current.PostFormAsync(settings.TokenUrl, data, cancellationToken).ConfigureAwait(false)).Content;

            // gives as described here: https://developers.google.com/identity/protocols/OAuth2InstalledApp
            //  "access_token":"1/fFAGRNJru1FTz70BzhT3Zg",
            //  "expires_in":3920,
            //  "token_type":"Bearer",

            IDictionary<string, object> accessTokenResult = JsonSerializer.Deserialize<Dictionary<string, object>>(accessTokenJsonResult, GreenshotJson.Options);
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
                    settings.AccessTokenExpires = DateTimeOffset.Now.AddSeconds(Convert.ToDouble(seconds));
                }
            }
        }

        /// <summary>
        /// Authorize in the default browser, the service redirects back to a listener on a loopback address (RFC 8252).
        /// The state is random for every authorization and checked, the code is protected with PKCE when the service supports it.
        /// </summary>
        /// <param name="settings">OAuth2Settings</param>
        /// <param name="userInteraction">IUserInteraction, the authorization fails with an InteractionRequiredException when it's not interactive</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>false if it was canceled, true if it worked, exception if not</returns>
        public static async Task<bool> AuthorizeAsync(OAuth2Settings settings, IUserInteraction userInteraction, CancellationToken cancellationToken)
        {
            if (userInteraction != null && !userInteraction.IsInteractive)
            {
                throw new InteractionRequiredException($"Authorize {settings.CloudServiceName}");
            }

            using var codeReceiver = new LocalServerCodeReceiver();
            settings.RedirectUrl = codeReceiver.Start(settings.LoopbackPorts);
            string state = CreateRandomString();
            settings.CodeVerifier = settings.UsePkce ? CreateRandomString() : null;
            try
            {
                Log.InfoFormat("Opening the browser to sign in to {0}", settings.CloudServiceName);
                using (Process.Start(new ProcessStartInfo(CreateAuthorizeUrl(settings, state)) { UseShellExecute = true }))
                {
                    // Only started
                }

                IDictionary<string, string> result = await codeReceiver.WaitForRedirectAsync(state, settings.CloudServiceName, cancellationToken).ConfigureAwait(false);
                if (result.TryGetValue(Code, out var code) && !string.IsNullOrEmpty(code))
                {
                    settings.Code = code;
                    await GenerateRefreshTokenAsync(settings, cancellationToken).ConfigureAwait(false);
                    return true;
                }

                ThrowOnError(result);
                return false;
            }
            finally
            {
                settings.CodeVerifier = null;
            }
        }

        /// <summary>
        /// The authorization URL with the client id, the state, the redirect URL, the PKCE challenge and the parameters of the service
        /// </summary>
        internal static string CreateAuthorizeUrl(OAuth2Settings settings, string state)
        {
            var parameters = new Dictionary<string, string>
            {
                { ClientId, settings.ClientId },
                { "response_type", Code },
                { "state", state },
                { RedirectUri, settings.RedirectUrl }
            };
            if (!string.IsNullOrEmpty(settings.CodeVerifier))
            {
                parameters.Add("code_challenge", CreateCodeChallenge(settings.CodeVerifier));
                parameters.Add("code_challenge_method", "S256");
            }

            foreach (var parameter in settings.AuthorizeParameters)
            {
                parameters[parameter.Key] = parameter.Value;
            }

            return settings.AuthorizeUrl + "?" + string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value ?? string.Empty)}"));
        }

        /// <summary>
        /// 32 random bytes as base64url (43 characters), for the state and the PKCE code verifier
        /// </summary>
        internal static string CreateRandomString()
        {
            byte[] bytes = new byte[32];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }

            return Base64Url(bytes);
        }

        /// <summary>
        /// The S256 PKCE code challenge: base64url of the SHA-256 of the verifier (RFC 7636 section 4.2)
        /// </summary>
        internal static string CreateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            return Base64Url(sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier)));
        }

        private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

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
