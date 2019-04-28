// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Drawing.Drawing2D;
using Greenshot.Addon.LegacyEditor.Drawing;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx;

namespace Greenshot.Addon.LegacyEditor.Memento
{
	/// <summary>
	///     The SurfaceCropMemento makes it possible to undo-redo an surface crop
	/// </summary>
	public sealed class SurfaceBackgroundChangeMemento : IMemento
	{
		private IBitmapWithNativeSupport _bitmap;
		private Matrix _matrix;
		private Surface _surface;

		public SurfaceBackgroundChangeMemento(Surface surface, Matrix matrix)
		{
			_surface = surface;
			_bitmap = surface.Screenshot;
			_matrix = matrix.Clone();
			// Make sure the reverse is applied
			_matrix.Invert();
		}

		public void Dispose()
		{
			Dispose(true);
		}

		public bool Merge(IMemento otherMemento)
		{
			return false;
		}

		public IMemento Restore()
		{
			var oldState = new SurfaceBackgroundChangeMemento(_surface, _matrix);
			_surface.UndoBackgroundChange(_bitmap, _matrix);
			_surface.Invalidate();
			return oldState;
		}

	    private void Dispose(bool disposing)
		{
		    if (!disposing)
		    {
		        return;
		    }
		    if (_matrix != null)
		    {
		        _matrix.Dispose();
		        _matrix = null;
		    }
		    if (_bitmap != null)
		    {
		        _bitmap.Dispose();
		        _bitmap = null;
		    }
		    _surface = null;
		}
	}
}