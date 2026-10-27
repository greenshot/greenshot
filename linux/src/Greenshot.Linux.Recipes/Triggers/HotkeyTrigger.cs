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

#nullable disable

using Greenshot.Linux.Recipes.Triggers;

namespace Greenshot.Linux.Recipes.Triggers
{
    /// <summary>
    /// A global hotkey: Wayland lets the GlobalShortcuts portal (KDE, GNOME, Hyprland) or the compositor bind keys, the app
    /// registers the hotkey there (<see cref="RecipeServices.Hotkeys"/>); "greenshot recipe &lt;id&gt;" runs the recipe from a binding
    /// </summary>
    public class HotkeyTrigger : TriggerBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HotkeyTrigger));
        private bool _registered;

        public string HotkeyString { get; set; }

        public HotkeyTrigger(string id, string name, string hotkeyString, string targetRecipeId)
            : base(id, name, targetRecipeId)
        {
            HotkeyString = hotkeyString;
        }

        public override void Start()
        {
            if (string.IsNullOrEmpty(HotkeyString)) return;
            Stop();
            var sequence = HotkeySequence.Parse(HotkeyString);
            if (!sequence.Validate(out string error))
            {
                Log.WarnFormat("Skipping hotkey '{0}' for {1}: {2}", HotkeyString, Name, error);
                return;
            }

            var hotkeys = RecipeServices.Hotkeys;
            if (hotkeys == null)
            {
                return;
            }

            hotkeys.Register(Id, Name, HotkeyString, () =>
            {
                Log.DebugFormat("Hotkey '{0}' pressed for trigger '{1}' -> recipe '{2}'", HotkeyString, Name, TargetRecipeId);
                OnTriggered();
            });
            _registered = true;
        }

        public override void Stop()
        {
            if (_registered)
            {
                RecipeServices.Hotkeys?.Unregister(Id);
                _registered = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Stop();
            }
        }
    }
}
