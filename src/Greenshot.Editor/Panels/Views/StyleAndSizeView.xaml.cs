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

using System.Windows;
using System.Windows.Controls;
using Greenshot.Editor.Panels.ViewModels;

namespace Greenshot.Editor.Panels.Views
{
    /// <summary>
    /// Copy and apply style and size: four buttons
    /// </summary>
    public partial class StyleAndSizeView : UserControl
    {
        public StyleAndSizeView()
        {
            InitializeComponent();
        }

        private StyleAndSizeViewModel ViewModel => DataContext as StyleAndSizeViewModel;

        private void CopyStyle_Click(object sender, RoutedEventArgs e) => ViewModel?.CopyStyle();

        private void ApplyStyle_Click(object sender, RoutedEventArgs e) => ViewModel?.ApplyStyle();

        private void CopySize_Click(object sender, RoutedEventArgs e) => ViewModel?.CopySize();

        private void ApplySize_Click(object sender, RoutedEventArgs e) => ViewModel?.ApplySize();
    }
}
