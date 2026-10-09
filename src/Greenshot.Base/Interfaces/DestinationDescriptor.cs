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

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// How a destination is presented, in UI neutral types. Menus are built from this by the UI layer.
    /// </summary>
    public sealed class DestinationDescriptor
    {
        /// <param name="displayName">Name shown in menus, settings, the picker</param>
        /// <param name="priority">Sort order, lower first</param>
        /// <param name="iconKey">Key of the icon, resolved by an IIconProvider (e.g. "resource:Clipboard.Image"), null for none</param>
        /// <param name="shortcut">Editor shortcut like "Ctrl+Shift+C", null for none</param>
        /// <param name="hasDynamicDestinations">True when <see cref="IDestination.GetDynamicDestinationsAsync"/> can return sub destinations</param>
        /// <param name="useDynamicsOnly">True when the destination itself can't be used, only its dynamic destinations</param>
        /// <param name="isLinkable">True when the destination returns a link</param>
        public DestinationDescriptor(string displayName, int priority = 10, string iconKey = null, string shortcut = null,
            bool hasDynamicDestinations = false, bool useDynamicsOnly = false, bool isLinkable = false)
        {
            DisplayName = displayName;
            Priority = priority;
            IconKey = iconKey;
            Shortcut = shortcut;
            HasDynamicDestinations = hasDynamicDestinations;
            UseDynamicsOnly = useDynamicsOnly;
            IsLinkable = isLinkable;
        }

        public string DisplayName { get; }

        public int Priority { get; }

        public string IconKey { get; }

        public string Shortcut { get; }

        public bool HasDynamicDestinations { get; }

        public bool UseDynamicsOnly { get; }

        public bool IsLinkable { get; }

        public override string ToString() => DisplayName;
    }
}
