/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Greenshot.Helpers {
	public class FormHelper {
		#region static
		public static void RestoreGeometry(Form formIn, Size size, Point location, FormWindowState state, Rectangle previousScreenbounds) {
			Rectangle screenbounds = WindowCapture.GetScreenBounds();
			
			// Used default settings if no previous screenbounds were given
			if (previousScreenbounds == Rectangle.Empty) {
				previousScreenbounds = screenbounds;
			}
			if (state != FormWindowState.Maximized && state != FormWindowState.Normal) {
				// Form was most likely minimized, should NOT use size/location!!
				formIn.WindowState = FormWindowState.Normal;
				return;
			}

			// Calculate the window rectangle to see if it would be visible
			Rectangle windowsRectangle = new Rectangle(location, size);
			
			// Only "restore" location if the screens didn't change or the window fits in the current bounds
			// Due to possible problems with the minimized/maximazed coordinates there are two checks
			if (screenbounds.Equals(previousScreenbounds) || screenbounds.Contains(windowsRectangle)) {
				// Make sure the location is taken
				formIn.StartPosition = FormStartPosition.Manual;
				formIn.Location = location;
				formIn.Size = size;
			}
			
			// Set state
			if (state == FormWindowState.Maximized || state == FormWindowState.Normal) {
				formIn.WindowState = state;
			}
		}
		
		public static void StoreGeometry(Form formIn, out Size size, out Point location, out FormWindowState state, out Rectangle previousScreenbounds ) {
			size = formIn.Size;
			location = formIn.Location;
			state = formIn.WindowState;
			previousScreenbounds = WindowCapture.GetScreenBounds();
		}
		#endregion

	}
}
