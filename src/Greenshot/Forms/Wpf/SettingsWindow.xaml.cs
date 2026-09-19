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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using Dapplo.Ini;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Wpf;
using Greenshot.Configuration;
using Greenshot.Helpers;
using BaseLanguage = Greenshot.Base.Core.Language;
using MessageBox = System.Windows.MessageBox;

namespace Greenshot.Forms.Wpf
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// Modern WPF replacement for the Windows Forms SettingsForm
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(SettingsWindow));
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow(string initialPluginName = null)
        {
            InitializeComponent();
            
            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;
            Icon = _viewModel.WindowIcon;
            
            // Apply theme
            Resources.MergedDictionaries.Add(ThemeManager.Instance.GetThemeResources());
            
            // Listen for theme changes
            System.ComponentModel.PropertyChangedEventHandler themeHandler = (s, e) =>
            {
                if (Dispatcher.CheckAccess())
                {
                    Resources.MergedDictionaries.Clear();
                    Resources.MergedDictionaries.Add(ThemeManager.Instance.GetThemeResources());
                }
                else if (!Dispatcher.HasShutdownStarted)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            Resources.MergedDictionaries.Clear();
                            Resources.MergedDictionaries.Add(ThemeManager.Instance.GetThemeResources());
                        }
                        catch
                        {
                            // Window or dispatcher shutting down
                        }
                    });
                }
            };
            ThemeManager.Instance.PropertyChanged += themeHandler;
            Closed += (s, e) => ThemeManager.Instance.PropertyChanged -= themeHandler;

            // Lazy plugin configuration: only select/load first plugin if the user navigates to the Plugins tab
            SettingsTabControl.SelectionChanged += (s, e) =>
            {
                if (SettingsTabControl.SelectedItem == PluginsTabItem && _viewModel.SelectedPlugin == null && _viewModel.Plugins?.Count > 0)
                {
                    _viewModel.SelectedPlugin = _viewModel.Plugins[0];
                }
            };

            if (!string.IsNullOrEmpty(initialPluginName))
            {
                SelectPlugin(initialPluginName);
            }
        }

        public void SelectPlugin(string pluginName)
        {
            if (string.IsNullOrWhiteSpace(pluginName)) return;
            SettingsTabControl.SelectedItem = PluginsTabItem;
            _viewModel.SelectPluginByName(pluginName);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ToggleTheme();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            HotkeyManager.UnregisterHotkeys();
            SaveSettings();
            HotkeyHelper.RegisterHotkeys();

            var mainForm = SimpleServiceProvider.Current.GetInstance<MainForm>();
            mainForm?.UpdateUi();

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
            string selectedPath = ModernFolderPicker.SelectFolder(
                this,
                _viewModel.CoreConfiguration.OutputFilePath,
                BaseLanguage.GetString("settings_storagelocation"));

            if (!string.IsNullOrEmpty(selectedPath))
            {
                _viewModel.CoreConfiguration.OutputFilePath = selectedPath;
            }
        }

        private void ShowPatternHelp_Click(object sender, RoutedEventArgs e)
        {
            string filenamepatternText = BaseLanguage.GetString(LangKey.settings_message_filenamepattern);
            // Convert %NUM% to ${NUM} for old language files!
            filenamepatternText = Regex.Replace(filenamepatternText, "%([a-zA-Z_0-9]+)%", @"${$1}");
            var dialog = new PatternHelpWindow(filenamepatternText)
            {
                Owner = this
            };
            dialog.ShowDialog();
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

            // Save clipboard formats
            if (_viewModel.ClipboardFormats != null)
            {
                _viewModel.CoreConfiguration.ClipboardFormats = _viewModel.ClipboardFormats
                    .Where(cf => cf.IsSelected)
                    .Select(cf => cf.Format)
                    .ToList();
            }

            try
            {
                if (_viewModel.AutoStartEnabled)
                {
                    if (!StartupHelper.HasRunAll())
                    {
                        StartupHelper.SetRunUser();
                    }
                }
                else
                {
                    if (StartupHelper.HasRunAll())
                    {
                        StartupHelper.DeleteRunAll();
                    }

                    if (StartupHelper.HasRunUser())
                    {
                        StartupHelper.DeleteRunUser();
                    }
                }
            }
            catch
            {
                // ignored
            }
            
            // Force save of all configuration sections
            IniConfigRegistry.Get()?.Save();
        }

        private void HotkeyDisplayControl_EditRequested(object sender, EventArgs e)
        {
            if (sender is UI.Controls.HotkeyDisplayControl displayControl)
            {
                HotkeyModal.Open(displayControl.HeaderText, displayControl.HotkeyString, newHotkey =>
                {
                    displayControl.HotkeyString = newHotkey;
                });
            }
        }
    }
}
