/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.DesktopWindowsManager.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Greenshot.Base.Core;

namespace Greenshot.Base.Controls
{
    /// <summary>
    /// This form allows us to show a Thumbnail preview of a window near the context menu when selecting a window to capture.
    /// Didn't make it completely "generic" yet, but at least most logic is in here so we don't have it in the tray menu.
    /// </summary>
    public sealed class ThumbnailForm : FormWithoutActivation
    {
        private IntPtr _thumbnailHandle = IntPtr.Zero;

        public ThumbnailForm()
        {
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = false;
            Enabled = false;
            BackColor = Color.White;

            // cleanup at close
            FormClosing += delegate { UnregisterThumbnail(); };
        }

        public new void Hide()
        {
            UnregisterThumbnail();
            base.Hide();
        }

        private void UnregisterThumbnail()
        {
            if (_thumbnailHandle == IntPtr.Zero) return;

            DwmApi.DwmUnregisterThumbnail(_thumbnailHandle);
            _thumbnailHandle = IntPtr.Zero;
        }

        /// <summary>
        /// Show the thumbnail of the supplied window above (or under) the parent Control
        /// </summary>
        /// <param name="window">WindowDetails</param>
        /// <param name="parentControl">Control</param>
        public void ShowThumbnail(WindowDetails window, Control parentControl)
        {
            if (parentControl == null)
            {
                ShowThumbnail(window, null, IntPtr.Zero);
                return;
            }

            ShowThumbnail(window, new NativeRect(parentControl.Left, parentControl.Top, parentControl.Width, parentControl.Height), parentControl.Handle);
        }

        /// <summary>
        /// Show the thumbnail of the supplied window above (or under) the bounds of a window, e.g. a WPF menu
        /// </summary>
        /// <param name="window">WindowDetails</param>
        /// <param name="alignTo">The screen bounds (in pixels) to align to, null to keep the location</param>
        /// <param name="insertAfter">The window the thumbnail is placed on top of, IntPtr.Zero for none</param>
        public void ShowThumbnail(WindowDetails window, NativeRect? alignTo, IntPtr insertAfter)
        {
            UnregisterThumbnail();

            DwmApi.DwmRegisterThumbnail(Handle, window.Handle, out _thumbnailHandle);
            if (_thumbnailHandle == IntPtr.Zero) return;

            var result = DwmApi.DwmQueryThumbnailSourceSize(_thumbnailHandle, out var sourceSize);
            if (result.Failed())
            {
                DwmApi.DwmUnregisterThumbnail(_thumbnailHandle);
                return;
            }

            if (sourceSize.IsEmpty)
            {
                DwmApi.DwmUnregisterThumbnail(_thumbnailHandle);
                return;
            }

            int thumbnailHeight = 200;
            int thumbnailWidth = (int) (thumbnailHeight * (sourceSize.Width / (float) sourceSize.Height));
            if (alignTo.HasValue && thumbnailWidth > alignTo.Value.Width)
            {
                thumbnailWidth = alignTo.Value.Width;
                thumbnailHeight = (int) (thumbnailWidth * (sourceSize.Height / (float) sourceSize.Width));
            }

            Width = thumbnailWidth;
            Height = thumbnailHeight;
            // Prepare the displaying of the Thumbnail
            var dwmThumbnailProperties = new DwmThumbnailProperties()
            {
                Opacity = 255,
                Visible = true,
                SourceClientAreaOnly = false,
                Destination = new NativeRect(0, 0, thumbnailWidth, thumbnailHeight)
            };
            result = DwmApi.DwmUpdateThumbnailProperties(_thumbnailHandle, ref dwmThumbnailProperties);
            if (result.Failed())
            {
                DwmApi.DwmUnregisterThumbnail(_thumbnailHandle);
                return;
            }

            if (alignTo.HasValue)
            {
                AlignTo(alignTo.Value);
            }

            if (!Visible)
            {
                Show();
            }

            // Make sure it's on "top"!
            if (insertAfter != IntPtr.Zero)
            {
                User32Api.SetWindowPos(Handle, insertAfter, 0, 0, 0, 0, WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE | WindowPos.SWP_NOACTIVATE);
            }
        }

        public void AlignToControl(Control alignTo)
        {
            AlignTo(new NativeRect(alignTo.Left, alignTo.Top, alignTo.Width, alignTo.Height));
        }

        /// <summary>
        /// Place the thumbnail centered above the bounds, or under them when there is no room above
        /// </summary>
        /// <param name="alignTo">Screen bounds in pixels</param>
        public void AlignTo(NativeRect alignTo)
        {
            var screenBounds = DisplayInfo.ScreenBounds;
            if (screenBounds.Contains(alignTo.Left, alignTo.Top - Height))
            {
                Location = new Point(alignTo.Left + (alignTo.Width / 2) - (Width / 2), alignTo.Top - Height);
            }
            else
            {
                Location = new Point(alignTo.Left + (alignTo.Width / 2) - (Width / 2), alignTo.Bottom);
            }
        }
    }
}