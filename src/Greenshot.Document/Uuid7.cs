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

namespace Greenshot.Document
{
    /// <summary>
    /// UUID version 7 (RFC 9562): the first 48 bits are the creation time in Unix milliseconds, the rest is random,
    /// so ids sort by creation and tell when something was created. Works like Guid.CreateVersion7 of .NET 9 and later,
    /// which replaces CreateVersion7 when Greenshot moves to .NET 10.
    /// </summary>
    public static class Uuid7
    {
        public static Guid CreateVersion7() => CreateVersion7(DateTimeOffset.UtcNow);

        public static Guid CreateVersion7(DateTimeOffset timestamp)
        {
            long milliseconds = timestamp.ToUnixTimeMilliseconds();
            if (milliseconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp), "The time is before 1970");
            }

            // A random version 4 guid, with the time, version 7 and variant 10xx put in like .NET does
            byte[] bytes = Guid.NewGuid().ToByteArray();
            unchecked
            {
                short c = (short)((BitConverter.ToInt16(bytes, 6) & 0x0FFF) | 0x7000);
                return new Guid((int)(milliseconds >> 16), (short)milliseconds, c,
                    (byte)((bytes[8] & 0x3F) | 0x80), bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15]);
            }
        }

        /// <summary>
        /// The creation time of a version 7 id, false for other ids (e.g. those of Guid.NewGuid)
        /// </summary>
        public static bool TryGetTimestamp(Guid id, out DateTimeOffset timestamp)
        {
            byte[] bytes = id.ToByteArray();
            if (bytes[7] >> 4 != 7 || (bytes[8] & 0xC0) != 0x80)
            {
                timestamp = default;
                return false;
            }

            long milliseconds = ((long)BitConverter.ToUInt32(bytes, 0) << 16) | BitConverter.ToUInt16(bytes, 4);
            timestamp = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
            return true;
        }
    }
}
