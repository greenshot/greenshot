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

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Icons;
using Dapplo.Windows.Icons.SafeHandles;
using Dapplo.Windows.Messages.Enumerations;
using Dapplo.Windows.User32;
using Greenshot.Editor.Forms;
using ColorDialog = Greenshot.Editor.Forms.ColorDialog;

namespace Greenshot.Editor.Controls
{
    /// <summary>
    /// This code was supplied by Hi-Coder as a patch for Greenshot
    /// Needed some modifications to be stable.
    /// </summary>
    public sealed class Pipette : Label, IMessageFilter, IDisposable
    {
        private MovableShowColorForm _movableShowColorForm;
        private bool _dragging;
        private Cursor _cursor;
        private readonly Bitmap _image;
        private const int VkEsc = 27;

        public event EventHandler<PipetteUsedArgs> PipetteUsed;

        public Pipette()
        {
            BorderStyle = BorderStyle.FixedSingle;
            _dragging = false;
            _image = (Bitmap) new ComponentResourceManager(typeof(ColorDialog)).GetObject("pipette.Image");
            Image = _image;
            _cursor = CreateCursor(_image, 1, 14);
            _movableShowColorForm = new MovableShowColorForm();
            Application.AddMessageFilter(this);
        }

        /// <summary>
        /// Create a cursor from the supplied bitmap & hotspot coordinates
        /// </summary>
        /// <param name="bitmap">Bitmap to create an icon from</param>
        /// <param name="hotspotX">Hotspot X coordinate</param>
        /// <param name="hotspotY">Hotspot Y coordinate</param>
        /// <returns>Cursor</returns>
        private static Cursor CreateCursor(Bitmap bitmap, int hotspotX, int hotspotY)
        {
            using SafeIconHandle iconHandle = new SafeIconHandle(bitmap.GetHicon());
            NativeIconMethods.GetIconInfo(iconHandle, out var iconInfo);
            iconInfo.Hotspot = new NativePoint(hotspotX, hotspotY);
            iconInfo.IsIcon = false;
            var icon = NativeIconMethods.CreateIconIndirect(ref iconInfo);
            return new Cursor(icon);
        }

        /// <summary>
        /// The bulk of the clean-up code is implemented in Dispose(bool)
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// This Dispose is called from the Dispose and the Destructor.
        /// </summary>
        /// <param name="disposing">When disposing==true all non-managed resources should be freed too!</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_cursor != null)
                {
                    _cursor.Dispose();
                }

                _movableShowColorForm?.Dispose();
            }

            _movableShowColorForm = null;
            _cursor = null;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Handle the mouse down on the Pipette "label", we take the capture and move the zoomer to the current location
        /// </summary>
        /// <param name="e">MouseEventArgs</param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                User32Api.SetCapture(Handle);
                _movableShowColorForm.MoveTo(PointToScreen(new Point(e.X, e.Y)));
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// Handle the mouse up on the Pipette "label", we release the capture and fire the PipetteUsed event
        /// </summary>
        /// <param name="e">MouseEventArgs</param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                //Release Capture should consume MouseUp when canceled with the escape key 
                User32Api.ReleaseCapture();
                PipetteUsed?.Invoke(this, new PipetteUsedArgs(_movableShowColorForm.Color));
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// Handle the mouse Move event, we move the ColorUnderCursor to the current location.
        /// </summary>
        /// <param name="e">MouseEventArgs</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                //display the form on the right side of the cursor by default;
                Point zp = PointToScreen(new Point(e.X, e.Y));
                _movableShowColorForm.MoveTo(zp);
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// Handle the MouseCaptureChanged event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            if (Capture)
            {
                _dragging = true;
                Image = null;
                Cursor c = _cursor;
                Cursor = c;
                _movableShowColorForm.Visible = true;
            }
            else
            {
                _dragging = false;
                Image = _image;
                Cursor = Cursors.Arrow;
                _movableShowColorForm.Visible = false;
            }

            Update();
            base.OnMouseCaptureChanged(e);
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (_dragging)
            {
                if (m.Msg == (int) WindowsMessages.WM_CHAR)
                {
                    if ((int) m.WParam == VkEsc)
                    {
                        User32Api.ReleaseCapture();
                    }
                }
            }

            return false;
        }
    }

    public class PipetteUsedArgs : EventArgs
    {
        public Color Color;

        public PipetteUsedArgs(Color c)
        {
            Color = c;
        }
    }
}