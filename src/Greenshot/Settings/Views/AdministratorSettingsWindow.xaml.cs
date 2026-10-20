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
using System.IO;
using System.Text;
using System.Windows;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;
using Greenshot.Settings.ViewModels;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Settings.Views
{
    /// <summary>
    /// Shows the lines for greenshot-fixed.ini, built from the check boxes of the settings, to copy or save.
    /// Plugins and destinations which aren't installed can be added by their name.
    /// </summary>
    public partial class AdministratorSettingsWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AdministratorSettingsWindow));
        private readonly SettingsViewModel _viewModel;

        public AdministratorSettingsWindow(SettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
#if GREENSHOT_LIGHT
            // Greenshot Light has no plugins
            NoUnsignedQuestionBox.Visibility = Visibility.Collapsed;
            AddPluginButton.Visibility = Visibility.Collapsed;
#endif
            UpdateSettingsText();
        }

        private bool OnlyAllowChecked => OnlyAllowCheckedButton.IsChecked == true;

        private void UpdateSettingsText()
        {
            SettingsText.Text = _viewModel.BuildAdministratorSettings(OnlyAllowChecked, NoUserExclusionsBox.IsChecked == true, NoUnsignedQuestionBox.IsChecked == true);
        }

        private void Option_Changed(object sender, RoutedEventArgs e)
        {
            // Raised while InitializeComponent builds the check boxes
            if (_viewModel != null)
            {
                UpdateSettingsText();
            }
        }

        /// <summary>
        /// Blocked, or allowed when only what is checked is allowed; it shows up in the list of the settings too
        /// </summary>
        private void AddPlugin_Click(object sender, RoutedEventArgs e)
        {
#if !GREENSHOT_LIGHT
            AddName(_viewModel.AddPluginName(NameBox.Text, OnlyAllowChecked));
#endif
        }

        private void AddDestination_Click(object sender, RoutedEventArgs e) => AddName(_viewModel.AddDestinationName(NameBox.Text, OnlyAllowChecked));

        private void AddName(bool added)
        {
            if (added)
            {
                NameBox.Clear();
                UpdateSettingsText();
            }

            NameBox.Focus();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            ClipboardHelper.SetClipboardData(SettingsText.Text);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                FileName = "greenshot-fixed.ini",
                DefaultExt = ".ini",
                Filter = "INI (*.ini)|*.ini"
            };
            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                File.WriteAllText(dialog.FileName, SettingsText.Text, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't save the administrator settings", ex);
                ThemedMessageBox.Show(this, ex.Message, Texts.Settings.RestrictForAllUsersTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
