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

using System.Drawing;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Wpf;
using Greenshot.Editor.Controls;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The editor window takes its colors from the Greenshot theme: light keeps the system colors, dark the dark palette
    /// </summary>
    [Collection(TestCollections.WpfThemeState)]
    public class EditorThemeTests
    {
        public EditorThemeTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void EditorTheme_FollowsTheGreenshotTheme()
        {
            var themeManager = ThemeManager.Instance;
            var previousTheme = themeManager.Theme;
            try
            {
                themeManager.Theme = UiTheme.Light;
                themeManager.Refresh();
                if (themeManager.IsHighContrast)
                {
                    // High contrast always uses the system colors
                    Assert.False(EditorTheme.Current.IsDark);
                    return;
                }

                var light = EditorTheme.Current;
                Assert.False(light.IsDark);
                Assert.Equal(SystemColors.Control, light.Background);
                Assert.Equal(SystemColors.ControlText, light.Foreground);

                themeManager.Theme = UiTheme.Dark;
                themeManager.Refresh();
                var dark = EditorTheme.Current;
                Assert.True(dark.IsDark);
                var background = themeManager.CurrentPalette.BackgroundBrush.Color;
                Assert.Equal(Color.FromArgb(background.A, background.R, background.G, background.B), dark.Background);
                // Text must be readable on the background
                Assert.True(dark.Foreground.GetBrightness() - dark.Background.GetBrightness() > 0.5f);
            }
            finally
            {
                themeManager.Theme = previousTheme;
                themeManager.Refresh();
            }
        }
    }
}
