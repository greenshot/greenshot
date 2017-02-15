#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

#endregion

namespace GreenshotPlugin.Interfaces.Drawing.Adorners
{
	public interface IAdorner
	{
		/// <summary>
		///     Returns if this adorner is active
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		///     The current edit status, this is needed to locate the adorner to send events to
		/// </summary>
		EditStatus EditStatus { get; }

		/// <summary>
		///     The owner of this adorner
		/// </summary>
		IDrawableContainer Owner { get; }

		/// <summary>
		///     Gets the cursor that should be displayed for this behavior.
		/// </summary>
		Cursor Cursor { get; }

		/// <summary>
		///     Is the current point "over" the Adorner?
		///     If this is the case, the
		/// </summary>
		/// <param name="point">Point to test</param>
		/// <returns>true if so</returns>
		bool HitTest(Point point);

		/// <summary>
		///     Handle the MouseDown event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs">MouseEventArgs</param>
		void MouseDown(object sender, MouseEventArgs mouseEventArgs);

		/// <summary>
		///     Handle the MouseUp event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs">MouseEventArgs</param>
		void MouseUp(object sender, MouseEventArgs mouseEventArgs);

		/// <summary>
		///     Handle the MouseMove event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="mouseEventArgs">MouseEventArgs</param>
		void MouseMove(object sender, MouseEventArgs mouseEventArgs);

		/// <summary>
		///     Draw the adorner
		/// </summary>
		/// <param name="paintEventArgs">PaintEventArgs</param>
		void Paint(PaintEventArgs paintEventArgs);

		/// <summary>
		///     Called if the owner is transformed
		/// </summary>
		/// <param name="matrix">Matrix</param>
		void Transform(Matrix matrix);
	}
}