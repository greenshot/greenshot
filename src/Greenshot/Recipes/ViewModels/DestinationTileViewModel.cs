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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Recipes;
using Greenshot.Base.Wpf;
using Greenshot.Configuration;
using Greenshot.Editor.Destinations;
using Greenshot.Base.Threading;
using Greenshot.Base.Languages;

namespace Greenshot.Recipes.ViewModels
{
    public class DestinationTileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public IDestination Destination { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        private ImageSource _iconSource;

        public ImageSource IconSource
        {
            get => _iconSource;
            set
            {
                _iconSource = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconSource)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IconVisibility)));
            }
        }

        public Visibility IconVisibility => IconSource != null ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Load the icon of the destination, must be started on the UI thread
        /// </summary>
        public async System.Threading.Tasks.Task LoadIconAsync()
        {
            IconSource = await DestinationIcons.GetImageSourceAsync(Destination?.Descriptor?.IconKey).ConfigureAwait(true);
        }
        public string BadgeText { get; set; }
        public Visibility BadgeVisibility => !string.IsNullOrEmpty(BadgeText) ? Visibility.Visible : Visibility.Collapsed;
        public SolidColorBrush BadgeBackgroundBrush { get; set; } = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69));

        public SolidColorBrush TextPrimaryBrush => WpfThemeHelper.TextPrimary;
        public SolidColorBrush TextSecondaryBrush => WpfThemeHelper.TextSecondary;
        public SolidColorBrush CardBackgroundBrush => WpfThemeHelper.CardBackground;
        public SolidColorBrush CardBorderBrush => WpfThemeHelper.CardBorder;
        public SolidColorBrush AccentBrush => WpfThemeHelper.Accent;

        public void NotifyThemeChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextPrimaryBrush)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextSecondaryBrush)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CardBackgroundBrush)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CardBorderBrush)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccentBrush)));
        }
    }
}
