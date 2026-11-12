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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Editor;
using Greenshot.Base.Interfaces.Forms;

namespace Greenshot.Editor.Panels
{
    /// <summary>
    /// The context of the panels of one editor window. The window tells it which capture is shown, the context forwards the events
    /// of that capture, so the panels never rebind on a tab switch.
    /// </summary>
    internal sealed class EditorPanelContext : IEditorPanelContext
    {
        private readonly Action<string, bool> _showPanel;
        private readonly Action _focusCanvas;
        private ISurface _surface;

        public EditorPanelContext(IEditorWindow window, Action<string, bool> showPanel, Action focusCanvas)
        {
            Window = window;
            _showPanel = showPanel;
            _focusCanvas = focusCanvas;
        }

        /// <inheritdoc />
        public IEditorWindow Window { get; }

        /// <inheritdoc />
        public ISurface CurrentSurface => _surface;

        /// <inheritdoc />
        public event EventHandler CurrentSurfaceChanged;

        /// <inheritdoc />
        public event EventHandler SelectionChanged;

        /// <inheritdoc />
        public event EventHandler DrawingModeChanged;

        /// <inheritdoc />
        public event EventHandler ElementsChanged;

        /// <summary>
        /// The window shows another capture, or none (null)
        /// </summary>
        public void SetSurface(ISurface surface)
        {
            if (ReferenceEquals(_surface, surface))
            {
                return;
            }

            _surface = surface;
            CurrentSurfaceChanged?.Invoke(this, EventArgs.Empty);
        }

        public void OnSelectionChanged() => SelectionChanged?.Invoke(this, EventArgs.Empty);

        public void OnDrawingModeChanged() => DrawingModeChanged?.Invoke(this, EventArgs.Empty);

        public void OnElementsChanged() => ElementsChanged?.Invoke(this, EventArgs.Empty);

        /// <inheritdoc />
        public void ShowPanel(string panelId, bool activate) => _showPanel?.Invoke(panelId, activate);

        /// <inheritdoc />
        public void FocusCanvas() => _focusCanvas?.Invoke();
    }
}
