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
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Gdi32.Structs;
using Dapplo.Windows.User32;
using Greenshot.Gfx;
using Greenshot.Gfx.Structs;

namespace Greenshot.Addons.Core
{
    /// <summary>
    ///     This allows us to repeatedly capture the screen via GDI
    /// </summary>
    public class BitmapScreenCapture : IDisposable
    {
        private readonly bool _useStretch;
        private bool _hasFrame;
        private readonly SafeWindowDcHandle _desktopDcHandle;
        private readonly SafeCompatibleDcHandle _safeCompatibleDcHandle;
        private readonly SafeDibSectionHandle _safeDibSectionHandle;
        private readonly SafeSelectObjectHandle _safeSelectObjectHandle;
        private readonly IBitmapWithNativeSupport _bitmap;

        /// <summary>
        /// Return the source rectangle
        /// </summary>
        public NativeRect SourceRect { get; }

        /// <summary>
        /// Return the source rectangle
        /// </summary>
        public NativeSize DestinationSize { get; }

        /// <summary>
        /// The constructor
        /// </summary>
        /// <param name="sourceCaptureBounds">NativeRect, optional, with the source area from the screen</param>
        /// <param name="requestedSize">NativeSize, optional, specifying the resulting size</param>
        public BitmapScreenCapture(NativeRect? sourceCaptureBounds = null, NativeSize? requestedSize = null)
        {
            SourceRect = sourceCaptureBounds ?? DisplayInfo.ScreenBounds;

            // Check if a size was specified, if this differs we need to stretch / scale
            if (requestedSize.HasValue && requestedSize.Value != SourceRect.Size)
            {
                DestinationSize = requestedSize.Value;
                _useStretch = true;
            }
            else
            {
                DestinationSize = SourceRect.Size;
                _useStretch = false;
            }

            // Get a Device Context for the desktop
            _desktopDcHandle = SafeWindowDcHandle.FromDesktop();

            // Create a Device Context which is compatible with the desktop Device Context
            _safeCompatibleDcHandle = Gdi32Api.CreateCompatibleDC(_desktopDcHandle);
            // Create BitmapInfoHeader, which is later used in the CreateDIBSection 
            var bitmapInfoHeader = BitmapInfoHeader.Create(DestinationSize.Width, DestinationSize.Height, 32);
            // Create a DibSection, a device-independent bitmap (DIB)
            _safeDibSectionHandle = Gdi32Api.CreateDIBSection(_desktopDcHandle, ref bitmapInfoHeader, DibColors.RgbColors, out var bits, IntPtr.Zero, 0);

            // select the device-independent bitmap in the device context, storing the previous.
            // This is needed, so every interaction with the DC will go into the DIB.
            _safeSelectObjectHandle = _safeCompatibleDcHandle.SelectObject(_safeDibSectionHandle);
            
            // Create a wrapper around the bitmap data 
            _bitmap = new UnmanagedBitmap<Bgr32>(bits, DestinationSize.Width, DestinationSize.Height);
        }

        /// <summary>
        /// Capture a frame from the screen
        /// </summary>
        public void CaptureFrame()
        {
            if (_useStretch)
            {
                // capture from source and blt over (make copy) to the DIB (via the DC)
                // use stretching as the source and destination have different sizes
                Gdi32Api.StretchBlt(
                    _safeCompatibleDcHandle, 0, 0, DestinationSize.Width, DestinationSize.Height, // Destination
                    _desktopDcHandle, SourceRect.X, SourceRect.Y, SourceRect.Width, SourceRect.Height, // source
                    RasterOperations.SourceCopy | RasterOperations.CaptureBlt);
            }
            else
            {
                // capture from source and blt over (make copy) to the DIB (via the DC)
                Gdi32Api.BitBlt(
                    _safeCompatibleDcHandle, 0, 0, DestinationSize.Width, DestinationSize.Height, // Destination
                    _desktopDcHandle, SourceRect.X, SourceRect.Y, // Source
                    RasterOperations.SourceCopy | RasterOperations.CaptureBlt);
            }

            _hasFrame = true;
        }

        /// <summary>
        /// Get the frame, captured with the previous CaptureFrame call
        /// </summary>
        /// <returns>IBitmapWithNativeSupport</returns>
        public IBitmapWithNativeSupport CurrentFrameAsBitmap() => _hasFrame ? _bitmap : null;

        /// <summary>
        /// Dispose all DC, DIB, handles etc
        /// </summary>
        public void Dispose()
        {
            _safeSelectObjectHandle.Dispose();
            _safeDibSectionHandle.Dispose();
            _safeCompatibleDcHandle.Dispose();
            _desktopDcHandle.Dispose();
        }
 
    }
}