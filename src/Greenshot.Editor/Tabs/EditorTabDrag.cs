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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.DesktopWindowsManager.Enums;
using Greenshot.Editor.Forms;

namespace Greenshot.Editor.Tabs
{
    /// <summary>
    /// A tab which is dragged with the mouse, like in a browser. Inside its strip it changes its place. Dragged out of the strip
    /// an image of the tab follows the mouse and the tab leaves a gap:
    /// over the tabs of another editor window these make room and the image shows the tab where it lands,
    /// anywhere else the image shows the tab with a picture of the capture, there the tab gets its own window.
    /// Nothing moves before the mouse button is released, Escape cancels. When the tab is the only one of its window, the window
    /// is invisible while the tab is outside of it.
    /// The strip keeps the mouse capture during the whole drag, so all the mouse moves arrive at the strip.
    /// </summary>
    internal sealed class EditorTabDrag
    {
        private readonly EditorTabStrip _sourceStrip;
        private readonly EditorTab _tab;
        private readonly Point _startLocation;
        private readonly int _startIndex;
        // Where the mouse is on the tab
        private readonly Point _grabOffset;
        private bool _started;
        private EditorTabDragImage _image;
        private EditorTabStrip _dropTarget;
        private int _dropIndex;
        // The window of the only tab is invisible during the drag
        private bool _sourceWindowHidden;

        public EditorTabDrag(EditorTabStrip sourceStrip, EditorTab tab, Point startLocation, Point grabOffset)
        {
            _sourceStrip = sourceStrip;
            _tab = tab;
            _startLocation = startLocation;
            _startIndex = sourceStrip.IndexOf(tab);
            _grabOffset = grabOffset;
        }

        /// <summary>
        /// The mouse moved to the location (screen coordinates)
        /// </summary>
        public void MoveTo(Point location)
        {
            if (!_started)
            {
                var dragSize = SystemInformation.DragSize;
                if (Math.Abs(location.X - _startLocation.X) < dragSize.Width && Math.Abs(location.Y - _startLocation.Y) < dragSize.Height)
                {
                    return;
                }

                _started = true;
            }

            var stripLocation = _sourceStrip.PointToClient(location);
            if (_sourceStrip.IsNear(stripLocation))
            {
                // Back in its own strip: the tab itself moves
                HideImage();
                SetDropTarget(null);
                _sourceStrip.MoveTab(_tab, _sourceStrip.GetMoveIndex(stripLocation));
                return;
            }

            _sourceStrip.DraggedTab = _tab;
            _image ??= new EditorTabDragImage(_tab);
            if (_sourceStrip.Tabs.Count == 1)
            {
                HideSourceWindow(true);
            }

            SetDropTarget(FindTabStripAt(location));
            if (_dropTarget != null)
            {
                _dropIndex = _dropTarget.GetInsertIndex(_dropTarget.PointToClient(location));
                _dropTarget.ShowDropIndex(_dropIndex);
                _image.ShowInStrip(_dropTarget, _dropTarget.DropBounds);
                return;
            }

            _image.ShowAsWindow(_sourceStrip, _sourceStrip.TabSize, new Point(location.X - _grabOffset.X, location.Y - _grabOffset.Y));
        }

        /// <summary>
        /// The mouse button was released at the location (screen coordinates)
        /// </summary>
        public void Finish(Point location)
        {
            bool outside = _image is { Visible: true };
            var dropTarget = _dropTarget;
            End();
            if (!outside)
            {
                return;
            }

            var sourceWindow = _sourceStrip.Window;
            if (dropTarget != null)
            {
                var targetWindow = dropTarget.Window;
                sourceWindow.MoveTab(_tab, targetWindow, _dropIndex);
                // The window of the only tab closed, else it is shown again
                HideSourceWindow(false);
                targetWindow.Activate();
                return;
            }

            // The tab of the new window is where the image of the tab was
            var stripOrigin = _sourceStrip.PointToScreen(Point.Empty);
            var windowLocation = new Point(location.X - _grabOffset.X - _sourceStrip.FirstTabLeft - (stripOrigin.X - sourceWindow.Left),
                location.Y - _grabOffset.Y - (stripOrigin.Y - sourceWindow.Top));
            if (sourceWindow.Tabs.Count == 1)
            {
                // The only tab: the window itself moves there
                if (sourceWindow.WindowState == FormWindowState.Maximized)
                {
                    sourceWindow.WindowState = FormWindowState.Normal;
                }

                sourceWindow.Location = windowLocation;
                HideSourceWindow(false);
                sourceWindow.Activate();
                return;
            }

            sourceWindow.DetachTab(_tab, windowLocation).Activate();
        }

        /// <summary>
        /// The drag was cancelled, the tab goes back to where it was
        /// </summary>
        public void Cancel()
        {
            End();
            HideSourceWindow(false);
            _sourceStrip.MoveTab(_tab, _startIndex);
        }

        private void End()
        {
            SetDropTarget(null);
            _sourceStrip.DraggedTab = null;
            _image?.Dispose();
            _image = null;
        }

        private void HideImage()
        {
            _sourceStrip.DraggedTab = null;
            _image?.Hide();
            HideSourceWindow(false);
        }

        /// <summary>
        /// Make the window of the strip invisible, or visible again. The desktop window manager hides (cloaks) it, for Windows
        /// the window stays visible: it keeps the mouse capture and the drag goes on.
        /// </summary>
        private void HideSourceWindow(bool hide)
        {
            var sourceWindow = _sourceStrip.Window;
            if (_sourceWindowHidden == hide || sourceWindow == null || sourceWindow.IsDisposed)
            {
                return;
            }

            _sourceWindowHidden = hide;
            uint cloak = hide ? 1u : 0u;
            DwmApi.DwmSetWindowAttribute(sourceWindow.Handle, DwmWindowAttributes.Cloak, ref cloak, sizeof(uint));
        }

        private void SetDropTarget(EditorTabStrip target)
        {
            if (target == _dropTarget)
            {
                return;
            }

            _dropTarget?.ShowDropIndex(-1);
            _dropTarget = target;
        }

        /// <summary>
        /// The tab strip of another editor window at the location (or close to it), which isn't covered by another window
        /// </summary>
        private EditorTabStrip FindTabStripAt(Point location)
        {
            var sourceWindow = _sourceStrip.Window;
            var candidates = ImageEditorForm.OpenWindows
                .Where(window => window != sourceWindow && !window.IsClosing && window.Visible && window.WindowState != FormWindowState.Minimized
                                 && window.TabStrip.IsNear(window.TabStrip.PointToClient(location)))
                .ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            // The first window from the top which contains the location is the one the user sees there
            var point = new NativePoint(location.X, location.Y);
            foreach (var window in InteropWindowQuery.GetTopWindows())
            {
                bool hidden = window.Handle == _image?.Handle || (_sourceWindowHidden && window.Handle == sourceWindow.Handle);
                if (hidden || !window.IsVisible() || !window.GetInfo().Bounds.Contains(point))
                {
                    continue;
                }

                return candidates.FirstOrDefault(candidate => candidate.Handle == window.Handle)?.TabStrip;
            }

            return null;
        }
    }
}
