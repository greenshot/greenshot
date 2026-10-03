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
using System.Linq;
using Greenshot.Base.Pipeline;

namespace Greenshot.Recipes
{
    /// <summary>
    /// Record representing an approved external capture recipe file.
    /// </summary>
    public class RecipeTrustRecord
    {
        /// <summary>
        /// Origin of a recipe written by an AI tool, followed by the AI tool's name
        /// </summary>
        public const string AiOriginPrefix = "ai:";

        public string FilePath { get; set; }
        public string Sha256Hash { get; set; }
        public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Before the approvals per recipe (<see cref="Recipes"/>): one decision for every gated action of the file
        /// </summary>
        public bool AllowExternalCommands { get; set; }
        public string RecipeName { get; set; }
        public string RecipeVersion { get; set; }

        /// <summary>
        /// Who made the file: null for files the user imported or saved, "ai:" and the AI tool's name for a recipe an AI tool proposed
        /// </summary>
        public string Origin { get; set; }

        /// <summary>
        /// The approved file content, to show what changed when the file is modified (only for small files)
        /// </summary>
        public string ApprovedContent { get; set; }

        /// <summary>
        /// What the user approved per recipe of the file. Null for records from before this list: they approve every recipe
        /// and trigger of the file, see <see cref="GetApproval"/>.
        /// </summary>
        public List<RecipeApproval> Recipes { get; set; }

        public bool IsAiCreated => Origin != null && Origin.StartsWith(AiOriginPrefix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The approval of a recipe of this file, null when the user didn't approve it (yet).
        /// For an old record: every trigger, external commands as decided, network and file system access
        /// (they weren't asked for before, so recipes the user already uses keep working).
        /// </summary>
        public RecipeApproval GetApproval(string recipeId)
        {
            if (Recipes == null)
            {
                var gates = new List<RecipeGateType> { RecipeGateType.NetworkAccess, RecipeGateType.FileSystemAccess };
                if (AllowExternalCommands)
                {
                    gates.Add(RecipeGateType.ExternalCommand);
                    gates.Add(RecipeGateType.Custom);
                }
                return new RecipeApproval { RecipeId = recipeId, AllTriggers = true, AllowedGates = gates };
            }
            return Recipes.FirstOrDefault(r => string.Equals(r.RecipeId, recipeId, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// What the user approved for one recipe: which triggers may start it and which gated actions it may do.
    /// </summary>
    public class RecipeApproval
    {
        public string RecipeId { get; set; }

        /// <summary>
        /// The approved triggers, see <see cref="RecipeApprovalPolicy.GetTriggerKey"/>
        /// </summary>
        public List<string> ApprovedTriggers { get; set; } = new List<string>();

        /// <summary>
        /// Every trigger is approved (records from before the approval per trigger)
        /// </summary>
        public bool AllTriggers { get; set; }

        public List<RecipeGateType> AllowedGates { get; set; } = new List<RecipeGateType>();

        /// <summary>
        /// The recipe replaces the built-in recipe with the same id
        /// </summary>
        public bool ReplacesBuiltIn { get; set; }

        public bool IsTriggerApproved(string triggerKey) => AllTriggers || (ApprovedTriggers?.Contains(triggerKey, StringComparer.OrdinalIgnoreCase) ?? false);

        public bool IsGateAllowed(RecipeGateType gateType) => AllowedGates?.Contains(gateType) ?? false;
    }
}
