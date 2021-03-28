/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

namespace GreenshotPlugin.Interfaces.Drawing.Adorners
{
    public interface IAdorner
    {
        /// <summary>
        /// Returns if this adorner is active
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// These are the bounds of the adorner
        /// </summary>
        Rectangle Bounds { get; }

        /// <summary>
        /// The current edit status, this is needed to locate the adorner to send events to
        /// </summary>
        EditStatus EditStatus { get; }

        /// <summary>
        /// The owner of this adorner
        /// </summary>
        IDrawableContainer Owner { get; }

        /// <summary>
        /// Is the current point "over" the Adorner?
        /// If this is the case, the
        /// </summary>
        /// <param name="point">Point to test</param>
        /// <returns>true if so</returns>
        bool HitTest(Point point);

        /// <summary>
        /// Handle the MouseDown event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mouseEventArgs">MouseEventArgs</param>
        void MouseDown(object sender, MouseEventArgs mouseEventArgs);

        /// <summary>
        /// Handle the MouseUp event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mouseEventArgs">MouseEventArgs</param>
        void MouseUp(object sender, MouseEventArgs mouseEventArgs);

        /// <summary>
        /// Handle the MouseMove event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mouseEventArgs">MouseEventArgs</param>
        void MouseMove(object sender, MouseEventArgs mouseEventArgs);

        /// <summary>
        /// Gets the cursor that should be displayed for this behavior.
        /// </summary>
        Cursor Cursor { get; }

        /// <summary>
        /// Draw the adorner
        /// </summary>
        /// <param name="paintEventArgs">PaintEventArgs</param>
        void Paint(PaintEventArgs paintEventArgs);

        /// <summary>
        /// Called if the owner is transformed
        /// </summary>
        /// <param name="matrix">Matrix</param>
        void Transform(Matrix matrix);

        /// <summary>
        /// Adjust UI elements to the supplied DPI settings
        /// </summary>
        /// <param name="dpi"></param>
        void AdjustToDpi(uint dpi);
    }
}