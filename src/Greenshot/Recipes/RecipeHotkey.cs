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
using Greenshot.Base.Languages;
using Greenshot.Base.Recipes;

namespace Greenshot.Recipes
{
    /// <summary>
    /// A hotkey of a recipe which is on, see <see cref="RecipeManager.GetActiveHotkeys"/>
    /// </summary>
    public class RecipeHotkey
    {
        /// <summary>
        /// The order and the names of the hotkeys of the general settings, before the hotkeys of the other recipes
        /// </summary>
        private static readonly IReadOnlyList<(string Setting, Func<string> Name)> Settings = new (string, Func<string>)[]
        {
            ("FullscreenHotkey", () => Texts.Core.ContextmenuCapturefullscreen),
            ("WindowHotkey", () => Texts.Core.ContextmenuCapturewindow),
            ("RegionHotkey", () => Texts.Core.ContextmenuCapturearea),
            ("LastregionHotkey", () => Texts.Core.ContextmenuCapturelastregion),
            ("ClipboardHotkey", () => Texts.Core.ContextmenuCaptureclipboard)
        };

        public CaptureRecipe Recipe { get; set; }

        /// <summary>
        /// The hotkey used now, empty when there is none
        /// </summary>
        public string Hotkey { get; set; }

        /// <summary>
        /// A hotkey of the general settings (e.g. "RegionHotkey"), stored in [Core] of greenshot.ini
        /// </summary>
        public string SettingName { get; set; }

        /// <summary>
        /// A Hotkey option of the recipe, stored in [RecipeOptions]
        /// </summary>
        public RecipeOption Option { get; set; }

        /// <summary>
        /// The hotkey is written in the recipe file: it is changed in the recipe editor
        /// </summary>
        public bool IsInRecipeFile => SettingName == null && Option == null;

        public string DisplayName
        {
            get
            {
                foreach (var setting in Settings)
                {
                    if (string.Equals(setting.Setting, SettingName, StringComparison.OrdinalIgnoreCase)) return setting.Name();
                }
                return RecipeText.Translate(Recipe?.Name ?? Recipe?.Id);
            }
        }

        /// <summary>
        /// Sorts the hotkeys of the general settings first, in the order the settings always showed them
        /// </summary>
        public int SortOrder
        {
            get
            {
                for (int i = 0; i < Settings.Count; i++)
                {
                    if (string.Equals(Settings[i].Setting, SettingName, StringComparison.OrdinalIgnoreCase)) return i;
                }
                return Settings.Count;
            }
        }
    }
}
