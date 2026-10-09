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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Gdi32.Structs;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Base.Capturing
{
    /// <summary>
    /// The GDI fallback: copies what is displayed (BitBlt from the desktop), a window capture is the window's area of the screen
    /// including whatever covers it. Works everywhere, also where Windows.Graphics.Capture can't (e.g. some remote sessions).
    /// </summary>
    public sealed class GdiCaptureBackend : IScreenCaptureBackend
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(GdiCaptureBackend));

        /// <summary>
        /// The name of this backend, as it is stored in the capture metadata
        /// </summary>
        public const string BackendName = "Gdi";

        /// <inheritdoc />
        public string Name => BackendName;

        /// <inheritdoc />
        public bool IsAvailable => true;

        /// <inheritdoc />
        public bool CapturesWindowContentOnly => false;

        /// <inheritdoc />
        public Task<Bitmap> CaptureRectangleAsync(NativeRect captureBounds, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CopyFromScreen(captureBounds));
        }

        /// <inheritdoc />
        public async Task<Bitmap> CaptureWindowAsync(IInteropWindow window, CancellationToken cancellationToken = default)
        {
            // What is on the screen is captured, so the window has to be visible and in front, a minimized window is restored
            await window.ToForegroundAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            // Read the bounds again, they changed when the window was restored
            var windowRectangle = window.GetInfo(forceUpdate: true).Bounds.Intersect(DisplayInfo.ScreenBounds);
            return CopyFromScreen(windowRectangle);
        }

        /// <summary>
        /// Helper method to create an exception that might explain what is wrong while capturing
        /// </summary>
        /// <param name="method">string with current method</param>
        /// <param name="captureBounds">NativeRect of what we want to capture</param>
        /// <returns></returns>
        private static Exception CreateCaptureException(string method, NativeRect captureBounds)
        {
            Exception exceptionToThrow = User32Api.CreateWin32Exception(method);
            if (!captureBounds.IsEmpty)
            {
                exceptionToThrow.Data.Add("Height", captureBounds.Height);
                exceptionToThrow.Data.Add("Width", captureBounds.Width);
            }

            return exceptionToThrow;
        }

        /// <summary>
        /// This method will use User32 code to capture the specified captureBounds from the screen
        /// </summary>
        /// <param name="captureBounds">NativeRect with the bounds to capture</param>
        /// <returns>Bitmap which is captured from the screen at the location specified by the captureBounds</returns>
        public static Bitmap CopyFromScreen(NativeRect captureBounds)
        {
            Bitmap returnBitmap = null;
            if (captureBounds.Height <= 0 || captureBounds.Width <= 0)
            {
                Log.Warn("Nothing to capture, ignoring!");
                return null;
            }

            Log.Debug("CopyFromScreen called");

            // .NET GDI+ Solution, according to some post this has a GDI+ leak...
            // See https://connect.microsoft.com/VisualStudio/feedback/details/344752/gdi-object-leak-when-calling-graphics-copyfromscreen
            // Bitmap capturedBitmap = new Bitmap(captureBounds.Width, captureBounds.Height);
            // using (Graphics graphics = Graphics.FromImage(capturedBitmap)) {
            //    graphics.CopyFromScreen(captureBounds.Location, Point.Empty, captureBounds.Size, CopyPixelOperation.CaptureBlt);
            // }
            // capture.Image = capturedBitmap;
            // capture.Location = captureBounds.Location;

            using (var desktopDcHandle = SafeWindowDcHandle.FromDesktop())
            {
                if (desktopDcHandle.IsInvalid)
                {
                    // Get Exception before the error is lost
                    Exception exceptionToThrow = CreateCaptureException("desktopDCHandle", captureBounds);
                    // throw exception
                    throw exceptionToThrow;
                }

                // create a device context we can copy to
                using SafeCompatibleDcHandle safeCompatibleDcHandle = Gdi32Api.CreateCompatibleDC(desktopDcHandle);
                // Check if the device context is there, if not throw an error with as much info as possible!
                if (safeCompatibleDcHandle.IsInvalid)
                {
                    // Get Exception before the error is lost
                    Exception exceptionToThrow = CreateCaptureException("CreateCompatibleDC", captureBounds);
                    // throw exception
                    throw exceptionToThrow;
                }

                // Create BITMAPINFOHEADER for CreateDIBSection
                var bitmapInfoHeader = BitmapV5Header.Create(captureBounds.Width, captureBounds.Height, 24);

                // Make sure the last error is set to 0
                Kernel32Api.SetLastError(0);

                // create a bitmap we can copy it to, using GetDeviceCaps to get the width/height
                using SafeDibSectionHandle safeDibSectionHandle = Gdi32Api.CreateDIBSection(desktopDcHandle, ref bitmapInfoHeader, DibColors.RgbColors, out _, IntPtr.Zero, 0);
                if (safeDibSectionHandle.IsInvalid)
                {
                    // Get Exception before the error is lost
                    var exceptionToThrow = CreateCaptureException("CreateDIBSection", captureBounds);
                    exceptionToThrow.Data.Add("hdcDest", safeCompatibleDcHandle.DangerousGetHandle().ToInt32());
                    exceptionToThrow.Data.Add("hdcSrc", desktopDcHandle.DangerousGetHandle().ToInt32());

                    // Throw so people can report the problem
                    throw exceptionToThrow;
                }

                // select the bitmap object and store the old handle
                using (safeCompatibleDcHandle.SelectObject(safeDibSectionHandle))
                {
                    // bitblt over (make copy)
                    // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                    Gdi32Api.BitBlt(safeCompatibleDcHandle, 0, 0, captureBounds.Width, captureBounds.Height, desktopDcHandle, captureBounds.X, captureBounds.Y,
                        RasterOperations.SourceCopy | RasterOperations.CaptureBlt);
                }

                // get a .NET image object for it
                // A suggestion for the "A generic error occurred in GDI+." E_FAIL/0�80004005 error is to re-try...
                bool success = false;
                ExternalException exception = null;
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        // Collect all screens inside this capture
                        List<Screen> screensInsideCapture = new List<Screen>();
                        foreach (Screen screen in Screen.AllScreens)
                        {
                            if (screen.Bounds.IntersectsWith(captureBounds))
                            {
                                screensInsideCapture.Add(screen);
                            }
                        }

                        // Check all all screens are of an equal size
                        bool offscreenContent;
                        using (Region captureRegion = new Region(captureBounds))
                        {
                            // Exclude every visible part
                            foreach (Screen screen in screensInsideCapture)
                            {
                                captureRegion.Exclude(screen.Bounds);
                            }

                            // If the region is not empty, we have "offscreenContent"
                            using Graphics screenGraphics = Graphics.FromHwnd(User32Api.GetDesktopWindow());
                            offscreenContent = !captureRegion.IsEmpty(screenGraphics);
                        }

                        // Check if we need to have a transparent background, needed for offscreen content
                        if (offscreenContent)
                        {
                            Bitmap tmpBitmap = Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle());
                            if (tmpBitmap.Width <= 0 || tmpBitmap.Height <= 0)
                            {
                                Log.Warn($"DIB section returned degenerate bitmap dimensions ({tmpBitmap.Width}x{tmpBitmap.Height}), skipping offscreen capture.");
                                returnBitmap = tmpBitmap;
                                success = true;
                                break;
                            }
                            // Create a new bitmap which has a transparent background
                            using (tmpBitmap)
                            {
                                returnBitmap = ImageHelper.CreateEmpty(tmpBitmap.Width, tmpBitmap.Height, PixelFormat.Format32bppArgb, Color.Transparent, tmpBitmap.HorizontalResolution, tmpBitmap.VerticalResolution);
                                // Content will be copied here
                                using Graphics graphics = Graphics.FromImage(returnBitmap);
                                // For all screens copy the content to the new bitmap

                                foreach (var displayInfo in DisplayInfo.AllDisplayInfos)
                                {
                                    // Make sure the bounds are offset to the capture bounds
                                    var displayBounds = displayInfo.Bounds.Offset(-captureBounds.X, -captureBounds.Y);
                                    graphics.DrawImage(tmpBitmap, displayBounds, displayBounds.X, displayBounds.Y, displayBounds.Width, displayBounds.Height, GraphicsUnit.Pixel);
                                }
                            }
                        }
                        else
                        {
                            // All screens, which are inside the capture, are of equal size
                            // assign image to Capture, the image will be disposed there..
                            returnBitmap = Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle());
                            if (returnBitmap.Width <= 0 || returnBitmap.Height <= 0)
                            {
                                Log.Warn($"DIB section returned degenerate bitmap dimensions ({returnBitmap.Width}x{returnBitmap.Height}), skipping capture.");
                                success = true;
                                break;
                            }
                        }

                        // We got through the capture without exception
                        success = true;
                        break;
                    }
                    catch (ExternalException ee)
                    {
                        Log.Warn("Problem getting bitmap at try " + i + " : ", ee);
                        exception = ee;
                    }
                    catch (ArgumentException ae)
                    {
                        Log.Warn("Invalid bitmap dimensions at try " + i + " : ", ae);
                        exception = new System.Runtime.InteropServices.ExternalException(ae.Message, ae);
                    }
                }

                if (!success)
                {
                    Log.Error("Still couldn't create Bitmap!");
                    if (exception != null)
                    {
                        throw exception;
                    }
                }
            }

            return returnBitmap;
        }
    }
}
