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

using Greenshot.Plugin.Drawing;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

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
			Rectangle surfaceBounds = new Rectangle(0, 0, Owner.Parent.Width, Owner.Parent.Height);
			// Check if gripper inside the parent (surface), if not we need to move it inside
			// This was made for BUG-1682
			if (!surfaceBounds.Contains(newGripperLocation))
			{
				if (newGripperLocation.X > surfaceBounds.Right)
				{
					newGripperLocation.X = surfaceBounds.Right - 5;
				}
				if (newGripperLocation.X < surfaceBounds.Left)
				{
					newGripperLocation.X = surfaceBounds.Left;
				}
				if (newGripperLocation.Y > surfaceBounds.Bottom)
				{
					newGripperLocation.Y = surfaceBounds.Bottom - 5;
				}
				if (newGripperLocation.Y < surfaceBounds.Top)
				{
					newGripperLocation.Y = surfaceBounds.Top;
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
			Rectangle clipRectangle = paintEventArgs.ClipRectangle;

			var bounds = Bounds;
			targetGraphics.FillRectangle(Brushes.Green, bounds.X, bounds.Y, bounds.Width, bounds.Height);
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
