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
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dapplo.Log;
using Dapplo.Windows.App;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.DesktopWindowsManager.Structs;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.User32;
using Greenshot.Core.Configuration;
using Greenshot.Core.Enums;
using Greenshot.Core.Interfaces;
using Greenshot.Core.Sources;
using Greenshot.Gfx.Structs;
using Color = System.Drawing.Color;

namespace Greenshot.Core.Extensions
{
    /// <summary>
    ///     Greenshot versions of the extension methods for the InteropWindow
    /// </summary>
    public static class InteropWindowCaptureExtensions
    {
        private static readonly LogSource Log = new LogSource();
        private static readonly Bgra32 TransparentColor = new Bgra32
        {
            A = 255,
            R = 0,
            G = 0,
            B = 0
        };

        /// <summary>
        ///     Extension method to capture a bitmap of the screen area where the InteropWindow is located
        /// </summary>
        /// <param name="interopWindow">InteropWindow</param>
        /// <param name="clientBounds">true to use the client bounds</param>
        /// <returns>ICaptureElement</returns>
        public static ICaptureElement<BitmapSource> CaptureFromScreen(this IInteropWindow interopWindow, bool clientBounds = false)
        {
            var bounds = clientBounds ? interopWindow.GetInfo().ClientBounds: interopWindow.GetInfo().Bounds;
            var result = ScreenSource.CaptureRectangle(bounds);
            return result;
        }

        /// <summary>
        ///     Return an Image representing the Window!
        ///     As GDI+ draws it, it will be without Aero borders!
        ///     TODO: If there is a parent, this could be removed with SetParent, and set back afterwards.
        /// </summary>
        /// <returns>ICaptureElement</returns>
        public static ICaptureElement<BitmapSource> PrintWindow(this IInteropWindow interopWindow)
        {
            var returnBitmap = interopWindow.PrintWindow<BitmapSource>();
            if (interopWindow.HasParent || !interopWindow.IsMaximized())
            {
                return new CaptureElement<BitmapSource>(interopWindow.GetInfo().Bounds.Location, returnBitmap);
            }
            Log.Debug().WriteLine("Correcting for maximalization");
            var borderSize = interopWindow.GetInfo().BorderSize;
            var bounds = interopWindow.GetInfo().Bounds;
            var borderRectangle = new NativeRect(borderSize.Width, borderSize.Height, bounds.Width - 2 * borderSize.Width, bounds.Height - 2 * borderSize.Height);
            return new CaptureElement<BitmapSource>(interopWindow.GetInfo().Bounds.Location, new CroppedBitmap(returnBitmap, borderRectangle));
        }


