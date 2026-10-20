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
using log4net;
using Microsoft.Win32;

namespace Greenshot.Settings.Views
{
    /// <summary>
    /// Shows the lines for greenshot-fixed.ini, built from the check boxes of the settings, to copy or save
    /// </summary>
    public partial class AdministratorSettingsWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AdministratorSettingsWindow));
        private readonly Func<bool, bool, string> _buildSettings;

        /// <param name="buildSettings">The lines, with "allow only what is checked" and "users can't switch off"</param>
        public AdministratorSettingsWindow(Func<bool, bool, string> buildSettings)
        {
            InitializeComponent();
            _buildSettings = buildSettings;
            SettingsText.Text = _buildSettings(false, false);
        }

        private void Option_Changed(object sender, RoutedEventArgs e)
        {
            // Raised while InitializeComponent builds the check boxes
            if (_buildSettings != null)
            {
                SettingsText.Text = _buildSettings(OnlyAllowCheckedButton.IsChecked == true, NoUserExclusionsBox.IsChecked == true);
            }
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
                ThemedMessageBox.Show(this, ex.Message, Texts.Settings.AdministratorSettings, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
