/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Drawing;
using System.Drawing.Drawing2D;
using Greenshot.Addon.Interfaces;

namespace Greenshot.Addon.Editor.Memento
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
			_matrix = matrix.Clone();
			// Make sure the reverse is applied
			_matrix.Invert();
		}

		public bool Merge(IMemento otherMemento)
		{
			return false;
		}

		public string ActionDescription
		{
			get
			{
				//return LangKey.editor_crop;
				return "";
			}
		}

		public IMemento Restore()
		{
			var oldState = new SurfaceBackgroundChangeMemento(_surface, _matrix);
			_surface.UndoBackgroundChange(_image, _matrix);
			_surface.Invalidate();
			return oldState;
		}

		#region IDisposable Support

		private bool _disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects).
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
				}
				_surface = null;
				_disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}

		#endregion
	}
}