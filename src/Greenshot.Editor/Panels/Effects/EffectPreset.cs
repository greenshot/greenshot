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

using System;

namespace Greenshot.Editor.Panels.Effects
{
    /// <summary>
    /// A named set of settings of an effect: picking it sets the fields of the panel, which can be changed afterwards
    /// </summary>
    public sealed class EffectPreset
    {
        private readonly Action<EffectsPanelViewModel> _apply;

        public EffectPreset(string name, Action<EffectsPanelViewModel> apply)
        {
            Name = name;
            _apply = apply;
        }

        /// <summary>
        /// The translated name, shown on the button
        /// </summary>
        public string Name { get; }

        internal void ApplyTo(EffectsPanelViewModel viewModel) => _apply(viewModel);
    }
}
