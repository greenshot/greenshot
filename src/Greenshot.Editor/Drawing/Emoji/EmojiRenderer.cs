/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.Editor.Drawing.Emoji
{
    /// <summary>
    /// This will render Emoji
    /// </summary>
    internal static class EmojiRenderer
    {
        /// <summary>
        /// The emoji which is shown on the emoji button of the editor
        /// </summary>
        public const string EmojiButtonEmoji = "\uD83D\uDE0A";

        /// <summary>
        /// The size of the image of the emoji button of the editor
        /// </summary>
        public const int EmojiButtonSize = 32;

        private static readonly FontCollection TwemojiFontCollection = new();
        private static readonly ConcurrentDictionary<string, Lazy<Task<System.Drawing.Image>>> SharedBitmaps = new();

        private static readonly Lazy<FontFamily> TwemojiFontFamily = new(() =>
        {
            var twemojiFontFile = Path.Combine(EnvironmentInfo.GetApplicationFolder(), "Twemoji.Mozilla.ttf");
            if (!File.Exists(twemojiFontFile))
            {
                throw new FileNotFoundException($"Can't find {twemojiFontFile}, bad installation?");
            }

            using var fileStream = new FileStream(twemojiFontFile, FileMode.Open, FileAccess.Read);
            TwemojiFontCollection.Add(fileStream);
            TwemojiFontCollection.TryGet("Twemoji Mozilla", out var fontFamily);
            
            return fontFamily;
        });

        public static Image<Rgba32> GetImage(string emoji, int iconSize)
        {
            var image = new Image<Rgba32>(iconSize, iconSize);

            RenderEmoji(emoji, iconSize, image);
            return image;
        }

        private static void RenderEmoji(string emoji, int iconSize, Image image)
        {
            var fontFamily = TwemojiFontFamily.Value;
            var font = fontFamily.CreateFont(iconSize * 0.95f, FontStyle.Regular);
            var verticalOffset = font.Size * 0.045f;
            var textOptions = new RichTextOptions(font)
            {
                Origin = new PointF(iconSize / 2.0f, iconSize / 2.0f + verticalOffset),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            image.Mutate(x => x.DrawText(textOptions, emoji, Color.Black));
        }

        /// <summary>
        /// Render the emoji once, on the thread pool, the result is shared: don't dispose or change it.
        /// </summary>
        /// <param name="emoji">string with the emoji</param>
        /// <param name="iconSize">int with the size</param>
        /// <returns>Task with the shared image</returns>
        public static Task<System.Drawing.Image> GetSharedBitmapAsync(string emoji, int iconSize)
        {
            // PARALLEL: the first rendering loads ImageSharp and the Twemoji font, which takes about a second, the UI must not wait for it
#pragma warning disable RS0030 // R10: documented parallel branch
            return SharedBitmaps.GetOrAdd($"{emoji}|{iconSize}", _ => new Lazy<Task<System.Drawing.Image>>(() => Task.Run(() => GetBitmap(emoji, iconSize)))).Value;
#pragma warning restore RS0030
        }

        public static System.Drawing.Image GetBitmap(string emoji, int iconSize)
        {
            int width = iconSize;
            int height = iconSize;

            var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                unsafe
                {
                    var image = Image.WrapMemory<Bgra32>((void*)bitmapData.Scan0, width: width, height: height);
                    RenderEmoji(emoji, iconSize, image);
                }
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        public static System.Windows.Media.Imaging.BitmapSource GetBitmapSource(string emoji, int iconSize)
        {
            var pixelFormat = System.Windows.Media.PixelFormats.Bgra32;
            int width = iconSize;
            int height = iconSize;
            int stride = (width * pixelFormat.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[stride * height];

            var image = Image.WrapMemory<Bgra32>(byteMemory: pixels, width: width, height: height);

            RenderEmoji(emoji, iconSize, image);

            var source = System.Windows.Media.Imaging.BitmapSource.Create(pixelWidth: width, pixelHeight: height, dpiX: 96, dpiY: 96, pixelFormat: pixelFormat, palette: null, pixels: pixels, stride: stride);
            source.Freeze();

            return source;
        }
    }
}
