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

using Greenshot.Helpers;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GreenshotPlugin.Interfaces.Drawing;

namespace Greenshot.Drawing.Adorners
{
	/// <summary>
	/// This is the default "legacy" gripper adorner, not the one used for the tail in the speech-bubble
	/// </summary>
	public class ResizeAdorner : AbstractAdorner
	{
		private Rectangle _boundsBeforeResize = Rectangle.Empty;
		private RectangleF _boundsAfterResize = RectangleF.Empty;

		public Positions Position { get; private set; }

		public ResizeAdorner(IDrawableContainer owner, Positions position) : base(owner)
		{
			Position = position;
		}

		/// <summary>
		/// Returns the cursor for when the mouse is over the adorner
		/// </summary>
		public override Cursor Cursor
		{
			get
			{
				bool isNotSwitched = Owner.Width >= 0;
				if (Owner.Height < 0)
				{
					isNotSwitched = !isNotSwitched;
				}
				switch (Position)
				{
					case Positions.TopLeft:
					case Positions.BottomRight:
						return isNotSwitched ? Cursors.SizeNWSE : Cursors.SizeNESW;
					case Positions.TopRight:
					case Positions.BottomLeft:
						return isNotSwitched ? Cursors.SizeNESW : Cursors.SizeNWSE;
					case Positions.MiddleLeft:
					case Positions.MiddleRight:
						return Cursors.SizeWE;
					case Positions.TopCenter:
					case Positions.BottomCenter:
						return Cursors.SizeNS;
					default:
						return Cursors.SizeAll;
				}
			}
		}

		/// <summary>
		/// Handle the mouse down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs"></param>
		public override void MouseDown(object sender, MouseEventArgs mouseEventArgs)
		{
			EditStatus = EditStatus.RESIZING;
			_boundsBeforeResize = new Rectangle(Owner.Left, Owner.Top, Owner.Width, Owner.Height);
			_boundsAfterResize = _boundsBeforeResize;
		}

		/// <summary>
		/// Handle the mouse move
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs"></param>
		public override void MouseMove(object sender, MouseEventArgs mouseEventArgs)
		{
			if (EditStatus != EditStatus.RESIZING)
			{
				return;
			}
			Owner.Invalidate();
			Owner.MakeBoundsChangeUndoable(false);

			// reset "workbench" rectangle to current bounds
			_boundsAfterResize.X = _boundsBeforeResize.X;
			_boundsAfterResize.Y = _boundsBeforeResize.Y;
			_boundsAfterResize.Width = _boundsBeforeResize.Width;
			_boundsAfterResize.Height = _boundsBeforeResize.Height;

			// calculate scaled rectangle
			ScaleHelper.Scale(ref _boundsAfterResize, Position, new PointF(mouseEventArgs.X, mouseEventArgs.Y), ScaleHelper.GetScaleOptions());

			// apply scaled bounds to this DrawableContainer
			Owner.ApplyBounds(_boundsAfterResize);

			Owner.Invalidate();
		}

		/// <summary>
		/// Return the location of the adorner
		/// </summary>
		public override Point Location {
			get
			{
				int x = 0,y = 0;
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
						y = Owner.Top + (Owner.Height / 2);
						break;
					case Positions.TopCenter:
						x = Owner.Left + (Owner.Width / 2);
						y = Owner.Top;
						break;
					case Positions.BottomCenter:
						x = Owner.Left + (Owner.Width / 2);
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
						y = Owner.Top + (Owner.Height / 2);
						break;
				}
				return new Point(x, y);
			}
		}

		/// <summary>
		/// Draw the adorner
		/// </summary>
		/// <param name="paintEventArgs">PaintEventArgs</param>
		public override void Paint(PaintEventArgs paintEventArgs)
		{
			Graphics targetGraphics = paintEventArgs.Graphics;

			var bounds = Bounds;
			GraphicsState state = targetGraphics.Save();

			targetGraphics.SmoothingMode = SmoothingMode.None;
			targetGraphics.CompositingMode = CompositingMode.SourceCopy;
			targetGraphics.PixelOffsetMode = PixelOffsetMode.Half;
			targetGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;

			targetGraphics.FillRectangle(Brushes.Black, bounds.X, bounds.Y, bounds.Width , bounds.Height);
			targetGraphics.Restore(state);
		}
	}
}
