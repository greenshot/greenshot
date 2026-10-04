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

using System.Windows.Input;
using System.Windows.Media;

namespace Greenshot.Base.Interfaces.Capture
{
    /// <summary>
    /// Runs next to the active capture tool for as long as the CaptureWindow is open, e.g. a color picker which shows the color
    /// under the cursor while a region is selected. An overlay gets the mouse moves, the keys nobody else used, and draws on its
    /// own layer above the tool's layer. It never gets mouse clicks, those belong to the active tool.
    /// Derive from CaptureOverlay, which has empty implementations.
    /// </summary>
    public interface ICaptureOverlay
    {
        /// <summary>
        /// The CaptureWindow opened
        /// </summary>
        void Attach(ICaptureToolHost host);

        /// <summary>
        /// The cursor moved, after the active tool handled the move
        /// </summary>
        void OnMouseMove();

        /// <summary>
        /// The active tool changed, see ICaptureToolHost.ActiveTool
        /// </summary>
        void OnToolChanged();

        /// <summary>
        /// A key which neither the active tool nor the CaptureWindow used
        /// </summary>
        /// <returns>true when the overlay handled the key, the later overlays don't get it</returns>
        bool OnKeyDown(Key key);

        /// <summary>
        /// Draw on the layer of the overlay. Called by ICaptureToolHost.Redraw(overlay) and when the detected features changed.
        /// </summary>
        void Draw(DrawingContext drawingContext);
    }

    /// <summary>
    /// Base for capture overlays, with empty implementations
    /// </summary>
    public abstract class CaptureOverlay : ICaptureOverlay
    {
        /// <summary>
        /// The CaptureWindow, set when the window opened
        /// </summary>
        protected ICaptureToolHost Host { get; private set; }

        public virtual void Attach(ICaptureToolHost host)
        {
            Host = host;
        }

        public virtual void OnMouseMove()
        {
        }

        public virtual void OnToolChanged()
        {
        }

        public virtual bool OnKeyDown(Key key) => false;

        public virtual void Draw(DrawingContext drawingContext)
        {
        }
    }
}
