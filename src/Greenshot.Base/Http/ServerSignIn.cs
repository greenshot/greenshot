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
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf.ViewModels;
using Greenshot.Base.Wpf.Views;
using log4net;

namespace Greenshot.Base.Http
{
    /// <summary>
    /// The sign-in to the server of a plugin (Jira, Confluence): the credentials of this session or the ones stored in the
    /// Windows Credential Manager first, the sign-in window only when there are none or the server rejects them.
    /// </summary>
    public sealed class ServerSignIn
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ServerSignIn));
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly Func<string> _url;
        private readonly Func<string, ServerCredentials, CancellationToken, Task<string>> _connectAsync;
        private volatile SignedIn _signedIn;
        private volatile bool _verified;

        /// <param name="serviceName">the name of the service in the sign-in window, e.g. Jira</param>
        /// <param name="url">the url of the server from the configuration, also the target in the Credential Manager</param>
        /// <param name="connectAsync">
        /// connects with the credentials to the url and returns the name of the account,
        /// throws <see cref="UnauthorizedAccessException"/> when the server rejects them
        /// </param>
        /// <param name="options">what the sign-in window offers</param>
        public ServerSignIn(string serviceName, Func<string> url, Func<string, ServerCredentials, CancellationToken, Task<string>> connectAsync, ServerSignInOptions options = null)
        {
            ServiceName = serviceName;
            _url = url ?? throw new ArgumentNullException(nameof(url));
            _connectAsync = connectAsync ?? throw new ArgumentNullException(nameof(connectAsync));
            Options = options ?? new ServerSignInOptions();
        }

        /// <summary>
        /// The sign-in changed: signed in, out or to another account
        /// </summary>
        public event EventHandler Changed;

        public string ServiceName { get; }

        public ServerSignInOptions Options { get; }

        /// <summary>
        /// The url of the server in the configuration
        /// </summary>
        public string Url => _url()?.Trim();

        /// <summary>
        /// True when signed in to the url of the configuration
        /// </summary>
        public bool IsSignedIn => _signedIn is { } signedIn && signedIn.Url == Url;

        /// <summary>
        /// The account which is signed in, null when not signed in
        /// </summary>
        public string Account => IsSignedIn ? _signedIn.Account : null;

        /// <summary>
        /// True when the Credential Manager has credentials for the url
        /// </summary>
        public bool HasStoredCredentials => CredentialStore.Read(Url) != null;

        /// <summary>
        /// Make sure there is a checked sign-in: with the credentials of this session or the stored ones, otherwise the sign-in window
        /// </summary>
        /// <returns>false when the user canceled the sign-in</returns>
        /// <exception cref="Exception">no connection to the server</exception>
        public async Task<bool> EnsureSignedInAsync(CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                string url = Url;
                var signedIn = _signedIn;
                if (_verified && signedIn != null && signedIn.Url == url)
                {
                    return true;
                }

                var sessionCredentials = signedIn?.Url == url ? signedIn.Credentials : null;
                var storedCredentials = CredentialStore.Read(url);
                string error = null;
                foreach (var credentials in new[] { sessionCredentials, storedCredentials })
                {
                    if (credentials == null || (credentials == storedCredentials && SameAs(credentials, sessionCredentials)))
                    {
                        continue;
                    }

                    try
                    {
                        await ConnectAsync(url, credentials, cancellationToken).ConfigureAwait(false);
                        return true;
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Log.InfoFormat("{0} rejected the credentials of {1}: {2}", ServiceName, credentials, ex.Message);
                        error = DescribeError(ex);
                    }
                }

                return await PromptAsync(url, sessionCredentials ?? storedCredentials, storedCredentials != null, error, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Show the sign-in window, also when signed in (another account, a new token)
        /// </summary>
        /// <returns>false when the user canceled</returns>
        public async Task<bool> SignInAsync(CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                string url = Url;
                var storedCredentials = CredentialStore.Read(url);
                var credentials = _signedIn?.Url == url ? _signedIn.Credentials : storedCredentials;
                return await PromptAsync(url, credentials, storedCredentials != null, null, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Connect again with the credentials of this session or the stored ones, without the sign-in window
        /// </summary>
        /// <returns>the account, null when there are no credentials</returns>
        /// <exception cref="UnauthorizedAccessException">the server rejected the credentials</exception>
        public async Task<string> TestAsync(CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                string url = Url;
                var credentials = _signedIn?.Url == url ? _signedIn.Credentials : CredentialStore.Read(url);
                if (credentials == null)
                {
                    return null;
                }

                await ConnectAsync(url, credentials, cancellationToken).ConfigureAwait(false);
                return Account;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// The next <see cref="EnsureSignedInAsync"/> checks the credentials again, e.g. after a session timeout
        /// </summary>
        public void Recheck()
        {
            _verified = false;
        }

        /// <summary>
        /// Forget the credentials, also the stored ones
        /// </summary>
        public void SignOut()
        {
            _verified = false;
            _signedIn = null;
            if (!CredentialStore.Delete(Url))
            {
                Log.WarnFormat("Couldn't remove the stored credentials of {0}", ServiceName);
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// The text for the user when connecting failed
        /// </summary>
        public static string DescribeError(Exception exception) =>
            exception switch
            {
                SignInDeniedException => Texts.Core.SigninDenied,
                UnauthorizedAccessException => Texts.Core.SigninRejected,
                _ => string.Format(Texts.Core.SigninFailed, exception?.Message)
            };

        private async Task<bool> PromptAsync(string url, ServerCredentials credentials, bool remember, string error, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(url))
            {
                return false;
            }

            var viewModel = new ServerSignInViewModel(ServiceName, url, Options, credentials, remember || !GreenshotEnvironment.LeavesNoTraces, error,
                (newCredentials, token) => ConnectAsync(url, newCredentials, token));
            var result = await UiDispatcher.Current.InvokeAsync(() => ServerSignInWindow.Ask(viewModel), cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return false;
            }

            bool stored = viewModel.Remember ? CredentialStore.Write(url, result) : CredentialStore.Delete(url);
            if (!stored)
            {
                Log.WarnFormat("Couldn't update the stored credentials of {0}", ServiceName);
            }

            return true;
        }

        private async Task ConnectAsync(string url, ServerCredentials credentials, CancellationToken cancellationToken)
        {
            string account;
            try
            {
                account = await _connectAsync(url, credentials, cancellationToken).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                // The credentials of the session don't work anymore (token revoked, password changed)
                var signedIn = _signedIn;
                if (signedIn != null && signedIn.Url == url && SameAs(credentials, signedIn.Credentials))
                {
                    _verified = false;
                    _signedIn = null;
                    Changed?.Invoke(this, EventArgs.Empty);
                }

                throw;
            }

            _signedIn = new SignedIn(url, credentials, string.IsNullOrEmpty(account) ? credentials.ToString() : account);
            _verified = true;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private static bool SameAs(ServerCredentials credentials, ServerCredentials other) =>
            other != null && credentials.UserName == other.UserName && credentials.Secret == other.Secret;

        private sealed class SignedIn
        {
            public SignedIn(string url, ServerCredentials credentials, string account)
            {
                Url = url;
                Credentials = credentials;
                Account = account;
            }

            public string Url { get; }

            public ServerCredentials Credentials { get; }

            public string Account { get; }
        }
    }
}
