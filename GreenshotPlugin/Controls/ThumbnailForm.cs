using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using System.Drawing;
using GreenshotPlugin.UnmanagedHelpers;

namespace GreenshotPlugin.Controls {
    public class ThumbnailForm : FormWithoutActivation {
        private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();

        private IntPtr thumbnailHandle = IntPtr.Zero;
        private Rectangle parentMenuBounds = Rectangle.Empty;

        public ThumbnailForm() {
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = false;
            Enabled = false;
            if (conf.WindowCaptureMode == WindowCaptureMode.Auto || conf.WindowCaptureMode == WindowCaptureMode.Aero) {
                BackColor = Color.FromArgb(255, conf.DWMBackgroundColor.R, conf.DWMBackgroundColor.G, conf.DWMBackgroundColor.B);
            } else {
                BackColor = Color.White;
            }

            // cleanup at close
            this.FormClosing += delegate {
                UnregisterThumbnail();
            };
        }

        public new void Hide() {
            UnregisterThumbnail();
            base.Hide();
        }

        private void UnregisterThumbnail() {
            if (thumbnailHandle != IntPtr.Zero) {
                DWM.DwmUnregisterThumbnail(thumbnailHandle);
                thumbnailHandle = IntPtr.Zero;
            }
        }

        public void ShowThumbnail(WindowDetails window, Control parentControl) {
            UnregisterThumbnail();

            DWM.DwmRegisterThumbnail(Handle, window.Handle, out thumbnailHandle);
            if (thumbnailHandle != IntPtr.Zero) {
                Rectangle windowRectangle = window.WindowRectangle;
                int thumbnailHeight = 200;
                int thumbnailWidth = (int)(thumbnailHeight * ((float)windowRectangle.Width / (float)windowRectangle.Height));
                if (parentControl != null && thumbnailWidth > parentControl.Width) {
                    thumbnailWidth = parentControl.Width;
                    thumbnailHeight = (int)(thumbnailWidth * ((float)windowRectangle.Height / (float)windowRectangle.Width));
                }
                Width = thumbnailWidth;
                Height = thumbnailHeight;
                // Prepare the displaying of the Thumbnail
                DWM_THUMBNAIL_PROPERTIES props = new DWM_THUMBNAIL_PROPERTIES();
                props.Opacity = (byte)255;
                props.Visible = true;
                props.SourceClientAreaOnly = false;
                props.Destination = new RECT(0, 0, thumbnailWidth, thumbnailHeight);
                DWM.DwmUpdateThumbnailProperties(thumbnailHandle, ref props);
                if (parentControl != null) {
                    AlignToControl(parentControl);
                }

                if (!Visible) {
                    Show();
                }
                // Make sure it's on "top"!
                if (parentControl != null) {
                    User32.SetWindowPos(Handle, parentControl.Handle, 0, 0, 0, 0, WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE | WindowPos.SWP_NOACTIVATE);
                }
            }
        }

        public void AlignToControl(Control alignTo) {
            Rectangle screenBounds = WindowCapture.GetScreenBounds();
            if (screenBounds.Contains(alignTo.Left, alignTo.Top - Height)) {
                Location = new Point(alignTo.Left + (alignTo.Width / 2) - (Width / 2), alignTo.Top - Height);
            } else {
                Location = new Point(alignTo.Left + (alignTo.Width / 2) - (Width / 2), alignTo.Bottom);
            }
        }
    }
}
