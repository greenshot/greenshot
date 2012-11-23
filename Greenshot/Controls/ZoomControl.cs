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
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using Greenshot.Plugin;
using System.Drawing.Drawing2D;
using GreenshotPlugin.Controls;
using GreenshotPlugin.UnmanagedHelpers;

namespace Greenshot.Forms {
	/// <summary>
	/// This control will show the area around the mouse of the current capture
	/// </summary>
	public class ZoomControl : Control {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ZoomControl));
		private Image imageToZoom = null;
		private Point zoomLocation = Point.Empty;
		private const int distanceX = 20;
		private const int distanceY = 20;

		/// <summary>
		/// Create a zoom control for the image and with the size
		/// </summary>
		/// <param name="imageToZoom"></param>
		/// <param name="zoomSize"></param>
		public ZoomControl(Image imageToZoom, Size zoomSize) {
			InitializeComponent();
			this.Width = zoomSize.Width;
			this.Height = zoomSize.Height;
			this.imageToZoom = imageToZoom;
			Zoom = 400;
		}

		/// <summary>
		/// Override some special form parameters
		/// </summary>
		protected override CreateParams CreateParams {
			get {
				var parms = base.CreateParams;
				parms.Style &= ~((int)WindowStyleFlags.WS_CLIPCHILDREN);  // Turn off WS_CLIPCHILDREN which prevents the screen to be cleared behind this form
				parms.ExStyle |= (int)ExtendedWindowStyleFlags.WS_EX_NOACTIVATE | (int)ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW | (int)ExtendedWindowStyleFlags.WS_EX_TOPMOST;
				//parms.ExStyle |= (int)ExtendedWindowStyleFlags.WS_EX_TRANSPARENT;
				parms.Parent = IntPtr.Zero;		// Make parentless
				return parms;
			}
		}

		/// <summary>
		/// Shows the control as a floating Window child 
		/// of the desktop.  To hide the control again,
		/// use the <see cref="Visible"/> property.
		/// </summary>
		public void ShowFloating() {
			if (this.Handle == IntPtr.Zero) {
				base.CreateControl();
			}
			User32.SetParent(base.Handle, IntPtr.Zero);
			User32.ShowWindow(base.Handle, ShowWindowCommand.Show);
		}

		/// <summary>
		/// Make the mouse events pass through
		/// </summary>
		/// <param name="m"></param>
		protected override void WndProc(ref Message m) {
			const int HTTRANSPARENT = (-1);
			switch (m.Msg) {
				case (int)WindowsMessages.WM_NCHITTEST:
					m.Result = (IntPtr) HTTRANSPARENT;
					break;
				default:
					base.WndProc(ref m);
					break;
			}
		}

		/// <summary>
		/// Sets the location of the mouse on the screen (using screen coordinates, which might differ from bitmap coordindates in a multi screen setup)
		/// </summary>
		public Point MouseLocation {
			set {
				Rectangle tl = new Rectangle(value.X - (distanceX + Width), value.Y - (distanceY + Height), Width, Height);
				Rectangle tr = new Rectangle(value.X + distanceX, value.Y - (distanceY + Height), Width, Height);
				Rectangle bl = new Rectangle(value.X - (distanceX + Width), value.Y + distanceY, Width, Height);
				Rectangle br = new Rectangle(value.X + distanceX, value.Y + distanceY, Width, Height);
				Rectangle screenBounds = Screen.GetBounds(value);
				if (screenBounds.Contains(br)) {
					this.Location = br.Location;
				} else if (screenBounds.Contains(bl)) {
					this.Location = bl.Location;
				} else if (screenBounds.Contains(tr)) {
					this.Location = tr.Location;
				} else {
					this.Location = tl.Location;
				}

				this.Invalidate();
			}
		}
		
		/// <summary>
		/// Gets or sets the location of the bitmap to be displayed in zoom (using bitmap coordinates, which might differ from screen coordinates in a multi screen setup)
		/// </summary>
		public Point ZoomLocation {
			get {
				return zoomLocation;
			}
			set {
				zoomLocation = value;
			}
			
		}

		/// <summary>
		/// The zoom property
		/// </summary>
		public int Zoom {
			get;
			set;
		}

		/// <summary>
		/// This makes sure there is no background painted, as we have complete "paint" control it doesn't make sense to do otherwise.
		/// </summary>
		/// <param name="pevent"></param>
		protected override void OnPaintBackground(PaintEventArgs pevent) {
		}

		/// <summary>
		/// Paint the zoom area and the cross
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e) {
			if (imageToZoom == null) {
				return;
			}
			Graphics graphics = e.Graphics;
			graphics.Clear(Color.Black);
			graphics.SmoothingMode = SmoothingMode.None;
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.CompositingQuality = CompositingQuality.HighSpeed;
			graphics.PixelOffsetMode = PixelOffsetMode.Half;
			Rectangle clipRectangle = e.ClipRectangle;
			float zoomFactor = (float)100 / (float)Zoom;

			int sourceWidth = (int)(Width * zoomFactor);
			int sourceHeight = (int)(Height * zoomFactor);
			Rectangle sourceRectangle = new Rectangle(ZoomLocation.X - (sourceWidth / 2) + 1, ZoomLocation.Y - (sourceHeight / 2) + 1, sourceWidth, sourceHeight);
			Rectangle destinationRectangle = new Rectangle(0, 0, Width, Height);
			//graphics.StretchBlt((Bitmap)imageToZoom, sourceRectangle, destinationRectangle);
			graphics.DrawImage(imageToZoom, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);

			int pixelThickness = Zoom / 100;
			using (Pen pen = new Pen(Color.Black, pixelThickness)) {
				int halfWidth = (Width >> 1) - (pixelThickness >> 1);
				int halfWidthEnd = (Width >> 1) - pixelThickness;
				int halfHeight = (Height >> 1) - (pixelThickness >> 1);
				int halfHeightEnd = (Height >> 1) - pixelThickness;
				graphics.DrawLine(pen, halfWidth, 0, halfWidth, halfHeightEnd);
				graphics.DrawLine(pen, halfWidth, halfHeightEnd + pixelThickness, halfWidth, Height);
				graphics.DrawLine(pen, 0, halfHeight, halfWidthEnd, halfHeight);
				graphics.DrawLine(pen, halfWidthEnd + pixelThickness, halfHeight, Width, halfHeight);
			}
		}

		private void InitializeComponent() {
			this.SuspendLayout();
			this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			// Only double-buffer when we are not in a TerminalServerSession
			this.DoubleBuffered = !System.Windows.Forms.SystemInformation.TerminalServerSession;
			this.Name = "Zoom";
			this.ResumeLayout(false);
		}
	}
}
