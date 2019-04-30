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

using System;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.SafeHandles;

namespace Greenshot.Addons.Controls
{
	/// <summary>
	///     This code was supplied by Hi-Coder as a patch for Greenshot
	///     Needed some modifications to be stable.
	/// </summary>
	public partial class MovableShowColorForm : Form
	{
		/// <summary>
		/// Default constructor for a control
		/// </summary>
		public MovableShowColorForm()
		{
			InitializeComponent();
		}

		/// <summary>
		/// The color under the cursor
		/// </summary>
		public Color ColorUnderCursor => preview.BackColor;

	    /// <summary>
		///     Move the MovableShowColorForm to the specified location and display the color under the (current mouse) coordinates
		/// </summary>
		/// <param name="screenCoordinates">Coordinates</param>
		public void MoveTo(Point screenCoordinates)
		{
			var currentColor = GetPixelColor(screenCoordinates);
			preview.BackColor = currentColor;
			html.Text = "#" + currentColor.Name.Substring(2).ToUpper();
			red.Text = "" + currentColor.R;
			blue.Text = "" + currentColor.B;
			green.Text = "" + currentColor.G;
			alpha.Text = "" + currentColor.A;

			var cursorSize = Cursor.Current.Size;
			var hotspot = Cursor.Current.HotSpot;

			var zoomerLocation = new Point(screenCoordinates.X, screenCoordinates.Y);
			zoomerLocation.X += cursorSize.Width + 5 - hotspot.X;
			zoomerLocation.Y += cursorSize.Height + 5 - hotspot.Y;

			foreach (var screen in Screen.AllScreens)
			{
				var screenRectangle = screen.Bounds;
			    if (!screen.Bounds.Contains(screenCoordinates))
			    {
			        continue;
			    }
			    if (zoomerLocation.X < screenRectangle.X)
			    {
			        zoomerLocation.X = screenRectangle.X;
			    }
			    else if (zoomerLocation.X + Width > screenRectangle.X + screenRectangle.Width)
			    {
			        zoomerLocation.X = screenCoordinates.X - Width - 5 - hotspot.X;
			    }

			    if (zoomerLocation.Y < screenRectangle.Y)
			    {
			        zoomerLocation.Y = screenRectangle.Y;
			    }
			    else if (zoomerLocation.Y + Height > screenRectangle.Y + screenRectangle.Height)
			    {
			        zoomerLocation.Y = screenCoordinates.Y - Height - 5 - hotspot.Y;
			    }
			    break;
			}
			Location = zoomerLocation;
			Update();
		}

		/// <summary>
		///     Get the color from the pixel on the screen at "x,y"
		/// </summary>
		/// <param name="screenCoordinates">Point with the coordinates</param>
		/// <returns>Color at the specified screenCoordinates</returns>
		private static Color GetPixelColor(Point screenCoordinates)
		{
			using (var screenDc = SafeWindowDcHandle.FromDesktop())
			{
				try
				{
					var pixel = Gdi32Api.GetPixel(screenDc, screenCoordinates.X, screenCoordinates.Y);
					var color = Color.FromArgb(255, (int) (pixel & 0xFF), (int) (pixel & 0xFF00) >> 8, (int) (pixel & 0xFF0000) >> 16);
					return color;
				}
				catch (Exception)
				{
					return Color.Empty;
				}
			}
		}
	}
}