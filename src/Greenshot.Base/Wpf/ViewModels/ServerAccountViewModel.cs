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
using log4net;

namespace Greenshot.Base.Wpf.ViewModels
{
    /// <summary>
    /// The account part of a plugin's settings: who is signed in, sign in, test the connection, sign out.
    /// </summary>
    public sealed class ServerAccountViewModel : INotifyPropertyChanged
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ServerAccountViewModel));
        private readonly ServerSignIn _signIn;
        private string _status;
        private bool _isError;
        private bool _isBusy;

        public ServerAccountViewModel(ServerSignIn signIn)
        {
            _signIn = signIn ?? throw new ArgumentNullException(nameof(signIn));
            Refresh();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Signed in as, stored or not signed in; or the result of the last action
        /// </summary>
        public string Status => _status;

        public bool IsError => _isError;

        public bool IsIdle => !_isBusy;

        /// <summary>
        /// There is something to sign out from
        /// </summary>
        public bool CanSignOut => !_isBusy && (_signIn.IsSignedIn || _signIn.HasStoredCredentials);

        /// <summary>
        /// Show the state of the sign-in, e.g. after the url changed
        /// </summary>
        public void Refresh()
        {
            if (_signIn.IsSignedIn)
            {
                SetStatus(string.Format(Texts.Core.SigninAccountSignedIn, _signIn.Account), false);
            }
            else
            {
                SetStatus(_signIn.HasStoredCredentials ? Texts.Core.SigninAccountStored : Texts.Core.SigninAccountNotSignedIn, false);
            }
        }

        public async Task SignInAsync(CancellationToken cancellationToken)
        {
            await RunAsync(async token =>
            {
                await _signIn.SignInAsync(token);
                Refresh();
            }, cancellationToken);
        }

        public async Task TestAsync(CancellationToken cancellationToken)
        {
            await RunAsync(async token =>
            {
                SetStatus(Texts.Core.SigninConnecting, false);
                string account = await _signIn.TestAsync(token);
                if (account == null)
                {
                    SetStatus(Texts.Core.SigninAccountNotSignedIn, false);
                    return;
                }

                SetStatus(string.Format(Texts.Core.SigninAccountSignedIn, account), false);
            }, cancellationToken);
        }

        public void SignOut()
        {
            _signIn.SignOut();
            Refresh();
        }

        private async Task RunAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
        {
            if (_isBusy)
            {
                return;
            }

            SetBusy(true);
            try
            {
                await action(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.Info($"Connecting to {_signIn.ServiceName} failed", ex);
                SetStatus(ServerSignIn.DescribeError(ex), true);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool isBusy)
        {
            _isBusy = isBusy;
            OnPropertyChanged(nameof(IsIdle));
            OnPropertyChanged(nameof(CanSignOut));
        }

        private void SetStatus(string status, bool isError)
        {
            _status = status;
            _isError = isError;
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsError));
            OnPropertyChanged(nameof(CanSignOut));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
