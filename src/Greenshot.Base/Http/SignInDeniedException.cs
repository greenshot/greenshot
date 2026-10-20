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
using System.Net;

namespace Greenshot.Base.Http
{
    /// <summary>
    /// The server doesn't take credentials for now, e.g. Jira or Confluence Data Center ask for a CAPTCHA after failed sign-ins.
    /// </summary>
    public class SignInDeniedException : UnauthorizedAccessException
    {
        public SignInDeniedException(string reason) : base($"Sign-in denied: {reason}")
        {
        }

        /// <summary>
        /// Throw when the server answered 403 with the header which says why it denies the sign-in,
        /// e.g. X-Authentication-Denied-Reason of Jira and Confluence Data Center
        /// </summary>
        public static void ThrowWhenDenied(HttpResult result, string reasonHeader)
        {
            if (result?.StatusCode == HttpStatusCode.Forbidden && result.Headers.TryGetValue(reasonHeader, out var reason))
            {
                throw new SignInDeniedException(reason);
            }
        }
    }
}
