/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System.Drawing;
using System.Windows.Forms;

namespace Greenshot.Editor.Controls
{
    /// <summary>
    /// See: https://nickstips.wordpress.com/2010/03/03/c-panel-resets-scroll-position-after-focus-is-lost-and-regained/
    /// </summary>
    public class NonJumpingPanel : Panel
    {
        protected override Point ScrollToControl(Control activeControl) =>
            // Returning the current location prevents the panel from
            // scrolling to the active control when the panel loses and regains focus
            DisplayRectangle.Location;

        /// <summary>
        /// Add horizontal scrolling to the panel, when using the wheel and the shift key is pressed
        /// </summary>
        /// <param name="e">MouseEventArgs</param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            //Check if Scrollbars available and CTRL key pressed -> Zoom IN OUT
            if ((VScroll || HScroll) && (ModifierKeys & Keys.Control) == Keys.Control)
            {
                VScroll = false;
                HScroll = false;
                base.OnMouseWheel(e);
                VScroll = true;
                HScroll = true;
            }
            else
            {
                //Vertical Scoll with SHIFT key pressed
                if (VScroll && (ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    VScroll = false;
                    base.OnMouseWheel(e);
                    VScroll = true;
                }
                else
                {
                    base.OnMouseWheel(e);
                }
            }
        }
    }
}