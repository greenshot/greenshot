#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.App;
using Dapplo.Windows.Common;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Icons;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.User32.Enums;
using Greenshot.Gfx;
using Greenshot.Gfx.FastBitmap;
using GreenshotPlugin.Core.Enums;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;

#endregion

namespace GreenshotPlugin.Core
{
    /// <summary>
    ///     Greenshot versions of the extension methods for the InteropWindow
    /// </summary>
    public static class InteropWindowExtensions
    {
        /// <summary>
        /// Returns the intersection of two windows
        /// </summary>
        /// <param name="window1">IInteropWindow</param>
        /// <param name="window2">IInteropWindow</param>
        /// <returns>NativeRect</returns>
        public static NativeRect Intersection(this IInteropWindow window1, IInteropWindow window2)
        {
            var nativeRect1 = window1.GetInfo().Bounds;
            var nativeRect2 = window2.GetInfo().Bounds;
            return nativeRect1.Intersect(nativeRect2);
        }

        /// <summary>
        /// Returns the intersection of a window and a NativeRect
        /// </summary>
        /// <param name="window1">IInteropWindow</param>
        /// <param name="nativeRect2">NativeRect</param>
        /// <returns>NativeRect</returns>
        public static NativeRect Intersection(this IInteropWindow window1, NativeRect nativeRect2)
        {
            var nativeRect1 = window1.GetInfo().Bounds;
            return nativeRect1.Intersect(nativeRect2);
        }

        /// <summary>
        /// Returns the number of pixels in the NativeRect
        /// </summary>
        /// <param name="nativeRect">NativeRect</param>
        /// <returns>long</returns>
        public static long Pixels(this NativeRect nativeRect)
        {
            return nativeRect.Width * nativeRect.Height;
        }
    }
}