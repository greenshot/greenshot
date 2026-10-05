/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Greenshot.FileFormat.Legacy
{
    /// <summary>
    /// The layout of a legacy .greenshot file:
    /// [PNG of the capture] [NRBF payload with the elements] [Int64 length of the payload] ["GreenshotMM.mm" marker, 14 ASCII bytes]
    /// </summary>
    public sealed class LegacyGreenshotFile
    {
        /// <summary>Length of the "GreenshotMM.mm" marker at the end of the file</summary>
        public const int MarkerLength = 14;

        private const string MarkerPrefix = "Greenshot";
        private const int TrailerLength = MarkerLength + sizeof(long);

        private LegacyGreenshotFile(string marker, long elementsOffset, long elementsLength)
        {
            Marker = marker;
            ElementsOffset = elementsOffset;
            ElementsLength = elementsLength;
        }

        /// <summary>The marker, e.g. "Greenshot01.03"</summary>
        public string Marker { get; }

        /// <summary>The version from the marker, the version of Greenshot which wrote the file (e.g. 1.3), null if it can't be parsed</summary>
        public Version Version
        {
            get
            {
                var versionPart = Marker.Substring(MarkerPrefix.Length);
                return Version.TryParse(versionPart, out var version) ? version : null;
            }
        }

        /// <summary>Position of the NRBF payload with the elements in the stream</summary>
        public long ElementsOffset { get; }

        /// <summary>Length of the NRBF payload</summary>
        public long ElementsLength { get; }

        /// <summary>
        /// Check the end of the stream for the .greenshot marker and the payload length.
        /// The position of the stream is changed.
        /// </summary>
        /// <param name="stream">Seekable stream with the complete file</param>
        /// <param name="file">The located parts of the file</param>
        /// <returns>true if the stream ends like a .greenshot file</returns>
        public static bool TryLocate(Stream stream, out LegacyGreenshotFile file)
        {
            file = null;
            if (stream == null || !stream.CanSeek || stream.Length < TrailerLength)
            {
                return false;
            }

            var trailer = new byte[TrailerLength];
            stream.Seek(-TrailerLength, SeekOrigin.End);
            if (!ReadExactly(stream, trailer))
            {
                return false;
            }

            string marker = Encoding.ASCII.GetString(trailer, sizeof(long), MarkerLength);
            if (!marker.StartsWith(MarkerPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            long elementsLength = BitConverter.ToInt64(trailer, 0);
            if (elementsLength < 0 || elementsLength > stream.Length - TrailerLength)
            {
                return false;
            }

            file = new LegacyGreenshotFile(marker, stream.Length - TrailerLength - elementsLength, elementsLength);
            return true;
        }

        /// <summary>
        /// Read the elements of the file
        /// </summary>
        /// <param name="stream">The same stream which was passed to <see cref="TryLocate"/></param>
        /// <returns>The elements</returns>
        /// <exception cref="LegacyFormatException">When the elements can't be read</exception>
        public IReadOnlyList<LegacyElement> ReadElements(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            stream.Seek(ElementsOffset, SeekOrigin.Begin);
            return LegacyElementReader.Read(stream);
        }

        public override string ToString() => $"{Marker}, elements at {ElementsOffset} ({ElementsLength} bytes)";

        private static bool ReadExactly(Stream stream, byte[] buffer)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = stream.Read(buffer, offset, buffer.Length - offset);
                if (read <= 0)
                {
                    return false;
                }

                offset += read;
            }

            return true;
        }
    }
}
