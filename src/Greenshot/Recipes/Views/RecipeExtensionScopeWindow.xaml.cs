/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Greenshot.Base.Core;
using Greenshot.Forms.Wpf;
using log4net;
using Greenshot.Base.Languages;

namespace Greenshot.Recipes.Views
{
    /// <summary>
    /// Where an extension is used: the captures (recipes) and the destinations, as two aligned lists.
    /// Works on copies of the items; OK writes them back.
    /// </summary>
    public partial class RecipeExtensionScopeWindow : Window
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RecipeExtensionScopeWindow));
        private readonly IReadOnlyList<RecipeScopeItem> _captures;
        private readonly IReadOnlyList<RecipeScopeItem> _destinations;
        private readonly List<RecipeScopeItem> _captureCopies;
        private readonly List<RecipeScopeItem> _destinationCopies;

        /// <param name="extensionName">Shown in the title</param>
        /// <param name="captures">The recipes the extension can change</param>
        /// <param name="destinations">The destinations, empty when the extension doesn't run per destination</param>
        public RecipeExtensionScopeWindow(string extensionName, IReadOnlyList<RecipeScopeItem> captures, IReadOnlyList<RecipeScopeItem> destinations)
        {
            InitializeComponent();
            try
            {
                Icon = ImageHelper.ToBitmapSource(GreenshotResources.GetGreenshotIcon());
            }
            catch (Exception ex)
            {
                Log.Debug("Could not set window icon", ex);
            }

            _captures = captures ?? Array.Empty<RecipeScopeItem>();
            _destinations = destinations ?? Array.Empty<RecipeScopeItem>();
            _captureCopies = _captures.Select(i => new RecipeScopeItem(i.Id, i.Name, i.IsChecked)).ToList();
            _destinationCopies = _destinations.Select(i => new RecipeScopeItem(i.Id, i.Name, i.IsChecked)).ToList();

            string title = string.Format(Texts.Settings.RecipesScopeTitle, extensionName);
            Title = title;
            TitleText.Text = title;
            CapturesList.ItemsSource = _captureCopies;
            DestinationsList.ItemsSource = _destinationCopies;
            if (_destinationCopies.Count == 0)
            {
                DestinationsCard.Visibility = Visibility.Collapsed;
            }
            foreach (var item in _destinationCopies)
            {
                item.PropertyChanged += OnDestinationChanged;
            }
            UpdateOk();
        }

        private void OnDestinationChanged(object sender, PropertyChangedEventArgs e) => UpdateOk();

        /// <summary>
        /// At least one destination: without one the extension would never run (switch it off instead)
        /// </summary>
        private void UpdateOk()
        {
            bool valid = _destinationCopies.Count == 0 || _destinationCopies.Any(i => i.IsChecked);
            OkButton.IsEnabled = valid;
            NoDestinationText.Visibility = valid ? Visibility.Collapsed : Visibility.Visible;
        }

        private void CapturesAll_Click(object sender, RoutedEventArgs e) => SetAll(_captureCopies, true);

        private void CapturesNone_Click(object sender, RoutedEventArgs e) => SetAll(_captureCopies, false);

        private void DestinationsAll_Click(object sender, RoutedEventArgs e) => SetAll(_destinationCopies, true);

        private void DestinationsNone_Click(object sender, RoutedEventArgs e) => SetAll(_destinationCopies, false);

        private static void SetAll(IEnumerable<RecipeScopeItem> items, bool isChecked)
        {
            foreach (var item in items)
            {
                item.IsChecked = isChecked;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _captures.Count; i++)
            {
                _captures[i].IsChecked = _captureCopies[i].IsChecked;
            }
            for (int i = 0; i < _destinations.Count; i++)
            {
                _destinations[i].IsChecked = _destinationCopies[i].IsChecked;
            }
            DialogResult = true;
            Close();
        }
    }
}
