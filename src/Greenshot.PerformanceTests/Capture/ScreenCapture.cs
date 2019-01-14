#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

#endregion

#region Usings

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Dapplo.Log;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.Gdi32.Structs;
using Greenshot.Addons.Core;

#endregion

namespace Greenshot.PerformanceTests.Capture
{
    /// <summary>
    ///     The screen Capture code
    /// </summary>
    public class ScreenCapture : IDisposable
    {
        private readonly SafeWindowDcHandle _desktopDcHandle;
        private readonly SafeCompatibleDcHandle _safeCompatibleDcHandle;
        private readonly bool _useStretch;
        private readonly SafeDibSectionHandle _safeDibSectionHandle;
        private readonly SafeSelectObjectHandle _safeSelectObjectHandle;

        /// <summary>
        /// Return the source rectangle
        /// </summary>
        public NativeRect SourceRect { get; }

        /// <summary>
        /// Return the source rectangle
        /// </summary>
        public NativeSize DestinationSize { get; }

        public ScreenCapture(NativeRect sourceCaptureBounds, NativeSize? requestedSize = null)
        {
            SourceRect = sourceCaptureBounds;

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

            _desktopDcHandle = SafeWindowDcHandle.FromDesktop();
            _safeCompatibleDcHandle = Gdi32Api.CreateCompatibleDC(_desktopDcHandle);
            // Create BitmapInfoHeader for CreateDIBSection
            var bitmapInfoHeader = BitmapInfoHeader.Create(DestinationSize.Width, DestinationSize.Height, 32);

            _safeDibSectionHandle = Gdi32Api.CreateDIBSection(_desktopDcHandle, ref bitmapInfoHeader, DibColors.PalColors, out _, IntPtr.Zero, 0);

            // select the bitmap object and store the old handle
            _safeSelectObjectHandle = _safeCompatibleDcHandle.SelectObject(_safeDibSectionHandle);
        }

        public void CaptureFrame()
        {
            if (_useStretch)
            {
                // capture & blt over (make copy)
                Gdi32Api.StretchBlt(
                    _safeCompatibleDcHandle, 0, 0, DestinationSize.Width, DestinationSize.Height, // Destination
                    _desktopDcHandle, SourceRect.X, SourceRect.Y, SourceRect.Width, SourceRect.Height, // source
                    RasterOperations.SourceCopy | RasterOperations.CaptureBlt);
            }
            else
            {
                // capture & blt over (make copy)
                Gdi32Api.BitBlt(
                    _safeCompatibleDcHandle, 0, 0, DestinationSize.Width, DestinationSize.Height, // Destination
                    _desktopDcHandle, SourceRect.X, SourceRect.Y, // Source
                    RasterOperations.SourceCopy | RasterOperations.CaptureBlt);
            }
        }

        /// <summary>
        /// Get the current frame as BitmapSource
        /// </summary>
        /// <returns>BitmapSource</returns>
        public BitmapSource CurrentFrameAsBitmapSource()
        {
            return Imaging.CreateBitmapSourceFromHBitmap(_safeDibSectionHandle.DangerousGetHandle(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        /// <summary>
        /// Get the current frame as Bitmap
        /// </summary>
        /// <returns>Bitmap</returns>
        public Bitmap CurrentFrameAsBitmap()
        {
            return Image.FromHbitmap(_safeDibSectionHandle.DangerousGetHandle());
        }

        public void Dispose()
        {
            _safeSelectObjectHandle.Dispose();
            _safeDibSectionHandle.Dispose();
            _safeCompatibleDcHandle.Dispose();
            _desktopDcHandle.Dispose();
        }
    }
}