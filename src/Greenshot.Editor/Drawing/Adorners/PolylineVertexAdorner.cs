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

using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Drawing.Adorners
{
    /// <summary>
    /// Moves one vertex while leaving every other vertex in place.
    /// </summary>
    public sealed class PolylineVertexAdorner : AbstractAdorner
    {
        private readonly int _index;
        private bool _isMoveMadeUndoable;
        private Point _dragOffset;

        private PolylineContainer Polyline => (PolylineContainer)Owner;

        public PolylineVertexAdorner(PolylineContainer owner, int index) : base(owner)
        {
            _index = index;
        }

        public override NativePoint Location => Polyline.GetVertexPosition(_index);

        public override bool HitTest(NativePoint point)
        {
            // Handles are painted at a fixed screen size, independently of image zoom.
            return Owner.Parent == null
                ? base.HitTest(point)
                : BoundsOnSurface.Inflate(3, 3).Contains(Owner.Parent.ToSurfaceCoordinates(point));
        }

        public override void MouseDown(object sender, MouseEventArgs mouseEventArgs)
        {
            if (mouseEventArgs.Button != MouseButtons.Left)
            {
                return;
            }

            EditStatus = EditStatus.MOVING;
            _isMoveMadeUndoable = false;
            Point vertex = Polyline.GetVertexPosition(_index);
            _dragOffset = new Point(mouseEventArgs.X - vertex.X, mouseEventArgs.Y - vertex.Y);
        }

        public override void MouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            if (!IsActive)
            {
                return;
            }

            Point point = new Point(mouseEventArgs.X - _dragOffset.X, mouseEventArgs.Y - _dragOffset.Y);
            if (point == Polyline.GetVertexPosition(_index))
            {
                return;
            }

            point = Polyline.ConstrainVertex(_index, point, (Control.ModifierKeys & Keys.Shift) != 0);
            if (point == Polyline.GetVertexPosition(_index))
            {
                return;
            }

            if (!_isMoveMadeUndoable)
            {
                Polyline.Parent?.MakeUndoable(Polyline.CreateVertexChangeMemento(), false);
                _isMoveMadeUndoable = true;
            }

            Polyline.MoveVertex(_index, point);
        }

        public override void MouseUp(object sender, MouseEventArgs mouseEventArgs)
        {
            // Include the release position even if no last MouseMove was delivered.
            MouseMove(sender, mouseEventArgs);
            base.MouseUp(sender, mouseEventArgs);
            _isMoveMadeUndoable = false;
        }
    }
}
