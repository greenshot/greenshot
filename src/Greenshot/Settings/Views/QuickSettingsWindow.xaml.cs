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
using System.Windows;
using System.Windows.Interop;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using Greenshot.Settings.ViewModels;
using log4net;

namespace Greenshot.Settings.Views
{
    /// <summary>
    /// The quick settings in a window (OpenWindow step, Window QuickSettings), e.g. for a hotkey or a click on the tray icon.
    /// The same settings as the quick settings of the tray menu, stored as soon as they change.
    /// </summary>
    public partial class QuickSettingsWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(QuickSettingsWindow));
        private static QuickSettingsWindow _openWindow;

        private readonly QuickSettingsViewModel _viewModel;

        private QuickSettingsWindow(IGreenshotShell shell)
        {
            InitializeComponent();
            _viewModel = new QuickSettingsViewModel(QuickSettings.Create(), tab => OpenSettings(shell, tab));
            DataContext = _viewModel;
            try
            {
                Icon = ImageHelper.ToBitmapSource(GreenshotResources.GetGreenshotIcon());
            }
            catch (Exception ex)
            {
                Log.Debug("Could not set the window icon", ex);
            }
            // The tray menu or the settings may have changed a value while the window was in the background
            Activated += (sender, args) => _viewModel.Refresh();
        }

        /// <summary>
        /// Show the quick settings, or bring the open window to the front; on the UI thread
        /// </summary>
        /// <param name="shell">Opens the settings, null when there is none (the settings buttons do nothing then)</param>
        public static void ShowOrActivate(IGreenshotShell shell)
        {
            if (_openWindow != null && _openWindow.IsLoaded)
            {
                _openWindow.Activate();
                WindowHelper.ToForeground(new WindowInteropHelper(_openWindow).Handle);
                return;
            }

            var window = new QuickSettingsWindow(shell);
            if (shell != null)
            {
                _ = new WindowInteropHelper(window) { Owner = shell.OwnerHandle };
            }
            window.Closed += (sender, args) =>
            {
                if (ReferenceEquals(_openWindow, window))
                {
                    _openWindow = null;
                }
            };
            _openWindow = window;
            // Not modal: the flow which opened it ends, the editors stay usable
            window.Show();
            window.Activate();
        }

        private void OpenSettings(IGreenshotShell shell, string tab)
        {
            Close();
            if (shell == null)
            {
                return;
            }
            // The settings window is modal: opened after this click is handled
            UiDispatcher.Current.InvokeAsync(() => shell.ShowSetting(null, tab)).FireAndLog("Open the settings from the quick settings", Log);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
