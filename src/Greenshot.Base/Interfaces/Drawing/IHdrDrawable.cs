/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using Dapplo.Windows.Common.Structs;
using Greenshot.ImageSharpDrawing;
using SixLabors.ImageSharp;

namespace Greenshot.Base.Interfaces.Drawing
{
    /// <summary>
    /// An element (e.g. of a plugin) which draws itself with ImageSharp, e.g. on the pixels of an HDR file: the editor's own element types are
    /// drawn by the editor (ElementDrawing), an element of another type is only drawn in the HDR file when it implements this. Without it, the
    /// capture loses its HDR content while the element is on it.
    /// </summary>
    public interface IHdrDrawable
    {
        /// <summary>
        /// How the element is drawn on an image whose top left corner is at the origin (in the coordinates of the surface), in the values of
        /// the image (see ImageValues: for an HDR image linear light, its white at the white of the elements). Everything is taken from the
        /// element now: the drawing runs later on another thread, while the element may change.
        /// </summary>
        /// <param name="origin">Where the top left corner of the image to draw on is</param>
        /// <returns>The drawing, null when the element draws nothing</returns>
        Action<Image, ImageValues> CreateHdrDrawing(NativePoint origin);
    }
}
