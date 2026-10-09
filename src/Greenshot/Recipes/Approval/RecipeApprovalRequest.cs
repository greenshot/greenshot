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

using System.Collections.Generic;
using Greenshot.Base.Recipes;

namespace Greenshot.Recipes.Approval
{
    /// <summary>
    /// What the approval window shows: the recipe, the exact content that is approved, and where it came from.
    /// </summary>
    public sealed class RecipeApprovalRequest
    {
        public CaptureRecipe Recipe { get; set; }

        /// <summary>
        /// The file the recipe is (or will be) saved in
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The content that was read (or will be written) once: shown, parsed and hashed, so the approval is for exactly this
        /// </summary>
        public string Content { get; set; }

        public string ContentHash { get; set; }

        public RecipeValidationResult Validation { get; set; }

        /// <summary>
        /// The trust record of the file, for an earlier approval
        /// </summary>
        public RecipeTrustRecord PreviousRecord { get; set; }

        /// <summary>
        /// The AI tool which proposed the recipe, null when the user imported or saved it
        /// </summary>
        public string ProposedByName { get; set; }

        public string ProposedByPath { get; set; }

        public string ProposedBySigner { get; set; }

        /// <summary>
        /// What the AI tool says the user asked for (the AI's words)
        /// </summary>
        public string AiRequest { get; set; }

        /// <summary>
        /// The AI tool's explanation of the recipe (the AI's words)
        /// </summary>
        public string AiExplanation { get; set; }

        /// <summary>
        /// The recipe this one replaces (a built-in recipe or the current version), to show the changes
        /// </summary>
        public CaptureRecipe ReplacedRecipe { get; set; }

        /// <summary>
        /// The JSON of the replaced recipe or of the previously approved file, for the diff
        /// </summary>
        public string PreviousContent { get; set; }

        public bool ReplacesBuiltIn { get; set; }

        /// <summary>
        /// Start with every trigger and gated action switched off (AI proposals, and changed files an AI tool created)
        /// </summary>
        public bool StartSwitchedOff { get; set; }

        /// <summary>
        /// The user saved the recipe in Greenshot's recipe editor: the reasons why the change needs a decision, null otherwise
        /// </summary>
        public IReadOnlyList<string> OwnEditReasons { get; set; }

        /// <summary>
        /// For an own edit: the switches as they will be (unchanged triggers keep theirs, the new ones the user added are on)
        /// </summary>
        public RecipeApproval SuggestedApproval { get; set; }

        /// <summary>
        /// Only show the recipe and its approval (details): nothing can be switched, the only button is Close
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// An automatic step (recipe extension) instead of a recipe: <see cref="Recipe"/> is its view as a recipe without triggers
        /// </summary>
        public RecipeExtension Extension { get; set; }

        /// <summary>
        /// For an automatic step: which recipes it changes, where and when, in plain words
        /// </summary>
        public IReadOnlyList<string> ExtensionReach { get; set; }

        public bool IsOwnEdit => OwnEditReasons != null;

        public bool IsAiProposal => !string.IsNullOrEmpty(ProposedByName);
    }
}
