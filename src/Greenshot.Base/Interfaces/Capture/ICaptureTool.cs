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
using CaptureMode = Greenshot.Base.Interfaces.CaptureMode;

namespace Greenshot.Base.Interfaces.Capture
{
    /// <summary>
    /// A way to select something on the frozen capture, e.g. a region, a window or text.
    /// The CaptureWindow takes care of everything the tools share (cursor, crosshair, zoomer, hotspots, keys to move the cursor and to switch tools),
    /// and passes the input to the active tool. Derive from CaptureTool, which has empty implementations.
    /// </summary>
    public interface ICaptureTool
    {
        /// <summary>
        /// The capture mode which is reported when this tool made the selection
        /// </summary>
        CaptureMode Mode { get; }

        /// <summary>
        /// The key which switches to this tool, Key.None for none (space toggles between region and window)
        /// </summary>
        Key ShortcutKey { get; }

        /// <summary>
        /// True to show the zoomer (when it is enabled), read when the tool becomes active
        /// </summary>
        bool ShowsZoomer { get; }

        /// <summary>
        /// True to show the crosshair through the cursor, read after every mouse move
        /// </summary>
        bool ShowsCrosshair { get; }

        /// <summary>
        /// The tool becomes active
        /// </summary>
        void Activate(ICaptureToolHost host);

        /// <summary>
        /// Another tool becomes active
        /// </summary>
        void Deactivate();

        /// <summary>
        /// The cursor moved, ICaptureToolHost.CursorPosition is where it is now
        /// </summary>
        void OnMouseMove();

        /// <summary>
        /// The left mouse button was pressed (not on a hotspot)
        /// </summary>
        void OnMouseDown();

        /// <summary>
        /// The left mouse button was released
        /// </summary>
        void OnMouseUp();

        /// <summary>
        /// A key was pressed, before the CaptureWindow handles it
        /// </summary>
        /// <returns>true when the tool handled the key</returns>
        bool OnKeyDown(Key key);

        /// <summary>
        /// Draw on the layer of the tool, which is below the selection. Called by ICaptureToolHost.Redraw and when the detected features changed.
        /// </summary>
        void Draw(DrawingContext drawingContext);
    }

    /// <summary>
    /// Base for capture tools, with empty implementations
    /// </summary>
    public abstract class CaptureTool : ICaptureTool
    {
        /// <summary>
        /// The CaptureWindow, set when the tool is activated
        /// </summary>
        protected ICaptureToolHost Host { get; private set; }

        public abstract CaptureMode Mode { get; }

        public virtual Key ShortcutKey => Key.None;

        public virtual bool ShowsZoomer => true;

        public virtual bool ShowsCrosshair => false;

        public virtual void Activate(ICaptureToolHost host)
        {
            Host = host;
        }

        public virtual void Deactivate()
        {
        }

        public virtual void OnMouseMove()
        {
        }

        public virtual void OnMouseDown()
        {
        }

        public virtual void OnMouseUp()
        {
        }

        public virtual bool OnKeyDown(Key key) => false;

        public virtual void Draw(DrawingContext drawingContext)
        {
        }
    }
}
