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
using System.Net.Http.Headers;
using System.Text;

namespace Greenshot.Base.Http
{
    /// <summary>
    /// What a user signs in to a server with: a user name with a password (or an API token), sent as Basic,
    /// or only a personal access token, sent as Bearer.
    /// </summary>
    public sealed class ServerCredentials
    {
        public ServerCredentials(string userName, string secret)
        {
            UserName = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
            Secret = secret ?? string.Empty;
        }

        /// <summary>
        /// The user name or e-mail address, null for a personal access token
        /// </summary>
        public string UserName { get; }

        /// <summary>
        /// The password, API token or personal access token
        /// </summary>
        public string Secret { get; }

        /// <summary>
        /// True when the secret is a personal access token without a user name
        /// </summary>
        public bool IsToken => UserName == null;

        /// <summary>
        /// The Authorization header for these credentials
        /// </summary>
        public AuthenticationHeaderValue CreateAuthorization() =>
            IsToken
                ? new AuthenticationHeaderValue("Bearer", Secret)
                : new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{UserName}:{Secret}")));

        /// <summary>
        /// Never the secret, these can end up in a log
        /// </summary>
        public override string ToString() => IsToken ? "(token)" : UserName;
    }
}
