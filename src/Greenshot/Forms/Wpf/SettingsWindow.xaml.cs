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
using System.Windows;
using System.Windows.Forms;
using Dapplo.Ini;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Wpf;
using Greenshot.Configuration;
using Greenshot.Helpers;
using Greenshot.Base.Languages;
using Greenshot.Recipes.Views;
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

        public SettingsWindow(string initialPluginName = null, string initialTabName = null)
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
                    _ = Dispatcher.InvokeAsync(() =>
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

#if GREENSHOT_LIGHT
            // Greenshot Light has no plugins and no AI tools
            SettingsTabControl.Items.Remove(PluginsTabItem);
            SettingsTabControl.Items.Remove(AiToolsTabItem);
#else
            PluginsTabItem.Content = new PluginsSettingsPage();
            AiToolsTabItem.Content = new AiToolsSettingsPage();

            // Lazy plugin configuration: only select/load first plugin if the user navigates to the Plugins tab
            SettingsTabControl.SelectionChanged += (s, e) =>
            {
                if (SettingsTabControl.SelectedItem == PluginsTabItem && _viewModel.SelectedPlugin == null && _viewModel.Plugins?.Count > 0)
                {
                    _viewModel.SelectedPlugin = _viewModel.Plugins[0];
                }
            };
#endif

            if (!string.IsNullOrEmpty(initialTabName))
            {
                SelectTab(initialTabName);
            }
            if (!string.IsNullOrEmpty(initialPluginName))
            {
                SelectPlugin(initialPluginName);
            }
        }

        public void SelectTab(string tabName)
        {
            if (string.IsNullOrWhiteSpace(tabName)) return;

            string normalized = tabName.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "general":
                    SettingsTabControl.SelectedIndex = 0;
                    break;
                case "capture":
                    SettingsTabControl.SelectedIndex = 1;
                    break;
                case "output":
                    SettingsTabControl.SelectedIndex = 2;
                    break;
                case "destination":
                case "destinations":
                    SettingsTabControl.SelectedIndex = 3;
                    break;
                case "editor":
                    SettingsTabControl.SelectedIndex = 4;
                    break;
                case "printer":
                case "print":
                    SettingsTabControl.SelectedIndex = 5;
                    break;
                case "plugin":
                case "plugins":
                    SettingsTabControl.SelectedItem = PluginsTabItem;
                    break;
                case "recipes":
                    // By name: the tabs before it are not always there
                    SettingsTabControl.SelectedItem = RecipesTabItem;
                    break;
                case "expert":
                case "expertsettings":
                    // By name: the AI tools and plugins tabs before it are not always there
                    if (_viewModel.IsExpertTabVisible)
                    {
                        SettingsTabControl.SelectedItem = ExpertTabItem;
                    }
                    break;
                default:
                    SelectPlugin(tabName);
                    break;
            }
        }

        public void SelectPlugin(string pluginName)
        {
#if !GREENSHOT_LIGHT
            if (string.IsNullOrWhiteSpace(pluginName)) return;
            SettingsTabControl.SelectedItem = PluginsTabItem;
            _viewModel.SelectPluginByName(pluginName);
#endif
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ClampToWorkingArea();
        }

        private void ClampToWorkingArea()
        {
            try
            {
                // Get the working area of the screen the window is currently on
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var screen = System.Windows.Forms.Screen.FromHandle(hwnd);
                var workArea = screen.WorkingArea;

                // Account for DPI scaling (WPF uses device-independent units at 96 DPI,
                // but Screen.WorkingArea returns physical pixels)
                var source = PresentationSource.FromVisual(this);
                double dpiScaleX = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1.0;
                double dpiScaleY = source?.CompositionTarget?.TransformFromDevice.M22 ?? 1.0;

                double availableW = workArea.Width * dpiScaleX;
                double availableH = workArea.Height * dpiScaleY;

                // Clamp window size to available working area
                if (Width > availableW) Width = availableW;
                if (Height > availableH) Height = availableH;

                // Re-center within the working area
                Left = (workArea.Left * dpiScaleX) + (availableW - Width) / 2;
                Top = (workArea.Top * dpiScaleY) + (availableH - Height) / 2;
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to clamp window to working area", ex);
            }
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
                Texts.Settings.Storagelocation);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                _viewModel.CoreConfiguration.OutputFilePath = selectedPath;
            }
        }

        private void ShowPatternHelp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PatternHelpWindow(Texts.Settings.MessageFilenamepattern)
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

            _viewModel.SaveRecipeOptions();

#if !GREENSHOT_LIGHT
            _viewModel.SaveAiToolSettings();
#endif

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

        /// <summary>
        /// The swatch of a color option of a recipe: pick the color with the editor's color picker
        /// </summary>
        private void RecipeOptionColor_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is RecipeOptionItem item))
            {
                return;
            }

            var colorWindow = new Greenshot.Editor.Forms.ColorPickerWindow
            {
                Owner = this,
                SelectedColor = RecipeOptionColors.Parse(item.TextValue)
            };
            if (colorWindow.ShowDialog() == true)
            {
                item.Value = RecipeOptionColors.Format(colorWindow.SelectedColor);
            }
        }

        /// <summary>
        /// "Change…" of an extension: where it is used, the captures and the destinations
        /// </summary>
        private void RecipeExtensionScope_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is RecipeOptionGroup group) || group.UseIn == null)
            {
                return;
            }

            var scopeWindow = new RecipeExtensionScopeWindow(group.Name, group.UseIn, group.OnlyDestinations)
            {
                Owner = this
            };
            scopeWindow.ShowDialog();
        }

        private void HotkeyDisplayControl_EditRequested(object sender, EventArgs e)
        {
            if (sender is Greenshot.Base.Wpf.HotkeyDisplayControl displayControl)
            {
                HotkeyModal.Open(displayControl.HeaderText, displayControl.HotkeyString, newHotkey =>
                {
                    displayControl.HotkeyString = newHotkey;
                });
            }
        }
    }
}
