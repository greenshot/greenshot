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
using Greenshot.Base.Triggers;

namespace Greenshot.Triggers
{
    /// <summary>
    /// Trigger that fires when a user clicks the corresponding item in the systray context menu.
    /// </summary>
    public class ContextMenuTrigger : TriggerBase
    {
        public string MenuItemText { get; set; }
        public string Group { get; set; } = "Recipes";
        public int Order { get; set; } = 0;

        public ContextMenuTrigger(string id, string name, string menuItemText, string targetRecipeId, string group = "Recipes", int order = 0)
            : base(id, name, targetRecipeId)
        {
            MenuItemText = menuItemText;
            Group = string.IsNullOrWhiteSpace(group) ? "Recipes" : group;
            Order = order;
        }

        public override void Start()
        {
            // Context menu triggers are active when registered
        }

        public override void Stop()
        {
        }

        /// <summary>
        /// Invoked when the user clicks this item in the context menu.
        /// </summary>
        public void Fire(IDictionary<string, object> parameters = null)
        {
            OnTriggered(parameters);
        }
    }
}
