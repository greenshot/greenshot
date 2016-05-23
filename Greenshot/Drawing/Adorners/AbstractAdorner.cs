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
using Greenshot.Plugin.Drawing.Adorners;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System;
using GreenshotPlugin.Interfaces.Drawing;

namespace Greenshot.Drawing.Adorners
{
	public class AbstractAdorner : IAdorner
	{
		public virtual EditStatus EditStatus { get; protected set; } = EditStatus.IDLE;

		protected Size _size = new Size(4, 4);

		public AbstractAdorner(IDrawableContainer owner)
		{
			Owner = owner;
		}

		/// <summary>
		/// Returns the cursor for when the mouse is over the adorner
		/// </summary>
		public virtual Cursor Cursor
		{
			get
			{
				return Cursors.SizeAll;
			}
		}

		public virtual IDrawableContainer Owner
		{
			get;
			set;
		}

		/// <summary>
		/// Test if the point is inside the adorner
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public virtual bool HitTest(Point point)
		{
			Rectangle hitBounds = Bounds;
			hitBounds.Inflate(3, 3);
			return hitBounds.Contains(point);
		}

		/// <summary>
		/// Handle the mouse down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs"></param>
		public virtual void MouseDown(object sender, MouseEventArgs mouseEventArgs)
		{
		}

		/// <summary>
		/// Handle the mouse move
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs"></param>
		public virtual void MouseMove(object sender, MouseEventArgs mouseEventArgs)
		{
		}

		/// <summary>
		/// Handle the mouse up
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs"></param>
		public virtual void MouseUp(object sender, MouseEventArgs mouseEventArgs)
		{
			EditStatus = EditStatus.IDLE;
		}

		/// <summary>
		/// Return the location of the adorner
		/// </summary>
		public virtual Point Location
		{
			get;
			set;
		}

		/// <summary>
		/// Return the bounds of the Adorner
		/// </summary>
		public virtual Rectangle Bounds
		{
			get
			{
				Point location = Location;
				return new Rectangle(location.X - (_size.Width / 2), location.Y - (_size.Height / 2), _size.Width, _size.Height);
			}
		}

		/// <summary>
		/// The adorner is active if the edit status is not idle or undrawn
		/// </summary>
		public virtual bool IsActive
		{
			get
			{
				return EditStatus != EditStatus.IDLE && EditStatus != EditStatus.UNDRAWN;
			}
		}

		/// <summary>
		/// Draw the adorner
		/// </summary>
		/// <param name="paintEventArgs">PaintEventArgs</param>
		public virtual void Paint(PaintEventArgs paintEventArgs)
		{
		}

		/// <summary>
		/// We ignore the Transform, as the coordinates are directly bound to those of the owner
		/// </summary>
		/// <param name="matrix"></param>
		public virtual void Transform(Matrix matrix)
		{
		}
	}
}
