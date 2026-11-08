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

using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Greenshot.Base.Wpf.Views
{
    /// <summary>
    /// The list of definitions on the settings page of a plugin (see docs/vNext/destination-definitions.md): a title which names
    /// the list for a screen reader, one row per definition (icon, name, what it does), and a text when there is none.
    /// The plugin puts it on its own page and adds its own buttons.
    /// </summary>
    public partial class DefinitionListView : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(DefinitionListView), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty EmptyTextProperty =
            DependencyProperty.Register(nameof(EmptyText), typeof(string), typeof(DefinitionListView), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(IEnumerable), typeof(DefinitionListView), new PropertyMetadata(null, OnItemsChanged));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(DefinitionListView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        private static readonly DependencyPropertyKey HasItemsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasItems), typeof(bool), typeof(DefinitionListView), new PropertyMetadata(false));

        public static readonly DependencyProperty HasItemsProperty = HasItemsPropertyKey.DependencyProperty;

        public DefinitionListView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The title above the list, also its name for a screen reader
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Shown instead of the list when there is no definition
        /// </summary>
        public string EmptyText
        {
            get => (string)GetValue(EmptyTextProperty);
            set => SetValue(EmptyTextProperty, value);
        }

        /// <summary>
        /// The rows, usually an ObservableCollection of <see cref="Definitions.DefinitionListItem"/>
        /// </summary>
        public IEnumerable Items
        {
            get => (IEnumerable)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>
        /// True when there is a row
        /// </summary>
        public bool HasItems => (bool)GetValue(HasItemsProperty);

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (DefinitionListView)d;
            if (e.OldValue is INotifyCollectionChanged oldItems)
            {
                oldItems.CollectionChanged -= view.OnCollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newItems)
            {
                newItems.CollectionChanged += view.OnCollectionChanged;
            }

            view.UpdateHasItems();
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => UpdateHasItems();

        private void UpdateHasItems()
        {
            var enumerator = Items?.GetEnumerator();
            SetValue(HasItemsPropertyKey, enumerator?.MoveNext() == true);
        }
    }
}
