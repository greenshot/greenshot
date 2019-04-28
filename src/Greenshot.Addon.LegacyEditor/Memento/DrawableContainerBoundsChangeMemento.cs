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

using System.Collections.Generic;
using System.Drawing;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addon.LegacyEditor.Drawing;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces.Drawing;

namespace Greenshot.Addon.LegacyEditor.Memento
{
	/// <summary>
	///     The DrawableContainerBoundsChangeMemento makes it possible to undo-redo an IDrawableContainer resize and move
	/// </summary>
	public class DrawableContainerBoundsChangeMemento : IMemento
	{
		private readonly List<NativePoint> _points = new List<NativePoint>();
		private readonly List<Size> _sizes = new List<Size>();
		private IDrawableContainerList _listOfdrawableContainer;

		public DrawableContainerBoundsChangeMemento(IDrawableContainerList listOfdrawableContainer)
		{
			_listOfdrawableContainer = listOfdrawableContainer;
			StoreBounds();
		}

		public DrawableContainerBoundsChangeMemento(IDrawableContainer drawableContainer)
		{
			_listOfdrawableContainer = new DrawableContainerList
			{
				drawableContainer
			};
			_listOfdrawableContainer.Parent = drawableContainer.Parent;
			StoreBounds();
		}

		public void Dispose()
		{
			Dispose(true);
		}

		public bool Merge(IMemento otherMemento)
		{
			if (!(otherMemento is DrawableContainerBoundsChangeMemento other))
			{
				return false;
			}

			if (ObjectExtensions.CompareLists(_listOfdrawableContainer, other._listOfdrawableContainer))
			{
				// Lists are equal, as we have the state already we can ignore the new memento
				return true;
			}
			return false;
		}

		public IMemento Restore()
		{
			var oldState = new DrawableContainerBoundsChangeMemento(_listOfdrawableContainer);
			for (var index = 0; index < _listOfdrawableContainer.Count; index++)
			{
				var drawableContainer = _listOfdrawableContainer[index];
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
			foreach (var drawableContainer in _listOfdrawableContainer)
			{
				_points.Add(drawableContainer.Location);
				_sizes.Add(drawableContainer.Size);
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_listOfdrawableContainer?.Dispose();
			}
			_listOfdrawableContainer = null;
		}
	}
}