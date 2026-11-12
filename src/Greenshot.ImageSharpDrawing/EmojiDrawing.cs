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
using System.IO;
using System.Linq;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.ImageSharpDrawing
{
    /// <summary>
    /// Draws an emoji in color with the Twemoji font which comes with Greenshot, so it looks the same everywhere
    /// </summary>
    public static class EmojiDrawing
    {
        /// <summary>
        /// The name of the font file, next to the Greenshot assemblies
        /// </summary>
        public const string FontFileName = "Twemoji.Mozilla.ttf";

        private static readonly FontCollection Fonts = new();

        private static readonly Lazy<FontFamily> Family = new(() =>
        {
            var folder = Path.GetDirectoryName(typeof(EmojiDrawing).Assembly.Location) ?? AppContext.BaseDirectory;
            var file = Path.Combine(folder, FontFileName);
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"Can't find {file}, bad installation?", file);
            }

            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            return Fonts.Add(stream);
        });

        /// <summary>
        /// Draws the emoji in the middle of a square
        /// </summary>
        /// <param name="context">What is drawn on, a drawing transform of it (e.g. a rotation) applies</param>
        /// <param name="emoji">The emoji</param>
        /// <param name="center">The middle of the square</param>
        /// <param name="size">The side of the square in pixels</param>
        public static void Draw(IImageProcessingContext context, string emoji, PointF center, float size)
        {
            if (string.IsNullOrEmpty(emoji) || size <= 0)
            {
                return;
            }

            var font = Family.Value.CreateFont(size * 0.95f, FontStyle.Regular);
            var options = new RichTextOptions(font)
            {
                // The glyphs of the font sit a bit high
                Origin = new PointF(center.X, center.Y + font.Size * 0.045f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            context.DrawText(options, emoji, Color.Black);
        }
    }
}
