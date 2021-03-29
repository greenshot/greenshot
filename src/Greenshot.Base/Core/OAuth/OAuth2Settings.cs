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

namespace Greenshot.Base.Core.OAuth
{
    /// <summary>
    /// Settings for the OAuth 2 protocol
    /// </summary>
    public class OAuth2Settings
    {
        public OAuth2Settings()
        {
            AdditionalAttributes = new Dictionary<string, string>();
            // Create a default state
            var state = Guid.NewGuid().ToString();
            // Only store a small part of the GUID
            State = state.Substring(0, state.IndexOf('-') - 1);
            AuthorizeMode = OAuth2AuthorizeMode.Unknown;
        }

        public OAuth2AuthorizeMode AuthorizeMode { get; set; }

        /// <summary>
        /// Specify the name of the cloud service, so it can be used in window titles, logs etc
        /// </summary>
        public string CloudServiceName { get; set; }

        /// <summary>
        /// Specify the size of the embedded Browser, if using this
        /// </summary>
        public Size BrowserSize { get; set; }

        /// <summary>
        /// The OAuth 2 client id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The OAuth 2 client secret
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// The OAuth 2 state, this is something that is passed to the server, is not processed but returned back to the client.
        /// e.g. a correlation ID
        /// Default this is filled with a new Guid
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// The authorization URL where the values of this class can be "injected"
        /// </summary>
        public string AuthUrlPattern { get; set; }

        /// <summary>
        /// Get formatted Auth url (this will call a FormatWith(this) on the AuthUrlPattern
        /// </summary>
        public string FormattedAuthUrl => AuthUrlPattern.FormatWith(this);

        /// <summary>
        /// The URL to get a Token
        /// </summary>
        public string TokenUrl { get; set; }

        /// <summary>
        /// This is the redirect URL, in some implementations this is automatically set (LocalServerCodeReceiver)
        /// In some implementations this could be e.g. urn:ietf:wg:oauth:2.0:oob or urn:ietf:wg:oauth:2.0:oob:auto
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
        public IDictionary<string, string> AdditionalAttributes { get; set; }

        /// <summary>
        /// This contains the code returned from the authorization, but only shortly after it was received.
        /// It will be cleared as soon as it was used.
        /// </summary>
        public string Code { get; set; }
    }
}