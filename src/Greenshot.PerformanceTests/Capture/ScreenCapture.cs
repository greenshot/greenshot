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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
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
using Greenshot.Addons.Config.Impl;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

#endregion

namespace Greenshot.Addons.Core
{
    /// <summary>
    ///     The screen Capture code
    /// </summary>
    public class ScreenCapture : IDisposable
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ICoreConfiguration _coreConfiguration;
        private readonly NativeRect _captureBounds;
        private readonly SafeWindowDcHandle _desktopDcHandle;
        private readonly SafeCompatibleDcHandle _safeCompatibleDcHandle;
        private readonly BitmapInfoHeader _bitmapInfoHeader;
        private readonly SafeDibSectionHandle _safeDibSectionHandle;
        private readonly SafeSelectObjectHandle _safeSelectObjectHandle;

        public ScreenCapture(ICoreConfiguration coreConfiguration, NativeRect captureBounds)
        {
            _coreConfiguration = coreConfiguration;
            _captureBounds = captureBounds;
            _desktopDcHandle = SafeWindowDcHandle.FromDesktop();
            _safeCompatibleDcHandle = Gdi32Api.CreateCompatibleDC(_desktopDcHandle);
            // Create BitmapInfoHeader for CreateDIBSection
            _bitmapInfoHeader = BitmapInfoHeader.Create(captureBounds.Width, captureBounds.Height, 24);

            _safeDibSectionHandle = Gdi32Api.CreateDIBSection(_desktopDcHandle, ref _bitmapInfoHeader, DibColors.PalColors, out _, IntPtr.Zero, 0);

            // select the bitmap object and store the old handle
            _safeSelectObjectHandle = _safeCompatibleDcHandle.SelectObject(_safeDibSectionHandle);
        }

        public void CaptureFrame()
        {
                // bit-blt over (make copy)
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                Gdi32Api.BitBlt(_safeCompatibleDcHandle, 0, 0, _captureBounds.Width, _captureBounds.Height, _desktopDcHandle, _captureBounds.X, _captureBounds.Y,
                    RasterOperations.SourceCopy | RasterOperations.CaptureBlt);

                //return Imaging.CreateBitmapSourceFromHBitmap(_safeDibSectionHandle.DangerousGetHandle(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
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