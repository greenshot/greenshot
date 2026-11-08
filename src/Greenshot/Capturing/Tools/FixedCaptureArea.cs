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
using System.Linq;
using System.Text.RegularExpressions;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Capturing.Tools
{
    /// <summary>
    /// An entry of the fixed size capture: a size and, optionally, a place for it: an offset from the top left corner of a monitor
    /// or of the content of a program's window. In greenshot.ini like a geometry of X11:
    /// "1280x720" only the size, "1280x720+100+200" 100, 200 from the top left of the main monitor,
    /// "1280x720+100+200@1920+0" from the top left of the monitor at 1920, 0, "1280x720+10-30@notepad.exe" from the top left
    /// of the content of a window of notepad.exe. A minus is a negative offset (not from the right or bottom as in X11).
    /// </summary>
    public readonly struct FixedCaptureArea : IEquatable<FixedCaptureArea>
    {
        private static readonly Regex AreaPattern = new Regex(@"^\s*(\d+)\s*[xX×]\s*(\d+)\s*(?:([+-]\s*\d+)\s*([+-]\s*\d+))?\s*(?:@\s*(.+?))?\s*$", RegexOptions.CultureInvariant);
        private static readonly Regex PointPattern = new Regex(@"^([+-]?\s*\d+)\s*([+-]\s*\d+)$", RegexOptions.CultureInvariant);

        public FixedCaptureArea(NativeSize size, NativePoint? offset = null, NativePoint? monitor = null, string program = null)
        {
            Size = size;
            Offset = offset;
            bool hasProgram = offset.HasValue && !string.IsNullOrWhiteSpace(program);
            Program = hasProgram ? program.Trim() : null;
            // The main monitor (0, 0) is the default, it isn't stored
            Monitor = offset.HasValue && !hasProgram && monitor is { } origin && (origin.X != 0 || origin.Y != 0) ? origin : null;
        }

        public NativeSize Size { get; }

        /// <summary>
        /// The top left corner from the top left corner of the monitor or of the window, null when the area has no place
        /// </summary>
        public NativePoint? Offset { get; }

        /// <summary>
        /// The top left corner of the monitor in screen coordinates, null for the main monitor (0, 0) or a program
        /// </summary>
        public NativePoint? Monitor { get; }

        /// <summary>
        /// The executable (e.g. notepad.exe) whose window the place is in, null when it is on a monitor
        /// </summary>
        public string Program { get; }

        public bool IsEmpty => Size.IsEmpty;

        public bool HasPlace => Offset.HasValue;

        /// <summary>
        /// The place on the monitor in screen coordinates, null when it has none or is in a window
        /// </summary>
        public NativeRect? MonitorBounds => Offset is { } offset && Program == null
            ? new NativeRect((Monitor?.X ?? 0) + offset.X, (Monitor?.Y ?? 0) + offset.Y, Size.Width, Size.Height)
            : null;

        /// <summary>
        /// True when the program is the one of the executable, with or without .exe, ignoring the case
        /// </summary>
        public bool IsProgram(string processName) =>
            Program != null && string.Equals(WithoutExe(Program), WithoutExe(processName), StringComparison.OrdinalIgnoreCase);

        private static string WithoutExe(string name)
        {
            name = (name ?? string.Empty).Trim();
            return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 4) : name;
        }

        /// <summary>
        /// "1280x720", "1280x720+100+200", "1280x720+100+200@1920+0" or "1280x720+10-30@notepad.exe" (also with × or spaces)
        /// to an area, empty when it isn't one
        /// </summary>
        public static FixedCaptureArea Parse(string text)
        {
            var match = text == null ? null : AreaPattern.Match(text);
            if (match == null || !match.Success
                || !int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int width)
                || !int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int height)
                || width <= 0 || height <= 0)
            {
                return default;
            }
            var size = new NativeSize(width, height);
            if (!match.Groups[3].Success)
            {
                // A size alone, "@" needs a place
                return match.Groups[5].Success ? default : new FixedCaptureArea(size);
            }
            if (!TryParseNumber(match.Groups[3].Value, out int x) || !TryParseNumber(match.Groups[4].Value, out int y))
            {
                return default;
            }
            var offset = new NativePoint(x, y);
            if (!match.Groups[5].Success)
            {
                return new FixedCaptureArea(size, offset);
            }
            var relativeTo = match.Groups[5].Value;
            var monitorMatch = PointPattern.Match(relativeTo);
            if (monitorMatch.Success)
            {
                return TryParseNumber(monitorMatch.Groups[1].Value, out int monitorX) && TryParseNumber(monitorMatch.Groups[2].Value, out int monitorY)
                    ? new FixedCaptureArea(size, offset, new NativePoint(monitorX, monitorY))
                    : default;
            }
            // A program: a name with a letter (else it is a broken monitor), without what an executable name can't have
            return relativeTo.Any(char.IsLetter) && relativeTo.IndexOfAny(new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|', '@', ',' }) < 0
                ? new FixedCaptureArea(size, offset, program: relativeTo)
                : default;
        }

        private static bool TryParseNumber(string text, out int value) =>
            int.TryParse(text.Replace(" ", string.Empty), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);

        private static string FormatOffset(NativePoint point) =>
            FormattableString.Invariant($"{(point.X < 0 ? "-" : "+")}{Math.Abs((long)point.X)}{(point.Y < 0 ? "-" : "+")}{Math.Abs((long)point.Y)}");

        /// <summary>
        /// The area as it is stored in greenshot.ini, e.g. "1280x720", "1280x720+100+200", "1280x720+100+200@-1920+0" or "1280x720+10-30@notepad.exe"
        /// </summary>
        public override string ToString()
        {
            var text = FormattableString.Invariant($"{Size.Width}x{Size.Height}");
            if (Offset is { } offset)
            {
                text += FormatOffset(offset);
                if (Program != null)
                {
                    text += "@" + Program;
                }
                else if (Monitor is { } monitor)
                {
                    text += "@" + FormattableString.Invariant($"{monitor.X}{(monitor.Y < 0 ? "-" : "+")}{Math.Abs((long)monitor.Y)}");
                }
            }
            return text;
        }

        public bool Equals(FixedCaptureArea other) =>
            Size == other.Size && Nullable.Equals(Offset, other.Offset) && Nullable.Equals(Monitor, other.Monitor)
            && string.Equals(Program, other.Program, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => obj is FixedCaptureArea other && Equals(other);

        public override int GetHashCode() =>
            (Size.Width, Size.Height, Offset?.X, Offset?.Y, Monitor?.X, Monitor?.Y, Program?.ToUpperInvariant()).GetHashCode();

        public static bool operator ==(FixedCaptureArea left, FixedCaptureArea right) => left.Equals(right);

        public static bool operator !=(FixedCaptureArea left, FixedCaptureArea right) => !left.Equals(right);
    }
}