        /// <summary>
        ///     Helper method to check if it is allowed to capture the process using GDI
        /// </summary>
        /// <param name="process">Process owning the window</param>
        /// <param name="captureConfiguration">ICaptureConfiguration</param>
        /// <returns>true if it's allowed</returns>
        public static bool IsGdiAllowed(Process process, ICaptureConfiguration captureConfiguration)
        {
            if (process == null)
            {
                return true;
            }

            if (captureConfiguration.NoGDICaptureForProduct == null || captureConfiguration.NoGDICaptureForProduct.Count <= 0)
            {
                return true;
            }

            try
            {
                var productName = process.MainModule.FileVersionInfo.ProductName;
                if (productName != null && captureConfiguration.NoGDICaptureForProduct.Contains(productName.ToLower()))
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
        ///     Capture DWM Window, this needs needs to be async to prevent Application.DoEvents (which is horribe)
        /// </summary>
        /// <param name="interopWindow">IInteropWindow</param>
        /// <param name="captureConfiguration">ICaptureConfiguration configuration for the settings</param>
        /// <returns>ICaptureElement with the capture</returns>
        public static async ValueTask<ICaptureElement<BitmapSource>> CaptureDwmWindow(this IInteropWindow interopWindow, ICaptureConfiguration captureConfiguration)
        {
            // The capture
            ICaptureElement<BitmapSource> capturedBitmap = null;
            var thumbnailHandle = IntPtr.Zero;
            Form tempForm = null;
            var tempFormShown = false;
            try
            {
                tempForm = new Form
                {
                    ShowInTaskbar = false,
                    FormBorderStyle = FormBorderStyle.None,
                    TopMost = true
                };

                // Register the Thumbnail
                Dwm.DwmRegisterThumbnail(tempForm.Handle, interopWindow.Handle, out thumbnailHandle);

                // Get the original size
                Dwm.DwmQueryThumbnailSourceSize(thumbnailHandle, out var sourceSize);

                if (sourceSize.Width <= 0 || sourceSize.Height <= 0)
                {
                    return null;
                }

                // Calculate the location of the temp form
                var windowRectangle = interopWindow.GetInfo().Bounds;
                var formLocation = windowRectangle.Location;
                var borderSize = new Size();
                var doesCaptureFit = false;
                if (!interopWindow.IsMaximized())
                {
                    // Assume using it's own location
                    formLocation = windowRectangle.Location;
                    using (var workingArea = new Region())
                    {
                        // Find the screen where the window is and check if it fits
                        foreach (var displayInfo in DisplayInfo.AllDisplayInfos)
                        {
                            workingArea.Union(displayInfo.WorkingArea);
                        }

                        // If the formLocation is not inside the visible area
                        if (!workingArea.AreRectangleCornersVisisble(windowRectangle))
                        {
                            // If none found we find the biggest screen
                            foreach (var displayInfo in DisplayInfo.AllDisplayInfos)
                            {
                                var newWindowRectangle = new NativeRect(displayInfo.WorkingArea.Location, windowRectangle.Size);
                                if (!workingArea.AreRectangleCornersVisisble(newWindowRectangle))
                                {
                                    continue;
                                }

                                formLocation = displayInfo.Bounds.Location;
                                doesCaptureFit = true;
                                break;
                            }
                        }
                        else
                        {
                            doesCaptureFit = true;
                        }
                    }
                }
                else if (!WindowsVersion.IsWindows8OrLater)
                {
                    //GetClientRect(out windowRectangle);
                    borderSize = interopWindow.GetInfo().BorderSize;
                    formLocation = new NativePoint(windowRectangle.X - borderSize.Width, windowRectangle.Y - borderSize.Height);
                }

                tempForm.Location = formLocation;
                tempForm.Size = sourceSize;

                // Prepare rectangle to capture from the screen.
                var captureRectangle = new NativeRect(formLocation.X, formLocation.Y, sourceSize.Width, sourceSize.Height);
                if (interopWindow.IsMaximized())
                {
                    // Correct capture size for maximized window by offsetting the X,Y with the border size
                    // and subtracting the border from the size (2 times, as we move right/down for the capture without resizing)
                    captureRectangle = captureRectangle.Inflate(borderSize.Width, borderSize.Height);
                }
                else
                {
                    // TODO: Also 8.x?
                    if (WindowsVersion.IsWindows10)
                    {
                        captureRectangle = captureRectangle.Inflate(captureConfiguration.Win10BorderCrop.Width, captureConfiguration.Win10BorderCrop.Height);
                    }

                    if (captureConfiguration.WindowCaptureMode == WindowCaptureModes.Auto)
                    {
                        // check if the capture fits
                        if (!doesCaptureFit)
                        {
                            // if GDI is allowed.. (a screenshot won't be better than we comes if we continue)
                            using (var thisWindowProcess = Process.GetProcessById(interopWindow.GetProcessId()))
                            {
                                if (!interopWindow.IsApp() && IsGdiAllowed(thisWindowProcess, captureConfiguration))
                                {
                                    // we return null which causes the capturing code to try another method.
                                    return null;
                                }
                            }
                        }
                    }
                }
                // Prepare the displaying of the Thumbnail
                var props = new DwmThumbnailProperties
                {
                    Opacity = 255,
                    Visible = true,
                    Destination = new NativeRect(0, 0, sourceSize.Width, sourceSize.Height)
                };
                Dwm.DwmUpdateThumbnailProperties(thumbnailHandle, ref props);
                tempForm.Show();
                tempFormShown = true;

                // Intersect with screen
                captureRectangle = captureRectangle.Intersect(DisplayInfo.ScreenBounds);

                // Check if we make a transparent capture
                if (captureConfiguration.WindowCaptureMode == WindowCaptureModes.AeroTransparent)
                {
                    // Use white, later black to capture transparent
                    tempForm.BackColor = Color.White;
                    // Make sure everything is visible
                    tempForm.Refresh();
                    await Task.Yield();

                    try
                    {
                        var whiteBitmap = ScreenSource.CaptureRectangle(captureRectangle);
                        // Apply a white color
                        tempForm.BackColor = Color.Black;
                        // Make sure everything is visible
                        tempForm.Refresh();
                        // Make sure the application window is active, so the colors & buttons are right
                        await Task.Yield();
                        var blackBitmap = ScreenSource.CaptureRectangle(captureRectangle);
                        capturedBitmap = ApplyTransparency(blackBitmap, whiteBitmap);
                    }
                    catch (Exception e)
                    {
                        Log.Warn().WriteLine(e, "Exception: ");
                        // Some problem occurred, cleanup and make a normal capture
                        capturedBitmap = null;
                    }
                }
                // If no capture up till now, create a normal capture.
                if (capturedBitmap == null)
                {
                    // Remove transparency, this will break the capturing
                    if (captureConfiguration.WindowCaptureMode != WindowCaptureModes.Auto)
                    {
                        tempForm.BackColor = Color.FromArgb(255, captureConfiguration.DWMBackgroundColor.R, captureConfiguration.DWMBackgroundColor.G, captureConfiguration.DWMBackgroundColor.B);
                    }
                    else
                    {
                        var colorizationColor = Dwm.ColorizationSystemDrawingColor;
                        // Modify by losing the transparency and increasing the intensity (as if the background color is white)
                        colorizationColor = Color.FromArgb(255, (colorizationColor.R + 255) >> 1, (colorizationColor.G + 255) >> 1, (colorizationColor.B + 255) >> 1);
                        tempForm.BackColor = colorizationColor;
                    }
                    // Make sure everything is visible
                    tempForm.Refresh();
                    if (!interopWindow.IsApp())
                    {
                        // Make sure the application window is active, so the colors & buttons are right
                        await interopWindow.ToForegroundAsync();
                    }
                    // Make sure all changes are processed and visible
                    await Task.Yield();
                    // Capture from the screen
                    capturedBitmap = ScreenSource.CaptureRectangle(captureRectangle);
                }
/*                if (capturedBitmap != null)
                {
                    // Not needed for Windows 8
                    if (!WindowsVersion.IsWindows8OrLater)
                    {
                        // Only if the Inivalue is set, not maximized and it's not a tool window.
                        if (captureConfiguration.WindowCaptureRemoveCorners && !interopWindow.IsMaximized() && !interopWindow.GetInfo().ExtendedStyle.HasFlag(ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW))
                        {
                            // Remove corners
                            if (!Image.IsAlphaPixelFormat(capturedBitmap.PixelFormat))
                            {
                                Log.Debug().WriteLine("Changing pixelformat to Alpha for the RemoveCorners");
                                var tmpBitmap = capturedBitmap.CloneBitmap(PixelFormat.Format32bppArgb) as Bitmap;
                                capturedBitmap.Dispose();
                                capturedBitmap = tmpBitmap;
                            }
                            RemoveCorners(capturedBitmap);
                        }
                    }
                }*/
            }
            finally
            {
                if (thumbnailHandle != IntPtr.Zero)
                {
                    // Unregister (cleanup), as we are finished we don't need the form or the thumbnail anymore
                    Dwm.DwmUnregisterThumbnail(thumbnailHandle);
                }
                if (tempForm != null)
                {
                    if (tempFormShown)
                    {
                        tempForm.Close();
                    }
                    tempForm.Dispose();
                    tempForm = null;
                }
            }

            return capturedBitmap;
        }

/*        /// <summary>
        ///     Helper method to remove the corners from a DMW capture
        /// </summary>
        /// <param name="image">The bitmap to remove the corners from.</param>
        private static void RemoveCorners(Bitmap image)
        {
            using (var fastBitmap = FastBitmapFactory.Create(image))
            {
                for (var y = 0; y < CoreConfiguration.WindowCornerCutShape.Count; y++)
                {
                    for (var x = 0; x < CoreConfiguration.WindowCornerCutShape[y]; x++)
                    {
                        fastBitmap.SetColorAt(x, y, ref _transparentColor);
                        fastBitmap.SetColorAt(image.Width - 1 - x, y, ref _transparentColor);
                        fastBitmap.SetColorAt(image.Width - 1 - x, image.Height - 1 - y, ref _transparentColor);
                        fastBitmap.SetColorAt(x, image.Height - 1 - y, ref _transparentColor);
                    }
                }
            }
        }*/

        /// <summary>
        ///     Apply transparency by comparing a transparent capture with a black and white background
        ///     A "Math.min" makes sure there is no overflow, but this could cause the picture to have shifted colors.
        ///     The pictures should have been taken without differency, except for the colors.
        /// </summary>
        /// <param name="blackBitmap">ICaptureElement with the black image</param>
        /// <param name="whiteBitmap">ICaptureElement with the white image</param>
        /// <returns>ICaptureElement with transparency</returns>
        private static ICaptureElement<BitmapSource> ApplyTransparency(ICaptureElement<BitmapSource> blackBitmap, ICaptureElement<BitmapSource> whiteBitmap)
        {
            var blackBuffer = new WriteableBitmap(blackBitmap.Content);
            var whiteBuffer = new WriteableBitmap(whiteBitmap.Content);
            var blackBitmapSource = blackBitmap.Content;
            var result = new WriteableBitmap((int)blackBitmapSource.Width, (int)blackBitmapSource.Height, blackBitmapSource.DpiX, blackBitmapSource.DpiY, PixelFormats.Bgra32, null);

            try
            {
                result.Lock();
                blackBuffer.Lock();
                whiteBuffer.Lock();

                unsafe
                {
                    var blackPixels = (Bgra32*)blackBuffer.BackBuffer;
                    var whitePixels = (Bgra32*)whiteBuffer.BackBuffer;
                    var target = (Bgra32*) result.BackBuffer;
                    for (var y = 0; y < blackBuffer.Height; y++)
                    {
                        for (var x = 0; x < blackBuffer.Width; x++)
                        {
                            ref var c0 = ref blackPixels[x];
                            ref var c1 = ref whitePixels[x];
                            // Calculate alpha as double in range 0-1
                            var alpha = c0.R - c1.R + 255;
                            if (alpha == 255)
                            {
                                // Alpha == 255 means no change!
                                target[x] = c0;
                            }
                            else if (alpha == 0)
                            {
                                // Complete transparency, use transparent pixel
                                target[x] = TransparentColor;
                            }
                            else
                            {
                                // Calculate original color
                                var originalAlpha = (byte) Math.Min(255, alpha);
                                var alphaFactor = alpha / 255d;
                                //Log.Debug().WriteLine("Alpha {0} & c0 {1} & c1 {2}", alpha, c0, c1);
                                var originalRed = (byte) Math.Min(255, c0.R / alphaFactor);
                                var originalGreen = (byte) Math.Min(255, c0.G / alphaFactor);
                                var originalBlue = (byte) Math.Min(255, c0.B / alphaFactor);

                                var originalColor = new Bgra32()
                                {
                                    A = originalAlpha,
                                    R = originalRed,
                                    G = originalGreen,
                                    B = originalBlue
                                };
                                //Color originalColor = Color.FromArgb(originalAlpha, originalRed, c0.G, c0.B);
                                target[x] = originalColor;
                            }

                        }
                        blackPixels += blackBuffer.BackBufferStride;
                        whitePixels += whiteBuffer.BackBufferStride;
                        target += result.BackBufferStride;
                    }
                }
            }
            finally
            {
                result.Unlock();
                blackBuffer.Unlock();
                whiteBuffer.Unlock();
            }
            return new CaptureElement<BitmapSource>(blackBitmap.Bounds.Location, result);
        }
    }
}