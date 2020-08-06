/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using GreenshotPlugin.Interfaces.Drawing;

namespace Greenshot.Drawing.Adorners
{
	/// <summary>
	/// This implements the special "gripper" for the Speech-Bubble tail
	/// </summary>
	public class TargetAdorner : AbstractAdorner
	{

		public TargetAdorner(IDrawableContainer owner, Point location) : base(owner)
		{
			Location = location;
		}

		/// <summary>
		/// Handle the mouse down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs"></param>
		public override void MouseDown(object sender, MouseEventArgs mouseEventArgs)
		{
			EditStatus = EditStatus.MOVING;
		}

		/// <summary>
		/// Handle the mouse move
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs"></param>
		public override void MouseMove(object sender, MouseEventArgs mouseEventArgs)
		{
			if (EditStatus != EditStatus.MOVING)
			{
				return;
			}

			Owner.Invalidate();
			Point newGripperLocation = new Point(mouseEventArgs.X, mouseEventArgs.Y);
			Rectangle imageBounds = new Rectangle(0, 0, Owner.Parent.Image.Width, Owner.Parent.Image.Height);
			// Check if gripper inside the parent (surface), if not we need to move it inside
			// This was made for BUG-1682
			if (!imageBounds.Contains(newGripperLocation))
			{
				if (newGripperLocation.X > imageBounds.Right)
				{
					newGripperLocation.X = imageBounds.Right - 5;
				}
				if (newGripperLocation.X < imageBounds.Left)
				{
					newGripperLocation.X = imageBounds.Left;
				}
				if (newGripperLocation.Y > imageBounds.Bottom)
				{
					newGripperLocation.Y = imageBounds.Bottom - 5;
				}
				if (newGripperLocation.Y < imageBounds.Top)
				{
					newGripperLocation.Y = imageBounds.Top;
				}
			}

			Location = newGripperLocation;
			Owner.Invalidate();
		}

		/// <summary>
		/// Draw the adorner
		/// </summary>
		/// <param name="paintEventArgs">PaintEventArgs</param>
		public override void Paint(PaintEventArgs paintEventArgs)
		{
			Graphics targetGraphics = paintEventArgs.Graphics;

			var bounds = BoundsOnSurface;
            targetGraphics.FillRectangle(Brushes.Green, bounds);
			targetGraphics.DrawRectangle(new Pen(Brushes.White), bounds);
		}

		/// <summary>
		/// Made sure this adorner is transformed
		/// </summary>
		/// <param name="matrix">Matrix</param>
		public override void Transform(Matrix matrix)
		{
			if (matrix == null)
			{
				return;
			}
			Point[] points = new[] { Location };
			matrix.TransformPoints(points);
			Location = points[0];
		}
	}
}
