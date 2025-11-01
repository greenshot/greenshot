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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Gdi32.Structs;
using Dapplo.Windows.Icons;
using Dapplo.Windows.Icons.SafeHandles;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The Window Capture code
    /// </summary>
    public static class WindowCapture
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowCapture));
        private static readonly CoreConfiguration Configuration = IniConfig.GetIniSection<CoreConfiguration>();

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

            var cursorInfo = CursorInfo.Create();
            if (!NativeCursorMethods.GetCursorInfo(ref cursorInfo)) return capture;
            if (cursorInfo.Flags != CursorInfoFlags.Showing) return capture;

            using SafeIconHandle safeIcon = NativeIconMethods.CopyIcon(cursorInfo.CursorHandle);
            if (!NativeIconMethods.GetIconInfo(safeIcon, out var iconInfo)) return capture;

            NativePoint cursorLocation = User32Api.GetCursorLocation();
            // Align cursor location to Bitmap coordinates (instead of Screen coordinates)
            var x = cursorLocation.X - iconInfo.Hotspot.X - capture.ScreenBounds.X;
            var y = cursorLocation.Y - iconInfo.Hotspot.Y - capture.ScreenBounds.Y;
            // Set the location
            capture.CursorLocation = new NativePoint(x, y);

            using (Icon icon = Icon.FromHandle(safeIcon.DangerousGetHandle()))
            {
                capture.Cursor = icon;
            }
            iconInfo.BitmaskBitmapHandle.Dispose();
            iconInfo.ColorBitmapHandle.Dispose();
            return capture;
        }

        /// <summary>
        /// This method will call the CaptureRectangle with the screenbounds, therefor Capturing the whole screen.
        /// </summary>
        /// <returns>A Capture Object with the Screen as an Image</returns>
        public static ICapture CaptureScreen(ICapture capture)
        {
            if (capture == null)
            {
                capture = new Capture();
            }

            return CaptureRectangle(capture, capture.ScreenBounds);
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
        /// Helper method to check if it is allowed to capture the process using DWM
        /// </summary>
        /// <param name="process">Process owning the window</param>
        /// <returns>true if it's allowed</returns>
        public static bool IsDwmAllowed(Process process)
        {
            if (process == null) return true;
            if (Configuration.NoDWMCaptureForProduct == null ||
                Configuration.NoDWMCaptureForProduct.Count <= 0) return true;

            try
            {
                string productName = process.MainModule?.FileVersionInfo.ProductName;
                if (productName != null && Configuration.NoDWMCaptureForProduct.Contains(productName.ToLower()))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
            }

            return true;
        }

        /// <summary>
        /// Helper method to check if it is allowed to capture the process using GDI
        /// </summary>
        /// <param name="process">Process owning the window</param>
        /// <returns>true if it's allowed</returns>
        public static bool IsGdiAllowed(Process process)
        {
            if (process == null) return true;
            if (Configuration.NoGDICaptureForProduct == null ||
                Configuration.NoGDICaptureForProduct.Count <= 0) return true;

            try
            {
                string productName = process.MainModule?.FileVersionInfo.ProductName;
                if (productName != null && Configuration.NoGDICaptureForProduct.Contains(productName.ToLower()))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex.Message);
            }

            return true;
        }

        /// <summary>
        /// This method will use User32 code to capture the specified captureBounds from the screen
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

            Image capturedImage = null;
            // If the CaptureHandler has a handle use this, otherwise use the CaptureRectangle here
            if (CaptureHandler.CaptureScreenRectangle != null)
            {
                try
                {
                    capturedImage = CaptureHandler.CaptureScreenRectangle(captureBounds);
                }
                catch
                {
                    // ignored
                }
            }

            // If no capture, use the normal screen capture
            if (capturedImage == null)
            {
                capturedImage = CaptureRectangle(captureBounds);
            }

            capture.Image = capturedImage;
            capture.Location = captureBounds.Location;
            return capture.Image == null ? null : capture;
        }

        /// <summary>
        /// This method will use User32 code to capture the specified captureBounds from the screen
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

            capture.Image = CaptureRectangle(captureBounds);
            capture.Location = captureBounds.Location;
            return capture.Image == null ? null : capture;
        }

        /// <summary>
        /// This method will use User32 code to capture the specified captureBounds from the screen
        /// </summary>
        /// <param name="captureBounds">NativeRect with the bounds to capture</param>
        /// <returns>Bitmap which is captured from the screen at the location specified by the captureBounds</returns>
        public static Bitmap CaptureRectangle(NativeRect captureBounds)
        {
            Bitmap returnBitmap = null;
            if (captureBounds.Height <= 0 || captureBounds.Width <= 0)
            {
                Log.Warn("Nothing to capture, ignoring!");
                return null;
            }

            Log.Debug("CaptureRectangle Called!");

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
                            using Bitmap tmpBitmap = Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle());
                            // Create a new bitmap which has a transparent background
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
                        else
                        {
                            // All screens, which are inside the capture, are of equal size
                            // assign image to Capture, the image will be disposed there..
                            returnBitmap = Image.FromHbitmap(safeDibSectionHandle.DangerousGetHandle());
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