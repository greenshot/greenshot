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
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using Greenshot.Base.Wpf.Views;
using Greenshot.Helpers;
using Greenshot.Settings;
using Greenshot.Settings.ViewModels;

namespace Greenshot.Recipes.Views
{
    /// <summary>
    /// The options of a recipe (<see cref="RecipeOptionViewModel"/>), one row each: on the Recipes tab of the settings and in the
    /// approval window of a recipe from the catalog
    /// </summary>
    public partial class RecipeOptionsView : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(RecipeOptionsView), new PropertyMetadata(null));

        public RecipeOptionsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The <see cref="RecipeOptionViewModel"/>s to show
        /// </summary>
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// A Hotkey option wants a new hotkey: the window shows its hotkey editor and sets <see cref="HotkeyDisplayView.HotkeyString"/>
        /// </summary>
        public event EventHandler<HotkeyDisplayView> HotkeyEditRequested;

        private void OnHotkeyEditRequested(object sender, EventArgs e)
        {
            if (sender is HotkeyDisplayView display)
            {
                HotkeyEditRequested?.Invoke(this, display);
            }
        }

        private void OnColorClicked(object sender, RoutedEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is RecipeOptionViewModel item))
            {
                return;
            }

            var colorWindow = new Greenshot.Editor.Views.ColorPickerWindow
            {
                Owner = Window.GetWindow(this),
                SelectedColor = RecipeOptionColors.Parse(item.TextValue)
            };
            if (colorWindow.ShowDialog() == true)
            {
                item.Value = RecipeOptionColors.Format(colorWindow.SelectedColor);
            }
        }

        private void OnFolderClicked(object sender, RoutedEventArgs e)
        {
            if (!((sender as FrameworkElement)?.DataContext is RecipeOptionViewModel item))
            {
                return;
            }

            string selectedPath = ModernFolderPicker.SelectFolder(Window.GetWindow(this), item.TextValue, item.Label);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                item.Value = selectedPath;
            }
        }
    }
}
