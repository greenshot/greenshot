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
using System.Windows;
using Greenshot.Base.Core;
using Greenshot.Base.Http;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf.ViewModels;

namespace Greenshot.Base.Wpf.Views
{
    /// <summary>
    /// Sign in to a server, see <see cref="ServerSignInViewModel"/>. Closes when the server accepted the credentials,
    /// stays open with the reason when it didn't.
    /// </summary>
    public partial class ServerSignInWindow : Window
    {
        private readonly ServerSignInViewModel _viewModel;
        private readonly CancellationTokenSource _closed = new CancellationTokenSource();

        public ServerSignInWindow(ServerSignInViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
            SecretBox.Password = viewModel.Secret ?? string.Empty;
            Loaded += (_, _) =>
            {
                Activate();
                // Focus goes where something is missing
                if (viewModel.ShowsUserName && string.IsNullOrEmpty(viewModel.UserName))
                {
                    UserNameBox.Focus();
                }
                else
                {
                    SecretBox.Focus();
                    SecretBox.SelectAll();
                }
            };
            Closed += (_, _) => _closed.Cancel();
        }

        /// <summary>
        /// Show the window (on the UI thread)
        /// </summary>
        /// <returns>the credentials the server accepted, null when the user canceled</returns>
        public static ServerCredentials Ask(ServerSignInViewModel viewModel)
        {
            var window = new ServerSignInWindow(viewModel);
            return window.ShowDialog() == true ? viewModel.Result : null;
        }

        private void SecretBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.Secret = SecretBox.Password;
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            AsyncCommand.Run(async () =>
            {
                if (await _viewModel.SignInAsync(_closed.Token))
                {
                    if (!_closed.IsCancellationRequested)
                    {
                        DialogResult = true;
                    }
                }
                else if (_viewModel.ShowsUserName && string.IsNullOrEmpty(_viewModel.UserName))
                {
                    UserNameBox.Focus();
                }
                else
                {
                    SecretBox.Focus();
                }
            }, "Sign in to the server");
        }

        private void CreateApiToken_Click(object sender, RoutedEventArgs e)
        {
            ExceptionHelper.OpenUrl(_viewModel.ApiTokenUrl);
        }
    }
}
