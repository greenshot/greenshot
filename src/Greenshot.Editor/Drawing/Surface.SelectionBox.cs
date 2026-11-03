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
            ClearSnapGuide(); HideFocusFrame(); Focus(); Capture = true;
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
            var seen = new HashSet<Guid>();
            foreach (var member in _elements.OfType<DrawableContainer>().Where(e => e != _cropContainer && e.IsUndoable && !e.IsConfirmable))
            {
                if (member.GroupId != Guid.Empty && !seen.Add(member.GroupId)) continue;
                var unit = GroupMembers(member).ToArray();
                var visual = unit.Select(e => (Rectangle)e.Bounds).Aggregate(Rectangle.Union);
                if (visual.Left < bounds.Left || visual.Right > bounds.Right || visual.Top < bounds.Top || visual.Bottom > bounds.Bottom) continue;
                if (_selectionBoxToggle && _selectionBoxInitial.Contains(member)) foreach (var element in unit) next.Remove(element);
                else foreach (var element in unit) next.Add(element);
            }
            SetBoxSelection(next); Invalidate(); return true;
        }

        private bool FinishSelectionBox(MouseEventArgs e)
        {
            if (!_selectionBoxStart.HasValue || e.Button != MouseButtons.Left) return false;
            MoveSelectionBox(e.Location);
            if (!_selectionBoxActive && !_selectionBoxToggle) SetBoxSelection(Array.Empty<IDrawableContainer>());
            _selectionBoxStart = null; _selectionBoxInitial = null; _selectionBoxActive = false;
            Capture = false; Invalidate(); return true;
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
            if (!Capture) CancelSelectionBox();
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
