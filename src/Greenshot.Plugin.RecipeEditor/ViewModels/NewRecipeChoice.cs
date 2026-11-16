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

using Greenshot.Base.Recipes;

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
        CaptureRegionToFile
    }

    /// <summary>
    /// One entry of the New recipe window: a template, or an existing recipe to start from a copy of
    /// </summary>
    public class NewRecipeChoice
    {
        public NewRecipeChoice(NewRecipeTemplate template, string title, string description, string group)
        {
            Template = template;
            Title = title;
            Description = description;
            Group = group;
        }

        public NewRecipeChoice(CaptureRecipe source, string group)
        {
            Source = source;
            Title = source.DisplayName;
            Description = source.DisplayDescription;
            Group = group;
        }

        /// <summary>
        /// The template, when <see cref="Source"/> is null
        /// </summary>
        public NewRecipeTemplate Template { get; }

        /// <summary>
        /// The recipe which is copied, null for a template
        /// </summary>
        public CaptureRecipe Source { get; }

        public string Title { get; }

        public string Description { get; }

        /// <summary>
        /// The heading the entry is shown under
        /// </summary>
        public string Group { get; }
    }
}
