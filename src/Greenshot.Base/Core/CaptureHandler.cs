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

using System.Drawing;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// This is the method signature which is used to capture a rectangle from the screen.
    /// </summary>
    /// <param name="captureBounds">NativeRect</param>
    /// <returns>Captured Bitmap</returns>
    public delegate Bitmap CaptureScreenRectangleHandler(NativeRect captureBounds);

    /// <summary>
    /// This is a hack to experiment with different screen capture routines
    /// </summary>
    public static class CaptureHandler
    {
        /// <summary>
        /// By changing this value, null is default
        /// </summary>
        public static CaptureScreenRectangleHandler CaptureScreenRectangle { get; set; }
    }
}