//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.Collections.Generic;
using System.Drawing;
using Greenshot.Addon.Editor.Drawing;
using Greenshot.Addon.Editor.Helpers;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Drawing;

#endregion

namespace Greenshot.Addon.Editor.Memento
{
	/// <summary>
	///     The DrawableContainerBoundsChangeMemento makes it possible to undo-redo an IDrawableContainer resize & move
	/// </summary>
	public class DrawableContainerBoundsChangeMemento : IMemento
	{
		private readonly IList<Point> _points = new List<Point>();
		private readonly IList<Size> _sizes = new List<Size>();
		private IDrawableContainerList _listOfdrawableContainer;

		public DrawableContainerBoundsChangeMemento(IDrawableContainerList listOfdrawableContainer)
		{
			_listOfdrawableContainer = listOfdrawableContainer;
			StoreBounds();
		}

		public DrawableContainerBoundsChangeMemento(IDrawableContainer drawableContainer)
		{
			_listOfdrawableContainer = new DrawableContainerList {drawableContainer};
			_listOfdrawableContainer.Parent = drawableContainer.Parent;
			StoreBounds();
		}

		public bool Merge(IMemento otherMemento)
		{
			DrawableContainerBoundsChangeMemento other = otherMemento as DrawableContainerBoundsChangeMemento;
			if (other != null)
			{
				if (Objects.CompareLists(_listOfdrawableContainer, other._listOfdrawableContainer))
				{
					// Lists are equal, as we have the state already we can ignore the new memento
					return true;
				}
			}
			return false;
		}

		public IMemento Restore()
		{
			var oldState = new DrawableContainerBoundsChangeMemento(_listOfdrawableContainer);
			for (int index = 0; index < _listOfdrawableContainer.Count; index++)
			{
				IDrawableContainer drawableContainer = _listOfdrawableContainer[index];
				// Before
				drawableContainer.Invalidate();
				drawableContainer.Left = _points[index].X;
				drawableContainer.Top = _points[index].Y;
				drawableContainer.Width = _sizes[index].Width;
				drawableContainer.Height = _sizes[index].Height;
				// After
				drawableContainer.Invalidate();
				drawableContainer.Parent.Modified = true;
			}
			return oldState;
		}

		private void StoreBounds()
		{
			foreach (IDrawableContainer drawableContainer in _listOfdrawableContainer)
			{
				_points.Add(drawableContainer.Location);
				_sizes.Add(drawableContainer.Size);
			}
		}

		#region IDisposable Support

		private bool _disposedValue; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects).
				}
				_listOfdrawableContainer = null;

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