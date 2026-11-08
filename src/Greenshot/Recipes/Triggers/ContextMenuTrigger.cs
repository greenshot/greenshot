/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using Greenshot.Base.Recipes.Triggers;

namespace Greenshot.Recipes.Triggers
{
    /// <summary>
    /// Trigger that fires when a user clicks the corresponding item in the systray context menu: in its Recipes menu, or in
    /// the tray menu itself with the group <see cref="TriggerConfig.MainMenuGroup"/>.
    /// </summary>
    public class ContextMenuTrigger : TriggerBase
    {
        public string MenuItemText { get; set; }
        public string Group { get; set; } = "Recipes";
        public int Order { get; set; } = 0;

        /// <summary>
        /// The name of the icon of the item, one of the icons of the tray menu, null for none
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// <see cref="TriggerConfig.ExpandDisplays"/> or <see cref="TriggerConfig.ExpandWindows"/> for a sub menu, null for an item
        /// </summary>
        public string Expand { get; set; }

        public ContextMenuTrigger(string id, string name, string menuItemText, string targetRecipeId, string group = "Recipes", int order = 0)
            : base(id, name, targetRecipeId)
        {
            MenuItemText = menuItemText;
            Group = string.IsNullOrWhiteSpace(group) ? "Recipes" : group;
            Order = order;
        }

        /// <summary>
        /// True when the item is in the tray menu itself, not in its Recipes menu
        /// </summary>
        public bool IsInMainMenu => string.Equals(Group?.Trim(), TriggerConfig.MainMenuGroup, System.StringComparison.OrdinalIgnoreCase);

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
        /// <param name="parameters">What the item of a sub menu passes to the recipe (e.g. TargetWindow), null for nothing</param>
        public void Fire(IDictionary<string, object> parameters = null)
        {
            OnTriggered(parameters ?? new Dictionary<string, object>());
        }
    }
}
