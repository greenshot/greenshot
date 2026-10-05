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
using Greenshot.Base.Recipes.Pipeline;
using Greenshot.Base.Recipes.Triggers;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Definition of a DAG capture recipe / workflow.
    /// Encapsulates a Directed Acyclic Graph composed of flow-local nodes and flow transitions.
    /// Nodes can execute asynchronously, split to multiple concurrent branches, and merge into join nodes without loops.
    /// </summary>
    public class CaptureRecipe : FlowDefinition
    {
        public override string Kind => KindRecipe;

        /// <summary>
        /// Modular triggers configured for this recipe (e.g. Hotkey, ContextMenu, Clipboard).
        /// </summary>
        public List<TriggerConfig> Triggers { get; set; } = new List<TriggerConfig>();

        /// <summary>
        /// Whether this recipe should appear as an option in the systray context menu.
        /// </summary>
        public bool ShowInContextMenu { get; set; } = true;

        /// <summary>
        /// Indicates if this recipe has been overridden by an external configuration file.
        /// </summary>
        public bool IsOverridden { get; set; }

        /// <summary>
        /// Whether this recipe is currently activated / enabled.
        /// Disabled recipes do not register active triggers and cannot be triggered from menus or shortcuts.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// The file path this recipe was loaded from, if loaded from external JSON.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The AI tool which wrote the recipe file, null when it wasn't written by an AI tool. Set by Greenshot from the approval,
        /// never read from the recipe file.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string ProposedBy { get; set; }

        /// <summary>
        /// What happens when the recipe is started while a flow of it is still running; null means <see cref="FlowConcurrency.Parallel"/>.
        /// </summary>
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public FlowConcurrency? Concurrency { get; set; }

        /// <summary>
        /// Set by <see cref="RecipeComposer"/> on the recipe it composed: the extensions it put in (in the flow or for the
        /// destinations). ${option.extension.key} in their nodes reads their options. Never read from a file.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public IReadOnlyList<RecipeExtension> AppliedExtensions { get; set; } = Array.Empty<RecipeExtension>();

        /// <summary>
        /// Set by <see cref="RecipeComposer"/>: the chains of the extensions on the BeforeDestination slot, run by the export
        /// steps for each destination on its own copy of the capture.
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public IReadOnlyList<ExtensionChain> DestinationChains { get; set; } = Array.Empty<ExtensionChain>();

        /// <summary>
        /// True for a recipe made by <see cref="RecipeComposer"/>, which isn't composed again
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsComposed { get; set; }

        public CaptureRecipe()
        {
        }

        public CaptureRecipe(string id, string name, string description = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? id;
            Description = description;
        }

        public CaptureRecipe AddNode(RecipeNodeConfig node)
        {
            if (node != null)
            {
                if (Nodes == null) Nodes = new List<RecipeNodeConfig>();
                Nodes.Add(node);
            }
            return this;
        }

        public CaptureRecipe AddTrigger(TriggerConfig trigger)
        {
            if (trigger != null)
            {
                if (Triggers == null) Triggers = new List<TriggerConfig>();
                Triggers.Add(trigger);
            }
            return this;
        }

        /// <summary>
        /// A recipe AI tools run (it has an AI tool trigger): extensions only change it when they name it
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsAiToolRecipe => Triggers?.Any(t => string.Equals(t?.TriggerType, TriggerConfig.TypeAiTool, StringComparison.OrdinalIgnoreCase)) ?? false;

        /// <summary>
        /// Checks whether the recipe explicitly exports to an Image Editor destination.
        /// </summary>
        public bool HasEditorDestination()
        {
            if (Nodes == null || Nodes.Count == 0) return false;
            foreach (var n in Nodes)
            {
                if (string.Equals(n.StepType, WellKnownStepTypes.Editor, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (string.Equals(n.StepType, WellKnownStepTypes.Destinations, StringComparison.OrdinalIgnoreCase))
                {
                    var dests = n.GetParameter<List<string>>("DestinationDesignations");
                    if (dests != null && dests.Any(d => string.Equals(d, "Editor", StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether the recipe contains any video recording step.
        /// </summary>
        public bool HasVideoStep()
        {
            if (Nodes == null || Nodes.Count == 0) return false;
            return Nodes.Any(node =>
                string.Equals(node.StepType, WellKnownStepTypes.RecordVideo, StringComparison.OrdinalIgnoreCase));
        }

        public CaptureRecipe Clone()
        {
            var clone = new CaptureRecipe
            {
                ShowInContextMenu = ShowInContextMenu,
                IsOverridden = IsOverridden,
                IsEnabled = IsEnabled,
                FilePath = FilePath,
                ProposedBy = ProposedBy,
                Concurrency = Concurrency,
                AppliedExtensions = AppliedExtensions,
                DestinationChains = DestinationChains,
                IsComposed = IsComposed,
                Triggers = Triggers?.Select(t => t.Clone()).ToList() ?? new List<TriggerConfig>()
            };
            CopyTo(clone);
            return clone;
        }
    }
}
