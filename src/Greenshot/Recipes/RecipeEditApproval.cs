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
using System.Linq;
using Greenshot.Base.Recipes;
using Greenshot.Base.Triggers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Greenshot.Recipes
{
    /// <summary>
    /// What the user has to decide about a change made in the recipe editor
    /// </summary>
    public sealed class RecipeEditDecision
    {
        /// <summary>
        /// What the user has to decide, in words
        /// </summary>
        public List<string> Reasons { get; } = new List<string>();

        /// <summary>
        /// The keys of the new triggers which need a decision
        /// </summary>
        public List<string> TriggerKeys { get; } = new List<string>();

        public bool IsNeeded => Reasons.Count > 0;
    }

    /// <summary>
    /// The approval of a recipe the user saved in Greenshot's own recipe editor. The user is the author, so the approval is renewed
    /// without asking, unless the change adds something that needs a decision: a trigger which starts the recipe on its own or from
    /// outside (clipboard, schedule, command line, web pages, browser extension, AI tools), a kind of gated action that wasn't
    /// allowed before, or the replacement of a built-in recipe.
    /// </summary>
    public static class RecipeEditApproval
    {
        /// <summary>
        /// Triggers are compared as they are written to a recipe file: a trigger read from a file has camel-cased parameter names
        /// </summary>
        private static readonly JsonSerializer TriggerComparer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = { new StringEnumConverter() }
        });

        /// <summary>
        /// The approval for the edited recipe
        /// </summary>
        /// <param name="edited">The recipe as it will be saved</param>
        /// <param name="validation">Its validation, for the gated actions</param>
        /// <param name="approvedVersion">The previously approved version of the recipe, null when there is none</param>
        /// <param name="previousApproval">The previous approval, null when there is none</param>
        /// <param name="replacesBuiltIn">The recipe has the id of a built-in recipe</param>
        /// <param name="needsDecision">What the user has to decide (empty when the approval can be renewed without asking), and the keys of the new triggers it is about</param>
        public static RecipeApproval Create(CaptureRecipe edited, RecipeValidationResult validation, CaptureRecipe approvedVersion, RecipeApproval previousApproval,
            bool replacesBuiltIn, out RecipeEditDecision needsDecision)
        {
            needsDecision = new RecipeEditDecision();
            var approval = new RecipeApproval { RecipeId = edited?.Id, ReplacesBuiltIn = replacesBuiltIn };
            if (edited == null)
            {
                return approval;
            }

            if (replacesBuiltIn && previousApproval == null)
            {
                needsDecision.Reasons.Add($"It replaces the built-in recipe \"{edited.Id}\".");
            }

            // Triggers: unchanged ones keep their switch, new harmless ones are on, new ones which start the recipe on their own need a decision.
            // Without the approved version (an approval from before the content was kept) every risky trigger counts as new.
            var oldTriggers = approvedVersion?.Triggers ?? new List<TriggerConfig>();
            var used = new HashSet<int>();
            var matches = new Dictionary<int, int>();
            for (int i = 0; i < (edited.Triggers?.Count ?? 0); i++)
            {
                int match = FindSameTrigger(edited.Triggers[i], oldTriggers, used);
                if (match >= 0)
                {
                    used.Add(match);
                    matches[i] = match;
                }
            }

            foreach (var description in RecipeDescriber.DescribeTriggers(edited))
            {
                bool isBrowser = description.Key.EndsWith(RecipeApprovalPolicy.BrowserInvocationSuffix);
                int index = int.Parse(description.Key.Substring(0, description.Key.IndexOf(':')));
                if (previousApproval != null && matches.TryGetValue(index, out int oldIndex))
                {
                    var oldTrigger = oldTriggers[oldIndex];
                    string oldKey = isBrowser ? RecipeApprovalPolicy.GetBrowserInvocationKey(oldIndex, oldTrigger) : RecipeApprovalPolicy.GetTriggerKey(oldIndex, oldTrigger);
                    if (previousApproval.IsTriggerApproved(oldKey))
                    {
                        approval.ApprovedTriggers.Add(description.Key);
                    }
                    continue;
                }
                if (string.IsNullOrEmpty(description.Risk) || description.IsDisabled)
                {
                    // Harmless (hotkey, menu entry, ...) or switched off in the recipe: the user added it themselves
                    approval.ApprovedTriggers.Add(description.Key);
                    continue;
                }
                needsDecision.Reasons.Add(description.Label);
                needsDecision.TriggerKeys.Add(description.Key);
            }

            // Gated actions: what was allowed stays allowed, a new kind needs a decision
            foreach (var gateType in RecipeApprovalPolicy.GetGateTypes(validation))
            {
                if (previousApproval != null && previousApproval.IsGateAllowed(gateType))
                {
                    approval.AllowedGates.Add(gateType);
                }
                else
                {
                    needsDecision.Reasons.Add($"It needs {RecipeApprovalPolicy.GetGateName(gateType)}.");
                }
            }
            return approval;
        }

        /// <summary>
        /// The index of the same (unchanged) trigger in the old version, -1 when there is none
        /// </summary>
        internal static int FindSameTrigger(TriggerConfig trigger, IList<TriggerConfig> oldTriggers, ISet<int> used)
        {
            if (trigger == null)
            {
                return -1;
            }
            var token = JToken.FromObject(trigger, TriggerComparer);
            for (int i = 0; i < oldTriggers.Count; i++)
            {
                if (used.Contains(i) || oldTriggers[i] == null)
                {
                    continue;
                }
                if (JToken.DeepEquals(token, JToken.FromObject(oldTriggers[i], TriggerComparer)))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// The recipe with the id in the approved content of a trust record
        /// </summary>
        public static CaptureRecipe FindRecipe(string content, string recipeId)
        {
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }
            try
            {
                return RecipeSerializer.DeserializeList(content, validate: false)
                    .FirstOrDefault(r => string.Equals(r.Id, recipeId, System.StringComparison.OrdinalIgnoreCase));
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
