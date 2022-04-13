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
using System.Drawing;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// The interface to the Capture object, so Plugins can use it.
    /// </summary>
    public interface ICapture : IDisposable
    {
        /// <summary>
        /// The Capture Details
        /// </summary>
        ICaptureDetails CaptureDetails { get; set; }

        /// <summary>
        /// The captured Image
        /// </summary>
        Image Image { get; set; }

        /// <summary>
        /// Null the image
        /// </summary>
        void NullImage();

        /// <summary>
        /// Bounds on the screen from which the capture comes
        /// </summary>
        NativeRect ScreenBounds { get; set; }

        /// <summary>
        /// The cursor
        /// </summary>
        Icon Cursor { get; set; }

        /// <summary>
        /// Boolean to specify if the cursor is available
        /// </summary>
        bool CursorVisible { get; set; }

        /// <summary>
        /// Location of the cursor
        /// </summary>
        NativePoint CursorLocation { get; set; }

        /// <summary>
        /// Location of the capture
        /// </summary>
        NativePoint Location { get; set; }

        /// <summary>
        /// Crops the capture to the specified rectangle (with Bitmap coordinates!)
        /// </summary>
        /// <param name="cropRectangle">NativeRect with bitmap coordinates</param>
        bool Crop(NativeRect cropRectangle);

        /// <summary>
        /// Apply a translate to the mouse location. e.g. needed for crop
        /// </summary>
        /// <param name="x">x coordinates to move the mouse</param>
        /// <param name="y">y coordinates to move the mouse</param>
        void MoveMouseLocation(int x, int y);
    }
}