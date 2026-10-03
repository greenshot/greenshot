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
using Newtonsoft.Json;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// What recipes and recipe extensions have in common: a DAG of nodes with its flow, the options the user sets once
    /// and the plugins it needs. A <see cref="CaptureRecipe"/> adds triggers, a <see cref="RecipeExtension"/> adds where it
    /// goes into other recipes. Both are stored in the same file type, told apart by <see cref="Kind"/>.
    /// </summary>
    public abstract class FlowDefinition
    {
        public const string KindRecipe = "recipe";
        public const string KindExtension = "extension";

        /// <summary>
        /// "recipe" or "extension", written first in the file
        /// </summary>
        [JsonProperty("kind", Order = -10)]
        public abstract string Kind { get; }

        /// <summary>
        /// The schema version (e.g. "1.0").
        /// </summary>
        public string Version { get; set; } = "1.0";

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Explicit extension or plugin dependencies required to execute this flow.
        /// </summary>
        public List<RecipeRequirement> Requires { get; set; } = new List<RecipeRequirement>();

        /// <summary>
        /// Specified flow-local nodes configured for execution.
        /// </summary>
        public List<RecipeNodeConfig> Nodes { get; set; } = new List<RecipeNodeConfig>();

        /// <summary>
        /// Flow definition specifying entry point(s) and node transitions (edges).
        /// </summary>
        public RecipeFlowConfig Flow { get; set; } = new RecipeFlowConfig();

        /// <summary>
        /// Values the user sets once in Settings > Recipes, read by the nodes with ${option.key}. See <see cref="RecipeOption"/>.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<RecipeOption> Options { get; set; }

        /// <summary>
        /// Indicates if this is one of Greenshot's built-in recipes or extensions.
        /// </summary>
        public bool IsBuiltIn { get; set; }

        public RecipeOption FindOption(string key)
        {
            return Options?.FirstOrDefault(o => o != null && string.Equals(o.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        [JsonIgnore]
        public bool HasOptions => Options != null && Options.Any(o => o != null);

        public RecipeNodeConfig FindNode(string nodeId)
        {
            return Nodes?.FirstOrDefault(n => string.Equals(n.Id, nodeId, StringComparison.OrdinalIgnoreCase));
        }

        public RecipeNodeConfig FindFirstNodeByType(string stepType)
        {
            return Nodes?.FirstOrDefault(n => string.Equals(n.StepType, stepType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks whether the flow contains any destination/export steps.
        /// </summary>
        public bool HasDestinationStep()
        {
            if (Nodes == null || Nodes.Count == 0) return false;
            return Nodes.Any(n => n != null && WellKnownStepTypes.IsDestination(n.StepType));
        }

        /// <summary>
        /// Determines whether the flow contains any source step.
        /// </summary>
        public bool HasSourceStep()
        {
            if (Nodes == null || Nodes.Count == 0) return false;
            return Nodes.Any(node =>
                string.Equals(node?.StepType, WellKnownStepTypes.Source, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(node?.StepType, WellKnownStepTypes.RecordVideo, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Copies the common parts into a clone
        /// </summary>
        protected void CopyTo(FlowDefinition clone)
        {
            clone.Version = Version;
            clone.Id = Id;
            clone.Name = Name;
            clone.Description = Description;
            clone.IsBuiltIn = IsBuiltIn;
            clone.Requires = Requires?.ToList() ?? new List<RecipeRequirement>();
            clone.Nodes = Nodes?.Select(n => n?.Clone()).ToList() ?? new List<RecipeNodeConfig>();
            clone.Flow = Flow?.Clone() ?? new RecipeFlowConfig();
            clone.Options = Options?.Select(o => o?.Clone()).ToList();
        }

        public override string ToString() => Name ?? Id;
    }
}
