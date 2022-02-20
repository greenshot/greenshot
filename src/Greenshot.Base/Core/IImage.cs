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
using System.Drawing.Imaging;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The image interface, this abstracts an image
    /// </summary>
    public interface IImage : IDisposable
    {
        /// <summary>
        /// Height of the image, can be set to change
        /// </summary>
        int Height { get; set; }

        /// <summary>
        /// Width of the image, can be set to change.
        /// </summary>
        int Width { get; set; }

        /// <summary>
        /// Size of the image
        /// </summary>
        Size Size { get; }

        /// <summary>
        /// Pixelformat of the underlying image
        /// </summary>
        PixelFormat PixelFormat { get; }

        /// <summary>
        /// Vertical resolution of the underlying image
        /// </summary>
        float VerticalResolution { get; }

        /// <summary>
        /// Horizontal resolution of the underlying image
        /// </summary>
        float HorizontalResolution { get; }

        /// <summary>
        /// Underlying image, or an on demand rendered version with different attributes as the original
        /// </summary>
        Image Image { get; }
    }
}