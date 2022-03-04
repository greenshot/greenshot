/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Drawing;
using System.Drawing.Drawing2D;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Memento
{
    /// <summary>
    /// The SurfaceCropMemento makes it possible to undo-redo an surface crop
    /// </summary>
    public class SurfaceBackgroundChangeMemento : IMemento
    {
        private Image _image;
        private ISurface _surface;
        private Matrix _matrix;

        public SurfaceBackgroundChangeMemento(ISurface surface, Matrix matrix)
        {
            _surface = surface;
            _image = surface.Image;
            if (matrix != null)
            {
                _matrix = matrix.Clone();
                // Make sure the reverse is applied
                _matrix.Invert();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (_matrix != null)
            {
                _matrix.Dispose();
                _matrix = null;
            }

            if (_image != null)
            {
                _image.Dispose();
                _image = null;
            }

            _surface = null;
        }

        public bool Merge(IMemento otherMemento)
        {
            return false;
        }

        public IMemento Restore()
        {
            SurfaceBackgroundChangeMemento oldState = new SurfaceBackgroundChangeMemento(_surface, _matrix);
            _surface.UndoBackgroundChange(_image, _matrix);
            _surface.Invalidate();
            return oldState;
        }
    }
}