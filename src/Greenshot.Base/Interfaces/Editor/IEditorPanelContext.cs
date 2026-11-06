/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using Greenshot.Base.Interfaces.Forms;

namespace Greenshot.Base.Interfaces.Editor
{
    /// <summary>
    /// What a panel or a section next to the canvas knows of its editor window: the capture which is shown, the selection, and
    /// how to show a panel. One context per editor window. The events follow the capture which is shown, a panel never has to
    /// rebind when the user switches tabs. All members are used on the UI thread.
    /// </summary>
    public interface IEditorPanelContext
    {
        /// <summary>
        /// The editor window of the panel
        /// </summary>
        IEditorWindow Window { get; }

        /// <summary>
        /// The capture which is shown, null while the window switches tabs or closes
        /// </summary>
        ISurface CurrentSurface { get; }

        /// <summary>
        /// Another capture is shown: a tab was selected, added or closed
        /// </summary>
        event EventHandler CurrentSurfaceChanged;

        /// <summary>
        /// Elements of the shown capture were selected or deselected
        /// </summary>
        event EventHandler SelectionChanged;

        /// <summary>
        /// The tool of the shown capture changed, e.g. the Emoji tool was picked
        /// </summary>
        event EventHandler DrawingModeChanged;

        /// <summary>
        /// Elements of the shown capture were changed on the canvas: moved or resized with the mouse or the keys, undo, redo
        /// </summary>
        event EventHandler ElementsChanged;

        /// <summary>
        /// Show a panel
        /// </summary>
        /// <param name="panelId">The <see cref="IEditorPanel.Id"/></param>
        /// <param name="activate">true moves the keyboard into the panel, false shows it without taking the focus from the canvas</param>
        void ShowPanel(string panelId, bool activate);

        /// <summary>
        /// Give the keyboard back to the canvas, e.g. after a pick in a panel
        /// </summary>
        void FocusCanvas();
    }
}
