/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
namespace Greenshot.Forms {
	partial class CaptureForm {
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent() {
			this.SuspendLayout();
			// 
			// pictureBox
			// 
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnMouseMove);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnMouseDown);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.OnPaint);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnMouseUp);
			// 
			// CaptureForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.ClientSize = new System.Drawing.Size(0, 0);
			this.Cursor = System.Windows.Forms.Cursors.Cross;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.Name = "CaptureForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.TopMost = true;
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CaptureFormKeyDown);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.CaptureFormKeyUp);
			this.Visible = false;
			this.ResumeLayout(false);
		}
	}
}
