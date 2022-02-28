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
using Greenshot.Base.Interfaces.Drawing;

namespace Greenshot.Editor.Drawing.Fields
{
    /// <summary>
    /// Defines all FieldTypes + their default value.
    /// (The additional value is why this is not an enum)
    /// </summary>
    [Serializable]
    public class FieldType : IFieldType
    {
        public static readonly IFieldType ARROWHEADS = new FieldType(nameof(ARROWHEADS));
        public static readonly IFieldType BLUR_RADIUS = new FieldType(nameof(BLUR_RADIUS));
        public static readonly IFieldType BRIGHTNESS = new FieldType(nameof(BRIGHTNESS));
        public static readonly IFieldType FILL_COLOR = new FieldType(nameof(FILL_COLOR));
        public static readonly IFieldType FONT_BOLD = new FieldType(nameof(FONT_BOLD));
        public static readonly IFieldType FONT_FAMILY = new FieldType(nameof(FONT_FAMILY));
        public static readonly IFieldType FONT_ITALIC = new FieldType(nameof(FONT_ITALIC));
        public static readonly IFieldType FONT_SIZE = new FieldType(nameof(FONT_SIZE));
        public static readonly IFieldType TEXT_HORIZONTAL_ALIGNMENT = new FieldType(nameof(TEXT_HORIZONTAL_ALIGNMENT));
        public static readonly IFieldType TEXT_VERTICAL_ALIGNMENT = new FieldType(nameof(TEXT_VERTICAL_ALIGNMENT));
        public static readonly IFieldType HIGHLIGHT_COLOR = new FieldType(nameof(HIGHLIGHT_COLOR));
        public static readonly IFieldType LINE_COLOR = new FieldType(nameof(LINE_COLOR));
        public static readonly IFieldType LINE_THICKNESS = new FieldType(nameof(LINE_THICKNESS));
        public static readonly IFieldType MAGNIFICATION_FACTOR = new FieldType(nameof(MAGNIFICATION_FACTOR));
        public static readonly IFieldType PIXEL_SIZE = new FieldType(nameof(PIXEL_SIZE));
        public static readonly IFieldType PREVIEW_QUALITY = new FieldType(nameof(PREVIEW_QUALITY));
        public static readonly IFieldType SHADOW = new FieldType(nameof(SHADOW));
        public static readonly IFieldType PREPARED_FILTER_OBFUSCATE = new FieldType(nameof(PREPARED_FILTER_OBFUSCATE));
        public static readonly IFieldType PREPARED_FILTER_HIGHLIGHT = new FieldType(nameof(PREPARED_FILTER_HIGHLIGHT));
        public static readonly IFieldType FLAGS = new FieldType(nameof(FLAGS));
        public static readonly IFieldType CROPMODE = new FieldType(nameof(CROPMODE));


        public static IFieldType[] Values =
        {
            ARROWHEADS, BLUR_RADIUS, BRIGHTNESS, FILL_COLOR, FONT_BOLD, FONT_FAMILY, FONT_ITALIC, FONT_SIZE, TEXT_HORIZONTAL_ALIGNMENT, TEXT_VERTICAL_ALIGNMENT, HIGHLIGHT_COLOR,
            LINE_COLOR, LINE_THICKNESS, MAGNIFICATION_FACTOR, PIXEL_SIZE, PREVIEW_QUALITY, SHADOW, PREPARED_FILTER_OBFUSCATE, PREPARED_FILTER_HIGHLIGHT, FLAGS, CROPMODE
        };

        public string Name { get; set; }

        private FieldType(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            int hashCode = 0;
            unchecked
            {
                if (Name != null)
                    hashCode += 1000000009 * Name.GetHashCode();
            }

            return hashCode;
        }

        public override bool Equals(object obj)
        {
            FieldType other = obj as FieldType;
            if (other == null)
            {
                return false;
            }

            return Equals(Name, other.Name);
        }

        public static bool operator ==(FieldType a, FieldType b)
        {
            return Equals(a, b);
        }

        public static bool operator !=(FieldType a, FieldType b)
        {
            return !Equals(a, b);
        }
    }
}