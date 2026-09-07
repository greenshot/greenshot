/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
    /// Definition of a capture recipe / workflow.
    /// Encapsulates an ordered sequence of modular steps (source acquisition, interactive selection,
    /// feedback, processors, destination exports, notifications, and conditional blocks).
    /// Decoupled from triggers (hotkeys, menus, clipboard events) so any trigger can invoke any recipe.
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
        /// Ordered list of modular steps/blocks defining the complete flow.
        /// </summary>
        public List<RecipeStepConfig> Steps { get; set; } = new List<RecipeStepConfig>();

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

        public CaptureRecipe AddStep(RecipeStepConfig step)
        {
            if (step != null)
            {
                Steps.Add(step);
            }
            return this;
        }

        public CaptureRecipe AddTrigger(TriggerConfig trigger)
        {
            if (trigger != null)
            {
                Triggers.Add(trigger);
            }
            return this;
        }

        public RecipeStepConfig FindStep(string stepType)
        {
            return Steps.FirstOrDefault(s => string.Equals(s.StepType, stepType, StringComparison.OrdinalIgnoreCase));
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
                Triggers = new List<TriggerConfig>(Triggers.Count),
                Steps = new List<RecipeStepConfig>(Steps.Count)
            };

            foreach (var trigger in Triggers)
            {
                clone.Triggers.Add(trigger.Clone());
            }

            foreach (var step in Steps)
            {
                clone.Steps.Add(step.Clone());
            }

            return clone;
        }

        public override string ToString() => Name ?? Id;
    }
}
