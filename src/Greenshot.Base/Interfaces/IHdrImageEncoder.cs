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
    /// Writes HDR image (see HdrImage) as an HDR image file, lossless: JPEG XR (built into Windows).
    /// Registered as a service by the editor, which has the WIC encoders.
    /// </summary>
    public interface IHdrImageEncoder
    {
        /// <summary>
        /// True when this system can write the format
        /// </summary>
        /// <param name="formatId">jxr (see WellKnownFileFormats)</param>
        bool CanEncode(string formatId);

        /// <summary>
        /// Write the HDR image as an HDR file of the format
        /// </summary>
        /// <param name="hdrImage">The HDR image</param>
        /// <param name="formatId">jxr</param>
        /// <param name="destination">Stream to write to, untouched when it fails</param>
        /// <param name="captureDetails">The details written into the file (capture time, title...), may be null</param>
        /// <param name="includePrivateMetadata">Also write the details which are only for the user (see ExportImageMetadata)</param>
        /// <param name="error">Why it failed, null when it worked</param>
        /// <returns>true when the file was written</returns>
        bool TryEncode(HdrImage hdrImage, string formatId, Stream destination, ICaptureDetails captureDetails, bool includePrivateMetadata, out string error);
    }
}
