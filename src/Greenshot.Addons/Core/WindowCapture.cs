// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Gdi32.Structs;
using Dapplo.Windows.Icons;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;
using Greenshot.Gfx.Structs;

namespace Greenshot.Addons.Core
{
    /// <summary>
    ///     The Window Capture code
    /// </summary>
    public static class WindowCapture
    {
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        /// Set from DI via AddonsModule
        /// </summary>
        internal static ICoreConfiguration CoreConfiguration { get; set; }

        /// <summary>
        ///     Retrieves the cursor location safely, accounting for DPI settings in Vista/Windows 7. This implementation
        ///     can conveniently be used when the cursor location is needed to deal with a fullscreen bitmap.
        /// </summary>
        /// <returns>
        ///     NativePoint with cursor location, relative to the top left corner of the monitor setup (which itself might actually not
        ///     be on any screen)
        /// </returns>
        public static NativePoint GetCursorLocationRelativeToScreenBounds()
        {
            return GetLocationRelativeToScreenBounds(User32Api.GetCursorLocation());
        }

        /// <summary>
        ///     Converts locationRelativeToScreenOrigin to be relative to top left corner of all screen bounds, which might
        ///     be different in multi screen setups. This implementation
        ///     can conveniently be used when the cursor location is needed to deal with a fullscreen bitmap.
        /// </summary>
        /// <param name="locationRelativeToScreenOrigin">NativePoint</param>
        /// <returns>NativePoint</returns>
        public static NativePoint GetLocationRelativeToScreenBounds(NativePoint locationRelativeToScreenOrigin)
        {
            var bounds = DisplayInfo.ScreenBounds;
            return locationRelativeToScreenOrigin.Offset(-bounds.X, -bounds.Y);
        }

        /// <summary>
        ///     This method will capture the current Cursor by using User32 Code
        /// </summary>
        /// <returns>A Capture Object with the Mouse Cursor information in it.</returns>
        public static ICapture CaptureCursor(ICapture capture)
        {
            Log.Debug().WriteLine("Capturing the mouse cursor.");
            if (capture == null)
            {
                capture = new Capture();
            }

            var cursorInfo = CursorInfo.Create();
            if (!NativeCursorMethods.GetCursorInfo(ref cursorInfo))
            {
                return capture;
            }
            if (cursorInfo.Flags != CursorInfoFlags.Showing)
            {
                return capture;
            }
            using (var safeIcon = NativeIconMethods.CopyIcon(cursorInfo.CursorHandle))
            {
                if (!NativeIconMethods.GetIconInfo(safeIcon, out var iconInfo))
                {
                    return capture;
                }
                using (iconInfo.BitmaskBitmapHandle)
                using (iconInfo.ColorBitmapHandle)
                {
                    var cursorLocation = User32Api.GetCursorLocation();
                    // Align cursor location to Bitmap coordinates (instead of Screen coordinates)
                    var x = cursorLocation.X - iconInfo.Hotspot.X - capture.ScreenBounds.X;
                    var y = cursorLocation.Y - iconInfo.Hotspot.Y - capture.ScreenBounds.Y;
                    // Set the location
                    capture.CursorLocation = new NativePoint(x, y);

                    using (var icon = Icon.FromHandle(safeIcon.DangerousGetHandle()))
                    {
                        capture.Cursor = icon;
                    }
                }
            }
            return capture;
        }

        /// <summary>
        ///     This method will call the CaptureRectangle with the screen bounds, therefor Capturing the whole screen.
        /// </summary>
        /// <returns>A Capture Object with the Screen as an Image</returns>
        public static ICapture CaptureScreen(ICapture capture = null)
        {
            if (capture == null)
            {
                capture = new Capture();
            }
            return CaptureRectangle(capture, capture.ScreenBounds);
        }

        /// <summary>
        ///     Helper method to create an exception that might explain what is wrong while capturing
        /// </summary>
        /// <param name="method">string with current method</param>
        /// <param name="captureBounds">NativeRect of what we want to capture</param>
        /// <returns>Exception</returns>
        private static Exception CreateCaptureException(string method, NativeRect captureBounds)
        {
            var exceptionToThrow = User32Api.CreateWin32Exception(method);
            if (!captureBounds.IsEmpty)
            {
                exceptionToThrow.Data.Add("Height", captureBounds.Height);
                exceptionToThrow.Data.Add("Width", captureBounds.Width);
            }
            return exceptionToThrow;
        }

