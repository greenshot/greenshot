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
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Win10;
using Xunit;

namespace Greenshot.Tests.Native
{
    /// <summary>
    /// The word bounds of Win10OcrProvider: the engine reports the rects of rotated text deskewed, around the centre of the bitmap it read
    /// </summary>
    public class Win10OcrProviderTests
    {
        [Fact]
        public void ToImageRect_NoAngle_RemovesPaddingAndScale()
        {
            var bounds = Win10OcrProvider.ToImageRect(30, 20, 40, 10, 0, 200, 100, 0.5, 10, 5).Bounds;

            Assert.Equal(new NativeRect(40, 30, 80, 20), bounds);
        }

        [Fact]
        public void ToImageRect_90Degrees_RotatesClockwiseAroundTheBitmapCentre()
        {
            // Centre (100, 50). Rotating clockwise by 90 degrees (y points down): (x, y) -> (100 - (y - 50), 50 + (x - 100))
            // The corners (110, 50), (150, 50), (110, 60), (150, 60) become (100, 60), (100, 100), (90, 60), (90, 100)
            var bounds = Win10OcrProvider.ToImageRect(110, 50, 40, 10, 90, 200, 100, 1, 0, 0).Bounds;

            Assert.Equal(new NativeRect(90, 60, 10, 40), bounds);
        }

        [Theory]
        [InlineData(30d)]
        [InlineData(-25d)]
        [InlineData(10d)]
        public void ToImageRect_Angle_ContainsTheRotatedCorners(double angle)
        {
            const double x = 300, y = 80, width = 120, height = 24, bitmapWidth = 640, bitmapHeight = 360, scale = 0.5;
            const int paddingX = 7, paddingY = 3;

            var rotatedRect = Win10OcrProvider.ToImageRect(x, y, width, height, angle, bitmapWidth, bitmapHeight, scale, paddingX, paddingY);
            var bounds = rotatedRect.Bounds;
            var corners = rotatedRect.Corners;

            double radians = angle * Math.PI / 180;
            double centerX = bitmapWidth / 2, centerY = bitmapHeight / 2;
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            var expectedCorners = new[] { (x, y), (x + width, y), (x + width, y + height), (x, y + height) };
            for (int i = 0; i < expectedCorners.Length; i++)
            {
                var (cornerX, cornerY) = expectedCorners[i];
                // Rotate in the coordinates of the bitmap the engine read, then remove the padding and the scale
                double rotatedX = centerX + (cornerX - centerX) * Math.Cos(radians) - (cornerY - centerY) * Math.Sin(radians);
                double rotatedY = centerY + (cornerX - centerX) * Math.Sin(radians) + (cornerY - centerY) * Math.Cos(radians);
                double imageX = (rotatedX - paddingX) / scale;
                double imageY = (rotatedY - paddingY) / scale;
                Assert.Equal(imageX, corners[i].X, 9);
                Assert.Equal(imageY, corners[i].Y, 9);
                Assert.InRange(imageX, bounds.Left, bounds.Right);
                Assert.InRange(imageY, bounds.Top, bounds.Bottom);
                minX = Math.Min(minX, imageX);
                minY = Math.Min(minY, imageY);
                maxX = Math.Max(maxX, imageX);
                maxY = Math.Max(maxY, imageY);
            }

            // Tight: the box around the corners, only rounded outwards
            Assert.Equal((int)Math.Floor(minX), bounds.Left);
            Assert.Equal((int)Math.Floor(minY), bounds.Top);
            Assert.Equal((int)Math.Ceiling(maxX), bounds.Right);
            Assert.Equal((int)Math.Ceiling(maxY), bounds.Bottom);
        }

        /// <summary>
        /// Text drawn rotated, away from the centre of the image: the bounds of the line in the middle contain the centre of the drawn text.
        /// Without rotating the rects back they are off by about angle * distance to the centre of the image (100 pixels for the 30 degrees case).
        /// A single word is only found when it is rotated a lot, the engine needs a few lines to find small angles.
        /// </summary>
        [Theory]
        [InlineData(30f, 560, 420, "Secret", "Secret")]
        [InlineData(-20f, 250, 180, "Secret", "Secret")]
        [InlineData(0f, 1250, 150, "Secret", "Secret")]
        [InlineData(3f, 1250, 150, "The quick brown fox\nSecret jumps over\nthe lazy dog today", "Secret jumps over")]
        [InlineData(-3f, 1450, 120, "The quick brown fox\nSecret jumps over\nthe lazy dog today", "Secret jumps over")]
        public async Task DoOcrAsync_RotatedTextOffCentre_BoundsContainTheText(float angle, int centerX, int centerY, string text, string middleLine)
        {
            var provider = new Win10OcrProvider();
            var language = provider.GetAvailableLanguages().OrderBy(l => l.LanguageTag.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? 0 : 1).FirstOrDefault();
            if (language == null)
            {
                Assert.Skip("No OCR language is installed.");
            }

            int width = centerX > 800 ? 1600 : 800;
            using var image = new Bitmap(width, 600, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(image))
            using (var font = new Font("Arial", 28, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                graphics.Clear(Color.White);
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.TranslateTransform(centerX, centerY);
                // Positive is clockwise on the screen, like OcrResult.TextAngle
                graphics.RotateTransform(angle);
                graphics.DrawString(text, font, Brushes.Black, 0, 0, format);
            }

            var lines = await provider.DoOcrAsync(image, language.LanguageTag);

            var line = Assert.Single(lines.OfType<DetectedOcrLine>(), l => l.Text == middleLine);
            Assert.True(Contains(line.Bounds, centerX, centerY), $"{line.Bounds} doesn't contain the centre of the text ({centerX}, {centerY}), {line.RotatedBounds}");
            // Not far off either: the centre of the bounds is close to the centre of the text
            Assert.InRange(line.Bounds.Left + line.Bounds.Width / 2, centerX - 15, centerX + 15);
            Assert.InRange(line.Bounds.Top + line.Bounds.Height / 2, centerY - 15, centerY + 15);
            Assert.InRange(line.RotatedBounds.Angle, angle - 5, angle + 5);
            Assert.All(line.Words, word => Assert.Equal(line.RotatedBounds.Angle, word.RotatedBounds.Angle));
            // The rotated line is tight: its centre is where the text is drawn, and it contains the centre of each word
            var lineCenter = Center(line.RotatedBounds);
            Assert.InRange(lineCenter.X, centerX - 10, centerX + 10);
            Assert.InRange(lineCenter.Y, centerY - 10, centerY + 10);
            Assert.All(line.Words, word =>
            {
                var wordCenter = Center(word.RotatedBounds);
                Assert.True(line.RotatedBounds.Contains(wordCenter.X, wordCenter.Y));
            });
            if (middleLine != text)
            {
                // The other lines are above and below: their rotated bounds don't contain the centre of the middle line, their axis-aligned bounds can
                Assert.All(lines.Where(l => l != line), other => Assert.False(other.RotatedBounds.Contains(centerX, centerY)));
            }
        }

        private static System.Windows.Point Center(RotatedRect rect)
        {
            var corners = rect.Corners;
            return new System.Windows.Point((corners[0].X + corners[2].X) / 2, (corners[0].Y + corners[2].Y) / 2);
        }

        private static bool Contains(NativeRect rect, int x, int y) => x >= rect.Left && x < rect.Right && y >= rect.Top && y < rect.Bottom;
    }
}
