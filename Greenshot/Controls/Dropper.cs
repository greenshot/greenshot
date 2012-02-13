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
using System.Text;
using System.Windows.Forms;
using Greenshot.Plugin;
using Greenshot.Forms;
using System.Drawing;
using GreenshotPlugin.UnmanagedHelpers;

namespace Greenshot.Controls {
	/// <summary>
	/// This code was supplied by Hi-Coder as a patch for Greenshot
	/// Needed some modifications to be stable.
	/// </summary>
    public class Dropper : Label, IMessageFilter, IDisposable {
        private Zoomer zoomer;
        private bool dragging;
        private Cursor _cursor;
		private Bitmap _image;
        private const int VK_ESC = 27;

        public event EventHandler<DropperUsedArgs> DropperUsed;

        public Dropper() {
            BorderStyle = BorderStyle.FixedSingle;
            dragging = false;
			_image = (Bitmap)new System.ComponentModel.ComponentResourceManager(typeof(ColorDialog)).GetObject("dropper.Image");
			_cursor = CreateCursor((Bitmap)_image, 0, 15);
            zoomer = new Zoomer();
            zoomer.Visible = false;
            Application.AddMessageFilter(this);
        }

		/**
		 * Destructor
		 */
		~Dropper() {
			Dispose(false);
		}

		// The bulk of the clean-up code is implemented in Dispose(bool)

		/**
		 * This Dispose is called from the Dispose and the Destructor.
		 * When disposing==true all non-managed resources should be freed too!
		 */
		protected override void Dispose(bool disposing) {
			base.Dispose();
			if (disposing) {
				if (_cursor != null) {
					_cursor.Dispose();
				}
				if (zoomer != null) {
					zoomer.Dispose();
				}
			}
			zoomer = null;
			_cursor = null;
		}

		protected override void OnMouseDown(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                User32.SetCapture(this.Handle);
                zoomer.setHotSpot(PointToScreen(new Point(e.X, e.Y)));
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                //Release Capture should consume MouseUp when canceled with the escape key 
                User32.ReleaseCapture();
                DropperUsed(this, new DropperUsedArgs(zoomer.color));
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            if (dragging) {
                //display the form on the right side of the cursor by default;
                Point zp = PointToScreen(new Point(e.X, e.Y));
                zoomer.setHotSpot(zp);
            }
            base.OnMouseMove(e);
        }

        private Cursor CreateCursor(Bitmap bitmap, int x, int y) {
            IntPtr iconHandle = bitmap.GetHicon();
			IntPtr icon;
            IconInfo iconInfo = new IconInfo();
			User32.GetIconInfo(iconHandle, out iconInfo);
            iconInfo.xHotspot = 0;
            iconInfo.yHotspot = 15;
            iconInfo.fIcon = false;
            icon = User32.CreateIconIndirect(ref iconInfo);
			Cursor returnCursor = new Cursor(icon);
			//User32.DestroyIcon(icon);
			User32.DestroyIcon(iconHandle);
            return returnCursor;
        }

        protected override void OnMouseCaptureChanged(EventArgs e) {
            if (this.Capture) {
                dragging = true;
                Image = null;
                Cursor c = _cursor;
                Cursor = c;
                zoomer.Visible = true;
            } else {
                dragging = false;

                Image = _image;
                Cursor = Cursors.Arrow;
                zoomer.Visible = false;
            }
            base.OnMouseCaptureChanged(e);
        }

        #region IMessageFilter Members

        public bool PreFilterMessage(ref Message m) {
            if (dragging) {
				if (m.Msg == (int)WindowsMessages.WM_CHAR) {
					if ((int)m.WParam == VK_ESC) {
						User32.ReleaseCapture();
					}
				}
            }
            return false;
        }

        #endregion
    }

    public class DropperUsedArgs : EventArgs {
        public Color color;

        public DropperUsedArgs(Color c) {
            color = c;
        }
    }
}
