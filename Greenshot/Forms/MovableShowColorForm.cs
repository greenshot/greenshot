#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Native;
using Dapplo.Windows.SafeHandles;

#endregion

namespace Greenshot.Forms
{
	/// <summary>
	///     This code was supplied by Hi-Coder as a patch for Greenshot
	///     Needed some modifications to be stable.
	/// </summary>
	public partial class MovableShowColorForm : Form
	{
		public MovableShowColorForm()
		{
			InitializeComponent();
		}

		public Color color
		{
			get { return preview.BackColor; }
		}

		/// <summary>
		///     Move the MovableShowColorForm to the specified location and display the color under the (current mouse) coordinates
		/// </summary>
		/// <param name="screenCoordinates">Coordinates</param>
		public void MoveTo(Point screenCoordinates)
		{
			var c = GetPixelColor(screenCoordinates);
			preview.BackColor = c;
			html.Text = "#" + c.Name.Substring(2).ToUpper();
			red.Text = "" + c.R;
			blue.Text = "" + c.B;
			green.Text = "" + c.G;
			alpha.Text = "" + c.A;

			var cursorSize = Cursor.Current.Size;
			var hotspot = Cursor.Current.HotSpot;

			var zoomerLocation = new Point(screenCoordinates.X, screenCoordinates.Y);
			zoomerLocation.X += cursorSize.Width + 5 - hotspot.X;
			zoomerLocation.Y += cursorSize.Height + 5 - hotspot.Y;

			foreach (var screen in Screen.AllScreens)
			{
				var screenRectangle = screen.Bounds;
				if (screen.Bounds.Contains(screenCoordinates))
				{
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
					var pixel = Gdi32.GetPixel(screenDc, screenCoordinates.X, screenCoordinates.Y);
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