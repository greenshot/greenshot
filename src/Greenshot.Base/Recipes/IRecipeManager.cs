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

using System;
using System.Collections.Generic;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Registry managing available built-in and user-configured capture recipes.
    /// </summary>
    public interface IRecipeManager
    {
        /// <summary>
        /// Gets all currently registered recipes.
        /// </summary>
        IReadOnlyList<CaptureRecipe> GetAllRecipes();

        /// <summary>
        /// Resolves a recipe by its unique ID.
        /// </summary>
        CaptureRecipe GetRecipeById(string id);

        /// <summary>
        /// Registers or updates a recipe.
        /// </summary>
        void RegisterRecipe(CaptureRecipe recipe);

        /// <summary>
        /// Removes a custom recipe by ID (built-in recipes cannot be removed).
        /// </summary>
        bool UnregisterRecipe(string recipeId);

        /// <summary>
        /// Loads one or more recipes explicitly from a trusted JSON file path.
        /// Overrides built-in recipes if the recipe ID matches.
        /// </summary>
        RecipeValidationResult LoadRecipeFromFile(string filePath);

        /// <summary>
        /// Verifies that an external recipe's backing file on disk has not been modified since approval.
        /// If modified, interactively prompts for approval (if supported) and reloads the recipe.
        /// Returns the verified up-to-date recipe, or null if unapproved or rejected.
        /// </summary>
        CaptureRecipe EnsureRecipeApprovedAndUpToDate(CaptureRecipe currentRecipe);

        /// <summary>
        /// Resets an overridden built-in recipe back to its original default definition.
        /// </summary>
        bool ResetToDefault(string recipeId);

        /// <summary>
        /// Reloads built-in recipes and re-applies configured recipe files from greenshot.ini.
        /// </summary>
        void ReloadRecipes();

        /// <summary>
        /// Event raised when recipes are added, modified, or removed.
        /// </summary>
        event EventHandler RecipesChanged;
    }
}
