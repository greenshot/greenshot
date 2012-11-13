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

namespace Greenshot.Forms {
	/// <summary>
	/// This form will show the area around the mouse of the current capture
	/// </summary>
	public class ZoomForm : FormWithoutActivation {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ZoomForm));
		private ICapture captureToZoom = null;
		private Point mouseLocation = Point.Empty;

		public ZoomForm(ICapture captureToZoom) {
			InitializeComponent();
			this.captureToZoom = captureToZoom;
			Zoom = 400;
		}

		public Point MouseLocation {
			get {
				return mouseLocation;
			}
			set {
				mouseLocation = value;
				this.Location = new Point(mouseLocation.X + 20, mouseLocation.Y + 20);
				this.Invalidate();
			}
		}

		public int Zoom {
			get;
			set;
		}

		protected override void OnPaint(PaintEventArgs e) {
			if (captureToZoom == null || captureToZoom.Image == null) {
				return;
			}
			Graphics graphics = e.Graphics;
			graphics.SmoothingMode = SmoothingMode.None;
			graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			graphics.CompositingQuality = CompositingQuality.HighSpeed;
			graphics.PixelOffsetMode = PixelOffsetMode.None;
			Rectangle clipRectangle = e.ClipRectangle;
			float zoom = (float)100 / (float)Zoom;

			int sourceWidth = (int)(Width * zoom);
			int sourceHeight = (int)(Height * zoom);
			Rectangle sourceRectangle = new Rectangle(MouseLocation.X - (sourceHeight / 2), MouseLocation.Y - (sourceHeight / 2), sourceWidth, sourceHeight);
			Rectangle destinationRectangle = new Rectangle(0, 0, Width, Height);
			graphics.DrawImage(captureToZoom.Image, destinationRectangle, sourceRectangle, GraphicsUnit.Pixel);

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
			// 
			// ZoomForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.ClientSize = new System.Drawing.Size(100, 100);
			this.ControlBox = false;
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new Size(50, 50);
			this.Name = "Zoom";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.TopMost = true;
			this.ResumeLayout(false);
		}
	}
}
