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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Recipes;

namespace Greenshot.Recipes
{
    /// <summary>
    /// Greenshot Light has only the built-in recipes: it doesn't load, approve or save recipe files (RecipeManager.RecipeFiles.cs and
    /// RecipeManager.ExtensionFiles.cs aren't in it). These are the members of <see cref="IRecipeManager"/> which only work with files.
    /// </summary>
    public partial class RecipeManager
    {
        private const string NoRecipeFiles = "Greenshot Light has only the built-in recipes, it doesn't load or save recipe files.";

        private static RecipeValidationResult NoRecipeFilesResult()
        {
            var result = new RecipeValidationResult();
            result.AddError(NoRecipeFiles);
            return result;
        }

        public RecipeValidationResult LoadRecipeFromFile(string filePath) => NoRecipeFilesResult();

        public RecipeValidationResult SaveRecipeToFile(CaptureRecipe recipe, string filePath) => NoRecipeFilesResult();

        public RecipeValidationResult SaveExtensionToFile(RecipeExtension extension, string filePath) => NoRecipeFilesResult();

        public IReadOnlyList<string> GetSaveDecisionReasons(CaptureRecipe recipe, string filePath) => Array.Empty<string>();

        /// <summary>
        /// Only an automatic step from a file can be unloaded, Greenshot Light has none
        /// </summary>
        public bool UnregisterExtension(string extensionId) => false;

        /// <summary>
        /// A built-in recipe needs no approval and has no file which can change
        /// </summary>
        public Task<CaptureRecipe> EnsureRecipeApprovedAndUpToDateAsync(CaptureRecipe currentRecipe, CancellationToken cancellationToken = default) =>
            Task.FromResult(currentRecipe);

        /// <summary>
        /// Only a recipe from a file has an approval to review
        /// </summary>
        public RecipeValidationResult ReviewApproval(string recipeId) => null;

        /// <summary>
        /// The details are shown read-only in the approval window, which Greenshot Light doesn't have (nothing in it asks for it)
        /// </summary>
        public bool ShowRecipeDetails(string recipeId) => false;
    }
}
