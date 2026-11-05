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

namespace Greenshot.Base.Http
{
    /// <summary>
    /// What the sign-in window offers for a server.
    /// </summary>
    public sealed class ServerSignInOptions
    {
        /// <param name="apiTokenUrl">the page where a cloud user creates an API token, null when the server has no cloud variant</param>
        /// <param name="isCloud">true when the url is a cloud server, null for never</param>
        public ServerSignInOptions(string apiTokenUrl = null, Func<Uri, bool> isCloud = null)
        {
            ApiTokenUrl = apiTokenUrl;
            IsCloud = isCloud ?? (_ => false);
        }

        /// <summary>
        /// The page where a cloud user creates an API token, null when the server has no cloud variant
        /// </summary>
        public string ApiTokenUrl { get; }

        /// <summary>
        /// True when the url is a cloud server, it preselects the e-mail address and API token
        /// </summary>
        public Func<Uri, bool> IsCloud { get; }
    }
}
