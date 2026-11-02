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

namespace Greenshot.Base.Core.Enums
{
    /// <summary>
    /// The optional details Greenshot writes into an exported image, see ICoreConfiguration.ExportMetadata.
    /// The program name and the resolution are always written.
    /// </summary>
    public enum ExportMetadataField
    {
        /// <summary>
        /// When the screen was captured (EXIF DateTimeOriginal + OffsetTimeOriginal, xmp:CreateDate); an opened image only keeps the capture time its file had
        /// </summary>
        CaptureTime,

        /// <summary>
        /// ExportMetadataAuthor and ExportMetadataCopyright (EXIF Artist + Copyright, dc:creator + dc:rights)
        /// </summary>
        Author,

        /// <summary>
        /// Private: the window title (EXIF XPTitle, dc:title) and the web address of the browser tab (dc:source)
        /// </summary>
        Source,

        /// <summary>
        /// Private: the text OCR already found in the capture (EXIF XPComment, dc:description), it never starts OCR
        /// </summary>
        Text,

        /// <summary>
        /// Private: the content of the barcodes and QR codes found in the capture (greenshot:Barcodes)
        /// </summary>
        Barcodes
    }
}
