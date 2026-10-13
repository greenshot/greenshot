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
using System.Linq;

namespace Greenshot.Ai.ViewModels
{
    /// <summary>
    /// A recipe from a file in the settings, with its approval in a line
    /// </summary>
    public sealed class ApprovedRecipeViewModel
    {
        public ApprovedRecipeViewModel(Greenshot.Base.Recipes.CaptureRecipe recipe, Greenshot.Base.Recipes.RecipeDetails details)
        {
            Recipe = recipe;
            Details = details;
            string by = string.IsNullOrEmpty(details?.ProposedBy) ? "" : $" · written by {details.ProposedBy}";
            string state = details?.ApprovedAt == null ? "not approved"
                : details.IsApprovalCurrent ? $"approved {details.ApprovedAt:yyyy-MM-dd}"
                : "changed since its approval";
            int off = details?.Triggers?.Count(t => t.EndsWith("(off, not approved)", StringComparison.Ordinal)) ?? 0;
            string offText = off == 0 ? "" : off == 1 ? " · 1 trigger off" : $" · {off} triggers off";
            string changedBy = details?.ChangedBy?.Count > 0 ? $" · changed by {string.Join(", ", details.ChangedBy)}" : "";
            Title = details?.IsExtension == true ? $"Automatic step: {recipe.Name}" : recipe.Name;
            Subtitle = $"{state}{by}{offText}{changedBy}";
            NeedsAttention = details?.ApprovedAt == null || !details.IsApprovalCurrent || off > 0;
            DisplayText = $"{Title}{by} · {state}{offText}{changedBy}";
        }

        public string Title { get; }

        /// <summary>
        /// The approval state, who wrote it and the triggers left off
        /// </summary>
        public string Subtitle { get; }

        /// <summary>
        /// Not approved, changed since the approval, or triggers left off
        /// </summary>
        public bool NeedsAttention { get; }

        public Greenshot.Base.Recipes.CaptureRecipe Recipe { get; }

        public Greenshot.Base.Recipes.RecipeDetails Details { get; }

        public string RecipeId => Recipe.Id;

        public string DisplayText { get; }

        public override string ToString() => DisplayText;
    }
}
