/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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

using System.Collections.Generic;
using System.Drawing;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces.Drawing;

namespace GreenshotEditorPlugin.Memento
{
	/// <summary>
	/// The DrawableContainerBoundsChangeMemento makes it possible to undo-redo an IDrawableContainer resize & move
	/// </summary>
	public class DrawableContainerBoundsChangeMemento : IMemento
	{
		private IList<Point> _points = new List<Point>();
		private IList<Size> _sizes = new List<Size>();
		private IList<IDrawableContainer> _listOfdrawableContainer;

		private void StoreBounds()
		{
			foreach (IDrawableContainer drawableContainer in _listOfdrawableContainer)
			{
				_points.Add(drawableContainer.Location);
				_sizes.Add(drawableContainer.Size);
			}
		}

		public DrawableContainerBoundsChangeMemento(IList<IDrawableContainer> listOfdrawableContainer)
		{
			_listOfdrawableContainer = listOfdrawableContainer;
			StoreBounds();
		}

		public DrawableContainerBoundsChangeMemento(IDrawableContainer drawableContainer)
		{
			_listOfdrawableContainer = new List<IDrawableContainer>();
			_listOfdrawableContainer.Add(drawableContainer);
			StoreBounds();
		}

		public string ActionDescription
		{
			get
			{
				return "";
			}
		}

		public bool Merge(IMemento otherMemento)
		{
			DrawableContainerBoundsChangeMemento other = otherMemento as DrawableContainerBoundsChangeMemento;
			if (other != null)
			{
				if (Objects.CompareLists<IDrawableContainer>(_listOfdrawableContainer, other._listOfdrawableContainer))
				{
					// Lists are equal, as we have the state already we can ignore the new memento
					return true;
				}
			}
			return false;
		}

		public IMemento Restore()
		{
			DrawableContainerBoundsChangeMemento oldState = new DrawableContainerBoundsChangeMemento(_listOfdrawableContainer);
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

		#region IDisposable Support

		private bool _disposedValue = false; // To detect redundant calls

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