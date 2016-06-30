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

using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Drawing;

namespace Greenshot.Addon.Editor.Memento
{
	/// <summary>
	/// The DeleteElementMemento makes it possible to undo deleting an element
	/// </summary>
	public class DeleteElementMemento : IMemento
	{
		private ISurface _surface;
		private IDrawableContainer _drawableContainer;

		public DeleteElementMemento(ISurface surface, IDrawableContainer drawableContainer)
		{
			_surface = surface;
			_drawableContainer = drawableContainer;
		}

		public bool Merge(IMemento otherMemento)
		{
			return false;
		}

		public IMemento Restore()
		{
			// Before
			_drawableContainer.Invalidate();

			var oldState = new AddElementMemento(_surface, _drawableContainer);
			_surface.AddElement(_drawableContainer, false);
			// The container has a selected flag which represents the state at the moment it was deleted.
			if (_drawableContainer.Selected)
			{
				_surface.SelectElement(_drawableContainer);
			}

			// After
			_drawableContainer.Invalidate();
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
					if (_drawableContainer != null)
					{
						_drawableContainer = null;
					}
					_surface = null;
				}

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