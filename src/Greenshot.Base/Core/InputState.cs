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
using System.Windows.Forms;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The modifier keys and mouse buttons which are held right now, for the editor code which reacts to them while drawing
    /// (Shift keeps the aspect ratio, Ctrl scales from the center, a pressed mouse button blocks the canvas keys).
    /// In Greenshot these are those of Windows; the tests replace them, so typing or clicking on the desktop during a test run
    /// doesn't change the result.
    /// </summary>
    public static class InputState
    {
        private static Func<Keys> _modifierKeysSource = () => Control.ModifierKeys;
        private static Func<MouseButtons> _mouseButtonsSource = () => Control.MouseButtons;

        /// <summary>
        /// Where the modifier keys come from, Control.ModifierKeys unless replaced
        /// </summary>
        public static Func<Keys> ModifierKeysSource
        {
            get => _modifierKeysSource;
            set => _modifierKeysSource = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Where the mouse buttons come from, Control.MouseButtons unless replaced
        /// </summary>
        public static Func<MouseButtons> MouseButtonsSource
        {
            get => _mouseButtonsSource;
            set => _mouseButtonsSource = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// The modifier keys (Shift, Ctrl, Alt) which are held right now
        /// </summary>
        public static Keys ModifierKeys => _modifierKeysSource();

        /// <summary>
        /// The mouse buttons which are pressed right now
        /// </summary>
        public static MouseButtons MouseButtons => _mouseButtonsSource();
    }
}
