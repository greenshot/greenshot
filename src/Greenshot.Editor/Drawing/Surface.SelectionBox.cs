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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Drawing
{
    public sealed partial class Surface
    {
        private Point? _selectionBoxStart;
        private Point _selectionBoxEnd;
        private IDrawableContainer[] _selectionBoxInitial;
        private bool _selectionBoxActive;
        private bool _selectionBoxToggle;

        internal void BeginSelectionBox(Point start, Keys modifiers)
        {
            _selectionBoxStart = start;
            _selectionBoxEnd = start;
            _selectionBoxInitial = selectedElements.ToArray();
            _selectionBoxToggle = (modifiers & Keys.Shift) != 0;
            _selectionBoxActive = false;
            HideFocusFrame();
            Focus();
            Capture = true;
        }

        private Rectangle SelectionBoxBounds => Rectangle.FromLTRB(
            Math.Min(_selectionBoxStart.Value.X, _selectionBoxEnd.X),
            Math.Min(_selectionBoxStart.Value.Y, _selectionBoxEnd.Y),
            Math.Max(_selectionBoxStart.Value.X, _selectionBoxEnd.X),
            Math.Max(_selectionBoxStart.Value.Y, _selectionBoxEnd.Y));

        private float SelectionBoxLineWidth => Math.Max(1f, CurrentDpi / 96f);

        private Rectangle SelectionBoxPaintBounds
        {
            get
            {
                if (!_selectionBoxStart.HasValue || !_selectionBoxActive)
                {
                    return Rectangle.Empty;
                }

                Rectangle imageBounds = SelectionBoxBounds;
                double zoom = (double)_zoomFactor;
                var surfaceBounds = Rectangle.FromLTRB(
                    (int)Math.Floor(imageBounds.Left * zoom),
                    (int)Math.Floor(imageBounds.Top * zoom),
                    (int)Math.Ceiling(imageBounds.Right * zoom),
                    (int)Math.Ceiling(imageBounds.Bottom * zoom));
                int outlineMargin = (int)Math.Ceiling(SelectionBoxLineWidth / 2) + 1;
                surfaceBounds.Inflate(outlineMargin, outlineMargin);
                return surfaceBounds;
            }
        }

        private void InvalidateSelectionBox(Rectangle surfaceBounds)
        {
            if (!surfaceBounds.IsEmpty)
            {
                // Coordinates belong to this control; scrolling moves the control, not its paint origin.
                Invalidate(surfaceBounds);
            }
        }

        private void SetBoxSelection(IEnumerable<IDrawableContainer> selection)
        {
            var requestedSelection = new HashSet<IDrawableContainer>(selection.Where(_elements.Contains));
            var removedElements = selectedElements.Where(element => !requestedSelection.Contains(element)).ToArray();
            var addedElements = _elements.Where(element => requestedSelection.Contains(element) && !selectedElements.Contains(element)).ToArray();
            if (removedElements.Length == 0 && addedElements.Length == 0)
            {
                return;
            }

            foreach (var element in removedElements)
            {
                element.Invalidate();
                DeselectElement(element, generateEvents: false);
            }

            foreach (var element in addedElements)
            {
                SelectElement(element, invalidate: true, generateEvents: false);
            }

            _movingElementChanged?.Invoke(this, new SurfaceElementEventArgs { Elements = selectedElements });
        }

        private bool MoveSelectionBox(Point pointer)
        {
            if (!_selectionBoxStart.HasValue)
            {
                return false;
            }

            if (_selectionBoxActive && pointer == _selectionBoxEnd)
            {
                return true;
            }

            Rectangle previousPaintBounds = SelectionBoxPaintBounds;
            _selectionBoxEnd = pointer;
            double horizontalDistance = pointer.X - _selectionBoxStart.Value.X;
            double verticalDistance = pointer.Y - _selectionBoxStart.Value.Y;
            double screenDistanceSquared = (horizontalDistance * horizontalDistance + verticalDistance * verticalDistance)
                * Math.Pow((double)_zoomFactor, 2);
            if (!_selectionBoxActive && screenDistanceSquared < 16)
            {
                return true;
            }

            _selectionBoxActive = true;
            Rectangle selectionBounds = SelectionBoxBounds;
            var nextSelection = _selectionBoxToggle
                ? new HashSet<IDrawableContainer>(_selectionBoxInitial)
                : new HashSet<IDrawableContainer>();
            foreach (var element in _elements.Where(element => element != _cropContainer && element.IsUndoable && !element.IsConfirmable))
            {
                Rectangle elementBounds = element.Bounds;
                if (elementBounds.Left < selectionBounds.Left || elementBounds.Right > selectionBounds.Right
                    || elementBounds.Top < selectionBounds.Top || elementBounds.Bottom > selectionBounds.Bottom)
                {
                    continue;
                }

                if (_selectionBoxToggle && _selectionBoxInitial.Contains(element))
                {
                    nextSelection.Remove(element);
                }
                else
                {
                    nextSelection.Add(element);
                }
            }

            SetBoxSelection(nextSelection);
            InvalidateSelectionBox(previousPaintBounds);
            InvalidateSelectionBox(SelectionBoxPaintBounds);
            return true;
        }

        private void EndSelectionBox()
        {
            Rectangle previousPaintBounds = SelectionBoxPaintBounds;
            _selectionBoxStart = null;
            _selectionBoxInitial = null;
            _selectionBoxActive = false;
            Capture = false;
            InvalidateSelectionBox(previousPaintBounds);
        }

        private bool FinishSelectionBox(MouseEventArgs mouseEvent)
        {
            if (!_selectionBoxStart.HasValue || mouseEvent.Button != MouseButtons.Left)
            {
                return false;
            }

            MoveSelectionBox(mouseEvent.Location);
            bool wasDrag = _selectionBoxActive;
            EndSelectionBox();
            return wasDrag;
        }

        internal bool CancelSelectionBox()
        {
            if (!_selectionBoxStart.HasValue)
            {
                return false;
            }

            SetBoxSelection(_selectionBoxInitial);
            EndSelectionBox();
            return true;
        }

        protected override void OnMouseCaptureChanged(EventArgs eventArgs)
        {
            base.OnMouseCaptureChanged(eventArgs);
            if (!Capture)
            {
                CancelSelectionBox();
            }
        }

        private void PaintSelectionBox(Graphics graphics)
        {
            if (!_selectionBoxStart.HasValue || !_selectionBoxActive)
            {
                return;
            }

            var graphicsState = graphics.Save();
            graphics.ScaleTransform(_zoomFactor, _zoomFactor);
            using var fill = new SolidBrush(Color.FromArgb(30, SystemColors.Highlight));
            using var outline = new Pen(SystemColors.Highlight, SelectionBoxLineWidth / (float)_zoomFactor)
            {
                DashStyle = DashStyle.Dash
            };
            graphics.FillRectangle(fill, SelectionBoxBounds);
            graphics.DrawRectangle(outline, SelectionBoxBounds);
            graphics.Restore(graphicsState);
        }
    }
}