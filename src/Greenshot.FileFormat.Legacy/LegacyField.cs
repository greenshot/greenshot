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

namespace Greenshot.FileFormat.Legacy
{
    /// <summary>
    /// A field (property) of an element, e.g. LINE_THICKNESS = 2.
    /// </summary>
    public sealed class LegacyField
    {
        /// <summary>The name of the field type, e.g. "LINE_COLOR", "FONT_SIZE"</summary>
        public string FieldTypeName { get; internal set; }

        /// <summary>The scope, normally the name of the element type the field belongs to</summary>
        public string Scope { get; internal set; }

        /// <summary>
        /// The value: null, bool, int, long, float, double, decimal, string, <see cref="LegacyColor"/>,
        /// <see cref="LegacyEnumValue"/> or <see cref="LegacyUnsupportedValue"/>.
        /// </summary>
        public object Value { get; internal set; }

        public override string ToString() => $"{FieldTypeName}={Value}";
    }

    /// <summary>
    /// A System.Drawing.Color as it was serialized: either an ARGB value, a known color or a named color.
    /// </summary>
    public readonly struct LegacyColor
    {
        // Flags of System.Drawing.Color.state
        private const short StateKnownColorValid = 0x0001;
        private const short StateArgbValueValid = 0x0002;
        private const short StateNameValid = 0x0008;

        public LegacyColor(long value, short knownColor, short state, string name)
        {
            Value = value;
            KnownColor = knownColor;
            State = state;
            Name = name;
        }

        /// <summary>The ARGB value, only valid when <see cref="IsArgb"/> is true</summary>
        public long Value { get; }

        /// <summary>The value of the System.Drawing.KnownColor enum, only valid when <see cref="IsKnownColor"/> is true</summary>
        public short KnownColor { get; }

        public short State { get; }

        /// <summary>The name, only valid when <see cref="IsNamedColor"/> is true</summary>
        public string Name { get; }

        public bool IsKnownColor => (State & StateKnownColorValid) != 0;

        public bool IsNamedColor => (State & StateNameValid) != 0;

        public bool IsArgb => (State & StateArgbValueValid) != 0;

        /// <summary>The ARGB value as int, for Color.FromArgb</summary>
        public int ToArgb() => unchecked((int)Value);

        public override string ToString() => IsKnownColor ? $"KnownColor {KnownColor}" : IsNamedColor ? $"Color {Name}" : $"#{Value:X8}";
    }

    /// <summary>
    /// The value of an enum, e.g. FieldFlag, PreparedFilter, ArrowHeadCombination or StringAlignment.
    /// </summary>
    public readonly struct LegacyEnumValue
    {
        public LegacyEnumValue(string typeName, long value)
        {
            TypeName = typeName;
            Value = value;
        }

        /// <summary>The type name without namespace, e.g. "FieldFlag", "FilterContainer+PreparedFilter", "StringAlignment"</summary>
        public string TypeName { get; }

        public long Value { get; }

        public override string ToString() => $"{TypeName}({Value})";
    }

    /// <summary>
    /// A value of a type the reader doesn't support, the type name is kept for logging.
    /// </summary>
    public sealed class LegacyUnsupportedValue
    {
        public LegacyUnsupportedValue(string typeName)
        {
            TypeName = typeName;
        }

        public string TypeName { get; }

        public override string ToString() => $"Unsupported {TypeName}";
    }

    /// <summary>A System.Drawing.Point</summary>
    public readonly struct LegacyPoint
    {
        public LegacyPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public override string ToString() => $"{X},{Y}";
    }
}
