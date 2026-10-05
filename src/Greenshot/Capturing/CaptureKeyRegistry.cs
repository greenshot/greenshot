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
using System.Windows.Input;
using Greenshot.Base.Interfaces.Capture;

namespace Greenshot.Capturing
{
    /// <summary>
    /// The keys of the CaptureWindow, registered by the window, the tools and the overlays (in that order, so the built-in keys win).
    /// It is also the dispatch table: a key does what its binding says, a key nobody registered does nothing.
    /// </summary>
    public class CaptureKeyRegistry
    {
        private readonly List<CaptureKeyBinding> _bindings = new List<CaptureKeyBinding>();

        /// <summary>
        /// All registered keys, in the order of registration
        /// </summary>
        public IReadOnlyList<CaptureKeyBinding> Bindings => _bindings;

        /// <summary>
        /// Add a binding
        /// </summary>
        /// <exception cref="CaptureKeyConflictException">The key is already used, see CaptureKeyBinding.ConflictsWith</exception>
        /// <exception cref="ArgumentException">Ctrl, Alt or Windows alone, which only work together with another key</exception>
        public CaptureKeyBinding Register(CaptureKeyBinding binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }
            if (IsModifierOnly(binding.Key))
            {
                throw new ArgumentException($"{binding.Key} can only be used together with another key", nameof(binding));
            }
            var existing = _bindings.FirstOrDefault(other => other.ConflictsWith(binding));
            if (existing != null)
            {
                throw new CaptureKeyConflictException(binding, existing);
            }
            _bindings.Add(binding);
            return binding;
        }

        /// <summary>
        /// The binding for a pressed key while the given tool is active, null when there is none.
        /// When Shift is held and nothing has the key with Shift, the key without Shift is used: Shift keeps the selection
        /// to one direction, which should also work with the arrow keys.
        /// </summary>
        public CaptureKeyBinding Find(Key key, ModifierKeys modifiers, ICaptureTool activeTool)
        {
            if (key == Key.LeftShift || key == Key.RightShift)
            {
                // Pressing Shift itself reports Shift as a modifier
                modifiers &= ~ModifierKeys.Shift;
            }
            var binding = FindExact(key, modifiers, activeTool);
            if (binding == null && (modifiers & ModifierKeys.Shift) != 0)
            {
                binding = FindExact(key, modifiers & ~ModifierKeys.Shift, activeTool);
            }
            return binding;
        }

        private CaptureKeyBinding FindExact(Key key, ModifierKeys modifiers, ICaptureTool activeTool) =>
            // A tool key before an always active key, there can't be both (conflict) but this keeps the intent clear
            _bindings.FirstOrDefault(binding => binding.Tool != null && binding.Tool == activeTool && binding.Key == key && binding.Modifiers == modifiers)
            ?? _bindings.FirstOrDefault(binding => binding.Tool == null && binding.Key == key && binding.Modifiers == modifiers);

        private static bool IsModifierOnly(Key key) =>
            key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin or Key.System;
    }
}
