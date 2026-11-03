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

using System.Collections.Generic;
using System.Drawing;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Memento
{
    /// <summary>
    /// The DrawableContainerBoundsChangeMemento makes it possible to undo-redo an IDrawableContainer resize & move
    /// </summary>
    public class DrawableContainerBoundsChangeMemento : IMemento
    {
        private readonly List<Point> _points = new();
        private readonly List<Size> _sizes = new();
        private readonly List<Point[]> _vertices = new();
        private readonly List<double[]> _transforms = new();
        private readonly List<double> _groupAngles = new();
        private IDrawableContainerList _listOfDrawableContainer;
        private readonly bool _mergeable;

        private void StoreBounds()
        {
            foreach (IDrawableContainer drawableContainer in _listOfDrawableContainer)
            {
                _vertices.Add((drawableContainer as PolygonContainer)?.ModelVertices);
                _transforms.Add((drawableContainer as DrawableContainer)?.ObjectTransformState);
                _groupAngles.Add((drawableContainer as DrawableContainer)?.GroupReferenceAngle ?? 0);
                _points.Add(drawableContainer.Location);
                _sizes.Add(drawableContainer.Size);
            }
        }

        public DrawableContainerBoundsChangeMemento(IDrawableContainerList listOfDrawableContainer, bool mergeable = true)
        {
            _mergeable = mergeable;
            _listOfDrawableContainer = listOfDrawableContainer;
            StoreBounds();
        }

        public DrawableContainerBoundsChangeMemento(IDrawableContainer drawableContainer, bool mergeable = true)
        {
            _mergeable = mergeable;
            _listOfDrawableContainer = new DrawableContainerList
            {
                drawableContainer
            };
            _listOfDrawableContainer.Parent = drawableContainer.Parent;
            StoreBounds();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listOfDrawableContainer?.Dispose();
            }

            _listOfDrawableContainer = null;
        }

        public bool Merge(IMemento otherMemento)
        {
            if (otherMemento is not DrawableContainerBoundsChangeMemento other) return false;
            if (!_mergeable || !other._mergeable) return false;

            if (ObjectExtensions.CompareLists(_listOfDrawableContainer, other._listOfDrawableContainer))
            {
                // Lists are equal, as we have the state already we can ignore the new memento
                return true;
            }

            return false;
        }

        public IMemento Restore()
        {
            var oldState = new DrawableContainerBoundsChangeMemento(_listOfDrawableContainer, _mergeable);
            for (int index = 0; index < _listOfDrawableContainer.Count; index++)
            {
                IDrawableContainer drawableContainer = _listOfDrawableContainer[index];
                // Before
                drawableContainer.Invalidate();
                drawableContainer.Left = _points[index].X;
                drawableContainer.Top = _points[index].Y;
                drawableContainer.Width = _sizes[index].Width;
                drawableContainer.Height = _sizes[index].Height;
                if (drawableContainer is PolygonContainer polygon) polygon.SetVertices(_vertices[index]);
                if (drawableContainer is DrawableContainer shape) { shape.GroupReferenceAngle = _groupAngles[index]; shape.RestoreObjectTransform(_transforms[index]); }
                // After
                drawableContainer.Invalidate();
                drawableContainer.Parent.Modified = true;
            }

            return oldState;
        }
    }
}
