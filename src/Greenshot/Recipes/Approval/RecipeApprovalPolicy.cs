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
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;

namespace Greenshot.Recipes.Approval
{
    /// <summary>
    /// What an approval of a recipe means at run time: only the approved triggers can start it, and it only loads when every
    /// kind of gated action it contains (external commands, network, file system) is allowed.
    /// </summary>
    public static class RecipeApprovalPolicy
    {
        /// <summary>
        /// Suffix of the key of a Commandline trigger's AllowBrowserInvocation, which is approved separately
        /// </summary>
        public const string BrowserInvocationSuffix = ":browser";

        /// <summary>
        /// The key of a trigger in an approval: its position and type. The approval belongs to one version of the file (its hash),
        /// so the position identifies the trigger.
        /// </summary>
        public static string GetTriggerKey(int index, TriggerConfig trigger) => $"{index}:{trigger?.TriggerType}";

        public static string GetBrowserInvocationKey(int index, TriggerConfig trigger) => GetTriggerKey(index, trigger) + BrowserInvocationSuffix;

        /// <summary>
        /// A Commandline trigger which also lets web pages and the browser extension start the recipe
        /// </summary>
        public static bool AllowsBrowserInvocation(TriggerConfig trigger)
        {
            return trigger != null &&
                   string.Equals(trigger.TriggerType, TriggerConfig.TypeCommandline, StringComparison.OrdinalIgnoreCase) &&
                   trigger.GetParameter("AllowBrowserInvocation", false);
        }

        /// <summary>
        /// Marks the triggers of the (loaded) recipe as approved or not
        /// </summary>
        public static void Apply(CaptureRecipe recipe, RecipeApproval approval)
        {
            if (recipe?.Triggers == null)
            {
                return;
            }
            for (int i = 0; i < recipe.Triggers.Count; i++)
            {
                var trigger = recipe.Triggers[i];
                if (trigger == null)
                {
                    continue;
                }
                trigger.IsApproved = approval != null && approval.IsTriggerApproved(GetTriggerKey(i, trigger));
                trigger.IsBrowserInvocationApproved = approval != null && approval.IsTriggerApproved(GetBrowserInvocationKey(i, trigger));
            }
        }

        /// <summary>
        /// The kinds of gated actions of the recipe
        /// </summary>
        public static IReadOnlyList<RecipeGateType> GetGateTypes(RecipeValidationResult validationResult)
        {
            if (validationResult?.GatedActions == null)
            {
                return Array.Empty<RecipeGateType>();
            }
            return validationResult.GatedActions.Select(a => a.GateType).Distinct().OrderBy(g => g).ToList();
        }

        /// <summary>
        /// The kinds of gated actions of the recipe which the approval doesn't allow; the recipe isn't loaded when there are any
        /// </summary>
        public static IReadOnlyList<RecipeGateType> GetMissingGates(RecipeValidationResult validationResult, RecipeApproval approval)
        {
            return GetGateTypes(validationResult).Where(g => approval == null || !approval.IsGateAllowed(g)).ToList();
        }

        /// <summary>
        /// Name of a gate type for messages
        /// </summary>
        public static string GetGateName(RecipeGateType gateType)
        {
            return gateType switch
            {
                RecipeGateType.ExternalCommand => "external commands",
                RecipeGateType.NetworkAccess => "network access (uploads)",
                RecipeGateType.FileSystemAccess => "file system access",
                _ => "custom actions"
            };
        }
    }
}
