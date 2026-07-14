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
using System.Drawing;
using System.Linq;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.FileFormat.Dto.Container;

namespace Greenshot.Editor.FileFormat.Dto;

/// <summary>
/// All DTO classes should not contain any business logic. This applies to helper methods as well
/// So this is the place for them.
/// </summary>
public static class DtoHelper
{
    /// <summary>
    /// Get the value of a field from a <see cref="DrawableContainerDto"/> object. Null if not found.
    /// </summary>
    /// <param name="drawableContainer"></param>
    /// <param name="fieldType"></param>
    /// <returns></returns>
    public static object GetFieldValue(DrawableContainerDto drawableContainer, IFieldType fieldType)
    {
        return drawableContainer.Fields?
            .FirstOrDefault(x => x.FieldTypeName == fieldType.Name)?
            .Value.GetValue();
    }

    /// <summary>
    /// We store Color as an ARGB integer, so we have to compare two colors by their ARGB values
    /// </summary>
    /// <param name="leftColor"></param>
    /// <param name="rightColor"></param>
    /// <returns></returns>
    public static bool CompareColorValue(Color leftColor, Color rightColor)
    {
        return leftColor.ToArgb() == rightColor.ToArgb();
    }

    /// <summary>
    /// Helper method.
    /// <see cref="Color.ToString()"/> hides the ARGB value for KnownColor, so we have to use this method to print ARGB value every time.
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static string ArgbString(Color color) => $"ARGB({color.A}, {color.R}, {color.G}, {color.B})";

    /// <summary>
    /// Parses a color string in HTML format (e.g., "#AARRGGBB", "#RRGGBB", "#RGB", or named colors) to a <see cref="Color"/> object.
    /// </summary>
    /// <param name="colorString">The color string to parse.</param>
    /// <returns>The parsed Color, or Transparent if parsing fails.</returns>
    public static Color ParseColorString(string colorString)
    {
        var fallbackColor = Color.Transparent;

        if (string.IsNullOrEmpty(colorString)) return fallbackColor;
        
        // Support #AARRGGBB format
        if (colorString.StartsWith("#") && colorString.Length == 9)
        {
            try
            {
                uint argb = uint.Parse(colorString.Substring(1), System.Globalization.NumberStyles.HexNumber);
                return Color.FromArgb(unchecked((int)argb));
            }
            catch
            {
                return fallbackColor;
            }
        }
        
        // Fallback to ColorTranslator for other formats like #RGB or named colors
        try
        {
            return ColorTranslator.FromHtml(colorString);
        }
        catch
        {
            return fallbackColor;
        }
    }

    /// <summary>
    /// Formats a <see cref="Color"/> object to a string in #AARRGGBB format.
    /// </summary>
    /// <param name="color">The color to format.</param>
    /// <returns>The formatted color string.</returns>
    public static string FormatColorToString(Color color)
    {
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}