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
using System.Linq;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Xunit;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// Blur and pixelate follow the height of the text they cover
    /// </summary>
    public class ObfuscationStrengthTests
    {
        public ObfuscationStrengthTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static IOcrLineFeature Line(int left, int top, int width, int height) =>
            new DetectedOcrLine(new NativeRect(left, top, width, height), "text", new List<OcrWordInfo>());

        [Theory]
        [InlineData(12, 50, 6)]
        [InlineData(24, 50, 12)]
        [InlineData(13, 50, 7)]
        [InlineData(1, 50, 1)]
        [InlineData(400, 50, 100)]
        public void BlurRadius_FollowsTheTextHeight_WithinTheToolbarLimits(int textHeight, int percent, int expected)
        {
            Assert.Equal(expected, ObfuscationStrength.BlurRadius(textHeight, percent));
        }

        [Theory]
        [InlineData(12, 75, 9)]
        [InlineData(24, 75, 18)]
        [InlineData(1, 75, 2)]
        public void PixelSize_FollowsTheTextHeight_AndPixelatesAtLeastTwoPixels(int textHeight, int percent, int expected)
        {
            Assert.Equal(expected, ObfuscationStrength.PixelSize(textHeight, percent));
        }

        [Fact]
        public void CoveredTextHeight_IsTheMedianOfTheCoveredLines()
        {
            var lines = new[]
            {
                Line(0, 0, 100, 12),
                Line(0, 20, 100, 14),
                Line(0, 40, 100, 30),
                // Not covered: outside the area
                Line(0, 200, 100, 50),
            };

            Assert.Equal(14, ObfuscationStrength.CoveredTextHeight(lines, new NativeRect(10, 0, 50, 80)));
        }

        [Fact]
        public void CoveredTextHeight_IgnoresLinesTheAreaOnlyTouches()
        {
            // The area covers 3 of the 20 pixels of the line
            var lines = new[] { Line(0, 0, 100, 20) };

            Assert.Null(ObfuscationStrength.CoveredTextHeight(lines, new NativeRect(0, 17, 100, 30)));
            Assert.Null(ObfuscationStrength.CoveredTextHeight(null, new NativeRect(0, 0, 100, 30)));
        }

        [Fact]
        public void FitToText_SetsTheValueOfTheCurrentEffect()
        {
            using var surface = new Surface(new Bitmap(100, 100));
            var obfuscate = new ObfuscateContainer(surface);
            obfuscate.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, PreparedFilter.BLUR);

            ObfuscationStrength.FitToText(obfuscate, 20, 50, 75);
            Assert.Equal(10, Convert.ToInt32(obfuscate.GetField(FieldType.BLUR_RADIUS).Value));

            obfuscate.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, PreparedFilter.PIXELIZE);
            ObfuscationStrength.FitToText(obfuscate, 20, 50, 75);
            Assert.Equal(15, Convert.ToInt32(obfuscate.GetField(FieldType.PIXEL_SIZE).Value));
        }

        private static int Strength(DrawableContainer obfuscate) =>
            obfuscate.HasField(FieldType.BLUR_RADIUS) ? Convert.ToInt32(obfuscate.GetField(FieldType.BLUR_RADIUS).Value) : Convert.ToInt32(obfuscate.GetField(FieldType.PIXEL_SIZE).Value);

        private static int Expected(DrawableContainer obfuscate, int textHeight) =>
            obfuscate.HasField(FieldType.BLUR_RADIUS)
                ? ObfuscationStrength.BlurRadius(textHeight, ObfuscationStrength.DefaultBlurRadiusPercent)
                : ObfuscationStrength.PixelSize(textHeight, ObfuscationStrength.DefaultPixelSizePercent);

        [Fact]
        public void DrawnObfuscation_FitsTheOcrLinesItCovers()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            // Enter draws 160x80 at 120,110 (see SurfaceKeyboardTests)
            surface.CaptureDetails.Features.Add(Line(100, 130, 200, 20));
            surface.DrawingMode = DrawingModes.Obfuscate;

            Assert.True(surface.ProcessCanvasKey(Keys.Enter));

            var obfuscate = Assert.Single(surface.Elements.OfType<ObfuscateContainer>());
            Assert.Equal(Expected(obfuscate, 20), Strength(obfuscate));
            // The toolbar shows the value of the new obfuscation
            var fieldType = obfuscate.HasField(FieldType.BLUR_RADIUS) ? FieldType.BLUR_RADIUS : FieldType.PIXEL_SIZE;
            Assert.Equal(Strength(obfuscate), surface.FieldAggregator.GetField(fieldType).Value);
        }

        [Fact]
        public void DrawnObfuscation_WithoutCoveredOcrLines_KeepsTheLastUsedValue()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            // Enter draws 160x80 at 120,110, this line is outside
            surface.CaptureDetails.Features.Add(Line(0, 250, 400, 20));
            surface.DrawingMode = DrawingModes.Obfuscate;
            var undrawn = new ObfuscateContainer(surface);
            int lastUsed = Strength(undrawn);

            Assert.True(surface.ProcessCanvasKey(Keys.Enter));

            var obfuscate = Assert.Single(surface.Elements.OfType<ObfuscateContainer>());
            Assert.Equal(lastUsed, Strength(obfuscate));
        }
    }
}
