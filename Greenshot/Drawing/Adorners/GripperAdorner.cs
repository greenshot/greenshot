/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Plugin;
using Greenshot.Plugin.Drawing;
using Greenshot.Plugin.Drawing.Adorners;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Greenshot.Drawing.Adorners
{
	/// <summary>
	/// This is the default "legacy" gripper adorner, not the one used for the tail in the speech-bubble
	/// </summary>
	public class GripperAdorner : IAdorner
	{
		private Rectangle _boundsBeforeResize = Rectangle.Empty;
		private RectangleF _boundsAfterResize = RectangleF.Empty;
		private EditStatus _editStatus;

		public Positions Position { get; private set; }

		public GripperAdorner(IDrawableContainer owner, Positions position)
		{
			Owner = owner;
			Position = position;
		}

		/// <summary>
		/// Returns the cursor for when the mouse is over the adorner
		/// </summary>
		public Cursor Cursor
		{
			get
			{
				bool horizontalSwitched = Owner.Width < 0;
				switch (Position)
				{
					case Positions.TopLeft:
					case Positions.BottomRight:
						return horizontalSwitched ? Cursors.SizeNWSE : Cursors.SizeNESW;
					case Positions.TopRight:
					case Positions.BottomLeft:
						return horizontalSwitched ? Cursors.SizeNESW : Cursors.SizeNWSE;
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

		public IDrawableContainer Owner
		{
			get;
			set;
		}

		/// <summary>
		/// Test if the point is inside the adorner
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public bool HitTest(Point point)
		{
			return Bounds.Contains(point);
		}

		/// <summary>
		/// Handle the mouse down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs"></param>
		public void MouseDown(object sender, MouseEventArgs mouseEventArgs)
		{
			_editStatus = EditStatus.RESIZING;
			_boundsBeforeResize = new Rectangle(Owner.Left, Owner.Top, Owner.Width, Owner.Height);
			_boundsAfterResize = _boundsBeforeResize;
		}

		/// <summary>
		/// Handle the mouse move
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs"></param>
		public void MouseMove(object sender, MouseEventArgs mouseEventArgs)
		{
			Owner.Invalidate();
			int absX = Owner.Left + mouseEventArgs.X;
			int absY = Owner.Top + mouseEventArgs.Y;

			if (_editStatus.Equals(EditStatus.RESIZING))
			{
				Owner.MakeBoundsChangeUndoable(false);

				//SuspendLayout();

				// reset "workbench" rectangle to current bounds
				_boundsAfterResize.X = _boundsBeforeResize.X;
				_boundsAfterResize.Y = _boundsBeforeResize.Y;
				_boundsAfterResize.Width = _boundsBeforeResize.Width;
				_boundsAfterResize.Height = _boundsBeforeResize.Height;

				// calculate scaled rectangle
				ScaleHelper.Scale(ref _boundsAfterResize, Position, new PointF(absX, absY), ScaleHelper.GetScaleOptions());

				// apply scaled bounds to this DrawableContainer
				Owner.ApplyBounds(_boundsAfterResize);

				//ResumeLayout();
				Owner.DoLayout();
			}
			Owner.Invalidate();
		}

		/// <summary>
		/// Handle the mouse up
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs"></param>
		public void MouseUp(object sender, MouseEventArgs mouseEventArgs)
		{
			_editStatus = EditStatus.IDLE;
			Owner.Invalidate();
		}

		/// <summary>
		/// Return the location of the adorner
		/// </summary>
		public Point Location {
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
		/// Return the bounds of the Adorner
		/// </summary>
		public Rectangle Bounds
		{
			get
			{
				Point location = Location;
				Size size = new Size(10, 10);
				return new Rectangle(location.X - (size.Width / 2), location.Y - (size.Height / 2), size.Width, size.Height);
			}
		}

		/// <summary>
		/// Draw the adorner
		/// </summary>
		/// <param name="paintEventArgs">PaintEventArgs</param>
		public void Paint(PaintEventArgs paintEventArgs)
		{
			Graphics targetGraphics = paintEventArgs.Graphics;
			Rectangle clipRectangle = paintEventArgs.ClipRectangle;

			var bounds = Bounds;
			targetGraphics.DrawRectangle(Pens.Black, bounds.X, bounds.Y, bounds.Width , bounds.Height);
		}

		/// <summary>
		/// We ignore the Transform, as the coordinates are directly bound to those of the owner
		/// </summary>
		/// <param name="matrix"></param>
		public void Transform(Matrix matrix)
		{
		}
	}
}
