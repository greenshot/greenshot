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
using Greenshot.Base.Triggers;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Definition of a DAG capture recipe / workflow.
    /// Encapsulates a Directed Acyclic Graph composed of flow-local nodes and flow transitions.
    /// Nodes can execute asynchronously, split to multiple concurrent branches, and merge into join nodes without loops.
    /// </summary>
    public class CaptureRecipe
    {
        /// <summary>
        /// The recipe schema version (e.g. "1.0").
        /// </summary>
        public string Version { get; set; } = "1.0";

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Modular triggers configured for this recipe (e.g. Hotkey, ContextMenu, Clipboard).
        /// </summary>
        public List<TriggerConfig> Triggers { get; set; } = new List<TriggerConfig>();

        /// <summary>
        /// Specified flow-local nodes configured for execution.
        /// </summary>
        public List<RecipeNodeConfig> Nodes { get; set; } = new List<RecipeNodeConfig>();

        /// <summary>
        /// Flow definition specifying entry point(s) and node transitions (edges).
        /// </summary>
        public RecipeFlowConfig Flow { get; set; } = new RecipeFlowConfig();

        /// <summary>
        /// Whether this recipe should appear as an option in the systray context menu.
        /// </summary>
        public bool ShowInContextMenu { get; set; } = true;

        /// <summary>
        /// Indicates if this is one of Greenshot's default built-in recipes.
        /// </summary>
        public bool IsBuiltIn { get; set; }

        /// <summary>
        /// Indicates if this recipe has been overridden by an external configuration file.
        /// </summary>
        public bool IsOverridden { get; set; }

        /// <summary>
        /// The file path this recipe was loaded from, if loaded from external JSON.
        /// </summary>
        public string FilePath { get; set; }

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

        public RecipeNodeConfig FindNode(string nodeId)
        {
            return Nodes?.FirstOrDefault(n => string.Equals(n.Id, nodeId, StringComparison.OrdinalIgnoreCase));
        }

        public RecipeNodeConfig FindFirstNodeByType(string stepType)
        {
            return Nodes?.FirstOrDefault(n => string.Equals(n.StepType, stepType, StringComparison.OrdinalIgnoreCase));
        }

        public CaptureRecipe Clone()
        {
            var clone = new CaptureRecipe
            {
                Version = Version,
                Id = Id,
                Name = Name,
                Description = Description,
                ShowInContextMenu = ShowInContextMenu,
                IsBuiltIn = IsBuiltIn,
                IsOverridden = IsOverridden,
                FilePath = FilePath,
                Triggers = new List<TriggerConfig>(Triggers?.Count ?? 0),
                Nodes = new List<RecipeNodeConfig>(Nodes?.Count ?? 0),
                Flow = Flow?.Clone() ?? new RecipeFlowConfig()
            };

            if (Triggers != null)
            {
                foreach (var trigger in Triggers)
                {
                    clone.Triggers.Add(trigger.Clone());
                }
            }

            if (Nodes != null)
            {
                foreach (var node in Nodes)
                {
                    clone.Nodes.Add(node.Clone());
                }
            }

            return clone;
        }

        public override string ToString() => Name ?? Id;
    }
}
