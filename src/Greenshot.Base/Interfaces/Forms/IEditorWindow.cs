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

namespace Greenshot.Base.Interfaces.Forms
{
    /// <summary>
    /// An editor window as a plugin sees it, without the UI toolkit of the editor: to place a window of its own next to it
    /// and to add elements to the capture which is shown. The window of <see cref="Plugin.IEditorPlugin.InitializeEditor"/> implements it.
    /// </summary>
    public interface IEditorWindow
    {
        /// <summary>
        /// The handle of the window, e.g. as owner of a plugin window, which then stays above the editor
        /// </summary>
        IntPtr Handle { get; }

        /// <summary>
        /// The capture which is shown, a window can show several captures in tabs
        /// </summary>
        ISurface CurrentSurface { get; }

        /// <summary>
        /// Bring the window to the front and give it the keyboard
        /// </summary>
        void Activate();

        /// <summary>
        /// The window became the active window
        /// </summary>
        event EventHandler Activated;

        /// <summary>
        /// The window was closed
        /// </summary>
        event EventHandler Closed;
    }
}
