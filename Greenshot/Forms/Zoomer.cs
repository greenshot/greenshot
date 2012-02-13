/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows.Forms;
using System.Drawing;
using GreenshotPlugin.UnmanagedHelpers;

namespace Greenshot.Forms {
	/// <summary>
	/// This code was supplied by Hi-Coder as a patch for Greenshot
	/// Needed some modifications to be stable.
	/// </summary>
	public partial class Zoomer : Form {
		public Color color {
			get {
				return preview.BackColor;
			}
		}

		public Zoomer() {
			InitializeComponent();
		}

		public void setHotSpot(int x, int y) {
			Color c = GetPixelColor(x, y);
			preview.BackColor = c;
			 html.Text = "#" + c.Name.Substring(2).ToUpper();
			red.Text = "" + c.R;
			blue.Text = "" + c.B;
			green.Text = "" + c.G;
			alpha.Text = "" + c.A;

			Size cs = Cursor.Current.Size;
			Point hs = Cursor.Current.HotSpot;

			Point zp = new Point(x, y);
			zp.X += cs.Width + 5 - hs.X;
			zp.Y += cs.Height + 5 - hs.Y;

			foreach (Screen screen in Screen.AllScreens) {
				Rectangle screenRectangle = screen.Bounds;
				if (screen.Bounds.Contains(x, y)) {
					if (zp.X < screenRectangle.X) {
						zp.X = screenRectangle.X;
					} else if (zp.X + Width > screenRectangle.X + screenRectangle.Width) {
						zp.X = x - Width - 5 - hs.X;
					}

					if (zp.Y < screenRectangle.Y) {
						zp.Y = screenRectangle.Y;
					} else if (zp.Y + Height > screenRectangle.Y + screenRectangle.Height) {
						zp.Y = y - Height - 5 - hs.Y;
					}
					break;
				}
			}

			Location = zp;
			Invalidate();
		}

		public void setHotSpot(Point screenCoordinates) {
			setHotSpot(screenCoordinates.X, screenCoordinates.Y);
		}

		static private Color GetPixelColor(int x, int y) {
			IntPtr hdc = User32.GetDC(IntPtr.Zero);
			try {
				uint pixel = GDI32.GetPixel(hdc, x, y);
				Color color = Color.FromArgb(255, (int)(pixel & 0xFF), (int)(pixel & 0xFF00) >> 8, (int)(pixel & 0xFF0000) >> 16);
				return color;
			} catch (Exception) {
				return Color.Empty;
			} finally {
				if (hdc != IntPtr.Zero) {
					User32.ReleaseDC(IntPtr.Zero, hdc);
				}
			}
		}
	}
}
