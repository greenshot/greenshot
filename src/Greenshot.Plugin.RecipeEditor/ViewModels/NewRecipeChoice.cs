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

namespace Greenshot.Plugin.RecipeEditor.ViewModels
{
    /// <summary>
    /// What a new recipe starts with
    /// </summary>
    public enum NewRecipeTemplate
    {
        /// <summary>
        /// No trigger and no steps
        /// </summary>
        Empty,

        /// <summary>
        /// A hotkey which opens one of Greenshot's windows
        /// </summary>
        HotkeyOpensWindow,

        /// <summary>
        /// A hotkey which captures a region and saves it to a file
        /// </summary>
        CaptureRegionToFile,

        /// <summary>
        /// A copy of an existing recipe, which is picked in the New recipe window
        /// </summary>
        CopyOfRecipe
    }

    /// <summary>
    /// One entry of the New recipe window
    /// </summary>
    public class NewRecipeChoice
    {
        public NewRecipeChoice(NewRecipeTemplate template, string title, string description)
        {
            Template = template;
            Title = title;
            Description = description;
        }

        public NewRecipeTemplate Template { get; }

        public string Title { get; }

        public string Description { get; }

        /// <summary>
        /// The entry needs a recipe to copy
        /// </summary>
        public bool IsCopy => Template == NewRecipeTemplate.CopyOfRecipe;
    }
}
