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

using System.Windows;
using Greenshot.Base.IniFile;
using Greenshot.Base.Wpf;

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

        private void SaveSettings()
        {
            // Force save of all configuration sections
            IniConfig.Save();
        }
    }
}
