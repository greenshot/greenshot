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
using System.Collections.Generic;

namespace Greenshot.Base.Core.OAuth
{
    /// <summary>
    /// Settings for the OAuth 2 protocol
    /// </summary>
    public class OAuth2Settings
    {
        /// <summary>
        /// Specify the name of the cloud service, so it can be used in window titles, logs etc
        /// </summary>
        public string CloudServiceName { get; set; }

        /// <summary>
        /// The OAuth 2 client id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The OAuth 2 client secret
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// The authorization endpoint of the service, without parameters: client_id, response_type, state, redirect_uri and the PKCE challenge are added
        /// </summary>
        public string AuthorizeUrl { get; set; }

        /// <summary>
        /// Extra parameters for the authorization URL of this service, e.g. token_access_type=offline for Dropbox
        /// </summary>
        public IDictionary<string, string> AuthorizeParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The loopback ports of the redirect URL http://localhost:{port}/authorize/, tried in this order.
        /// Every service needs all of them registered as redirect URIs, as not every service accepts any port on a loopback redirect URI.
        /// </summary>
        public IList<int> LoopbackPorts { get; set; } = new List<int> { 31251, 31252, 31253, 31254, 31255 };

        /// <summary>
        /// Send a PKCE challenge (S256, RFC 7636) with the authorization and the verifier with the code, for a service that supports it
        /// </summary>
        public bool UsePkce { get; set; } = true;

        /// <summary>
        /// The URL to get a Token
        /// </summary>
        public string TokenUrl { get; set; }

        /// <summary>
        /// The loopback redirect URL of the running authorization, set by the LocalServerCodeReceiver
        /// </summary>
        public string RedirectUrl { get; set; }

        /// <summary>
        /// Bearer token for accessing OAuth 2 services
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Expire time for the AccessToken, this this time (-60 seconds) is passed a new AccessToken needs to be generated with the RefreshToken
        /// </summary>
        public DateTimeOffset AccessTokenExpires { get; set; }

        /// <summary>
        /// Return true if the access token is expired.
        /// Important "side-effect": if true is returned the AccessToken will be set to null!
        /// </summary>
        public bool IsAccessTokenExpired
        {
            get
            {
                if (AccessTokenExpires == default)
                {
                    return false;
                }

                bool expired = true;
                if (!string.IsNullOrEmpty(AccessToken))
                {
                    expired = DateTimeOffset.Now.AddSeconds(60) > AccessTokenExpires;
                }

                // Make sure the token is not usable
                if (expired)
                {
                    AccessToken = null;
                }

                return expired;
            }
        }

        /// <summary>
        /// Token used to get a new Access Token
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// Put anything in here which is needed for the OAuth 2 implementation of this specific service but isn't generic, e.g. for Google there is a "scope"
        /// </summary>
        public IDictionary<string, string> AdditionalAttributes { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The PKCE code verifier of the running authorization, never stored
        /// </summary>
        public string CodeVerifier { get; set; }

        /// <summary>
        /// This contains the code returned from the authorization, but only shortly after it was received.
        /// It will be cleared as soon as it was used.
        /// </summary>
        public string Code { get; set; }
    }
}