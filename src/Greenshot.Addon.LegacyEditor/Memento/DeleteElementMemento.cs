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

using System;
using Greenshot.Addon.LegacyEditor.Drawing;
using Greenshot.Addons.Interfaces.Drawing;

namespace Greenshot.Addon.LegacyEditor.Memento
{
	/// <summary>
	///     The DeleteElementMemento makes it possible to undo deleting an element
	/// </summary>
	public class DeleteElementMemento : IMemento
	{
		private readonly Surface _surface;
		private IDrawableContainer _drawableContainer;

		public DeleteElementMemento(Surface surface, IDrawableContainer drawableContainer)
		{
			_surface = surface;
			_drawableContainer = drawableContainer;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
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

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_drawableContainer != null)
				{
					_drawableContainer.Dispose();
					_drawableContainer = null;
				}
			}
		}
	}
}