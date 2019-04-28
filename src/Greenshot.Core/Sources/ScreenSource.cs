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
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Gdi32.Structs;
using Dapplo.Windows.User32;
using Greenshot.Core.Enums;
using Greenshot.Core.Interfaces;
using Greenshot.Gfx.Extensions;

namespace Greenshot.Core.Sources
{
    /// <summary>
    /// This does the screen capture
    /// </summary>
    public class ScreenSource : ISource<BitmapSource>
    {
        public ValueTask<ICaptureElement<BitmapSource>> Import(CancellationToken cancellationToken = default)
        {
            var screenbounds = DisplayInfo.ScreenBounds;
            var result = CaptureRectangle(screenbounds);
            return new ValueTask<ICaptureElement<BitmapSource>>(result);
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
            if (captureBounds.IsEmpty)
            {
                return exceptionToThrow;
            }

            exceptionToThrow.Data.Add("Height", captureBounds.Height);
            exceptionToThrow.Data.Add("Width", captureBounds.Width);
            return exceptionToThrow;
        }

        /// <summary>
        /// This captures the screen at the specified location
        /// </summary>
        /// <param name="captureBounds">NativeRect</param>
        /// <returns>ICaptureElement</returns>
        internal static ICaptureElement<BitmapSource> CaptureRectangle(NativeRect captureBounds)
        {
            BitmapSource capturedBitmapSource;
            if (captureBounds.IsEmpty)
            {
                return null;
            }

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
                    // Create BITMAPINFOHEADER for CreateDIBSection
                    var bmi = BitmapInfoHeader.Create(captureBounds.Width, captureBounds.Height, 24);

                    // Make sure the last error is set to 0
                    Win32.SetLastError(0);

                    // create a bitmap we can copy it to, using GetDeviceCaps to get the width/height
                    using (var safeDibSectionHandle = Gdi32Api.CreateDIBSection(desktopDcHandle, ref bmi, 0, out var _, IntPtr.Zero, 0))
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
                            // bitblt over (make copy)
                            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                            Gdi32Api.BitBlt(safeCompatibleDcHandle, 0, 0, captureBounds.Width, captureBounds.Height, desktopDcHandle, captureBounds.X, captureBounds.Y,
                                RasterOperations.SourceCopy | RasterOperations.CaptureBlt);
                        }

                        // Create BitmapSource from the DibSection
                        capturedBitmapSource = Imaging.CreateBitmapSourceFromHBitmap(safeDibSectionHandle.DangerousGetHandle(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

                        // Now cut away invisible parts

                        // Collect all screens inside this capture
                        var displaysInsideCapture = new List<DisplayInfo>();
                        foreach (var displayInfo in DisplayInfo.AllDisplayInfos)
                        {
                            if (displayInfo.Bounds.IntersectsWith(captureBounds))
                            {
                                displaysInsideCapture.Add(displayInfo);
                            }
                        }
                        // Check all all screens are of an equal size
                        bool offscreenContent;
                        using (var captureRegion = new Region(captureBounds))
                        {
                            // Exclude every visible part
                            foreach (var displayInfo in displaysInsideCapture)
                            {
                                captureRegion.Exclude(displayInfo.Bounds);
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
                            var modifiedImage = new WriteableBitmap(capturedBitmapSource.PixelWidth, capturedBitmapSource.PixelHeight, capturedBitmapSource.DpiX, capturedBitmapSource.DpiY, PixelFormats.Bgr32, capturedBitmapSource.Palette);
                            foreach (var displayInfo in DisplayInfo.AllDisplayInfos)
                            {
                                modifiedImage.CopyPixels(capturedBitmapSource, displayInfo.Bounds);
                            }

                            capturedBitmapSource = modifiedImage;
                        }
                    }
                }
            }
            var result = new CaptureElement<BitmapSource>(captureBounds.Location, capturedBitmapSource)
            {
                ElementType = CaptureElementType.Screen
            };
            return result;
        }
    }
}
