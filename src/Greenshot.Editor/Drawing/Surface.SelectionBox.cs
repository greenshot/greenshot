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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Drawing
{
    public sealed partial class Surface
    {
        private Point? _selectionBoxStart;
        private Point _selectionBoxEnd;
        private IDrawableContainer[] _selectionBoxInitial;
        private bool _selectionBoxActive, _selectionBoxToggle;

        internal void BeginSelectionBox(Point start, Keys modifiers)
        {
            _selectionBoxStart = start; _selectionBoxEnd = start;
            _selectionBoxInitial = selectedElements.ToArray();
            _selectionBoxToggle = (modifiers & Keys.Shift) != 0; _selectionBoxActive = false;
            HideFocusFrame(); Focus(); Capture = true;
        }

        private Rectangle SelectionBoxBounds => Rectangle.FromLTRB(Math.Min(_selectionBoxStart.Value.X, _selectionBoxEnd.X),
            Math.Min(_selectionBoxStart.Value.Y, _selectionBoxEnd.Y), Math.Max(_selectionBoxStart.Value.X, _selectionBoxEnd.X), Math.Max(_selectionBoxStart.Value.Y, _selectionBoxEnd.Y));

        private void SetBoxSelection(IEnumerable<IDrawableContainer> selection)
        {
            var next = new HashSet<IDrawableContainer>(selection.Where(_elements.Contains));
            if (next.SetEquals(selectedElements)) return;
            DeselectAllElements(); SelectElements(new DrawableContainerList(_elements.Where(next.Contains)));
        }

        private bool MoveSelectionBox(Point pointer)
        {
            if (!_selectionBoxStart.HasValue) return false;
            _selectionBoxEnd = pointer;
            double dx = pointer.X - _selectionBoxStart.Value.X, dy = pointer.Y - _selectionBoxStart.Value.Y;
            if (!_selectionBoxActive && (dx * dx + dy * dy) * Math.Pow((double)_zoomFactor, 2) < 16) return true;
            _selectionBoxActive = true;
            var bounds = SelectionBoxBounds;
            var next = _selectionBoxToggle ? new HashSet<IDrawableContainer>(_selectionBoxInitial) : new HashSet<IDrawableContainer>();
            foreach (var member in _elements.Where(e => e != _cropContainer && e.IsUndoable && !e.IsConfirmable))
            {
                Rectangle visual = member.Bounds;
                if (visual.Left < bounds.Left || visual.Right > bounds.Right || visual.Top < bounds.Top || visual.Bottom > bounds.Bottom) continue;
                if (_selectionBoxToggle && _selectionBoxInitial.Contains(member)) next.Remove(member);
                else next.Add(member);
            }
            SetBoxSelection(next); Invalidate(); return true;
        }

        private bool FinishSelectionBox(MouseEventArgs e)
        {
            if (!_selectionBoxStart.HasValue || e.Button != MouseButtons.Left) return false;
            MoveSelectionBox(e.Location);
            bool wasDrag = _selectionBoxActive;
            _selectionBoxStart = null; _selectionBoxInitial = null; _selectionBoxActive = false;
            Capture = false; Invalidate();
            // Below the drag threshold, keep upstream release-position hit testing and modifiers.
            return wasDrag;
        }

        internal bool CancelSelectionBox()
        {
            if (!_selectionBoxStart.HasValue) return false;
            var original = _selectionBoxInitial;
            _selectionBoxStart = null; _selectionBoxInitial = null; _selectionBoxActive = false;
            SetBoxSelection(original); Capture = false; Invalidate(); return true;
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture)
            {
                CancelSelectionBox();
                _mouseDown = false;
                if (_mouseDownElement != null) _mouseDownElement.Status = Greenshot.Base.Interfaces.Drawing.EditStatus.IDLE;
                _mouseDownElement = null;
            }
        }

        private void PaintSelectionBox(Graphics graphics)
        {
            if (!_selectionBoxStart.HasValue || !_selectionBoxActive) return;
            var saved = graphics.Save(); graphics.ScaleTransform(_zoomFactor, _zoomFactor);
            using var fill = new SolidBrush(Color.FromArgb(30, SystemColors.Highlight));
            using var pen = new Pen(SystemColors.Highlight, (float)(1 / (double)_zoomFactor)) { DashStyle = DashStyle.Dash };
            graphics.FillRectangle(fill, SelectionBoxBounds); graphics.DrawRectangle(pen, SelectionBoxBounds); graphics.Restore(saved);
        }
    }
}