        /// <summary>
        ///     Helper method to check if it is allowed to capture the process using DWM
        /// </summary>
        /// <param name="process">Process owning the window</param>
        /// <returns>true if it's allowed</returns>
        public static bool IsDwmAllowed(Process process)
        {
            if (process == null)
            {
                return true;
            }

            if (CoreConfiguration.NoDWMCaptureForProduct == null || CoreConfiguration.NoDWMCaptureForProduct.Count <= 0)
            {
                return true;
            }

            try
            {
                var productName = process.MainModule.FileVersionInfo.ProductName;
                if (productName != null && CoreConfiguration.NoDWMCaptureForProduct.Contains(productName.ToLower()))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Warn().WriteLine(ex.Message);
            }
            return true;
        }

        /// <summary>
        ///     Helper method to check if it is allowed to capture the process using GDI
        /// </summary>
        /// <param name="process">Process owning the window</param>
        /// <returns>true if it's allowed</returns>
        public static bool IsGdiAllowed(Process process)
        {
            if (process == null)
            {
                return true;
            }

            if (CoreConfiguration.NoGDICaptureForProduct == null || CoreConfiguration.NoGDICaptureForProduct.Count <= 0)
            {
                return true;
            }

            try
            {
                var productName = process.MainModule.FileVersionInfo.ProductName;
                if (productName != null && CoreConfiguration.NoGDICaptureForProduct.Contains(productName.ToLower()))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Warn().WriteLine(ex.Message);
            }
            return true;
        }

        /// <summary>
        ///     This method will use User32 code to capture the specified captureBounds from the screen
        /// </summary>
        /// <param name="capture">ICapture where the captured Bitmap will be stored</param>
        /// <param name="captureBounds">NativeRect with the bounds to capture</param>
        /// <returns>A Capture Object with a part of the Screen as an Image</returns>
        public static ICapture CaptureRectangle(ICapture capture, NativeRect captureBounds)
        {
            if (capture == null)
            {
                capture = new Capture();
            }
            IBitmapWithNativeSupport capturedBitmap = null;
            // If the CaptureHandler has a handle use this, otherwise use the CaptureRectangle here
            if (CaptureHandler.CaptureScreenRectangle != null)
            {
                try
                {
                    capturedBitmap = CaptureHandler.CaptureScreenRectangle(captureBounds);
                }
                catch
                {
                    // ignored
                }
            }
            // If no capture, use the normal screen capture
            if (capturedBitmap == null)
            {
                capturedBitmap = CaptureRectangle(captureBounds);
            }
            capture.Bitmap = capturedBitmap;
            capture.Location = captureBounds.Location;
            return capture.Bitmap == null ? null : capture;
        }

        /// <summary>
        ///     This method will use User32 code to capture the specified captureBounds from the screen
        /// </summary>
        /// <param name="capture">ICapture where the captured Bitmap will be stored</param>
        /// <param name="captureBounds">NativeRect with the bounds to capture</param>
        /// <returns>A Capture Object with a part of the Screen as an Image</returns>
        public static ICapture CaptureRectangleFromDesktopScreen(ICapture capture, NativeRect captureBounds)
        {
            if (capture == null)
            {
                capture = new Capture();
            }
            capture.Bitmap = CaptureRectangle(captureBounds);
            capture.Location = captureBounds.Location;
            return capture.Bitmap == null ? null : capture;
        }

