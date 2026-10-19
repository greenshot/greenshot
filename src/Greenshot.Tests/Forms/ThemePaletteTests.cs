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

using System.Windows.Media;
using Greenshot.Base.Wpf;
using Xunit;

namespace Greenshot.Tests.Forms
{
    public class ThemePaletteTests
    {
        [Theory]
        [InlineData(255, 255, 255, false)]
        [InlineData(255, 200, 0, false)]
        [InlineData(153, 235, 255, false)]
        [InlineData(0, 0, 0, true)]
        [InlineData(0, 90, 158, true)]
        public void ReadableForeground_PicksBlackOrWhite(byte r, byte g, byte b, bool white)
        {
            var foreground = ThemePalette.ReadableForeground(Color.FromRgb(r, g, b));
            Assert.Equal(white ? Colors.White : Colors.Black, foreground);
        }

        [Fact]
        public void Create_UsesTheAccentAndFreezesTheBrushes()
        {
            var accent = Color.FromRgb(0, 90, 158);
            var palette = ThemePalette.Create(true, accent);

            Assert.True(palette.IsDark);
            Assert.False(palette.IsHighContrast);
            Assert.Equal(accent, palette.AccentBrush.Color);
            Assert.Equal(Colors.White, palette.AccentForegroundBrush.Color);
            Assert.Same(palette.ForegroundBrush, palette.HighlightForegroundBrush);
            Assert.True(palette.BackgroundBrush.IsFrozen);
            // Created once, not on every get
            Assert.Same(palette.BackgroundBrush, palette.BackgroundBrush);
        }

        [Fact]
        public void HasSameColors_ComparesModeAndAccent()
        {
            var accent = Color.FromRgb(0, 90, 158);
            Assert.True(ThemePalette.Create(false, accent).HasSameColors(ThemePalette.Create(false, accent)));
            Assert.False(ThemePalette.Create(false, accent).HasSameColors(ThemePalette.Create(true, accent)));
            Assert.False(ThemePalette.Create(false, accent).HasSameColors(ThemePalette.Create(false, Colors.Orange)));
            Assert.False(ThemePalette.Create(false, accent).HasSameColors(null));
        }

        [Fact]
        public void CreateHighContrast_UsesTheSystemColors()
        {
            var palette = ThemePalette.CreateHighContrast();
            var window = System.Drawing.SystemColors.Window;
            var highlightText = System.Drawing.SystemColors.HighlightText;

            Assert.True(palette.IsHighContrast);
            Assert.Equal(Color.FromRgb(window.R, window.G, window.B), palette.BackgroundBrush.Color);
            Assert.Equal(Color.FromRgb(highlightText.R, highlightText.G, highlightText.B), palette.HighlightForegroundBrush.Color);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void StatusColors_HaveReadableContrast(bool isDarkTheme)
        {
            // WCAG 2.1 1.4.3: text needs a contrast ratio of at least 4.5:1 to its background
            var palette = isDarkTheme ? ThemePalette.Dark : ThemePalette.Light;

            Assert.True(Contrast(palette.SuccessBrush, palette.CardBackground) >= 4.5, "success text on a card");
            Assert.True(Contrast(palette.InfoBrush, palette.CardBackground) >= 4.5, "info text on a card");
            Assert.True(Contrast(palette.ErrorBrush, palette.ErrorBackground) >= 4.5, "error text in an error banner");
            Assert.True(Contrast(palette.WarningForegroundBrush, palette.WarningBrush) >= 4.5, "text on a warning button");
            Assert.True(Contrast(palette.DangerForegroundBrush, palette.DangerBrush) >= 4.5, "text on a danger button");
            Assert.True(Contrast(palette.DangerForegroundBrush, palette.DangerHoverBrush) >= 4.5, "text on a hovered danger button");
            Assert.True(Contrast(palette.DangerForegroundBrush, palette.DangerPressedBrush) >= 4.5, "text on a pressed danger button");
        }

        [Fact]
        public void CreateHighContrast_ShowsStatusInTheSystemColors()
        {
            var palette = ThemePalette.CreateHighContrast();

            Assert.Equal(palette.ForegroundBrush.Color, palette.SuccessBrush.Color);
            Assert.Equal(palette.ForegroundBrush.Color, palette.InfoBrush.Color);
            Assert.Equal(palette.BackgroundBrush.Color, palette.WarningForegroundBrush.Color);
            Assert.Equal(palette.AccentBrush.Color, palette.DangerBrush.Color);
            Assert.Equal(palette.AccentForegroundBrush.Color, palette.DangerForegroundBrush.Color);
        }

        private static double Contrast(SolidColorBrush foreground, SolidColorBrush background)
        {
            double a = ThemePalette.Luminance(foreground.Color) + 0.05;
            double b = ThemePalette.Luminance(background.Color) + 0.05;
            return a > b ? a / b : b / a;
        }
    }
}
