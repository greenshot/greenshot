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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Greenshot.Base.Wpf;
using Greenshot.Plugin.RecipeEditor.ViewModels;

namespace Greenshot.Plugin.RecipeEditor.Views
{
    /// <summary>
    /// Asks what a new recipe starts with: a template, or a copy of an existing recipe
    /// </summary>
    public partial class NewRecipeWindow : Window
    {
        /// <summary>
        /// The chosen entry, set when the window closes with Create
        /// </summary>
        public NewRecipeChoice SelectedChoice { get; private set; }

        public NewRecipeWindow(IList<NewRecipeChoice> choices)
        {
            InitializeComponent();

            var view = new ListCollectionView(new List<NewRecipeChoice>(choices));
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(NewRecipeChoice.Group)));
            ChoiceListBox.ItemsSource = view;
            if (choices.Count > 0)
            {
                ChoiceListBox.SelectedIndex = 0;
            }
            UpdateCreateButton();

            // The title bar in the colors of the theme, it follows theme changes from now on
            SourceInitialized += (s, e) => WindowFrameTheme.Attach(this);
            // The keyboard focus starts on the first entry, the arrow keys move through the list
            Loaded += (s, e) => _ = Dispatcher.BeginInvoke(new System.Action(() =>
            {
                if (ChoiceListBox.ItemContainerGenerator.ContainerFromIndex(ChoiceListBox.SelectedIndex) is ListBoxItem item)
                {
                    item.Focus();
                }
                else
                {
                    ChoiceListBox.Focus();
                }
            }), DispatcherPriority.Input);
        }

        private void UpdateCreateButton()
        {
            CreateButton.IsEnabled = ChoiceListBox.SelectedItem is NewRecipeChoice;
        }

        private void ChoiceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCreateButton();
        }

        private void Create()
        {
            if (ChoiceListBox.SelectedItem is NewRecipeChoice choice)
            {
                SelectedChoice = choice;
                DialogResult = true;
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            Create();
        }

        private void ChoiceListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only a double click on an entry, not on the scroll bar
            if (e.OriginalSource is DependencyObject source && ItemsControl.ContainerFromElement(ChoiceListBox, source) is ListBoxItem)
            {
                Create();
            }
        }
    }
}
