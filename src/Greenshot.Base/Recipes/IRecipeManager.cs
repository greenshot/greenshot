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
        /// Checks whether a recipe is currently activated / enabled.
        /// </summary>
        bool IsRecipeEnabled(string recipeId);

        /// <summary>
        /// Activates or deactivates (enables or disables) a recipe by ID.
        /// Persists the state to configuration and updates active triggers.
        /// </summary>
        void SetRecipeEnabled(string recipeId, bool enabled);

        /// <summary>
        /// Loads one or more recipes explicitly from a trusted JSON file path.
        /// Overrides built-in recipes if the recipe ID matches.
        /// </summary>
        RecipeValidationResult LoadRecipeFromFile(string filePath);

        /// <summary>
        /// Verifies that an external recipe's backing file on disk has not been modified since approval.
        /// If modified, interactively prompts for approval (if supported) and reloads the recipe.
        /// Returns the verified up-to-date recipe, or null if unapproved or rejected.
        /// The prompt (and the reload) run on the UI thread, callable from any thread.
        /// </summary>
        System.Threading.Tasks.Task<CaptureRecipe> EnsureRecipeApprovedAndUpToDateAsync(CaptureRecipe currentRecipe, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Resets an overridden built-in recipe back to its original default definition.
        /// </summary>
        bool ResetToDefault(string recipeId);

        /// <summary>
        /// Shows the approval of a recipe from a file again, so the user can switch triggers and permissions on or off.
        /// Returns null when the recipe has no file.
        /// </summary>
        RecipeValidationResult ReviewApproval(string recipeId);

        /// <summary>
        /// Saves a recipe from Greenshot's recipe editor to the file and registers it. The approval is renewed for exactly the saved
        /// content; the approval window is only shown when the change adds a trigger which starts the recipe on its own or from
        /// outside, a kind of gated action that wasn't allowed, or replaces a built-in recipe. Errors when it wasn't saved.
        /// </summary>
        RecipeValidationResult SaveRecipeToFile(CaptureRecipe recipe, string filePath);

        /// <summary>
        /// What saving the recipe (as edited in the recipe editor) to the file would ask the user to decide, empty when the save
        /// renews the approval without asking. See <see cref="SaveRecipeToFile"/>.
        /// </summary>
        IReadOnlyList<string> GetSaveDecisionReasons(CaptureRecipe recipe, string filePath);

        /// <summary>
        /// What the recipe does in plain words, its approval and the changes against the built-in recipe it replaces; null for an unknown id
        /// </summary>
        RecipeDetails GetRecipeDetails(string recipeId);

        /// <summary>
        /// Brings back every built-in recipe a file replaces
        /// </summary>
        void ResetAllToDefault();

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
