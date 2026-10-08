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
    }
}
