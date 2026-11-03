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

using System.IO;
using Greenshot.Base.Core;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// Reads an HDR image file (e.g. a JPEG XR Greenshot wrote, a Game Bar screenshot) as an HDR image, so it can be opened like an HDR capture.
    /// Registered as a service by the editor, which has the WIC decoders.
    /// </summary>
    public interface IHdrImageDecoder
    {
        /// <summary>
        /// Read the file as an HDR image, when it has HDR pixels (half or float); an 8-bit file is left to the normal loading
        /// </summary>
        /// <param name="stream">A seekable stream with the file, it is positioned back when the file isn't HDR</param>
        /// <param name="hdrImage">The complete HDR image with the SDR white level of the file (or of the HDR display), the caller disposes it</param>
        /// <returns>true when it is an HDR file and was read</returns>
        bool TryDecode(Stream stream, out HdrImage hdrImage);
    }
}
