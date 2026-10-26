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

namespace Greenshot.Linux.Editor.Fields;

/// <summary>
/// The properties an element can have, the FieldType of the Windows editor.
/// The property bar shows the fields of the selected elements or of the current tool.
/// </summary>
public enum FieldType
{
    LineThickness,
    LineColor,
    FillColor,
    Shadow,
    ArrowHeads,
    FontFamily,
    FontSize,
    FontBold,
    FontItalic,
    TextHorizontalAlignment,
    TextVerticalAlignment,
    HighlightFilter,
    ObfuscateFilter,
    BlurRadius,
    PixelSize,
    MagnificationFactor,
}

/// <summary>
/// ARROWHEADS of the Windows editor
/// </summary>
public enum ArrowHeads
{
    None,
    Start,
    End,
    Both,
}

/// <summary>
/// PREPARED_FILTER_HIGHLIGHT of the Windows editor
/// </summary>
public enum HighlightFilter
{
    TextHighlight,
    AreaHighlight,
    Grayscale,
    Magnification,
}

/// <summary>
/// PREPARED_FILTER_OBFUSCATE of the Windows editor
/// </summary>
public enum ObfuscateFilter
{
    Pixelize,
    Blur,
}

public enum TextHorizontalAlignment
{
    Left,
    Center,
    Right,
}

public enum TextVerticalAlignment
{
    Top,
    Center,
    Bottom,
}

public static class FieldTypeExtensions
{
    /// <summary>
    /// The name of the field in the Windows editor, used for the last used values in greenshot.ini
    /// </summary>
    public static string WindowsName(this FieldType field) => field switch
    {
        FieldType.LineThickness => "LINE_THICKNESS",
        FieldType.LineColor => "LINE_COLOR",
        FieldType.FillColor => "FILL_COLOR",
        FieldType.Shadow => "SHADOW",
        FieldType.ArrowHeads => "ARROWHEADS",
        FieldType.FontFamily => "FONT_FAMILY",
        FieldType.FontSize => "FONT_SIZE",
        FieldType.FontBold => "FONT_BOLD",
        FieldType.FontItalic => "FONT_ITALIC",
        FieldType.TextHorizontalAlignment => "TEXT_HORIZONTAL_ALIGNMENT",
        FieldType.TextVerticalAlignment => "TEXT_VERTICAL_ALIGNMENT",
        FieldType.HighlightFilter => "PREPARED_FILTER_HIGHLIGHT",
        FieldType.ObfuscateFilter => "PREPARED_FILTER_OBFUSCATE",
        FieldType.BlurRadius => "BLUR_RADIUS",
        FieldType.PixelSize => "PIXEL_SIZE",
        FieldType.MagnificationFactor => "MAGNIFICATION_FACTOR",
        _ => field.ToString(),
    };

    /// <summary>
    /// The text shown in the property bar
    /// </summary>
    public static string DisplayName(this FieldType field) => field switch
    {
        FieldType.LineThickness => "Line thickness",
        FieldType.LineColor => "Line color",
        FieldType.FillColor => "Fill color",
        FieldType.Shadow => "Shadow",
        FieldType.ArrowHeads => "Arrow heads",
        FieldType.FontFamily => "Font",
        FieldType.FontSize => "Font size",
        FieldType.FontBold => "Bold",
        FieldType.FontItalic => "Italic",
        FieldType.TextHorizontalAlignment => "Horizontal alignment",
        FieldType.TextVerticalAlignment => "Vertical alignment",
        FieldType.HighlightFilter => "Highlight",
        FieldType.ObfuscateFilter => "Obfuscate",
        FieldType.BlurRadius => "Blur radius",
        FieldType.PixelSize => "Pixel size",
        FieldType.MagnificationFactor => "Magnification",
        _ => field.ToString(),
    };
}
