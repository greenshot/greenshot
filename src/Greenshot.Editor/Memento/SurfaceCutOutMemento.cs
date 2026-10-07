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

using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Memento
{
    /// <summary>
    /// Makes it possible to undo / redo a crop out (horizontally or vertically).
    /// Unlike a normal crop only the elements after the cut move, the elements inside the cut are removed and a cut mark can be added.
    /// </summary>
    public class SurfaceCutOutMemento : IMemento
    {
        private Surface _surface;
        private Image _image;
        private IList<IDrawableContainer> _movedElements;
        private readonly NativePoint _offset;
        private IDrawableContainerList _elementsToAdd;
        private IDrawableContainerList _elementsToRemove;
        private IList<TornEdgeLayout> _edgeLayouts;

        /// <summary>
        /// Which sides of torn edges have an edge, and the room outside of them
        /// </summary>
        public sealed class TornEdgeLayout
        {
            public TornEdgeLayout(TornEdgeContainer container)
                : this(container, container.Edges, container.Margins)
            {
            }

            public TornEdgeLayout(TornEdgeContainer container, bool[] edges, int[] margins)
            {
                Container = container;
                Edges = edges;
                Margins = margins;
            }

            public TornEdgeContainer Container { get; }
            public bool[] Edges { get; }
            public int[] Margins { get; }
        }

        /// <summary>
        /// Create the memento
        /// </summary>
        /// <param name="surface">Surface</param>
        /// <param name="image">Image to restore</param>
        /// <param name="movedElements">the elements which need to be moved by offset</param>
        /// <param name="offset">NativePoint how far to move the elements</param>
        /// <param name="elementsToAdd">elements to add, owned by this memento until restored</param>
        /// <param name="elementsToRemove">elements to remove, these are on the surface</param>
        /// <param name="edgeLayouts">torn edges to change back, e.g. after a crop cut sides off</param>
        public SurfaceCutOutMemento(Surface surface, Image image, IList<IDrawableContainer> movedElements, NativePoint offset, IDrawableContainerList elementsToAdd, IDrawableContainerList elementsToRemove,
            IList<TornEdgeLayout> edgeLayouts = null)
        {
            _edgeLayouts = edgeLayouts;
            _surface = surface;
            _image = image;
            _movedElements = movedElements;
            _offset = offset;
            _elementsToAdd = elementsToAdd;
            _elementsToRemove = elementsToRemove;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;

            // Not restored: the image and the elements to add are not used anywhere else
            _image?.Dispose();
            _image = null;
            _elementsToAdd?.Dispose();
            _elementsToAdd = null;
            _elementsToRemove = null;
            _movedElements = null;
            _edgeLayouts = null;
            _surface = null;
        }

        public bool Merge(IMemento otherMemento)
        {
            return false;
        }

        public IMemento Restore()
        {
            var currentLayouts = _edgeLayouts?.Select(layout => new TornEdgeLayout(layout.Container)).ToList();
            var oldState = new SurfaceCutOutMemento(_surface, _surface.Image, _movedElements, new NativePoint(-_offset.X, -_offset.Y), _elementsToRemove, _elementsToAdd, currentLayouts);
            if (_edgeLayouts != null)
            {
                foreach (var layout in _edgeLayouts)
                {
                    layout.Container.SetLayout(layout.Edges, layout.Margins);
                }
            }

            _surface.ApplyCutOutState(_image, _movedElements, _offset, _elementsToAdd, _elementsToRemove);
            // The surface owns these now
            _image = null;
            _elementsToAdd = null;
            return oldState;
        }
    }
}
