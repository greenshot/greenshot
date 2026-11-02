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

using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Base.Http
{
    /// <summary>
    /// The sign-in to the account of a service, shown with <see cref="Wpf.Views.ServerAccountView"/>:
    /// <see cref="ServerSignIn"/> for a server (Jira, Confluence), <see cref="Core.OAuth.OAuth2SignIn"/> for an OAuth service (Box, Dropbox).
    /// </summary>
    public interface IAccountSignIn
    {
        /// <summary>
        /// The name of the service, for logs
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Signed in during this session, the account is known
        /// </summary>
        bool IsSignedIn { get; }

        /// <summary>
        /// The account that is signed in, null when not
        /// </summary>
        string Account { get; }

        /// <summary>
        /// Something is stored to sign in with, not checked yet
        /// </summary>
        bool HasStoredCredentials { get; }

        /// <summary>
        /// Sign in, also when signed in (another account)
        /// </summary>
        /// <returns>false when the user canceled</returns>
        Task<bool> SignInAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Connect with what is stored, without asking the user
        /// </summary>
        /// <returns>the account, null when nothing is stored</returns>
        Task<string> TestAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Forget the sign-in, also what is stored
        /// </summary>
        void SignOut();
    }
}
