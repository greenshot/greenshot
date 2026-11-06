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

using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// LeftClickAction and DoubleClickAction (ini only, #1708) move once to TrayClickRecipe and TrayDoubleClickRecipe
    /// </summary>
    public class CoreConfigurationTrayClickTests
    {
        public CoreConfigurationTrayClickTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public void Defaults_MenuOnClick_FolderOnDoubleClick()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();

            config.OnAfterLoad();

            Assert.Equal("recipe_tray_menu", config.TrayClickRecipe);
            Assert.Equal("recipe_open_capture_folder", config.TrayDoubleClickRecipe);
        }

        [Theory]
        [InlineData("DO_NOTHING", "")]
        [InlineData("SHOW_CONTEXT_MENU", "recipe_tray_menu")]
        [InlineData("OPEN_LAST_IN_EXPLORER", "recipe_open_capture_folder")]
        [InlineData("OPEN_LAST_IN_EDITOR", "recipe_last_capture_in_editor")]
        [InlineData("OPEN_SETTINGS", "recipe_open_settings")]
        [InlineData("OPEN_EMPTY_EDITOR", "recipe_empty_editor")]
        [InlineData("CAPTURE_REGION", "recipe_region")]
        [InlineData("CAPTURE_SCREEN", "recipe_fullscreen")]
        [InlineData("CAPTURE_WINDOW", "recipe_window")]
        [InlineData("CAPTURE_CLIPBOARD", "recipe_clipboard")]
        [InlineData("OPEN_CLIPBOARD_IN_EDITOR", "recipe_clipboard")]
        [InlineData("OPEN_FILE_IN_EDITOR", "recipe_file")]
        public void OldAction_MovesToTheRecipe_Once(string action, string recipeId)
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            config.SetRawValue("LeftClickAction", action);
            config.SetRawValue("DoubleClickAction", action);

            config.OnAfterLoad();

            Assert.Equal(recipeId, config.TrayClickRecipe);
            Assert.Equal(recipeId, config.TrayDoubleClickRecipe);

            // A later choice stays
            config.TrayClickRecipe = "recipe_ocr";
            config.OnAfterLoad();
            Assert.Equal("recipe_ocr", config.TrayClickRecipe);
        }

        [Fact]
        public void UnknownOldAction_KeepsTheDefault()
        {
            var config = new CoreConfigurationImpl();
            config.ResetToDefaults();
            config.SetRawValue("DoubleClickAction", "SOMETHING_ELSE");

            config.OnAfterLoad();

            Assert.Equal("recipe_open_capture_folder", config.TrayDoubleClickRecipe);
        }
    }
}
