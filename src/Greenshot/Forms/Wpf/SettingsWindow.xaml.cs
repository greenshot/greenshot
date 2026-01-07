/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using Greenshot.Base;
using Greenshot.Base.IniFile;
using Greenshot.Base.Wpf;
using Greenshot.Configuration;
using MessageBox = System.Windows.MessageBox;

namespace Greenshot.Forms.Wpf
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// Modern WPF replacement for the Windows Forms SettingsForm
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();
            
            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;
            
            // Apply theme
            Resources.MergedDictionaries.Add(ThemeManager.Instance.GetThemeResources());
            
            // Listen for theme changes
            ThemeManager.Instance.PropertyChanged += (s, e) =>
            {
                Resources.MergedDictionaries.Clear();
                Resources.MergedDictionaries.Add(ThemeManager.Instance.GetThemeResources());
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Save settings
            SaveSettings();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Don't save settings
            DialogResult = false;
            Close();
        }

        private void BrowseStorageLocation_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = _viewModel.CoreConfiguration.OutputFilePath;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _viewModel.CoreConfiguration.OutputFilePath = dialog.SelectedPath;
                }
            }
        }

        private void ShowPatternHelp_Click(object sender, RoutedEventArgs e)
        {
            string filenamepatternText = Greenshot.Base.Core.Language.GetString(LangKey.settings_message_filenamepattern);
            // Convert %NUM% to ${NUM} for old language files!
            filenamepatternText = Regex.Replace(filenamepatternText, "%([a-zA-Z_0-9]+)%", @"${$1}");
            MessageBox.Show(filenamepatternText, Greenshot.Base.Core.Language.GetString(LangKey.settings_filenamepattern));
        }

        private void IconSizeUp_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IconSize + 16 <= 256)
            {
                _viewModel.IconSize += 16;
            }
        }

        private void IconSizeDown_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.IconSize - 16 >= 16)
            {
                _viewModel.IconSize -= 16;
            }
        }

        private void SaveSettings()
        {
            // Save destinations
            var destinations = new List<string>();
            
            if (_viewModel.PickerSelected)
            {
                destinations.Add(nameof(WellKnownDestinations.Picker));
            }
            else
            {
                foreach (var destItem in _viewModel.Destinations.Where(d => d.IsSelected))
                {
                    destinations.Add(destItem.Destination.Designation);
                }
            }
            
            _viewModel.CoreConfiguration.OutputDestinations = destinations;
            
            // Force save of all configuration sections
            IniConfig.Save();
        }
    }
}
