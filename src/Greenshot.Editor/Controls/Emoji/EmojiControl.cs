/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Greenshot.Editor.Drawing.Emoji;
using PresentationSource = System.Windows.PresentationSource;

namespace Greenshot.Editor.Controls.Emoji
{
    internal class EmojiControl : Image
    {
        public static readonly DependencyProperty EmojiProperty = DependencyProperty.Register("Emoji", typeof(string), typeof(EmojiControl), new PropertyMetadata(default(string), OnEmojiPropertyChanged));

        private static void OnEmojiPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EmojiControl)d).Source = null;
        }

        public string Emoji
        {
            get => (string)GetValue(EmojiProperty);
            set => SetValue(EmojiProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (Source == null && !string.IsNullOrEmpty(Emoji))
            {
                // Get DPI from the visual tree for proper scaling
                double dpi = 96;
                var presentationSource = PresentationSource.FromVisual(this);
                if (presentationSource?.CompositionTarget != null)
                {
                    dpi = 96 * presentationSource.CompositionTarget.TransformToDevice.M11;
                }
                Source = EmojiRenderer.GetBitmapSource(Emoji, iconSize: 48, dpi: dpi);
            }

            base.OnRender(dc);
        }
    }
}
