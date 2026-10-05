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
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.DesktopWindowsManager.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Greenshot.Base.Core;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// A live (DWM) thumbnail of a window, shown next to a menu when hovering a window to capture.
    /// It never gets the focus or the mouse; positions and sizes are in screen pixels, as the menus (WPF and WinForms) report them.
    /// Must be used on the UI thread.
    /// </summary>
    public sealed class ThumbnailWindow : Window
    {
        private const int ThumbnailHeight = 200;
        private IntPtr _thumbnailHandle = IntPtr.Zero;

        public ThumbnailWindow()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            ShowActivated = false;
            Focusable = false;
            IsHitTestVisible = false;
            Topmost = false;
            WindowStartupLocation = WindowStartupLocation.Manual;
            Background = Brushes.White;
            Title = "Greenshot thumbnail";

            SourceInitialized += (sender, args) =>
            {
                // Like a disabled tool window: no activation, not in Alt+Tab, no mouse input
                var handle = Handle;
                var exStyle = (ExtendedWindowStyleFlags)User32Api.GetWindowLongWrapper(handle, WindowLongIndex.GWL_EXSTYLE);
                exStyle |= ExtendedWindowStyleFlags.WS_EX_NOACTIVATE | ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW;
                User32Api.SetWindowLongWrapper(handle, WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint)exStyle));
                var style = (WindowStyleFlags)User32Api.GetWindowLongWrapper(handle, WindowLongIndex.GWL_STYLE).ToInt64();
                User32Api.SetWindowLongWrapper(handle, WindowLongIndex.GWL_STYLE, new IntPtr((long)(style | WindowStyleFlags.WS_DISABLED)));
            };
            Closed += (sender, args) => UnregisterThumbnail();
        }

        private IntPtr Handle => new WindowInteropHelper(this).EnsureHandle();

        /// <summary>
        /// Hide the thumbnail, it can be shown again
        /// </summary>
        public new void Hide()
        {
            UnregisterThumbnail();
            base.Hide();
        }

        private void UnregisterThumbnail()
        {
            if (_thumbnailHandle == IntPtr.Zero)
            {
                return;
            }

            DwmApi.DwmUnregisterThumbnail(_thumbnailHandle);
            _thumbnailHandle = IntPtr.Zero;
        }

        /// <summary>
        /// Show the thumbnail of the window above (or under) the bounds of a menu
        /// </summary>
        /// <param name="window">WindowDetails</param>
        /// <param name="alignTo">The screen bounds (in pixels) to align to, null to keep the location</param>
        /// <param name="insertAfter">The window the thumbnail is placed on top of, IntPtr.Zero for none</param>
        public void ShowThumbnail(WindowDetails window, NativeRect? alignTo, IntPtr insertAfter)
        {
            UnregisterThumbnail();

            var handle = Handle;
            DwmApi.DwmRegisterThumbnail(handle, window.Handle, out _thumbnailHandle);
            if (_thumbnailHandle == IntPtr.Zero)
            {
                return;
            }

            var result = DwmApi.DwmQueryThumbnailSourceSize(_thumbnailHandle, out var sourceSize);
            if (result.Failed() || sourceSize.IsEmpty)
            {
                UnregisterThumbnail();
                return;
            }

            int thumbnailHeight = ThumbnailHeight;
            int thumbnailWidth = (int)(thumbnailHeight * (sourceSize.Width / (float)sourceSize.Height));
            if (alignTo.HasValue && thumbnailWidth > alignTo.Value.Width)
            {
                thumbnailWidth = alignTo.Value.Width;
                thumbnailHeight = (int)(thumbnailWidth * (sourceSize.Height / (float)sourceSize.Width));
            }

            var flags = WindowPos.SWP_NOACTIVATE | WindowPos.SWP_NOZORDER;
            int x = 0, y = 0;
            if (alignTo.HasValue)
            {
                var location = CalculateLocation(alignTo.Value, thumbnailWidth, thumbnailHeight);
                x = location.X;
                y = location.Y;
            }
            else
            {
                flags |= WindowPos.SWP_NOMOVE;
            }

            // Twice: moving to a monitor with another DPI makes WPF scale the window, the second call sets the pixel size again
            User32Api.SetWindowPos(handle, IntPtr.Zero, x, y, thumbnailWidth, thumbnailHeight, flags);
            User32Api.SetWindowPos(handle, IntPtr.Zero, x, y, thumbnailWidth, thumbnailHeight, flags);

            var dwmThumbnailProperties = new DwmThumbnailProperties
            {
                Opacity = 255,
                Visible = true,
                SourceClientAreaOnly = false,
                Destination = new NativeRect(0, 0, thumbnailWidth, thumbnailHeight)
            };
            result = DwmApi.DwmUpdateThumbnailProperties(_thumbnailHandle, ref dwmThumbnailProperties);
            if (result.Failed())
            {
                UnregisterThumbnail();
                return;
            }

            if (!IsVisible)
            {
                Show();
                // WPF may apply its own size and location when it shows the window the first time
                User32Api.SetWindowPos(handle, IntPtr.Zero, x, y, thumbnailWidth, thumbnailHeight, flags);
            }

            // Make sure it's on "top"!
            if (insertAfter != IntPtr.Zero)
            {
                User32Api.SetWindowPos(handle, insertAfter, 0, 0, 0, 0, WindowPos.SWP_NOMOVE | WindowPos.SWP_NOSIZE | WindowPos.SWP_NOACTIVATE);
            }
        }

        /// <summary>
        /// Centered above the bounds, or under them when there is no room above
        /// </summary>
        private static NativePoint CalculateLocation(NativeRect alignTo, int width, int height)
        {
            int x = alignTo.Left + (alignTo.Width / 2) - (width / 2);
            var screenBounds = DisplayInfo.ScreenBounds;
            if (screenBounds.Contains(new NativePoint(alignTo.Left, alignTo.Top - height)))
            {
                return new NativePoint(x, alignTo.Top - height);
            }

            return new NativePoint(x, alignTo.Bottom);
        }
    }
}