        /// <summary>
        ///     This method will use User32 code to capture the specified captureBounds from the screen
        /// </summary>
        /// <param name="captureBounds">NativeRect with the bounds to capture</param>
        /// <returns>Bitmap which is captured from the screen at the location specified by the captureBounds</returns>
        public static IBitmapWithNativeSupport CaptureRectangle(NativeRect captureBounds)
        {
            if (captureBounds.Height <= 0 || captureBounds.Width <= 0)
            {
                Log.Warn().WriteLine("Nothing to capture, ignoring!");
                return null;
            }
            Log.Debug().WriteLine("CaptureRectangle Called!");

            // .NET GDI+ Solution, according to some post this has a GDI+ leak...
            // See http://connect.microsoft.com/VisualStudio/feedback/details/344752/gdi-object-leak-when-calling-graphics-copyfromscreen
            // Bitmap capturedBitmap = new Bitmap(captureBounds.Width, captureBounds.Height);
            // using (Graphics graphics = Graphics.FromImage(capturedBitmap)) {
            //	graphics.CopyFromScreen(captureBounds.Location, NativePoint.Empty, captureBounds.Size, CopyPixelOperation.CaptureBlt);
            // }
            // capture.Image = capturedBitmap;
            // capture.Location = captureBounds.Location;

            using (var desktopDcHandle = SafeWindowDcHandle.FromDesktop())
            {
                if (desktopDcHandle.IsInvalid)
                {
                    // Get Exception before the error is lost
                    var exceptionToThrow = CreateCaptureException("desktopDCHandle", captureBounds);
                    // throw exception
                    throw exceptionToThrow;
                }

                // create a device context we can copy to
                using (var safeCompatibleDcHandle = Gdi32Api.CreateCompatibleDC(desktopDcHandle))
                {
                    // Check if the device context is there, if not throw an error with as much info as possible!
                    if (safeCompatibleDcHandle.IsInvalid)
                    {
                        // Get Exception before the error is lost
                        var exceptionToThrow = CreateCaptureException("CreateCompatibleDC", captureBounds);
                        // throw exception
                        throw exceptionToThrow;
                    }
                    // Create BitmapInfoHeader for CreateDIBSection
                    var bmi = BitmapInfoHeader.Create(captureBounds.Width, captureBounds.Height, 24);

                    // TODO: Enable when the function is available again
                    // Make sure the last error is set to 0
                    Win32.SetLastError(0);

                    // create a bitmap we can copy it to, using GetDeviceCaps to get the width/height
                    // the returned (out) IntPtr _ is not used for our purposes. It returns a pointer to the raw bits that make up the bitmap.
                    using (var safeDibSectionHandle = Gdi32Api.CreateDIBSection(desktopDcHandle, ref bmi, DibColors.PalColors, out _, IntPtr.Zero, 0))
                    {
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
                            // bit-blt over (make copy)
                            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                            Gdi32Api.BitBlt(safeCompatibleDcHandle, 0, 0, captureBounds.Width, captureBounds.Height, desktopDcHandle, captureBounds.X, captureBounds.Y,
                                RasterOperations.SourceCopy | RasterOperations.CaptureBlt);
                        }

                        // get a .NET image object for it
                        // A suggestion for the "A generic error occurred in GDI+." E_FAIL/0x80004005 error is to re-try...
                        ExternalException exception = null;
                        for (var i = 0; i < 3; i++)
                        {
                            try
                            {
                                // Collect all screens inside this capture
                                var screensInsideCapture = new List<Screen>();
                                foreach (var screen in Screen.AllScreens)
                                {
                                    if (screen.Bounds.IntersectsWith(captureBounds))
                                    {
                                        screensInsideCapture.Add(screen);
                                    }
                                }
                                // Check all all screens are of an equal size
                                bool offscreenContent;
                                using (var captureRegion = new Region(captureBounds))
                                {
                                    // Exclude every visible part
                                    foreach (var screen in screensInsideCapture)
                                    {
                                        captureRegion.Exclude(screen.Bounds);
                                    }
                                    // If the region is not empty, we have "offscreenContent"
                                    using (var screenGraphics = Graphics.FromHwnd(User32Api.GetDesktopWindow()))
                                    {
                                        offscreenContent = !captureRegion.IsEmpty(screenGraphics);
                                    }
                                }
                                // Check if we need to have a transparent background, needed for offscreen content
                                if (offscreenContent)
                                {
                                    using (var tmpBitmap = Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle()))
                                    {
                                        // Create a new bitmap which has a transparent background
                                        var returnBitmap = new UnmanagedBitmap<Bgra32>(tmpBitmap.Width, tmpBitmap.Height, tmpBitmap.HorizontalResolution, tmpBitmap.VerticalResolution);
                                        returnBitmap.Span.Fill(Color.Transparent.FromColorWithAlpha());
                                        // Content will be copied here
                                        using (var graphics = Graphics.FromImage(returnBitmap.NativeBitmap))
                                        {
                                            // For all screens copy the content to the new bitmap
                                            foreach (var screen in Screen.AllScreens)
                                            {
                                                // Make sure the bounds are with an offset to the capture bounds
                                                var screenBounds = screen.Bounds;
                                                screenBounds.Offset(-captureBounds.X, -captureBounds.Y);
                                                graphics.DrawImage(tmpBitmap, screenBounds, screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height, GraphicsUnit.Pixel);
                                            }
                                        }

                                        return returnBitmap;
                                    }
                                }
                                else
                                {
                                    // All screens, which are inside the capture, are of equal size
                                    // assign image to Capture, the image will be disposed there..
                                    // TODO: Optimize?
                                    return BitmapWrapper.FromBitmap(Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle()));
                                }
                            }
                            catch (ExternalException ee)
                            {
                                Log.Warn().WriteLine(ee, "Problem getting bitmap at try " + i + " : ");
                                exception = ee;
                            }
                        }
                        Log.Error().WriteLine(null, "Still couldn't create Bitmap!");
                        if (exception != null)
                        {
                            throw exception;
                        }
                    }
                }
            }

            return null;
        }
    }
}