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
using Greenshot.Editor.Forms;

namespace Greenshot.Editor.Tabs
{
    /// <summary>
    /// A tab which is dragged with the mouse, like in a browser. Inside its strip it changes its place. Dragged out of the strip
    /// the tab gets its own window (a window with only this tab is moved itself), which follows the mouse. Released over the tabs
    /// of another editor window, the tab moves into that window.
    /// The strip keeps the mouse capture during the whole drag, so all the mouse moves arrive at the strip.
    /// </summary>
    internal sealed class EditorTabDrag
    {
        private readonly EditorTabStrip _sourceStrip;
        private readonly EditorTab _tab;
        private readonly Point _startLocation;
        // Where the mouse is on the tab
        private readonly Point _grabOffset;
        private bool _started;
        private ImageEditorForm _movingWindow;
        // Where the mouse is on the moving window
        private Point _windowOffset;
        private EditorTabStrip _dropTarget;

        public EditorTabDrag(EditorTabStrip sourceStrip, EditorTab tab, Point startLocation, Point grabOffset)
        {
            _sourceStrip = sourceStrip;
            _tab = tab;
            _startLocation = startLocation;
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

            if (_movingWindow == null)
            {
                var stripLocation = _sourceStrip.PointToClient(location);
                if (_sourceStrip.IsNear(stripLocation))
                {
                    _sourceStrip.MoveTab(_tab, _sourceStrip.GetMoveIndex(stripLocation));
                    return;
                }

                StartMovingWindow(location);
            }

            _movingWindow.Location = new Point(location.X - _windowOffset.X, location.Y - _windowOffset.Y);
            UpdateDropTarget(location);
        }

        /// <summary>
        /// The mouse button was released at the location (screen coordinates)
        /// </summary>
        public void Finish(Point location)
        {
            if (_movingWindow == null)
            {
                return;
            }

            if (_dropTarget != null)
            {
                _dropTarget.ShowDropIndex(-1);
                var targetWindow = _dropTarget.Window;
                _movingWindow.MoveTab(_tab, targetWindow, _dropTarget.GetInsertIndex(_dropTarget.PointToClient(location)));
                targetWindow.Activate();
                return;
            }

            _movingWindow.Activate();
        }

        /// <summary>
        /// The tab leaves its strip: move its window when it is the only tab, else move the tab into a new window
        /// </summary>
        private void StartMovingWindow(Point location)
        {
            var sourceWindow = _sourceStrip.Window;
            bool onlyTab = sourceWindow.Tabs.Count == 1;
            if (onlyTab && sourceWindow.WindowState == FormWindowState.Maximized)
            {
                // The window itself is moved, it gets its normal size again
                sourceWindow.WindowState = FormWindowState.Normal;
            }

            // The mouse stays on the tab, which is the first tab of the moving window (a new window has the same layout)
            var stripOrigin = _sourceStrip.PointToScreen(Point.Empty);
            _windowOffset = new Point(stripOrigin.X - sourceWindow.Left + _sourceStrip.FirstTabLeft + _grabOffset.X, stripOrigin.Y - sourceWindow.Top + _grabOffset.Y);
            var windowLocation = new Point(location.X - _windowOffset.X, location.Y - _windowOffset.Y);

            _movingWindow = onlyTab ? sourceWindow : sourceWindow.DetachTab(_tab, windowLocation);
        }

        /// <summary>
        /// Mark where the tab would be inserted, when the mouse is over the tabs of another editor window
        /// </summary>
        private void UpdateDropTarget(Point location)
        {
            var target = FindTabStripAt(location);
            if (target != _dropTarget)
            {
                _dropTarget?.ShowDropIndex(-1);
                _dropTarget = target;
            }

            _dropTarget?.ShowDropIndex(_dropTarget.GetInsertIndex(_dropTarget.PointToClient(location)));
        }

        /// <summary>
        /// The tab strip of another editor window at the location, which isn't covered by another window
        /// </summary>
        private EditorTabStrip FindTabStripAt(Point location)
        {
            var candidates = ImageEditorForm.OpenWindows
                .Where(window => window != _movingWindow && window.Visible && window.WindowState != FormWindowState.Minimized
                                 && window.TabStrip.RectangleToScreen(window.TabStrip.ClientRectangle).Contains(location))
                .ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            // The first window from the top which contains the location is the one the user sees there
            var point = new NativePoint(location.X, location.Y);
            foreach (var window in InteropWindowQuery.GetTopWindows())
            {
                if (window.Handle == _movingWindow.Handle || !window.IsVisible() || !window.GetInfo().Bounds.Contains(point))
                {
                    continue;
                }

                return candidates.FirstOrDefault(candidate => candidate.Handle == window.Handle)?.TabStrip;
            }

            return null;
        }
    }
}
