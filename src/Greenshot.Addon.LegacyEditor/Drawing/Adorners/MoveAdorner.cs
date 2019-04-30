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

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Addons.Interfaces.Drawing;
using Greenshot.Gfx.Legacy;

namespace Greenshot.Addon.LegacyEditor.Drawing.Adorners
{
	/// <summary>
	///     This is the adorner for the line based containers
	/// </summary>
	public class MoveAdorner : AbstractAdorner
	{
		private NativeRectFloat _boundsAfterResize = NativeRectFloat.Empty;
		private NativeRectFloat _boundsBeforeResize = NativeRectFloat.Empty;
		private bool _initialMoveAfterMouseDown = true;
		public MoveAdorner(IDrawableContainer owner, Positions position) : base(owner)
		{
			Position = position;
		}

		public Positions Position { get; }

		/// <summary>
		///     Returns the cursor for when the mouse is over the adorner
		/// </summary>
		public override Cursor Cursor => Cursors.SizeAll;

		/// <summary>
		///     Return the location of the adorner
		/// </summary>
		public override NativePoint Location
		{
			get
			{
				int x = 0, y = 0;
				switch (Position)
				{
					case Positions.TopLeft:
						x = Owner.Left;
						y = Owner.Top;
						break;
					case Positions.BottomLeft:
						x = Owner.Left;
						y = Owner.Top + Owner.Height;
						break;
					case Positions.MiddleLeft:
						x = Owner.Left;
						y = Owner.Top + Owner.Height / 2;
						break;
					case Positions.TopCenter:
						x = Owner.Left + Owner.Width / 2;
						y = Owner.Top;
						break;
					case Positions.BottomCenter:
						x = Owner.Left + Owner.Width / 2;
						y = Owner.Top + Owner.Height;
						break;
					case Positions.TopRight:
						x = Owner.Left + Owner.Width;
						y = Owner.Top;
						break;
					case Positions.BottomRight:
						x = Owner.Left + Owner.Width;
						y = Owner.Top + Owner.Height;
						break;
					case Positions.MiddleRight:
						x = Owner.Left + Owner.Width;
						y = Owner.Top + Owner.Height / 2;
						break;
				}
				return new NativePoint(x, y);
			}
		}

		/// <summary>
		///     Handle the mouse down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs"></param>
		public override void MouseDown(object sender, MouseEventArgs mouseEventArgs)
		{
			EditStatus = EditStatus.Resizing;
			_boundsBeforeResize = new NativeRectFloat(Owner.Left, Owner.Top, Owner.Width, Owner.Height);
			_boundsAfterResize = _boundsBeforeResize;
			_initialMoveAfterMouseDown = true;

		}

		/// <summary>
		///     Handle the mouse move
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs"></param>
		public override void MouseMove(object sender, MouseEventArgs mouseEventArgs)
		{
			if (EditStatus != EditStatus.Resizing)
			{
				return;
			}
			Owner.Invalidate();
			// Make merges in this drag, meaning as long as it's not the initial move we merge
			Owner.MakeBoundsChangeUndoable(!_initialMoveAfterMouseDown);
			_initialMoveAfterMouseDown = false;

			// reset "workbench" rectangle to current bounds
			_boundsAfterResize = _boundsBeforeResize;

			// calculate scaled rectangle
			ScaleHelper.Scale(ref _boundsAfterResize, Position, new NativePointFloat(mouseEventArgs.X, mouseEventArgs.Y), ScaleHelper.GetScaleOptions());

			// apply scaled bounds to this DrawableContainer
			Owner.ApplyBounds(_boundsAfterResize.Round());

			Owner.Invalidate();
		}

		/// <summary>
		///     Draw the adorner
		/// </summary>
		/// <param name="paintEventArgs">PaintEventArgs</param>
		public override void Paint(PaintEventArgs paintEventArgs)
		{
			var targetGraphics = paintEventArgs.Graphics;

			var bounds = Bounds;
			var state = targetGraphics.Save();

			targetGraphics.SmoothingMode = SmoothingMode.None;
			targetGraphics.CompositingMode = CompositingMode.SourceCopy;
			targetGraphics.PixelOffsetMode = PixelOffsetMode.Half;
			targetGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;

			try
			{
				targetGraphics.FillRectangle(Brushes.Black, bounds.X, bounds.Y, bounds.Width, bounds.Height);
			}
			catch
			{
				// Ignore, BUG-2065
			}
			targetGraphics.Restore(state);
		}
	}
}