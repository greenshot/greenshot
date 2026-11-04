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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;

namespace Greenshot.Base.Core.OAuth
{
    /// <summary>
    /// The account of an OAuth 2 service (Box, Dropbox) for the plugin settings: sign in in the browser, test the stored refresh token, sign out.
    /// </summary>
    public sealed class OAuth2SignIn : IAccountSignIn
    {
        private readonly Func<OAuth2Settings> _createSettings;
        private readonly Action<OAuth2Settings> _store;
        private readonly Func<OAuth2Settings, IUserInteraction, CancellationToken, Task<string>> _getAccountAsync;
        private string _account;

        /// <param name="serviceName">the name of the service</param>
        /// <param name="createSettings">the settings of the service with the stored tokens</param>
        /// <param name="store">stores the tokens of the settings in the configuration, called on the UI thread</param>
        /// <param name="getAccountAsync">the name of the account (e.g. the e-mail address), with a request from OAuth2Helper.CreateOAuth2RequestAsync</param>
        public OAuth2SignIn(string serviceName, Func<OAuth2Settings> createSettings, Action<OAuth2Settings> store,
            Func<OAuth2Settings, IUserInteraction, CancellationToken, Task<string>> getAccountAsync)
        {
            ServiceName = serviceName;
            _createSettings = createSettings ?? throw new ArgumentNullException(nameof(createSettings));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _getAccountAsync = getAccountAsync ?? throw new ArgumentNullException(nameof(getAccountAsync));
        }

        public string ServiceName { get; }

        public bool IsSignedIn => _account != null && HasStoredCredentials;

        public string Account => IsSignedIn ? _account : null;

        public bool HasStoredCredentials => !string.IsNullOrEmpty(_createSettings().RefreshToken);

        public async Task<bool> SignInAsync(CancellationToken cancellationToken = default)
        {
            var settings = _createSettings();
            settings.RefreshToken = null;
            settings.AccessToken = null;
            settings.AccessTokenExpires = default;
            try
            {
                if (!await OAuth2Helper.AuthorizeAsync(settings, null, cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }

                _account = await _getAccountAsync(settings, HeadlessUserInteraction.Instance, cancellationToken).ConfigureAwait(false);
                return true;
            }
            finally
            {
                // Only a new sign-in replaces the stored one
                if (!string.IsNullOrEmpty(settings.RefreshToken))
                {
                    await StoreAsync(settings).ConfigureAwait(false);
                }
            }
        }

        public async Task<string> TestAsync(CancellationToken cancellationToken = default)
        {
            var settings = _createSettings();
            if (string.IsNullOrEmpty(settings.RefreshToken))
            {
                return null;
            }

            try
            {
                // Never opens the browser, a refresh token the service doesn't accept anymore is "not signed in"
                _account = await _getAccountAsync(settings, HeadlessUserInteraction.Instance, cancellationToken).ConfigureAwait(false);
            }
            catch (InteractionRequiredException)
            {
                _account = null;
            }
            finally
            {
                await StoreAsync(settings).ConfigureAwait(false);
            }

            return Account;
        }

        public void SignOut()
        {
            _account = null;
            var settings = _createSettings();
            settings.RefreshToken = null;
            settings.AccessToken = null;
            settings.AccessTokenExpires = default;
            // Sign out is clicked on the UI thread
            _store(settings);
        }

        private Task StoreAsync(OAuth2Settings settings) => UiDispatcher.Current.InvokeAsync(() => _store(settings), CancellationToken.None);
    }
}
