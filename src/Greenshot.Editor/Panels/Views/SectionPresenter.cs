/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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


using System.Windows;
using System.Windows.Controls;
using Greenshot.Editor.Panels.ViewModels;

namespace Greenshot.Editor.Panels.Views
{
    /// <summary>
    /// Shows the view of a section: the section creates it once, it's kept with the item and moved here when the section comes back
    /// </summary>
    public sealed class SectionPresenter : ContentControl
    {
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(
            nameof(Item), typeof(object), typeof(SectionPresenter), new PropertyMetadata(null, OnItemChanged));

        public SectionPresenter()
        {
            Focusable = false;
        }

        /// <summary>
        /// The EditorSectionItem
        /// </summary>
        public object Item
        {
            get => GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var presenter = (SectionPresenter)d;
            if (e.NewValue is not EditorSectionItem item)
            {
                presenter.Content = null;
                return;
            }

            item.View ??= item.Section.CreateView(item.ViewModel);
            // A section which comes back gets a new container, its view can't be in two places
            if (item.View is FrameworkElement { Parent: ContentControl previous } && !ReferenceEquals(previous, presenter))
            {
                previous.Content = null;
            }

            presenter.Content = item.View;
        }
    }
}
