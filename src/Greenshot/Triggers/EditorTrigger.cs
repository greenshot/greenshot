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
using Greenshot.Base.Triggers;

namespace Greenshot.Triggers
{
    /// <summary>
    /// Trigger that presents a recipe option directly inside the Image Editor menu.
    /// When fired, the recipe executes against the current open editor canvas.
    /// </summary>
    public class EditorTrigger : TriggerBase, IEditorTrigger
    {
        public override string TriggerType => TriggerConfig.TypeEditor;
        public string MenuItemText { get; set; }
        public string Group { get; set; } = "Recipes";
        public int Order { get; set; } = 0;

        public EditorTrigger(string id, string name, string menuItemText, string targetRecipeId, string group = "Recipes", int order = 0)
            : base(id, name, targetRecipeId)
        {
            MenuItemText = menuItemText;
            Group = string.IsNullOrWhiteSpace(group) ? "Recipes" : group;
            Order = order;
        }

        public EditorTrigger(string targetRecipeId, TriggerConfig config)
            : base(Guid.NewGuid().ToString("N"), config?.Name ?? config?.GetParameter<string>("MenuItemText") ?? "Editor Recipe", targetRecipeId)
        {
            if (config != null)
            {
                MenuItemText = config.GetParameter<string>("MenuItemText", config.Name ?? "Editor Recipe");
                Group = config.GetParameter<string>("Group", "Recipes");
                Order = config.GetParameter<int>("Order", 0);
            }
        }

        public override void Start()
        {
            // Editor triggers are active when registered and polled by open ImageEditorForms
        }

        public override void Stop()
        {
        }

        /// <summary>
        /// Invoked when the user clicks this recipe item in the Image Editor menu.
        /// </summary>
        public void Fire(IDictionary<string, object> parameters = null)
        {
            OnTriggered(parameters);
        }
    }
}
