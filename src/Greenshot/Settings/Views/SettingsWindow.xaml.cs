/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using Dapplo.Ini;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Wpf;
using Greenshot.Capturing.Tools;
using Greenshot.Helpers;
using Greenshot.Base.Languages;
using Greenshot.Recipes.Triggers;
using Greenshot.Recipes.Views;
using Greenshot.Settings.ViewModels;
using Greenshot.Views;
#if !GREENSHOT_LIGHT
using Greenshot.Ai.Views;
using Greenshot.Plugins.Views;
#endif

namespace Greenshot.Settings.Views
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

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            HotkeyManager.UnregisterHotkeys();
            SaveSettings();
            TriggerManager.Instance.RestartHotkeyTriggers();

            var shell = SimpleServiceProvider.Current.GetInstance<IGreenshotShell>(isOptional: true);
            shell?.UpdateUi();

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

        // The buttons go to the next multiple of 16, also from a typed value in between (e.g. 24 goes to 16 or 32)
        private void IconSizeUp_Click(object sender, RoutedEventArgs e)
        {
            int next = (_viewModel.IconSize / 16 + 1) * 16;
            if (next <= 256)
            {
                _viewModel.IconSize = next;
            }
        }

        private void IconSizeDown_Click(object sender, RoutedEventArgs e)
        {
            int previous = (_viewModel.IconSize - 1) / 16 * 16;
            if (previous >= 16)
            {
                _viewModel.IconSize = previous;
            }
        }

        /// <summary>
        /// The lines for greenshot-fixed.ini with the check boxes of the destinations and plugins, also before OK
        /// </summary>
        internal void AdministratorSettings_Click(object sender, RoutedEventArgs e)
        {
            new AdministratorSettingsWindow(_viewModel) { Owner = this }.ShowDialog();
        }

        private void RemoveFixedCaptureSize_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is FixedCaptureSizeItem item)
            {
                _viewModel.FixedCaptureSizes.Remove(item);
            }
        }

        // A recent capture becomes one of your own sizes
        private void KeepFixedCaptureSize_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is FixedCaptureSizeItem item)
            {
                _viewModel.KeepRecentFixedCaptureSize(item);
            }
        }

        // The fixed capture size which the popup changes, null when it adds one
        private FixedCaptureSizeItem _changedFixedCaptureSize;

        private void AddFixedCaptureSize_Click(object sender, RoutedEventArgs e)
        {
            OpenFixedCaptureSizePopup(null, AddFixedCaptureSizeButton);
        }

        // A click on a size opens the popup with its values below it
        private void ChangeFixedCaptureSize_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is FixedCaptureSizeItem item)
            {
                OpenFixedCaptureSizePopup(item, element);
            }
        }

        /// <summary>
        /// The popup to add a size (empty) or to change one (with its values)
        /// </summary>
        private void OpenFixedCaptureSizePopup(FixedCaptureSizeItem item, UIElement target)
        {
            _changedFixedCaptureSize = item;
            var area = item?.Area ?? default;
            string Number(int value) => value.ToString(CultureInfo.CurrentCulture);
            FixedCaptureSizeWidth.Text = item == null ? string.Empty : Number(area.Size.Width);
            FixedCaptureSizeHeight.Text = item == null ? string.Empty : Number(area.Size.Height);
            FixedCaptureSizeX.Text = area.Offset is { } offsetX ? Number(offsetX.X) : string.Empty;
            FixedCaptureSizeY.Text = area.Offset is { } offsetY ? Number(offsetY.Y) : string.Empty;
            FixedCaptureSizeProgram.ItemsSource = SettingsViewModel.GetProgramsOfOpenWindows();
            FixedCaptureSizeProgram.Text = area.Program ?? string.Empty;
            // The screens connected now, and that of the entry
            var choices = SettingsViewModel.GetFixedCapturePlaceChoices(area.Monitor);
            FixedCaptureSizeRelativeTo.ItemsSource = choices;
            FixedCaptureSizeRelativeTo.SelectedItem = area.Program != null
                ? choices.First(choice => choice.IsProgram)
                : choices.FirstOrDefault(choice => !choice.IsProgram && choice.Monitor == area.Monitor) ?? choices[0];
            string confirmText = item == null ? Texts.Settings.FixedCaptureSizesAdd : Texts.Settings.FixedCaptureSizesChange;
            FixedCaptureSizeConfirm.Content = item == null ? "+" : "✓";
            FixedCaptureSizeConfirm.ToolTip = confirmText;
            AutomationProperties.SetName(FixedCaptureSizeConfirm, confirmText);
            AddFixedCaptureSizePopup.PlacementTarget = target;
            AddFixedCaptureSizePopup.IsOpen = true;
        }

        // The cursor in the width
        private void AddFixedCaptureSizePopup_Opened(object sender, EventArgs e)
        {
            _ = Dispatcher.BeginInvoke(new Action(() =>
            {
                FixedCaptureSizeWidth.Focus();
                FixedCaptureSizeWidth.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        // Enter adds the size, Esc closes the popup
        private void AddFixedCaptureSizePopup_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddFixedCaptureSize();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                AddFixedCaptureSizePopup.IsOpen = false;
                e.Handled = true;
            }
        }

        private void AddFixedCaptureSizeConfirm_Click(object sender, RoutedEventArgs e)
        {
            AddFixedCaptureSize();
        }

        /// <summary>
        /// Choose the color of a new image in the editor
        /// </summary>
        private void NewImageColor_Click(object sender, RoutedEventArgs e)
        {
            var colorPicker = new Greenshot.Editor.Views.ColorPickerWindow
            {
                SelectedColor = _viewModel.NewImageColor,
                Owner = this
            };
            if (colorPicker.ShowDialog() == true)
            {
                _viewModel.NewImageColor = colorPicker.SelectedColor;
            }
        }

        /// <summary>
        /// Adds the size of the popup and closes it, a value which isn't a positive number keeps it open with that box focused.
        /// X and Y are optional, both or none; with them the place is relative to the chosen screen or to a window of the program.
        /// Without them a program or another screen than the main one means its top left corner.
        /// </summary>
        private void AddFixedCaptureSize()
        {
            if (!int.TryParse(FixedCaptureSizeWidth.Text.Trim(), out int width) || width <= 0)
            {
                FixedCaptureSizeWidth.Focus();
                FixedCaptureSizeWidth.SelectAll();
                return;
            }
            if (!int.TryParse(FixedCaptureSizeHeight.Text.Trim(), out int height) || height <= 0)
            {
                FixedCaptureSizeHeight.Focus();
                FixedCaptureSizeHeight.SelectAll();
                return;
            }
            var area = new FixedCaptureArea(new NativeSize(width, height));
            var relativeTo = FixedCaptureSizeRelativeTo.SelectedItem as FixedCapturePlaceChoice;
            // A program or another screen without X and Y is its top left corner, only the main screen without them is "no place"
            bool hasPlace = relativeTo?.IsProgram == true || relativeTo?.Monitor != null;
            bool hasX = !string.IsNullOrWhiteSpace(FixedCaptureSizeX.Text), hasY = !string.IsNullOrWhiteSpace(FixedCaptureSizeY.Text);
            if (hasX || hasY || hasPlace)
            {
                int x = 0, y = 0;
                if ((hasX || hasY) && !int.TryParse(FixedCaptureSizeX.Text.Trim(), out x))
                {
                    FixedCaptureSizeX.Focus();
                    FixedCaptureSizeX.SelectAll();
                    return;
                }
                if ((hasX || hasY) && !int.TryParse(FixedCaptureSizeY.Text.Trim(), out y))
                {
                    FixedCaptureSizeY.Focus();
                    FixedCaptureSizeY.SelectAll();
                    return;
                }
                string program = relativeTo?.IsProgram == true ? FixedCaptureSizeProgram.Text.Trim() : null;
                area = new FixedCaptureArea(area.Size, new NativePoint(x, y), relativeTo?.Monitor, program);
                // A program needs a name which can be stored, e.g. notepad.exe
                if (relativeTo?.IsProgram == true && (area.Program == null || FixedCaptureArea.Parse(area.ToString()) != area))
                {
                    FixedCaptureSizeProgram.Focus();
                    return;
                }
            }
            AddFixedCaptureSizePopup.IsOpen = false;
            if (_changedFixedCaptureSize != null)
            {
                _viewModel.ChangeFixedCaptureSize(_changedFixedCaptureSize, area);
                _changedFixedCaptureSize = null;
            }
            else
            {
                _viewModel.AddFixedCaptureSize(area);
            }
            AddFixedCaptureSizeButton.Focus();
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
                    destinations.Add(destItem.Designation);
                }

                // Keep the destinations which aren't there right now, e.g. of a plugin which isn't loaded
                destinations.AddRange(_viewModel.CoreConfiguration.OutputDestinations?
                    .Where(designation => designation != nameof(WellKnownDestinations.Picker)
                                          && _viewModel.Destinations.All(d => !string.Equals(d.Designation, designation, StringComparison.OrdinalIgnoreCase)))
                    ?? Enumerable.Empty<string>());
            }
            
            _viewModel.CoreConfiguration.OutputDestinations = destinations;
            _viewModel.SaveRestrictions();

            _viewModel.SaveRecipeOptions();
            _viewModel.SaveHotkeys();
            _viewModel.SaveFixedCaptureSizes();

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

            // Only when the checkbox was changed: saving other settings leaves the entries alone, e.g. the one of another Greenshot
            if (_viewModel.AutoStartEnabled != StartupHelper.IsAutostartEnabled())
            {
                if (_viewModel.AutoStartEnabled)
                {
                    StartupHelper.EnableAutostart();
                }
                else
                {
                    StartupHelper.DisableAutostart();
                }
            }
            
            // Force save of all configuration sections
            IniConfigRegistry.Get()?.Save();
        }

        /// <summary>
        /// The swatch of a color option of a recipe: pick the color with the editor's color picker
        /// </summary>
        /// <summary>
        /// "Change…" of an extension: where it is used, the captures and the destinations
        /// </summary>
        private void RecipeExtensionScope_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is RecipeOptionGroupViewModel group) || group.UseIn == null)
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
            if (sender is Greenshot.Base.Wpf.Views.HotkeyDisplayView displayControl)
            {
                HotkeyModal.Open(displayControl.HeaderText, displayControl.HotkeyString, newHotkey =>
                {
                    displayControl.HotkeyString = newHotkey;
                });
            }
        }
    }
}
