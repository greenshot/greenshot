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
using WpfPoint = System.Windows.Point;
using WpfRect = System.Windows.Rect;

namespace Greenshot.Tests.Native
{
    /// <summary>
    /// The boxes of Win10OcrProvider: the engine reports the rects of rotated text deskewed, around the centre of the bitmap it read
    /// </summary>
    public class Win10OcrProviderTests
    {
        [Fact]
        public void ToImageRect_RemovesPaddingAndScale()
        {
            Assert.Equal(new WpfRect(40, 30, 80, 20), Win10OcrProvider.ToImageRect(30, 20, 40, 10, 0.5, 10, 5));
            // The centre of a padded 200x100 bitmap
            Assert.Equal(new WpfPoint(180, 90), Win10OcrProvider.ToImageRotationCenter(200, 100, 0.5, 10, 5));
        }

        [Theory]
        [InlineData(30d)]
        [InlineData(-25d)]
        [InlineData(10d)]
        public void Box_Angle_BoundsAreTheBoxAroundTheRotatedCorners(double angle)
        {
            // As the engine reports it: in the coordinates of the padded and scaled bitmap
            const double x = 300, y = 80, width = 120, height = 24, bitmapWidth = 640, bitmapHeight = 360, scale = 0.5;
            const int paddingX = 7, paddingY = 3;

            var box = OcrBox.FromRect(Win10OcrProvider.ToImageRect(x, y, width, height, scale, paddingX, paddingY), angle,
                Win10OcrProvider.ToImageRotationCenter(bitmapWidth, bitmapHeight, scale, paddingX, paddingY));
            var bounds = box.Bounds;

            double radians = angle * Math.PI / 180;
            double centerX = bitmapWidth / 2, centerY = bitmapHeight / 2;
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var (cornerX, cornerY) in new[] { (x, y), (x + width, y), (x, y + height), (x + width, y + height) })
            {
                // Rotate in the coordinates of the bitmap the engine read, then remove the padding and the scale
                double rotatedX = centerX + (cornerX - centerX) * Math.Cos(radians) - (cornerY - centerY) * Math.Sin(radians);
                double rotatedY = centerY + (cornerX - centerX) * Math.Sin(radians) + (cornerY - centerY) * Math.Cos(radians);
                double imageX = (rotatedX - paddingX) / scale;
                double imageY = (rotatedY - paddingY) / scale;
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
            // The text itself keeps its size
            Assert.Equal(width / scale, box.Width, 6);
            Assert.Equal(height / scale, box.Height, 6);
        }

        [Fact]
        public void Line_IsTheUnionOfTheWords_AndOffsetMovesEverything()
        {
            var center = new WpfPoint(100, 100);
            var line = new DetectedOcrLine(default(OcrBox), "Two words", new List<OcrWordInfo>
            {
                new OcrWordInfo("Two", OcrBox.FromRect(new WpfRect(0, 0, 40, 10), 30, center)),
                new OcrWordInfo("words", OcrBox.FromRect(new WpfRect(60, 2, 40, 10), 30, center))
            });

            // The deskewed union of the words, turned the same way
            var expected = OcrBox.FromRect(new WpfRect(0, 0, 100, 12), 30, center);
            Assert.Equal(expected.Center.X, line.Box.Center.X, 6);
            Assert.Equal(expected.Center.Y, line.Box.Center.Y, 6);
            Assert.Equal(100, line.Box.Width, 6);
            Assert.Equal(12, line.Box.Height, 6);
            Assert.Equal(30, line.Box.Angle, 6);
            Assert.Null(line.Score);

            var bounds = line.Bounds;
            var wordBounds = line.Words[1].Bounds;
            line.Offset(-5, 20);

            Assert.Equal(new NativeRect(bounds.X - 5, bounds.Y + 20, bounds.Width, bounds.Height), line.Bounds);
            Assert.Equal(new NativeRect(wordBounds.X - 5, wordBounds.Y + 20, wordBounds.Width, wordBounds.Height), line.Words[1].Bounds);
        }

        /// <summary>
        /// Text drawn rotated, away from the centre of the image: the box of the line in the middle is where the drawn text is.
        /// Without rotating the rects back they are off by about angle * distance to the centre of the image (100 pixels for the 30 degrees case).
        /// A single word is only found when it is rotated a lot, the engine needs a few lines to find small angles.
        /// </summary>
        [Theory]
        [InlineData(30f, 560, 420, "Secret", "Secret")]
        [InlineData(-20f, 250, 180, "Secret", "Secret")]
        [InlineData(0f, 1250, 150, "Secret", "Secret")]
        [InlineData(3f, 1250, 150, "The quick brown fox\nSecret jumps over\nthe lazy dog today", "Secret jumps over")]
        [InlineData(-3f, 1450, 120, "The quick brown fox\nSecret jumps over\nthe lazy dog today", "Secret jumps over")]
        public async Task DoOcrAsync_RotatedTextOffCentre_BoxIsOnTheText(float angle, int centerX, int centerY, string text, string middleLine)
        {
            Assert.SkipWhen(Environment.OSVersion.Platform != PlatformID.Win32NT, "Windows OCR only runs on Windows.");
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

            var line = Assert.Single(lines, l => l.Text == middleLine);
            var textCenter = new WpfPoint(centerX, centerY);
            Assert.True(line.Box.Contains(textCenter), $"{line.Box} doesn't contain the centre of the text ({centerX}, {centerY})");
            Assert.InRange(line.Box.Angle, angle - 5, angle + 5);
            // Tight: its centre is where the text is drawn
            Assert.InRange(line.Box.Center.X, centerX - 10, centerX + 10);
            Assert.InRange(line.Box.Center.Y, centerY - 10, centerY + 10);
            if (middleLine != text)
            {
                // The other lines are above and below: their rotated boxes don't contain the centre of the middle line
                Assert.All(lines.Where(l => l != line), other => Assert.False(other.Box.Contains(textCenter)));
            }
        }
    }
}
