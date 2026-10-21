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
using System.Windows.Controls;
using Greenshot.Base.Threading;
using Greenshot.Base.Wpf.ViewModels;

namespace Greenshot.Base.Wpf.Views
{
    /// <summary>
    /// The account part of a plugin's settings page, see <see cref="ServerAccountViewModel"/>
    /// </summary>
    public partial class ServerAccountView : UserControl
    {
        private CancellationTokenSource _unloaded = new CancellationTokenSource();

        public ServerAccountView()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                if (_unloaded.IsCancellationRequested)
                {
                    _unloaded = new CancellationTokenSource();
                }
            };
            Unloaded += (_, _) => _unloaded.Cancel();
        }

        private ServerAccountViewModel ViewModel => DataContext as ServerAccountViewModel;

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel != null)
            {
                AsyncCommand.Run(() => viewModel.SignInAsync(_unloaded.Token), "Sign in from the settings");
            }
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel != null)
            {
                AsyncCommand.Run(() => viewModel.TestAsync(_unloaded.Token), "Test the connection from the settings");
            }
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.SignOut();
        }
    }
}
