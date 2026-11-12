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
using System.Drawing;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Editor.Configuration;
using Greenshot.Editor.Controls;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The editor window takes its colors from its own theme setting: light (default) keeps the system colors, dark the dark palette
    /// </summary>
    [Collection(TestCollections.WpfThemeState)]
    public class EditorThemeTests
    {
        public EditorThemeTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void EditorTheme_FollowsTheEditorSetting_DefaultLight()
        {
            var editorConfiguration = IniConfigHelper.EnsureSection<IEditorConfiguration>(() => new EditorConfigurationImpl());
            var previousTheme = editorConfiguration.EditorTheme;
            try
            {
                if (SystemInformation.HighContrast)
                {
                    // High contrast always uses the system colors
                    editorConfiguration.EditorTheme = UiTheme.Dark;
                    EditorTheme.Refresh();
                    Assert.False(EditorTheme.Current.IsDark);
                    return;
                }

                editorConfiguration.EditorTheme = UiTheme.Light;
                EditorTheme.Refresh();
                var light = EditorTheme.Current;
                Assert.False(light.IsDark);
                Assert.Equal(SystemColors.Control, light.Background);
                Assert.Equal(SystemColors.ControlText, light.Foreground);

                bool changed = false;
                EventHandler handler = (s, e) => changed = true;
                EditorTheme.Changed += handler;
                try
                {
                    editorConfiguration.EditorTheme = UiTheme.Dark;
                    EditorTheme.Refresh();
                }
                finally
                {
                    EditorTheme.Changed -= handler;
                }

                Assert.True(changed);
                var dark = EditorTheme.Current;
                Assert.True(dark.IsDark);
                // Text must be readable on the background
                Assert.True(dark.Foreground.GetBrightness() - dark.Background.GetBrightness() > 0.5f);
            }
            finally
            {
                editorConfiguration.EditorTheme = previousTheme;
                EditorTheme.Refresh();
            }
        }
    }
}
