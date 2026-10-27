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
using System.Globalization;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Capturing.Tools
{
    /// <summary>
    /// An entry of the fixed size capture: a size and, optionally, where its top left corner was, in screen pixels,
    /// and the top left corner of the monitor it was on. In greenshot.ini "1280x720", "1280x720@100:200"
    /// (also "1280x720@-1920:0" for a monitor left of the main one) or "1280x720@2020:200@1920:0" with the monitor.
    /// </summary>
    public readonly struct FixedCaptureArea : IEquatable<FixedCaptureArea>
    {
        public FixedCaptureArea(NativeSize size, NativePoint? location = null, NativePoint? monitor = null)
        {
            Size = size;
            Location = location;
            Monitor = location.HasValue ? monitor : null;
        }

        public NativeSize Size { get; }

        /// <summary>
        /// The top left corner in screen coordinates, null when the area has no place
        /// </summary>
        public NativePoint? Location { get; }

        /// <summary>
        /// The top left corner of the monitor the area was on, in screen coordinates; null when it isn't known,
        /// then the area counts as being on the main monitor (0,0)
        /// </summary>
        public NativePoint? Monitor { get; }

        /// <summary>
        /// Where the area is on its monitor, null when it has no place
        /// </summary>
        public NativePoint? OffsetOnMonitor => Location is { } location
            ? new NativePoint(location.X - (Monitor?.X ?? 0), location.Y - (Monitor?.Y ?? 0))
            : null;

        public bool IsEmpty => Size.IsEmpty;

        /// <summary>
        /// The area at its place in screen coordinates, null when it has no place
        /// </summary>
        public NativeRect? Bounds => Location is { } location ? new NativeRect(location, Size) : null;

        /// <summary>
        /// How the settings and the panel show it: "1280 × 720", with a place "1280 × 720 @ 100, 200", as the info panel shows a selection
        /// </summary>
        public string DisplayText => Location is { } location
            ? $"{Size.Width} × {Size.Height} @ {location.X}, {location.Y}"
            : $"{Size.Width} × {Size.Height}";

        /// <summary>
        /// "1280x720", "1280x720@100:200" or "1280x720@100:200@0:0" (also with × or spaces) to an area, empty when it isn't one
        /// </summary>
        public static FixedCaptureArea Parse(string text)
        {
            var parts = text?.Split('@');
            if (parts == null || parts.Length > 3)
            {
                return default;
            }
            var size = FixedSizeCaptureTool.ParseSize(parts[0]);
            if (size.IsEmpty)
            {
                return default;
            }
            if (parts.Length == 1)
            {
                return new FixedCaptureArea(size);
            }
            if (!TryParsePoint(parts[1], out var location))
            {
                return default;
            }
            if (parts.Length == 2)
            {
                return new FixedCaptureArea(size, location);
            }
            return TryParsePoint(parts[2], out var monitor) ? new FixedCaptureArea(size, location, monitor) : default;
        }

        /// <summary>
        /// "100:200" to a point
        /// </summary>
        private static bool TryParsePoint(string text, out NativePoint point)
        {
            var coordinates = text.Split(':');
            if (coordinates.Length == 2 && TryParseCoordinate(coordinates[0], out int x) && TryParseCoordinate(coordinates[1], out int y))
            {
                point = new NativePoint(x, y);
                return true;
            }
            point = default;
            return false;
        }

        private static bool TryParseCoordinate(string text, out int value) =>
            int.TryParse(text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);

        /// <summary>
        /// The area as it is stored in greenshot.ini, e.g. "1280x720", "1280x720@100:200" or "1280x720@2020:200@1920:0"
        /// </summary>
        public override string ToString()
        {
            var text = FormattableString.Invariant($"{Size.Width}x{Size.Height}");
            if (Location is { } location)
            {
                text += FormattableString.Invariant($"@{location.X}:{location.Y}");
            }
            if (Monitor is { } monitor)
            {
                text += FormattableString.Invariant($"@{monitor.X}:{monitor.Y}");
            }
            return text;
        }

        /// <summary>
        /// The same size at the same place; the monitor only tells where the area goes when its place isn't on the screens
        /// </summary>
        public bool Equals(FixedCaptureArea other) => Size == other.Size && Nullable.Equals(Location, other.Location);

        public override bool Equals(object obj) => obj is FixedCaptureArea other && Equals(other);

        public override int GetHashCode() => (Size.Width, Size.Height, Location?.X, Location?.Y).GetHashCode();

        public static bool operator ==(FixedCaptureArea left, FixedCaptureArea right) => left.Equals(right);

        public static bool operator !=(FixedCaptureArea left, FixedCaptureArea right) => !left.Equals(right);
    }
}
