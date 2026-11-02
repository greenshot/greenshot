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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Base.Languages;

namespace Greenshot.Base.Wpf.ViewModels
{
    /// <summary>
    /// The sign-in window of a server: how to sign in (cloud e-mail and API token, user name and password, personal access token),
    /// the credentials and whether to remember them. Signing in checks the credentials with the server, the window stays open when they are rejected.
    /// </summary>
    public sealed class ServerSignInViewModel : INotifyPropertyChanged
    {
        private readonly Func<ServerCredentials, CancellationToken, Task> _connectAsync;
        private bool _isCloud;
        private bool _isPassword;
        private bool _isToken;
        private string _userName;
        private bool _remember;
        private string _message;
        private bool _isError;
        private bool _isBusy;

        /// <param name="serviceName">e.g. Jira</param>
        /// <param name="url">the server</param>
        /// <param name="options">what to offer</param>
        /// <param name="credentials">the credentials to show, null for none</param>
        /// <param name="remember">the start value of "remember"</param>
        /// <param name="error">why the window is shown, e.g. the stored credentials were rejected; null for none</param>
        /// <param name="connectAsync">checks the credentials, throws when they don't work</param>
        public ServerSignInViewModel(string serviceName, string url, ServerSignInOptions options, ServerCredentials credentials, bool remember, string error,
            Func<ServerCredentials, CancellationToken, Task> connectAsync)
        {
            _connectAsync = connectAsync ?? throw new ArgumentNullException(nameof(connectAsync));
            Title = string.Format(Texts.Core.SigninTitle, serviceName);
            Url = url;
            ApiTokenUrl = options?.ApiTokenUrl;
            _userName = credentials?.UserName;
            Secret = credentials?.Secret;
            _remember = remember;
            _message = error;
            _isError = error != null;

            bool isCloudServer = ApiTokenUrl != null && Uri.TryCreate(url, UriKind.Absolute, out var uri) && options.IsCloud(uri);
            if (credentials?.IsToken == true)
            {
                _isToken = true;
            }
            else if (isCloudServer)
            {
                _isCloud = true;
            }
            else
            {
                _isPassword = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Title { get; }

        public string Url { get; }

        /// <summary>
        /// The page to create an API token, null when there is no cloud variant
        /// </summary>
        public string ApiTokenUrl { get; }

        public bool OffersCloud => ApiTokenUrl != null;

        /// <summary>
        /// Cloud: the e-mail address with an API token
        /// </summary>
        public bool IsCloud
        {
            get => _isCloud;
            set => SetKind(value, ref _isCloud);
        }

        /// <summary>
        /// The user name with the password
        /// </summary>
        public bool IsPassword
        {
            get => _isPassword;
            set => SetKind(value, ref _isPassword);
        }

        /// <summary>
        /// Only a personal access token
        /// </summary>
        public bool IsToken
        {
            get => _isToken;
            set => SetKind(value, ref _isToken);
        }

        public bool ShowsUserName => !_isToken;

        public string UserNameLabel => _isCloud ? Texts.Core.SigninEmail : Texts.Core.SigninUserName;

        public string SecretLabel => _isCloud ? Texts.Core.SigninApiToken : _isToken ? Texts.Core.SigninAccessToken : Texts.Core.SigninPassword;

        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The password or token, set by the view (a PasswordBox doesn't bind)
        /// </summary>
        public string Secret { get; set; }

        public bool Remember
        {
            get => _remember;
            set
            {
                _remember = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// What happens or why the sign-in didn't work, null for nothing
        /// </summary>
        public string Message => _message;

        public bool HasMessage => !string.IsNullOrEmpty(_message);

        /// <summary>
        /// True when the message is an error
        /// </summary>
        public bool IsError => _isError;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsIdle));
            }
        }

        public bool IsIdle => !_isBusy;

        /// <summary>
        /// The credentials which worked, null until then
        /// </summary>
        public ServerCredentials Result { get; private set; }

        /// <summary>
        /// Check the credentials with the server
        /// </summary>
        /// <returns>true when they work, otherwise <see cref="Message"/> says why not</returns>
        public async Task<bool> SignInAsync(CancellationToken cancellationToken)
        {
            if (_isBusy)
            {
                return false;
            }

            var credentials = new ServerCredentials(_isToken ? null : _userName, Secret);
            if ((!_isToken && credentials.UserName == null) || string.IsNullOrEmpty(credentials.Secret))
            {
                SetMessage(Texts.Core.SigninMissing, true);
                return false;
            }

            IsBusy = true;
            SetMessage(Texts.Core.SigninConnecting, false);
            try
            {
                await _connectAsync(credentials, cancellationToken);
                Result = credentials;
                SetMessage(null, false);
                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                SetMessage(ServerSignIn.DescribeError(ex), true);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void SetMessage(string message, bool isError)
        {
            _message = message;
            _isError = isError;
            OnPropertyChanged(nameof(Message));
            OnPropertyChanged(nameof(HasMessage));
            OnPropertyChanged(nameof(IsError));
        }

        private void SetKind(bool value, ref bool field)
        {
            if (!value || field)
            {
                return;
            }

            _isCloud = _isPassword = _isToken = false;
            field = true;
            OnPropertyChanged(nameof(IsCloud));
            OnPropertyChanged(nameof(IsPassword));
            OnPropertyChanged(nameof(IsToken));
            OnPropertyChanged(nameof(ShowsUserName));
            OnPropertyChanged(nameof(UserNameLabel));
            OnPropertyChanged(nameof(SecretLabel));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
