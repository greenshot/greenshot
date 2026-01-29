/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2025 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Text.Json.Serialization;

namespace Greenshot.Editor.FileFormat.Dto.Fields;

/// <summary>
/// Represents a field value that stores color information.
/// </summary>
public sealed class ColorFieldValueDto : FieldValueDto
{
    /// <summary>
    /// Color stored as a string in HTML format (e.g., "#AARRGGBB").
    /// </summary>
    [JsonInclude]
    private string Color { get; set; }

    [JsonIgnore]
    public Color Value
    {
        get
        {
            var fallbackColor = System.Drawing.Color.Transparent;

            if (string.IsNullOrEmpty(Color)) return fallbackColor;
            // Support #AARRGGBB format
            if (Color.StartsWith("#") && Color.Length == 9)
            {
                try
                {
                    uint argb = uint.Parse(Color.Substring(1), System.Globalization.NumberStyles.HexNumber);
                    return System.Drawing.Color.FromArgb(unchecked((int)argb));
                }
                catch
                {
                    return fallbackColor;
                }
            }
            // Fallback to ColorTranslator for other formats like #RGB or named colors
            try
            {
                return ColorTranslator.FromHtml(Color);
            }
            catch
            {
                return fallbackColor;
            }
        }
        set
        {
            Color = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
        }
    }

    public override object GetValue()
    {
        return Value;
    }
}
