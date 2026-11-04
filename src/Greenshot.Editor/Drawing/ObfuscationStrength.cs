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
using System.Collections.Generic;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// How strong blur and pixelate are, relative to the height of the text they cover.
    /// A fixed size that hides small text leaves large text readable, so the size follows the text.
    /// </summary>
    public static class ObfuscationStrength
    {
        /// <summary>
        /// Blur radius as percentage of the text height. From half the text height on, text can't be read by eye.
        /// </summary>
        public const int DefaultBlurRadiusPercent = 50;

        /// <summary>
        /// Pixel size as percentage of the text height. Pixelate needs more than blur: below about three quarters
        /// of the text height, the blocks of a known font can be told apart by trying candidate texts.
        /// </summary>
        public const int DefaultPixelSizePercent = 75;

        // The limits of the blur radius and pixel size in the editor's toolbar
        private const int MinimumBlurRadius = 1;
        private const int MinimumPixelSize = 2;
        private const int Maximum = 100;

        /// <summary>
        /// The median height of the OCR lines that the area covers, a line counts when at least half its height is inside
        /// </summary>
        /// <param name="ocrLines">The OCR lines, can be null</param>
        /// <param name="area">The area in image coordinates</param>
        /// <returns>The text height, null when the area covers no OCR line</returns>
        public static int? CoveredTextHeight(IEnumerable<IOcrLineFeature> ocrLines, NativeRect area)
        {
            if (ocrLines == null)
            {
                return null;
            }

            var heights = new List<int>();
            foreach (var line in ocrLines)
            {
                var lineBounds = line.Bounds;
                if (lineBounds.Height <= 0)
                {
                    continue;
                }
                var covered = lineBounds.Intersect(area);
                if (covered.Width > 0 && covered.Height * 2 >= lineBounds.Height)
                {
                    heights.Add(lineBounds.Height);
                }
            }

            if (heights.Count == 0)
            {
                return null;
            }
            heights.Sort();
            return heights[heights.Count / 2];
        }

        /// <summary>
        /// The blur radius for text of this height
        /// </summary>
        public static int BlurRadius(int textHeight, int percent) => FromTextHeight(textHeight, percent, MinimumBlurRadius);

        /// <summary>
        /// The pixel size for text of this height
        /// </summary>
        public static int PixelSize(int textHeight, int percent) => FromTextHeight(textHeight, percent, MinimumPixelSize);

        private static int FromTextHeight(int textHeight, int percent, int minimum)
        {
            int value = (int)Math.Round(textHeight * percent / 100.0, MidpointRounding.AwayFromZero);
            return Math.Min(Maximum, Math.Max(minimum, value));
        }

        /// <summary>
        /// Set the blur radius or pixel size of the container's current effect to fit text of this height
        /// </summary>
        /// <param name="container">The obfuscation</param>
        /// <param name="textHeight">The height of the text it covers</param>
        /// <param name="blurRadiusPercent">Blur radius as percentage of the text height</param>
        /// <param name="pixelSizePercent">Pixel size as percentage of the text height</param>
        public static void FitToText(ObfuscateContainer container, int textHeight, int blurRadiusPercent, int pixelSizePercent)
        {
            if (container.HasField(FieldType.BLUR_RADIUS))
            {
                container.SetFieldValue(FieldType.BLUR_RADIUS, BlurRadius(textHeight, blurRadiusPercent));
            }
            if (container.HasField(FieldType.PIXEL_SIZE))
            {
                container.SetFieldValue(FieldType.PIXEL_SIZE, PixelSize(textHeight, pixelSizePercent));
            }
        }

        /// <summary>
        /// The blur radius or pixel size of the container's current effect
        /// </summary>
        /// <param name="container">The obfuscation</param>
        /// <returns>The value, null when the effect has neither</returns>
        public static int? Strength(ObfuscateContainer container)
        {
            if (container.HasField(FieldType.BLUR_RADIUS))
            {
                return Convert.ToInt32(container.GetField(FieldType.BLUR_RADIUS).Value);
            }
            if (container.HasField(FieldType.PIXEL_SIZE))
            {
                return Convert.ToInt32(container.GetField(FieldType.PIXEL_SIZE).Value);
            }
            return null;
        }
    }
}
