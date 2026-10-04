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

using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Icons;
using Dapplo.Windows.User32;
using Greenshot.Base.Capturing;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Captures the screen, a part of it, a window or the cursor into an ICapture. The pixels come from ScreenCapture.
    /// </summary>
    public static class WindowCapture
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowCapture));

        /// <summary>
        /// Retrieves the cursor location safely, accounting for DPI settings in Vista/Windows 7. This implementation
        /// can conveniently be used when the cursor location is needed to deal with a fullscreen bitmap.
        /// </summary>
        /// <returns>
        /// Point with cursor location, relative to the top left corner of the monitor setup (which itself might actually not be on any screen)
        /// </returns>
        public static NativePoint GetCursorLocationRelativeToScreenBounds()
        {
            return GetLocationRelativeToScreenBounds(User32Api.GetCursorLocation());
        }

        /// <summary>
        /// Converts locationRelativeToScreenOrigin to be relative to top left corner of all screen bounds, which might
        /// be different in multi-screen setups. This implementation
        /// can conveniently be used when the cursor location is needed to deal with a fullscreen bitmap.
        /// </summary>
        /// <param name="locationRelativeToScreenOrigin"></param>
        /// <returns>Point</returns>
        public static NativePoint GetLocationRelativeToScreenBounds(NativePoint locationRelativeToScreenOrigin)
        {
            NativeRect bounds = DisplayInfo.ScreenBounds;
            return locationRelativeToScreenOrigin.Offset(-bounds.X, -bounds.Y);
        }

        /// <summary>
        /// This method will capture the current Cursor by using User32 Code
        /// </summary>
        /// <returns>A Capture Object with the Mouse Cursor information in it.</returns>
        public static ICapture CaptureCursor(ICapture capture)
        {
            Log.Debug("Capturing the mouse cursor.");
            if (capture == null)
            {
                capture = new Capture();
            }

            CapturedCursor capturedCursor;
            if (CursorHelper.TryGetCurrentCursor(out capturedCursor))
            {
                NativePoint cursorLocation = User32Api.GetCursorLocation();
                // Align cursor location to Bitmap coordinates (instead of Screen coordinates)
                NativePoint origin = (capture.Image != null)
                    ? capture.Location
                    : capture.ScreenBounds.Location;
                var x = cursorLocation.X - capturedCursor.HotSpot.X - origin.X;
                var y = cursorLocation.Y - capturedCursor.HotSpot.Y - origin.Y;
                // Set the location
                capture.CursorLocation = new NativePoint(x, y);
                capture.Cursor = capturedCursor;
            }
            return capture;
        }

        /// <summary>
        /// This method will call the CaptureRectangle with the screenbounds, therefore Capturing the whole screen.
        /// </summary>
        /// <returns>A Capture Object with the Screen as an Image</returns>
        public static Task<ICapture> CaptureScreenAsync(ICapture capture, CancellationToken cancellationToken = default)
        {
            if (capture == null)
            {
                capture = new Capture();
            }

            return CaptureRectangleAsync(capture, capture.ScreenBounds, cancellationToken);
        }

        /// <summary>
        /// Capture the area of the screen
        /// </summary>
        /// <param name="capture">ICapture where the captured Bitmap will be stored</param>
        /// <param name="captureBounds">NativeRect with the bounds to capture</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>A Capture Object with a part of the Screen as an Image, null when nothing could be captured</returns>
        public static async Task<ICapture> CaptureRectangleAsync(ICapture capture, NativeRect captureBounds, CancellationToken cancellationToken = default)
        {
            capture ??= new Capture();

            var result = await ScreenCapture.CaptureRectangleAsync(captureBounds, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return null;
            }

            capture.Image = result.Image;
            capture.Location = result.Location;
            capture.CaptureDetails.AddMetaData(ScreenCapture.CaptureMethodKey, result.Backend.Name);
            return capture;
        }

        /// <summary>
        /// Select the window to capture, resolving linked windows for special applications (e.g. TOAD, Excel).
        /// </summary>
        public static WindowDetails SelectCaptureWindow(WindowDetails windowToCapture)
        {
            if (windowToCapture == null) return null;

            NativeRect windowRectangle = windowToCapture.WindowRectangle;
            if (windowRectangle.Width == 0 || windowRectangle.Height == 0)
            {
                Log.WarnFormat("Window {0} has nothing to capture, using workaround to find other window of same process.", windowToCapture.Text);
                return WindowDetails.GetLinkedWindow(windowToCapture);
            }

            return windowToCapture;
        }

        /// <summary>
        /// Capture the window: Windows.Graphics.Capture, or what is displayed in the window's area when that is not possible.
        /// </summary>
        /// <param name="windowToCapture">WindowDetails</param>
        /// <param name="capture">ICapture to fill, null to create one</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>ICapture, null when nothing could be captured</returns>
        public static async Task<ICapture> CaptureWindowAsync(WindowDetails windowToCapture, ICapture capture = null, CancellationToken cancellationToken = default)
        {
            capture ??= new Capture();

            var result = await ScreenCapture.CaptureWindowAsync(windowToCapture, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return null;
            }

            capture.Image = result.Image;
            capture.Location = result.Location;
            capture.CaptureDetails.Title = windowToCapture.Text;
            capture.CaptureDetails.AddMetaData(ScreenCapture.CaptureMethodKey, result.Backend.Name);
            return capture;
        }
    }
}
