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
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Greenshot.ImageSharpDrawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.Base.Wpf.Emoji
{
    /// <summary>
    /// Shows an emoji with the Twemoji font, as the editors draw it
    /// </summary>
    public class EmojiControl : System.Windows.Controls.Image
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
                Source = BitmapSourceOf(Emoji, 48);
            }

            base.OnRender(dc);
        }

        /// <summary>
        /// The emoji as frozen bitmap of size x size pixels
        /// </summary>
        public static BitmapSource BitmapSourceOf(string emoji, int size)
        {
            int stride = size * 4;
            var pixels = new byte[stride * size];
            using (var image = SixLabors.ImageSharp.Image.WrapMemory<Bgra32>(pixels.AsMemory(), size, size))
            {
                image.Mutate(context => EmojiDrawing.Draw(context, emoji, new PointF(size / 2f, size / 2f), size));
            }

            var source = BitmapSource.Create(size, size, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixels, stride);
            source.Freeze();
            return source;
        }
    }
}
